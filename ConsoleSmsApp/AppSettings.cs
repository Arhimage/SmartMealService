namespace ConsoleSmsApp
{
    public class AppSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ServerEndpoint { get; set; } = string.Empty;
        public BasicAuthSettings BasicAuth { get; set; } = new();
    }

    public class BasicAuthSettings
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
