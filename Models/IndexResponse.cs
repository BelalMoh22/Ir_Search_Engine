namespace IRSearchEngine.Models
{
    public class IndexTermResponse
    {
        public string Term { get; set; } = string.Empty;
        public Dictionary<int, List<int>> Documents { get; set; } = new();
    }
}
