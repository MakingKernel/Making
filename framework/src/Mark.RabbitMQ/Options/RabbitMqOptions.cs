namespace Mark.RabbitMQ.Options;

/// <summary>
/// Configuration options for RabbitMQ connection.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// The hostname of the RabbitMQ server.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// The port number of the RabbitMQ server.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// The username for authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// The password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// The virtual host to use.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The connection name for identification.
    /// </summary>
    public string ClientProvidedName { get; set; } = "Mark.RabbitMQ";

    /// <summary>
    /// Whether to use SSL/TLS connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// The SSL server name.
    /// </summary>
    public string SslServerName { get; set; } = string.Empty;

    /// <summary>
    /// The SSL certificate path.
    /// </summary>
    public string SslCertPath { get; set; } = string.Empty;

    /// <summary>
    /// The SSL certificate passphrase.
    /// </summary>
    public string SslCertPassphrase { get; set; } = string.Empty;
}