using System.Text.RegularExpressions;

namespace IRSearchEngine.Services
{
    /// <summary>
    /// Arabic text preprocessing pipeline.
    /// Steps: Normalization → Tashkeel Removal → Tokenization → Stop-word Removal → Light Stemming
    /// </summary>
    public class ArabicProcessor
    {
        /// <summary>
        /// Standard Arabic stop words to be removed during preprocessing.
        /// </summary>
        private static readonly HashSet<string> StopWords = new()
        {
            "من", "في", "على", "الى", "إلى", "عن", "هذا", "هذه", "هو", "هي",
            "كان", "تكون", "ان", "أن", "إن", "لا", "ما", "مع", "او", "أو",
            "ذلك", "تلك", "التي", "الذي", "اللذين", "اللتين", "الذين", "اللاتي",
            "كل", "بعض", "غير", "بين", "حيث", "قد", "لقد", "ثم", "حتى",
            "اذا", "إذا", "لم", "لن", "كما", "عند", "منذ", "بل", "لكن",
            "اي", "أي", "فقط", "ايضا", "أيضا", "نحو", "خلال", "ضد", "بعد",
            "قبل", "فوق", "تحت", "عبر", "دون", "سوى", "مثل", "يكون", "كانت",
            "ليس", "وهو", "وهي", "هنا", "هناك", "الا", "إلا"
        };

        /// <summary>
        /// Prefixes to remove during light stemming.
        /// Order matters: longer prefixes first to avoid partial matches.
        /// </summary>
        private static readonly string[] Prefixes = { "وال", "بال", "كال", "فال", "لل", "ال", "و", "ب", "ك", "ل", "ف" };

        /// <summary>
        /// Suffixes to remove during light stemming.
        /// Order matters: longer suffixes first.
        /// </summary>
        private static readonly string[] Suffixes = { "ات", "ون", "ين", "ان", "تن", "تم", "هم", "هن", "ها", "ية", "ه", "ة", "ي" };

        /// <summary>
        /// Processes Arabic text through the full NLP pipeline.
        /// </summary>
        /// <param name="text">Raw Arabic text input.</param>
        /// <returns>List of cleaned, stemmed Arabic tokens.</returns>
        public List<string> Process(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Step 1: Normalization — standardize Arabic character variants
            string normalized = Normalize(text);

            // Step 2: Remove Tashkeel (diacritics)
            normalized = RemoveTashkeel(normalized);

            // Step 3: Tokenization — split into individual Arabic words
            List<string> tokens = Tokenize(normalized);

            // Step 4: Stop-word Removal
            tokens = RemoveStopWords(tokens);

            // Step 5: Light Stemming — rule-based prefix/suffix removal
            tokens = ApplyLightStemming(tokens);

            return tokens;
        }

        /// <summary>
        /// Normalizes Arabic text by standardizing character variants.
        /// Converts: أ, إ, آ → ا | ى → ي | ة → ه (optional, kept for better matching)
        /// </summary>
        private string Normalize(string text)
        {
            // Normalize Hamza variants: أ إ آ → ا
            text = Regex.Replace(text, "[أإآ]", "ا");

            // Normalize Alef Maqsura: ى → ي
            text = text.Replace('ى', 'ي');

            // Normalize Taa Marbouta: ة → ه
            text = text.Replace('ة', 'ه');

            // Remove non-Arabic characters (keep Arabic letters and spaces only)
            text = Regex.Replace(text, @"[^\u0600-\u06FF\s]", " ");

            // Collapse multiple spaces
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        /// <summary>
        /// Removes Arabic diacritical marks (Tashkeel/Harakat).
        /// Unicode range: \u064B (Fathatan) to \u065F (Waslah)
        /// Also removes Tatweel (Kashida): \u0640
        /// </summary>
        private string RemoveTashkeel(string text)
        {
            // Remove diacritics: Fathatan (\u064B) through Waslah (\u065F)
            text = Regex.Replace(text, @"[\u064B-\u065F]", "");

            // Remove Tatweel/Kashida (decorative elongation)
            text = text.Replace("\u0640", "");

            return text;
        }

        /// <summary>
        /// Tokenizes Arabic text by splitting on whitespace and filtering invalid tokens.
        /// Keeps only tokens containing valid Arabic characters.
        /// </summary>
        private List<string> Tokenize(string text)
        {
            return Regex.Split(text, @"\s+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Where(token => Regex.IsMatch(token, @"[\u0600-\u06FF]")) // Must contain Arabic chars
                .ToList();
        }

        /// <summary>
        /// Removes common Arabic stop words from the token list.
        /// </summary>
        private List<string> RemoveStopWords(List<string> tokens)
        {
            return tokens
                .Where(token => !StopWords.Contains(token))
                .ToList();
        }

        /// <summary>
        /// Applies light stemming to Arabic tokens using rule-based prefix and suffix removal.
        /// This approach is preferred over heavy root extraction for IR applications
        /// as it preserves more semantic meaning while still normalizing word forms.
        /// 
        /// Examples:
        ///   الجامعات → جامع  (remove prefix "ال", suffix "ات", then "ه")
        ///   والطلاب  → طلاب  (remove prefix "و" then "ال")
        ///   المعلمين → معلم  (remove prefix "ال", suffix "ين")
        /// </summary>
        private List<string> ApplyLightStemming(List<string> tokens)
        {
            return tokens
                .Select(LightStem)
                .Where(token => !string.IsNullOrWhiteSpace(token) && token.Length > 1)
                .ToList();
        }

        /// <summary>
        /// Applies light stemming rules to a single Arabic token.
        /// Removes known prefixes and suffixes while ensuring the remaining
        /// stem has a minimum length of 2 characters.
        /// </summary>
        private string LightStem(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length <= 2)
                return word;

            string original = word;

            // Step 1: Remove prefixes (try longest match first)
            foreach (var prefix in Prefixes)
            {
                if (word.StartsWith(prefix) && (word.Length - prefix.Length) >= 2)
                {
                    word = word.Substring(prefix.Length);
                    break; // Only remove one prefix
                }
            }

            // Step 2: Remove suffixes (try longest match first)
            foreach (var suffix in Suffixes)
            {
                if (word.EndsWith(suffix) && (word.Length - suffix.Length) >= 2)
                {
                    word = word.Substring(0, word.Length - suffix.Length);
                    break; // Only remove one suffix
                }
            }

            // Safety: if stemming reduced too aggressively, return original
            if (word.Length < 2)
                return original;

            return word;
        }
    }
}
