using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Settings.Settings;
using Microsoft.Extensions.Options;

namespace Api.Application.Communication.Services;

public interface ICommunicationService
{
    Task<bool> SendEmailAsync(string templateId, string to, object? dynamicTemplateData = null);
    Task<bool> AddContactToListAsync(string email, string firstName, string lastName);
}

public class CommunicationService : ICommunicationService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Communication service for sending emails via Mailtrap API
    /// </summary>
    /// <param name="smtpSettings"></param>
    /// <param name="logger"></param>
    /// <param name="httpClientFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CommunicationService(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<CommunicationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        if (smtpSettings == null)
        {
            throw new ArgumentNullException(nameof(smtpSettings));
        }

        _smtpSettings = smtpSettings.Value;
    }

    public async Task<bool> SendEmailAsync(string templateId, string to, object? dynamicTemplateData = null)
    {
        try
        {
            _logger.LogInformation("Sending email to: {To}", to);

            var requestPayload = new MailtrapEmailRequest
            {
                From = new EmailAddress { Email = _smtpSettings.From, Name = "Witnes" },
                To = new[] { new EmailAddress { Email = to } },
                TemplateUuid = templateId,
                TemplateVariables = dynamicTemplateData != null ? ConvertToDictionary(dynamicTemplateData) : null
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _smtpSettings.ApiKey);

            var response = await httpClient.PostAsync("https://send.api.mailtrap.io/api/send", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to: {To}", to);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send email to: {To}. Status: {StatusCode}, Response: {Response}",
                    to, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to: {To}", to);
            return false;
        }
    }

    private Dictionary<string, object> ConvertToDictionary(object data)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in data.GetType().GetProperties())
        {
            var value = property.GetValue(data);
            if (value != null)
            {
                dictionary[property.Name] = value;
            }
        }

        return dictionary;
    }

    private class MailtrapEmailRequest
    {
        public EmailAddress? From { get; set; }
        public EmailAddress[]? To { get; set; }
        public string? TemplateUuid { get; set; }
        public Dictionary<string, object>? TemplateVariables { get; set; }
    }

    private class EmailAddress
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public async Task<bool> AddContactToListAsync(string email, string firstName, string lastName)
    {
        try
        {
            // Skip if Mailtrap contact list settings are not configured
            if (string.IsNullOrEmpty(_smtpSettings.MailtrapAccountId) || string.IsNullOrEmpty(_smtpSettings.JoinRequestListId))
            {
                _logger.LogWarning("Mailtrap contact list settings not configured. Skipping contact addition for: {Email}", email);
                return true; // Return true to not block the flow
            }

            _logger.LogInformation("Adding contact to Mailtrap list: {Email}", email);

            var requestPayload = new
            {
                contact = new
                {
                    email = email,
                    fields = new Dictionary<string, object>
                    {
                        { "first_name", firstName },
                        { "last_name", lastName }
                    },
                    list_ids = new[] { int.Parse(_smtpSettings.JoinRequestListId!) }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Api-Token", _smtpSettings.ApiKey);

            // Mailtrap Contacts API endpoint (as per https://api-docs.mailtrap.io/docs/mailtrap-api-docs/7d76bbcbcf6e3-create-a-new-contact)
            var url = $"https://mailtrap.io/api/accounts/{_smtpSettings.MailtrapAccountId}/contacts";
            var response = await httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Contact added successfully to Mailtrap list: {Email}", email);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to add contact to Mailtrap list: {Email}. Status: {StatusCode}, Response: {Response}",
                    email, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding contact to Mailtrap list: {Email}", email);
            return false;
        }
    }

}
