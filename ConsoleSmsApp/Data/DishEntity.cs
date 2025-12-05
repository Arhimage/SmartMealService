namespace ConsoleSmsApp.Data
{
    public class DishEntity
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsWeighted { get; set; }
        public string FullPath { get; set; } = string.Empty;
    }
}
