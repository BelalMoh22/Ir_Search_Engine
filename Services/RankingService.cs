namespace IRSearchEngine.Services
{
    /// <summary>
    /// TF-IDF Ranking Service using Cosine Similarity.
    /// Computes relevance scores between query vectors and document vectors.
    /// </summary>
    public class RankingService
    {
        // Reference to the inverted index for TF and DF values
        private readonly InvertedIndexService _invertedIndex;

        public RankingService(InvertedIndexService invertedIndex)
        {
            _invertedIndex = invertedIndex;
        }

        /// <summary>
        /// Computes TF (Term Frequency) using logarithmic weighting.
        /// TF = 1 + log10(rawTF) if rawTF > 0, else 0
        /// </summary>
        public double ComputeTF(int rawTF)
        {
            // If term doesn't appear in document, TF is 0
            if (rawTF == 0) return 0;

            // Logarithmic TF weighting to dampen high-frequency terms
            return 1.0 + Math.Log10(rawTF);
        }

        /// <summary>
        /// Computes IDF (Inverse Document Frequency).
        /// IDF = log10(N / df) where N = total docs, df = docs containing term
        /// </summary>
        public double ComputeIDF(int totalDocuments, int documentFrequency)
        {
            // Prevent division by zero
            if (documentFrequency == 0) return 0;

            // Standard IDF formula
            return Math.Log10((double)totalDocuments / documentFrequency);
        }

        /// <summary>
        /// Computes TF-IDF weight for a term in a document.
        /// Weight = TF * IDF
        /// </summary>
        public double ComputeTFIDF(int rawTF, int totalDocuments, int documentFrequency)
        {
            // TF-IDF = TF(t,d) * IDF(t)
            double tf = ComputeTF(rawTF);
            double idf = ComputeIDF(totalDocuments, documentFrequency);
            return tf * idf;
        }

        /// <summary>
        /// Ranks documents against a query using TF-IDF weighted Cosine Similarity.
        /// Returns a sorted list of (docId, score) pairs from highest to lowest.
        /// </summary>
        public List<(int DocId, double Score)> RankDocuments(List<string> queryTerms, List<int>? candidateDocIds = null)
        {
            // Get total number of documents in the collection
            int N = _invertedIndex.GetTotalDocuments();

            // If no documents indexed, return empty
            if (N == 0) return new List<(int, double)>();

            // Determine which documents to score
            var docIds = candidateDocIds ?? _invertedIndex.GetAllDocumentIds();

            // Build query TF-IDF vector
            // Count term frequencies in the query
            var queryTF = new Dictionary<string, int>();
            foreach (var term in queryTerms)
            {
                // Count occurrences of each term in the query
                if (!queryTF.ContainsKey(term))
                    queryTF[term] = 0;
                queryTF[term]++;
            }

            // Compute query TF-IDF weights
            var queryVector = new Dictionary<string, double>();
            foreach (var kvp in queryTF)
            {
                // Get document frequency for this term
                int df = _invertedIndex.GetDocumentFrequency(kvp.Key);

                // Compute TF-IDF weight for the query term
                double weight = ComputeTFIDF(kvp.Value, N, df);
                queryVector[kvp.Key] = weight;
            }

            // Compute query vector magnitude for normalization
            double queryMagnitude = Math.Sqrt(queryVector.Values.Sum(w => w * w));

            // If query vector is zero, no ranking possible
            if (queryMagnitude == 0) return new List<(int, double)>();

            // Score each candidate document using cosine similarity
            var scores = new List<(int DocId, double Score)>();

            foreach (var docId in docIds)
            {
                // Compute dot product between query and document vectors
                double dotProduct = 0;

                // Compute document vector magnitude (only for query terms)
                double docMagnitude = 0;

                foreach (var term in queryVector.Keys)
                {
                    // Get raw term frequency in this document
                    int rawTF = _invertedIndex.GetTermFrequency(term, docId);

                    // Get document frequency for IDF
                    int df = _invertedIndex.GetDocumentFrequency(term);

                    // Compute TF-IDF weight for the document term
                    double docWeight = ComputeTFIDF(rawTF, N, df);

                    // Accumulate dot product: query_weight * doc_weight
                    dotProduct += queryVector[term] * docWeight;

                    // Accumulate squared magnitude for the document
                    docMagnitude += docWeight * docWeight;
                }

                // Compute document magnitude
                docMagnitude = Math.Sqrt(docMagnitude);

                // Compute cosine similarity: dot(q,d) / (|q| * |d|)
                double score = 0;
                if (queryMagnitude > 0 && docMagnitude > 0)
                {
                    score = dotProduct / (queryMagnitude * docMagnitude);
                }

                // Only include documents with non-zero scores
                if (score > 0)
                {
                    scores.Add((docId, Math.Round(score, 6)));
                }
            }

            // Sort results by score descending (highest relevance first)
            return scores.OrderByDescending(s => s.Score).ToList();
        }
    }
}
