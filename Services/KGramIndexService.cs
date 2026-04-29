namespace IRSearchEngine.Services
{
    /// <summary>
    /// K-Gram Index Service for supporting wildcard queries and spelling correction.
    /// Breaks vocabulary terms into character k-grams (default k=2, bigrams)
    /// and maps each k-gram to the set of terms that contain it.
    /// </summary>
    public class KGramIndexService
    {
        // The k-gram index: k-gram string → set of vocabulary terms containing it
        private Dictionary<string, HashSet<string>> _kgramIndex = new();

        // The value of k (gram size) — using bigrams by default
        private readonly int _k;

        // Reference to the inverted index to access the vocabulary
        private readonly InvertedIndexService _invertedIndex;

        public KGramIndexService(InvertedIndexService invertedIndex, int k = 2)
        {
            _invertedIndex = invertedIndex;
            _k = k;
        }

        /// <summary>
        /// Builds the k-gram index from the vocabulary in the inverted index.
        /// Each term is padded with $ markers and split into k-grams.
        /// Example: "learn" with k=2 → {"$l", "le", "ea", "ar", "rn", "n$"}
        /// </summary>
        public void BuildKGramIndex()
        {
            // Reset the k-gram index
            _kgramIndex = new Dictionary<string, HashSet<string>>();

            // Get all unique terms from the inverted index vocabulary
            var vocabulary = _invertedIndex.GetVocabulary();

            // Process each term in the vocabulary
            foreach (var term in vocabulary)
            {
                // Generate k-grams for this term
                var kgrams = GenerateKGrams(term);

                // Map each k-gram back to this term
                foreach (var kgram in kgrams)
                {
                    // Create the k-gram entry if it doesn't exist
                    if (!_kgramIndex.ContainsKey(kgram))
                        _kgramIndex[kgram] = new HashSet<string>();

                    // Add the term to this k-gram's set
                    _kgramIndex[kgram].Add(term);
                }
            }
        }

        /// <summary>
        /// Generates all k-grams for a given term.
        /// Pads the term with '$' at start and end.
        /// </summary>
        public List<string> GenerateKGrams(string term)
        {
            // Pad the term with $ boundary markers
            string padded = "$" + term + "$";

            // Extract all substrings of length k
            var kgrams = new List<string>();
            for (int i = 0; i <= padded.Length - _k; i++)
            {
                kgrams.Add(padded.Substring(i, _k));
            }

            return kgrams;
        }

        /// <summary>
        /// Expands a wildcard query pattern into matching vocabulary terms.
        /// Supports * at any position (e.g., "comput*", "*tion", "com*er").
        /// </summary>
        public List<string> ExpandWildcard(string pattern)
        {
            // Split the pattern by the wildcard character
            var parts = pattern.Split('*');

            // Generate k-grams for each non-empty part
            var candidateSets = new List<HashSet<string>>();

            foreach (var part in parts)
            {
                // Skip empty parts (from leading/trailing wildcards)
                if (string.IsNullOrWhiteSpace(part)) continue;

                // Generate k-grams for this part of the pattern
                var kgrams = GenerateKGrams(part);

                // For each k-gram, get the terms that contain it
                HashSet<string>? candidates = null;
                foreach (var kgram in kgrams)
                {
                    if (_kgramIndex.TryGetValue(kgram, out var terms))
                    {
                        if (candidates == null)
                            candidates = new HashSet<string>(terms);
                        else
                            candidates.IntersectWith(terms); // Intersect to narrow down
                    }
                    else
                    {
                        // K-gram not found, no matches possible for this part
                        candidates = new HashSet<string>();
                        break;
                    }
                }

                if (candidates != null)
                    candidateSets.Add(candidates);
            }

            // If no candidate sets, return empty
            if (candidateSets.Count == 0)
                return new List<string>();

            // Intersect all candidate sets
            var result = candidateSets[0];
            for (int i = 1; i < candidateSets.Count; i++)
            {
                result.IntersectWith(candidateSets[i]);
            }

            // Post-filter: verify candidates actually match the wildcard pattern
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$";

            return result
                .Where(term => System.Text.RegularExpressions.Regex.IsMatch(term, regex))
                .ToList();
        }

        /// <summary>
        /// Gets all terms that share k-grams with the given term.
        /// Used for spelling correction via Jaccard similarity.
        /// </summary>
        public Dictionary<string, double> GetSimilarTerms(string term, int maxResults = 5)
        {
            // Generate k-grams for the input term
            var termKGrams = new HashSet<string>(GenerateKGrams(term));

            // Collect all candidate terms that share at least one k-gram
            var candidates = new HashSet<string>();
            foreach (var kgram in termKGrams)
            {
                if (_kgramIndex.TryGetValue(kgram, out var terms))
                {
                    foreach (var t in terms)
                        candidates.Add(t);
                }
            }

            // Compute Jaccard similarity for each candidate
            var similarities = new Dictionary<string, double>();
            foreach (var candidate in candidates)
            {
                // Skip exact matches
                if (candidate == term) continue;

                // Get k-grams for the candidate term
                var candidateKGrams = new HashSet<string>(GenerateKGrams(candidate));

                // Jaccard = |A ∩ B| / |A ∪ B|
                var intersection = new HashSet<string>(termKGrams);
                intersection.IntersectWith(candidateKGrams);

                var union = new HashSet<string>(termKGrams);
                union.UnionWith(candidateKGrams);

                double jaccard = union.Count > 0 ? (double)intersection.Count / union.Count : 0;

                // Only include if similarity is above threshold
                if (jaccard > 0.2)
                    similarities[candidate] = jaccard;
            }

            // Return top results sorted by similarity descending
            return similarities
                .OrderByDescending(kvp => kvp.Value)
                .Take(maxResults)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>Returns the full k-gram index for debugging.</summary>
        public Dictionary<string, HashSet<string>> GetKGramIndex() => _kgramIndex;

        /// <summary>Returns the total number of unique k-grams.</summary>
        public int GetKGramCount() => _kgramIndex.Count;
    }
}
