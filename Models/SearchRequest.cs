namespace IRSearchEngine.Models
{
    /// <summary>
    /// Request model for search queries.
    /// Supports normal, phrase, proximity, and wildcard queries.
    /// </summary>
    public class SearchRequest
    {
        /// <summary>The search query string.</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>Optional language filter: "en", "ar", or empty for all.</summary>
        public string? Language { get; set; }
    }
}
