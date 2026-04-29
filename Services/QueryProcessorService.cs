using System.Text.RegularExpressions; // Import regex for query pattern matching
using IRSearchEngine.Models; // Import models for SearchResponse, SearchResult

namespace IRSearchEngine.Services
{
    /// <summary>
    /// Query Processor Service that handles supported query types:
    /// 1. Normal queries (AND logic) — e.g., "machine learning"
    /// 2. Proximity queries (/k operator) — e.g., "machine /3 learning"
    /// </summary>
    public class QueryProcessorService
    {
        // Reference to the text preprocessing service for query term processing
        private readonly TextProcessorService _textProcessor;

        // Reference to the inverted index for term lookups
        private readonly InvertedIndexService _invertedIndex;

        // Reference to the ranking service for TF-IDF + cosine similarity
        private readonly RankingService _rankingService;

        // Reference to the spelling correction service for "Did you mean?" suggestions
        private readonly SpellingCorrectionService _spellingService;

        /// <summary>
        /// Constructor: injects all required services via dependency injection.
        /// </summary>
        public QueryProcessorService(
            TextProcessorService textProcessor,       // For preprocessing query text
            InvertedIndexService invertedIndex,        // For looking up term postings
            KGramIndexService kgramIndex,              // Required by DI chain (used by spelling)
            RankingService rankingService,             // For TF-IDF ranking
            SpellingCorrectionService spellingService) // For spelling suggestions
        {
            _textProcessor = textProcessor;    // Store text processor reference
            _invertedIndex = invertedIndex;    // Store inverted index reference
            _rankingService = rankingService;  // Store ranking service reference
            _spellingService = spellingService; // Store spelling service reference
        }

        /// <summary>
        /// Main entry point: detects query type and processes accordingly.
        /// Supports: Normal (AND logic) and Proximity (/k operator).
        /// </summary>
        /// <param name="query">The raw query string from the user.</param>
        /// <param name="language">Optional language filter ("en" or "ar").</param>
        /// <returns>SearchResponse with ranked results and suggestions.</returns>
        public SearchResponse ProcessQuery(string query, string? language = null)
        {
            // Detect whether this is a normal or proximity query
            string queryType = DetectQueryType(query);
            // Process the query based on its detected type
            SearchResponse response = queryType switch
            {
                "Proximity" => ProcessProximityQuery(query, language), // Handle proximity /k syntax
                _ => ProcessNormalQuery(query, language)               // Default: normal AND query
            };
            response.Query = query;                        // Store original query
            response.QueryType = queryType;                // Store detected type
            response.TotalResults = response.Results.Count;   // Store result count
            return response; // Return the complete response
        }

        /// <summary>
        /// Detects the type of query based on its syntax.
        /// Proximity: contains /N pattern (e.g., "word1 /3 word2")
        /// Normal: everything else
        /// </summary>
        /// <param name="query">The raw query string.</param>
        /// <returns>"Proximity" or "Normal".</returns>
        private string DetectQueryType(string query)
        {
            query = query.Trim(); // Remove leading/trailing whitespace

            // Check for proximity pattern: word /number word
            if (Regex.IsMatch(query, @"\S+\s+/\d+\s+\S+"))
                return "Proximity"; // Matches proximity syntax

            return "Normal"; // Default: treat as normal query
        }

        /// <summary>
        /// Auto-detects the language of the query text.
        /// If any Arabic Unicode characters are found, returns "ar".
        /// Otherwise returns "en".
        /// </summary>
        /// <param name="text">The query text to analyze.</param>
        /// <returns>"ar" for Arabic, "en" for English.</returns>
        private string DetectLanguage(string text)
        {
            // Check if text contains any Arabic Unicode characters (range: 0x0600-0x06FF)
            if (Regex.IsMatch(text, @"[\u0600-\u06FF]"))
                return "ar"; // Arabic detected

            return "en"; // Default: English
        }

