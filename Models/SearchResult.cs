namespace IRSearchEngine.Models
{
    /// <summary>
    /// Represents a single search result with ranking score.
    /// </summary>
    public class SearchResult
    {
        /// <summary>The document ID from the database.</summary>
        public int DocumentId { get; set; }

        /// <summary>The TF-IDF cosine similarity score.</summary>
        public double Score { get; set; }

        /// <summary>A snippet of the document content for display.</summary>
        public string Snippet { get; set; } = string.Empty;

        /// <summary>The language of the document.</summary>
        public string Language { get; set; } = string.Empty;
    }

    /// <summary>
    /// Full response for a search query including results, suggestions, and metrics.
    /// </summary>
    public class SearchResponse
    {
        /// <summary>The original query submitted.</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>The type of query detected (Normal, Phrase, Proximity, Wildcard).</summary>
        public string QueryType { get; set; } = string.Empty;

        /// <summary>The preprocessed query terms used for searching.</summary>
        public List<string> ProcessedTerms { get; set; } = new();

        /// <summary>Ranked list of search results.</summary>
        public List<SearchResult> Results { get; set; } = new();

        /// <summary>Spelling suggestions if terms were not found.</summary>
        public Dictionary<string, List<string>> Suggestions { get; set; } = new();

        /// <summary>Total number of results found.</summary>
        public int TotalResults { get; set; }
    }
}
