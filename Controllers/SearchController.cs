using Microsoft.AspNetCore.Mvc;
using IRSearchEngine.Models;
using IRSearchEngine.Services;

namespace IRSearchEngine.Controllers
{
    /// <summary>
    /// Search API controller for text processing, querying, and spelling suggestions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly TextProcessorService _textProcessor;
        private readonly QueryProcessorService _queryProcessor;
        private readonly SpellingCorrectionService _spellingService;

        public SearchController(
            TextProcessorService textProcessor,
            QueryProcessorService queryProcessor,
            SpellingCorrectionService spellingService)
        {
            _textProcessor = textProcessor;
            _queryProcessor = queryProcessor;
            _spellingService = spellingService;
        }

        /// <summary>Health check endpoint.</summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "API is working", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Preprocesses raw text through the language-specific NLP pipeline.
        /// </summary>
        [HttpPost("process")]
        public IActionResult Process([FromBody] ProcessRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { error = "Text field is required." });

            if (string.IsNullOrWhiteSpace(request.Language))
                return BadRequest(new { error = "Language field is required. Use 'en' or 'ar'." });

            string lang = request.Language.ToLowerInvariant();
            if (lang != "en" && lang != "ar")
                return BadRequest(new { error = "Use 'en' for English or 'ar' for Arabic." });

            try
            {
                var tokens = _textProcessor.Process(request.Text, lang);
                return Ok(new ProcessResponse
                {
                    Tokens = tokens,
                    OriginalText = request.Text,
                    Language = lang,
                    TokenCount = tokens.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Executes a search query supporting Normal, Phrase, Proximity, and Wildcard types.
        /// Ranks results using TF-IDF Cosine Similarity.
        /// </summary>
        [HttpPost("query")]
        public IActionResult Query([FromBody] SearchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query is required." });

            try
            {
                var response = _queryProcessor.ProcessQuery(request.Query, request.Language);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Suggests spelling corrections for a term not found in the index.
        /// Uses Levenshtein distance and Jaccard similarity (k-gram).
        /// </summary>
        [HttpGet("suggest")]
        public IActionResult Suggest([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { error = "Term parameter is required." });

            try
            {
                var suggestions = _spellingService.SuggestCorrections(term);
                return Ok(new
                {
                    term = term,
                    suggestions = suggestions,
                    message = suggestions.Count > 0 ? $"Did you mean: {string.Join(", ", suggestions)}?" : "No suggestions found."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