        /// <summary>
        /// Processes a normal (free-text) query with AND logic.
        /// Steps:
        ///   1. Preprocess query terms (tokenize, stem, etc.)
        ///   2. Find documents containing ALL query terms (AND logic)
        ///   3. Check spelling for missing terms
        ///   4. Rank results using TF-IDF + Cosine Similarity
        /// </summary>
        /// <param name="query">The raw query string.</param>
        /// <param name="language">Optional language filter.</param>
        /// <returns>SearchResponse with ranked results.</returns>
        private SearchResponse ProcessNormalQuery(string query, string? language)
        {
            var response = new SearchResponse(); // Initialize empty response

            // Auto-detect language if not specified by the user
            string lang = language ?? DetectLanguage(query);

            // Preprocess query terms using the same pipeline as document indexing
            var queryTerms = _textProcessor.Process(query, lang);
            response.ProcessedTerms = queryTerms; // Store processed terms in response

            // If no valid terms remain after preprocessing, return empty results
            if (queryTerms.Count == 0) return response;

            // Check each query term for spelling corrections
            foreach (var term in queryTerms)
            {
                // If term is not found in the index vocabulary
                if (_invertedIndex.GetDocumentFrequency(term) == 0)
                {
                    // Get spelling suggestions using Levenshtein + Jaccard
                    var suggestions = _spellingService.SuggestCorrections(term);

                    // If suggestions exist, add them to the response
                    if (suggestions.Count > 0)
                        response.Suggestions[term] = suggestions;
                }
            }

            // Apply AND logic: find documents that contain ALL query terms
            HashSet<int>? candidateDocs = null; // Will hold the intersection of doc sets

            foreach (var term in queryTerms)
            {
                // Get the postings (documents) for this term
                var postings = _invertedIndex.GetTermPositions(term);

                if (postings != null)
                {
                    // Get the set of document IDs containing this term
                    var docSet = new HashSet<int>(postings.Keys);

                    if (candidateDocs == null)
                        candidateDocs = docSet;          // First term: initialize candidate set
                    else
                        candidateDocs.IntersectWith(docSet); // Subsequent terms: AND (intersect)
                }
                else
                {
                    // Term not found at all — AND result is empty
                    candidateDocs = new HashSet<int>();
                    break; // No need to check further
                }
            }

            // If no candidates found, return empty results
            if (candidateDocs == null || candidateDocs.Count == 0)
                return response;

            // Filter candidates by language if specified
            if (!string.IsNullOrEmpty(language))
            {
                // Get the language map for all indexed documents
                var docLangs = _invertedIndex.GetDocumentLanguages();

                // Remove documents that don't match the requested language
                candidateDocs.RemoveWhere(id =>
                    docLangs.ContainsKey(id) && docLangs[id] != language);
            }

            // Rank the candidate documents using TF-IDF + Cosine Similarity
            var ranked = _rankingService.RankDocuments(queryTerms, candidateDocs.ToList());

            // Build final search result objects with snippets
            response.Results = BuildResults(ranked);

            return response; // Return the complete response
        }

