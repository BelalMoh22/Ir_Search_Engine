namespace IRSearchEngine.Services
{
    /// <summary>
    /// Spelling Correction Service using Levenshtein Distance and Jaccard Similarity.
    /// When a query term is not found in the index, this service suggests corrections.
    /// </summary>
    public class SpellingCorrectionService
    {
        // Reference to the inverted index for vocabulary access
        private readonly InvertedIndexService _invertedIndex;

        // Reference to the k-gram index for Jaccard similarity
        private readonly KGramIndexService _kgramIndex;

        public SpellingCorrectionService(InvertedIndexService invertedIndex, KGramIndexService kgramIndex)
        {
            _invertedIndex = invertedIndex;
            _kgramIndex = kgramIndex;
        }

        /// <summary>
        /// Computes the Levenshtein (edit) distance between two strings.
        /// This measures the minimum number of single-character edits
        /// (insertions, deletions, substitutions) to transform one string into another.
        /// </summary>
        public int LevenshteinDistance(string source, string target)
        {
            // Handle edge cases for empty strings
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            // Get lengths of both strings
            int sourceLen = source.Length;
            int targetLen = target.Length;

            // Create the distance matrix (sourceLen+1) x (targetLen+1)
            var matrix = new int[sourceLen + 1, targetLen + 1];

            // Initialize first column: distance from empty string to source[0..i]
            for (int i = 0; i <= sourceLen; i++)
                matrix[i, 0] = i;

            // Initialize first row: distance from empty string to target[0..j]
            for (int j = 0; j <= targetLen; j++)
                matrix[0, j] = j;

            // Fill in the rest of the matrix using dynamic programming
            for (int i = 1; i <= sourceLen; i++)
            {
                for (int j = 1; j <= targetLen; j++)
                {
                    // Cost is 0 if characters match, 1 if they differ
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    // Take minimum of three operations:
                    matrix[i, j] = Math.Min(
                        Math.Min(
                            matrix[i - 1, j] + 1,       // Deletion
                            matrix[i, j - 1] + 1),      // Insertion
                        matrix[i - 1, j - 1] + cost);   // Substitution
                }
            }

            // Return the edit distance (bottom-right cell)
            return matrix[sourceLen, targetLen];
        }

        /// <summary>
        /// Suggests spelling corrections for a term not found in the index.
        /// Combines Levenshtein distance and Jaccard similarity (via k-grams)
        /// to find the best matching vocabulary terms.
        /// </summary>
        public List<string> SuggestCorrections(string term, int maxSuggestions = 5)
        {
            // Get the vocabulary from the inverted index
            var vocabulary = _invertedIndex.GetVocabulary();

            // If vocabulary is empty, no suggestions possible
            if (vocabulary.Count == 0) return new List<string>();

            // If the term already exists in the vocabulary, no correction needed
            if (vocabulary.Contains(term)) return new List<string>();

            // Strategy 1: Get candidates via k-gram Jaccard similarity
            var jaccardCandidates = _kgramIndex.GetSimilarTerms(term, maxSuggestions * 2);

            // Strategy 2: Compute Levenshtein distance for Jaccard candidates
            // Combine both scores for final ranking
            var scoredCandidates = new List<(string Term, double CombinedScore)>();

            foreach (var candidate in jaccardCandidates)
            {
                // Compute edit distance between input term and candidate
                int editDistance = LevenshteinDistance(term, candidate.Key);

                // Normalize edit distance to a 0-1 similarity score
                int maxLen = Math.Max(term.Length, candidate.Key.Length);
                double editSimilarity = maxLen > 0 ? 1.0 - ((double)editDistance / maxLen) : 0;

                // Combined score: weighted average of Jaccard and edit similarity
                // Jaccard weight = 0.4, Edit similarity weight = 0.6
                double combinedScore = (0.4 * candidate.Value) + (0.6 * editSimilarity);

                scoredCandidates.Add((candidate.Key, combinedScore));
            }

            // Also check vocabulary terms with small edit distance (for short terms)
            if (term.Length <= 6)
            {
                foreach (var vocabTerm in vocabulary)
                {
                    // Skip terms already scored via Jaccard
                    if (jaccardCandidates.ContainsKey(vocabTerm)) continue;

                    // Only consider terms with edit distance <= 2
                    int editDist = LevenshteinDistance(term, vocabTerm);
                    if (editDist <= 2)
                    {
                        int maxLen = Math.Max(term.Length, vocabTerm.Length);
                        double editSim = 1.0 - ((double)editDist / maxLen);
                        scoredCandidates.Add((vocabTerm, editSim * 0.6));
                    }
                }
            }

            // Return top suggestions sorted by combined score descending
            return scoredCandidates
                .OrderByDescending(c => c.CombinedScore)
                .Select(c => c.Term)
                .Distinct()
                .Take(maxSuggestions)
                .ToList();
        }
    }
}
