using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using IRSearchEngine.Models;
using IRSearchEngine.Services;

namespace IRSearchEngine.Controllers
{
    /// <summary>
    /// Controller for building and querying the Positional Inverted Index and K-Gram Index.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IndexController : ControllerBase
    {
        private readonly InvertedIndexService _invertedIndex;
        private readonly KGramIndexService _kgramIndex;
        private readonly IConfiguration _config;

        public IndexController(InvertedIndexService invertedIndex, KGramIndexService kgramIndex, IConfiguration config)
        {
            _invertedIndex = invertedIndex;
            _kgramIndex = kgramIndex;
            _config = config;
        }

        /// <summary>
        /// Builds both the Positional Inverted Index and K-Gram Index from the database.
        /// </summary>
        [HttpPost("build")]
        public async Task<IActionResult> BuildIndex()
        {
            try
            {
                // Get connection string from configuration
                var connectionString = _config.GetConnectionString("DefaultConnection")
                    ?? "Server=db50052.public.databaseasp.net; Database=db50052; User Id=db50052; Password=irProject; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True";

                List<Document> documents;

                // Fetch all documents from the database using Dapper
                using (var connection = new SqlConnection(connectionString))
                {
                    var sql = "SELECT Id, Content, Language FROM Documents";
                    var result = await connection.QueryAsync<Document>(sql);
                    documents = result.ToList();
                }

                if (documents.Count == 0)
                    return BadRequest(new { message = "No documents found. Please run SeedData.sql first." });

                // Step 1: Build the positional inverted index
                _invertedIndex.BuildIndex(documents);

                // Step 2: Build the k-gram index from the vocabulary
                _kgramIndex.BuildKGramIndex();

                var index = _invertedIndex.GetIndex();
                return Ok(new
                {
                    message = "Index built successfully.",
                    totalDocumentsIndexed = documents.Count,
                    totalTermsIndexed = index.Count,
                    totalKGrams = _kgramIndex.GetKGramCount()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to build index.", details = ex.Message });
            }
        }

        /// <summary>Returns the full positional inverted index as JSON.</summary>
        [HttpGet]
        public IActionResult GetFullIndex()
        {
            return Ok(_invertedIndex.GetIndex());
        }

        /// <summary>Returns postings for a specific term.</summary>
        [HttpGet("term/{term}")]
        public IActionResult GetTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { error = "Term cannot be empty." });

            var documents = _invertedIndex.GetTermPositions(term);
            if (documents == null || documents.Count == 0)
                return NotFound(new { error = $"Term '{term}' not found in the index." });

            return Ok(new IndexTermResponse { Term = term, Documents = documents });
        }
    }
}
