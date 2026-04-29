using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using IRSearchEngine.Models;

namespace IRSearchEngine.Controllers
{
    /// <summary>
    /// Controller for fetching documents from the database.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DocumentsController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>Returns all documents from the database.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var connectionString = _config.GetConnectionString("DefaultConnection")
                    ?? "Server=db50052.public.databaseasp.net; Database=db50052; User Id=db50052; Password=irProject; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True";

                using var connection = new SqlConnection(connectionString);
                var sql = "SELECT Id, Content, Language FROM Documents";
                var documents = await connection.QueryAsync<Document>(sql);

                return Ok(new
                {
                    totalDocuments = documents.Count(),
                    documents = documents.Select(d => new
                    {
                        d.Id,
                        d.Language,
                        contentPreview = d.Content.Length > 200 ? d.Content.Substring(0, 200) + "..." : d.Content
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
