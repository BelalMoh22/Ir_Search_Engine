namespace IRSearchEngine.Models
{
    /// <summary>
    /// Response model containing the processed tokens.
    /// </summary>
    public class ProcessResponse
    {
        /// <summary>
        /// The list of processed, normalized, and stemmed tokens.
        /// </summary>
        public List<string> Tokens { get; set; } = new();

        /// <summary>
        /// The original text that was processed.
        /// </summary>
        public string OriginalText { get; set; } = string.Empty;

        /// <summary>
        /// The language used for processing.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// Number of tokens after processing.
        /// </summary>
        public int TokenCount { get; set; }
    }
}
