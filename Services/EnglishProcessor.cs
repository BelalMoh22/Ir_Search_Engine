using System.Text.RegularExpressions;
using Porter2StemmerStandard;

namespace IRSearchEngine.Services
{
    /// <summary>
    /// English text preprocessing pipeline.
    /// Steps: Normalization → Tokenization → Stop-word Removal → Porter Stemming
    /// </summary>
    public class EnglishProcessor
    {
        private readonly EnglishPorter2Stemmer _stemmer;

        /// <summary>
        /// Standard English stop words to be removed during preprocessing.
        /// </summary>
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "is", "in", "at", "which", "on", "and", "a", "an", "of", "to", "for",
            "it", "its", "this", "that", "with", "as", "are", "was", "were", "be", "been",
            "being", "have", "has", "had", "do", "does", "did", "but", "or", "not", "no",
            "so", "if", "by", "from", "up", "out", "about", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again", "further",
            "then", "once", "here", "there", "when", "where", "why", "how", "all", "each",
            "every", "both", "few", "more", "most", "other", "some", "such", "only", "own",
            "same", "than", "too", "very", "can", "will", "just", "should", "now"
        };

        public EnglishProcessor()
        {
            _stemmer = new EnglishPorter2Stemmer();
        }

        /// <summary>
        /// Processes English text through the full NLP pipeline.
        /// </summary>
        /// <param name="text">Raw English text input.</param>
        /// <returns>List of cleaned, stemmed tokens.</returns>
        public List<string> Process(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Step 1: Normalization — lowercase and remove punctuation
            string normalized = Normalize(text);

            // Step 2: Tokenization — split into individual words
            List<string> tokens = Tokenize(normalized);

            // Step 3: Stop-word Removal
            tokens = RemoveStopWords(tokens);

            // Step 4: Stemming using Porter2 Stemmer
            tokens = ApplyStemming(tokens);

            return tokens;
        }

        /// <summary>
        /// Normalizes text: converts to lowercase and removes punctuation/special characters.
        /// </summary>
        private string Normalize(string text)
        {
            // Convert to lowercase
            text = text.ToLowerInvariant();

            // Remove punctuation and special characters, keep only letters and spaces
            text = Regex.Replace(text, @"[^a-z\s]", " ");

            // Collapse multiple spaces into one
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        /// <summary>
        /// Tokenizes text by splitting on whitespace and filtering empty tokens.
        /// </summary>
        private List<string> Tokenize(string text)
        {
            return Regex.Split(text, @"\s+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToList();
        }

        /// <summary>
        /// Removes common English stop words from the token list.
        /// </summary>
        private List<string> RemoveStopWords(List<string> tokens)
        {
            return tokens
                .Where(token => !StopWords.Contains(token))
                .ToList();
        }

        /// <summary>
        /// Applies Porter2 stemming to each token to reduce words to their root forms.
        /// Examples: running → run, learning → learn, models → model
        /// </summary>
        private List<string> ApplyStemming(List<string> tokens)
        {
            return tokens
                .Select(token => _stemmer.Stem(token).Value)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToList();
        }
    }
}
