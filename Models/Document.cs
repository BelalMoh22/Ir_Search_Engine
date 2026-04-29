namespace IRSearchEngine.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
