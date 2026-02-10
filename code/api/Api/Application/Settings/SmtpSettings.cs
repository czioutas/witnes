namespace Api.Settings.Settings
{
    public class SmtpSettings
    {
        public required string ApiKey { get; set; }
        public required string From { get; set; }
        public required string VerifyEmailTemplateId { get; set; }
        public required string PasswordResetTemplateId { get; set; }
        public required string UserInvitationTemplateId { get; set; }
        public string? InvoiceNotificationTemplateId { get; set; }
        public string? MailtrapAccountId { get; set; }
        public string? JoinRequestListId { get; set; }
    }
}
