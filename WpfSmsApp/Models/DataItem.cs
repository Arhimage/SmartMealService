namespace WpfSmsApp.Models
{
    public record DataItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
