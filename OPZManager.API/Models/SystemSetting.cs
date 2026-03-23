namespace OPZManager.API.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "LLM", "Embedding"
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
