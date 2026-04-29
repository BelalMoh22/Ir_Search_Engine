using IRSearchEngine.Models;

namespace IRSearchEngine.Services
{
    /// <summary>
    /// Builds and manages a Positional Inverted Index.
    /// Structure: Term → DocumentId → List of Positions
    /// Supports phrase queries, proximity queries, and TF-IDF ranking.
    /// </summary>
    public class InvertedIndexService
    {
        // Reference to the text preprocessing service
        private readonly TextProcessorService _textProcessor;

        // The positional inverted index: term -> docId -> positions
        private Dictionary<string, Dictionary<int, List<int>>> _index = new();

        // Total number of documents indexed
        private int _totalDocuments = 0;

        // Maps docId to document length (number of tokens after preprocessing)
        private Dictionary<int, int> _documentLengths = new();

        // Maps docId to document language
        private Dictionary<int, string> _documentLanguages = new();

        // Maps docId to raw content (for snippets)
        private Dictionary<int, string> _documentContents = new();

        // Set of all unique terms (vocabulary)
        private HashSet<string> _vocabulary = new();

        // Flag to check if index has been built
        public bool IsBuilt => _totalDocuments > 0;

        public InvertedIndexService(TextProcessorService textProcessor)
        {
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Builds the positional inverted index from a list of documents.
        /// Each document is preprocessed using the appropriate language pipeline.
        /// </summary>
        public void BuildIndex(List<Document> documents)
        {
            // Reset all index data structures for a clean rebuild
            _index = new Dictionary<string, Dictionary<int, List<int>>>();
            _documentLengths = new Dictionary<int, int>();
            _documentLanguages = new Dictionary<int, string>();
            _documentContents = new Dictionary<int, string>();
            _vocabulary = new HashSet<string>();
            _totalDocuments = documents.Count;

            // Process each document through the NLP pipeline
            foreach (var doc in documents)
            {
                // Store document metadata for later use
                _documentLanguages[doc.Id] = doc.Language;
                _documentContents[doc.Id] = doc.Content;

                // Preprocess the document content using language-specific pipeline
                var tokens = _textProcessor.Process(doc.Content, doc.Language);

                // Store document length for TF normalization
                _documentLengths[doc.Id] = tokens.Count;

                // Build positional index by iterating through each token with its position
                for (int position = 0; position < tokens.Count; position++)
                {
                    // Get the current term
                    string term = tokens[position];

                    // Add term to vocabulary set
                    _vocabulary.Add(term);

                    // Create entry for term if it doesn't exist
                    if (!_index.ContainsKey(term))
                        _index[term] = new Dictionary<int, List<int>>();

                    // Create entry for document under this term if it doesn't exist
                    if (!_index[term].ContainsKey(doc.Id))
                        _index[term][doc.Id] = new List<int>();

                    // Record the position of this term in this document
                    _index[term][doc.Id].Add(position);
                }
            }
        }

        /// <summary>Returns the full positional inverted index.</summary>
        public Dictionary<string, Dictionary<int, List<int>>> GetIndex() => _index;

        /// <summary>Returns the total number of indexed documents.</summary>
        public int GetTotalDocuments() => _totalDocuments;

        /// <summary>Returns the set of all unique terms in the index.</summary>
        public HashSet<string> GetVocabulary() => _vocabulary;

        /// <summary>Returns document languages map.</summary>
        public Dictionary<int, string> GetDocumentLanguages() => _documentLanguages;

        /// <summary>Returns document contents map.</summary>
        public Dictionary<int, string> GetDocumentContents() => _documentContents;

        /// <summary>Returns document lengths map.</summary>
        public Dictionary<int, int> GetDocumentLengths() => _documentLengths;

        /// <summary>
        /// Gets the postings (docId → positions) for a specific term.
        /// Returns null if the term is not in the index.
        /// </summary>
        public Dictionary<int, List<int>>? GetTermPositions(string term)
        {
            // Try exact match first
            if (_index.TryGetValue(term, out var docs))
                return docs;

            // Try lowercase match
            if (_index.TryGetValue(term.ToLowerInvariant(), out var docsLower))
                return docsLower;

            return null;
        }

        /// <summary>
        /// Gets the document frequency (df) for a term.
        /// df = number of documents containing the term.
        /// </summary>
        public int GetDocumentFrequency(string term)
        {
            // Return number of documents containing this term, or 0 if not found
            if (_index.TryGetValue(term, out var docs))
                return docs.Count;
            return 0;
        }

        /// <summary>
        /// Gets the term frequency (tf) for a term in a specific document.
        /// tf = number of times the term appears in the document.
        /// </summary>
        public int GetTermFrequency(string term, int docId)
        {
            // Check if term exists and has an entry for this document
            if (_index.TryGetValue(term, out var docs) && docs.TryGetValue(docId, out var positions))
                return positions.Count;
            return 0;
        }

        /// <summary>
        /// Returns all document IDs that have been indexed.
        /// </summary>
        public List<int> GetAllDocumentIds()
        {
            return _documentLengths.Keys.ToList();
        }
    }
}
