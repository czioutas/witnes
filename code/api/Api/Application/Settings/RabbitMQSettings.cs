using System.ComponentModel.DataAnnotations;

namespace Api.Application.Settings;

/// <summary>
/// RabbitMQ connection settings.
/// CAP manages its own queues and exchanges automatically.
/// </summary>
public class RabbitMQSettings
{
    [Required]
    public string Host { get; set; } = "localhost";

    [Required]
    public int Port { get; set; } = 5672;

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    [Required]
    public string VirtualHost { get; set; } = "/";

    [Required]
    public string ConnectionName { get; set; } = "Witnes";
}
