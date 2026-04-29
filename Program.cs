using IRSearchEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add controller services to the DI container
builder.Services.AddControllers();

// Register Text Processing Services (Singleton — stateless processors)
builder.Services.AddSingleton<EnglishProcessor>();
builder.Services.AddSingleton<ArabicProcessor>();
builder.Services.AddSingleton<TextProcessorService>();

// Register Indexing Services (Singleton — maintain index state in memory)
builder.Services.AddSingleton<InvertedIndexService>();
builder.Services.AddSingleton<KGramIndexService>();

// Register Ranking and Query Services (Singleton — depend on index state)
builder.Services.AddSingleton<RankingService>();
builder.Services.AddSingleton<SpellingCorrectionService>();
builder.Services.AddSingleton<QueryProcessorService>();

var app = builder.Build();

// Enable HTTPS redirection
app.UseHttpsRedirection();

// Enable serving static files from wwwroot (for the frontend UI)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

// Map controller routes
app.MapControllers();

app.Run();