        /// <summary>
        /// Processes a proximity query: finds documents where two terms
        /// appear within a specified distance (k) of each other.
        /// Format: "term1 /k term2" where k is the maximum allowed distance.
        /// Condition: |position1 - position2| ≤ k
        /// </summary>
        /// <param name="query">The raw proximity query string.</param>
        /// <param name="language">Optional language filter.</param>
        /// <returns>SearchResponse with ranked results.</returns>
        private SearchResponse ProcessProximityQuery(string query, string? language)
        {
            var response = new SearchResponse(); // Initialize empty response

            // Parse the proximity query using regex: "term1 /N term2"
            var match = Regex.Match(query.Trim(), @"^(.+?)\s+/(\d+)\s+(.+)$");

            // If the pattern doesn't match, fall back to normal query processing
            if (!match.Success)
                return ProcessNormalQuery(query, language);

            // Extract the two terms and the distance value
            string term1Raw = match.Groups[1].Value.Trim(); // First term (raw)
            int maxDistance = int.Parse(match.Groups[2].Value); // Maximum allowed distance (k)
            string term2Raw = match.Groups[3].Value.Trim(); // Second term (raw)

            // Auto-detect language from the combined terms
            string lang = language ?? DetectLanguage(term1Raw + " " + term2Raw);

            // Preprocess both terms through the NLP pipeline
            var terms1 = _textProcessor.Process(term1Raw, lang); // Process first term
            var terms2 = _textProcessor.Process(term2Raw, lang); // Process second term

            // If either term produces no tokens, return empty results
            if (terms1.Count == 0 || terms2.Count == 0)
                return response;

            // Take the first processed token from each term
            string term1 = terms1[0]; // Stemmed version of first term
            string term2 = terms2[0]; // Stemmed version of second term
            response.ProcessedTerms = new List<string> { term1, term2 }; // Store in response

            // Get the positional postings for both terms from the inverted index
            var postings1 = _invertedIndex.GetTermPositions(term1); // Postings for term1
            var postings2 = _invertedIndex.GetTermPositions(term2); // Postings for term2

            // If either term is not in the index, return empty results
            if (postings1 == null || postings2 == null)
                return response;

            // Find documents where both terms appear within maxDistance positions
            var matchingDocs = new List<int>(); // Will hold matching document IDs

            // Iterate through documents containing the first term
            foreach (var docId in postings1.Keys)
            {
                // Skip if the second term doesn't appear in this document
                if (!postings2.ContainsKey(docId)) continue;

                // Get position lists for both terms in this document
                var pos1 = postings1[docId]; // Positions of term1
                var pos2 = postings2[docId]; // Positions of term2

                // Check if any pair of positions satisfies |p1 - p2| ≤ k
                bool found = false; // Flag to track if a valid pair is found
                foreach (var p1 in pos1) // Iterate through positions of term1
                {
                    foreach (var p2 in pos2) // Iterate through positions of term2
                    {
                        // Check the proximity condition: absolute difference ≤ k
                        if (Math.Abs(p1 - p2) <= maxDistance)
                        {
                            found = true; // A valid proximity pair was found
                            break;        // No need to check more pairs
                        }
                    }
                    if (found) break; // Exit outer loop too
                }

                // If a valid pair was found, add this document to matches
                if (found) matchingDocs.Add(docId);
            }

            // Filter matching documents by language if specified
            if (!string.IsNullOrEmpty(language))
            {
                var docLangs = _invertedIndex.GetDocumentLanguages(); // Get language map
                matchingDocs.RemoveAll(id =>
                    docLangs.ContainsKey(id) && docLangs[id] != language); // Remove mismatches
            }

            // Rank the matching documents using TF-IDF + Cosine Similarity
            var queryTerms = new List<string> { term1, term2 }; // Query vector terms
            var ranked = _rankingService.RankDocuments(queryTerms, matchingDocs);

            // Build final search result objects with content snippets
            response.Results = BuildResults(ranked);

            return response; // Return the complete response
        }

        /// <summary>
        /// Builds SearchResult objects from ranked (docId, score) pairs.
        /// Includes a content snippet (first 200 chars) for each result.
        /// </summary>
        /// <param name="ranked">List of (DocId, Score) tuples sorted by score.</param>
        /// <returns>List of SearchResult objects for the API response.</returns>
        private List<SearchResult> BuildResults(List<(int DocId, double Score)> ranked)
        {
            // Get document content and language maps from the index
            var contents = _invertedIndex.GetDocumentContents(); // DocId → Content
            var langs = _invertedIndex.GetDocumentLanguages();   // DocId → Language

            // Map each ranked result to a SearchResult object
            return ranked.Select(r => new SearchResult
            {
                DocumentId = r.DocId, // The document ID
                Score = r.Score,      // The TF-IDF cosine similarity score

                // Create a snippet: first 200 characters of document content
                Snippet = contents.ContainsKey(r.DocId)
                    ? (contents[r.DocId].Length > 200
                        ? contents[r.DocId].Substring(0, 200) + "..." // Truncate long content
                        : contents[r.DocId])                           // Short content: show all
                    : "", // Fallback: empty snippet if content not found

                // Get the document language
                Language = langs.ContainsKey(r.DocId) ? langs[r.DocId] : "unknown"
            }).ToList(); // Convert to list and return
        }
    }
}
