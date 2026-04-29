namespace IRSearchEngine.Models
{
    /// <summary>
    /// Request model for the text processing endpoint.
    /// </summary>
    public class ProcessRequest
    {
        /// <summary>
        /// The raw text to be processed.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The language of the text: "en" for English, "ar" for Arabic.
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }
}
