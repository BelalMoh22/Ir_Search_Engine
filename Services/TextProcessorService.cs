namespace IRSearchEngine.Services
{
    /// <summary>
    /// Main text processing service that delegates to language-specific processors.
    /// Acts as a facade for the English and Arabic processing pipelines.
    /// Output is designed to be used for:
    ///   - Positional Inverted Index construction
    ///   - TF-IDF calculations
    ///   - Query parsing and processing
    /// </summary>
    public class TextProcessorService
    {
        private readonly EnglishProcessor _english;
        private readonly ArabicProcessor _arabic;

        public TextProcessorService(EnglishProcessor english, ArabicProcessor arabic)
        {
            _english = english;
            _arabic = arabic;
        }

        /// <summary>
        /// Processes text through the appropriate language-specific pipeline.
        /// </summary>
        /// <param name="text">Raw text to process.</param>
        /// <param name="language">"en" for English, "ar" for Arabic.</param>
        /// <returns>List of processed, stemmed tokens.</returns>
        /// <exception cref="ArgumentException">Thrown when language is not supported.</exception>
        public List<string> Process(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return language?.ToLowerInvariant() switch
            {
                "en" => _english.Process(text),
                "ar" => _arabic.Process(text),
                _ => throw new ArgumentException($"Unsupported language: '{language}'. Use 'en' or 'ar'.")
            };
        }
    }
}
