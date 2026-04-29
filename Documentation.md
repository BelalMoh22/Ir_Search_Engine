# IR Search Engine — Project Documentation

## Bilingual Information Retrieval System (English + Arabic)

---

## 1. System Architecture

### Technology Stack
- **Backend:** ASP.NET Core 8.0 Web API
- **Database:** SQL Server with NVARCHAR(MAX) for Unicode/Arabic support
- **Data Access:** Dapper (micro-ORM)
- **NLP:** Porter2StemmerStandard (English), Custom Light Stemmer (Arabic)
- **Frontend:** Vanilla HTML/CSS/JavaScript

### Project Structure
```
IRSearchEngine/
├── Controllers/
│   ├── SearchController.cs        # Search, preprocessing, spelling APIs
│   ├── IndexController.cs         # Index build and lookup APIs
│   └── DocumentsController.cs     # Document listing API
├── Services/
│   ├── EnglishProcessor.cs        # English NLP pipeline
│   ├── ArabicProcessor.cs         # Arabic NLP pipeline
│   ├── TextProcessorService.cs    # Language router facade
│   ├── InvertedIndexService.cs    # Positional inverted index
│   ├── KGramIndexService.cs       # K-gram index (spelling correction)
│   ├── RankingService.cs          # TF-IDF + Cosine Similarity
│   ├── SpellingCorrectionService.cs # Levenshtein + Jaccard
│   └── QueryProcessorService.cs   # Normal + Proximity query handling
├── Models/
│   ├── Document.cs
│   ├── ProcessRequest.cs / ProcessResponse.cs
│   ├── SearchRequest.cs / SearchResult.cs
│   └── IndexResponse.cs
├── wwwroot/index.html             # Frontend UI
├── Program.cs                     # DI registration + middleware
└── appsettings.json               # Connection string
```

### Data Flow
```
User Query → QueryProcessorService
  → DetectQueryType (Normal or Proximity)
  → TextProcessorService (preprocess query terms)
  → InvertedIndexService (lookup postings)
  → RankingService (TF-IDF + Cosine Similarity)
  → SpellingCorrectionService (if terms not found)
  → SearchResponse (ranked results + suggestions)
```

---

## 2. Preprocessing Pipeline

### English Pipeline (EnglishProcessor.cs)

| Step | Description | Example |
|------|-------------|---------|
| 1. Normalization | Lowercase + remove punctuation | "Machine Learning!" → "machine learning" |
| 2. Tokenization | Regex split on whitespace | "machine learning" → ["machine", "learning"] |
| 3. Stop-word Removal | Remove common words (the, is, in...) | ["machine", "learning"] → ["machine", "learning"] |
| 4. Porter Stemming | Reduce to root form | ["machine", "learning"] → ["machin", "learn"] |

### Arabic Pipeline (ArabicProcessor.cs)

| Step | Description | Example |
|------|-------------|---------|
| 1. Normalization | أ/إ/آ→ا, ى→ي, ة→ه | "الجامعة" → "الجامعه" |
| 2. Tashkeel Removal | Strip diacritics (U+064B–U+065F) | Remove harakat |
| 3. Tokenization | Regex split, Arabic chars only | Split into Arabic tokens |
| 4. Stop-word Removal | Remove من، في، على، هذا... | Filter stop words |
| 5. Light Stemming | Remove prefixes (ال,و,ب,ك,ل) + suffixes (ات,ون,ين,ه,ة) | "الجامعات" → "جامع" |

---

## 3. Indexing Design

### Positional Inverted Index

**Data Structure:**
```csharp
Dictionary<string, Dictionary<int, List<int>>>
// Term → DocumentId → [Position0, Position1, ...]
```

**Example:**
```json
{
  "learn": {
    "1": [1, 5, 12],
    "2": [3, 8]
  },
  "machin": {
    "1": [0, 4],
    "3": [2]
  }
}
```

**Purpose:**
- Normal queries: find documents containing terms
- Proximity queries: check |pos1 - pos2| ≤ k
- TF calculation: positions.Count = term frequency

### K-Gram Index

**Data Structure:**
```csharp
Dictionary<string, HashSet<string>>
// K-gram → Set of vocabulary terms
```

**Purpose:** Used for Jaccard similarity in spelling correction.
Terms are padded with `$` and split into bigrams.
Example: "learn" → {$l, le, ea, ar, rn, n$}

---

## 4. Query Processing Logic

### Supported Query Types

| Type | Syntax | Example | Logic |
|------|--------|---------|-------|
| Normal | free text | `machine learning` | AND: documents must contain ALL terms |
| Proximity | term1 /k term2 | `machine /3 learning` | Both terms within k positions |

### Normal Query Processing
1. Preprocess query through English/Arabic pipeline
2. For each term, get postings from inverted index
3. Intersect document sets (AND logic)
4. Rank results with TF-IDF + Cosine Similarity
5. Check spelling for terms not in vocabulary

### Proximity Query Processing
1. Parse "term1 /k term2" syntax
2. Preprocess both terms
3. Get positional postings for each term
4. For each document containing both terms:
   - Check if any pair of positions satisfies |p1 - p2| ≤ k
5. Rank matching documents

---

## 5. Ranking Method

### TF-IDF Weighting

**Term Frequency (TF):**
```
TF(t, d) = 1 + log₁₀(count of t in d)    if count > 0
TF(t, d) = 0                               if count = 0
```

**Inverse Document Frequency (IDF):**
```
IDF(t) = log₁₀(N / df(t))
where N = total documents, df = documents containing term
```

**TF-IDF Weight:**
```
W(t, d) = TF(t, d) × IDF(t)
```

### Cosine Similarity

```
similarity(q, d) = (q⃗ · d⃗) / (|q⃗| × |d⃗|)

where:
  q⃗ = TF-IDF vector of query terms
  d⃗ = TF-IDF vector of document terms (for query terms only)
```

Results are sorted from highest to lowest score.

### Spelling Correction

**Levenshtein Distance:** Minimum edit operations (insert, delete, substitute) to transform one string into another.

**Jaccard Similarity:** Using k-gram sets:
```
Jaccard(A, B) = |A ∩ B| / |A ∪ B|
```

**Combined Score:**
```
Score = 0.4 × Jaccard + 0.6 × EditSimilarity
```

---

## 6. Evaluation (Precision & Recall)

### Formulas

```
Precision = Relevant Retrieved / Total Retrieved
Recall    = Relevant Retrieved / Total Relevant
```

### Test Queries and Expected Results

#### Query 1: "machine learning" (Normal, English)
- **Expected relevant docs:** Documents about AI, ML (Docs 1, 2, 3, 6, 10)
- **Precision:** Relevant in top results / Total returned
- **Recall:** Relevant found / Total relevant in collection

#### Query 2: "machine /5 learning" (Proximity, English)
- **Expected:** Docs where "machine" and "learning" appear within 5 positions
- Subset of normal query results (stricter matching)

#### Query 3: "الذكاء الاصطناعي" (Normal, Arabic)
- **Expected relevant docs:** Arabic AI documents (Doc 11, and others mentioning AI)
- Tests Arabic preprocessing pipeline end-to-end

#### Query 4: "cybersecurity network" (Normal, English)
- **Expected:** Cybersecurity document (Doc 3) and related docs
- Tests AND logic with specific domain terms

#### Query 5: "تعليم /3 رقمي" (Proximity, Arabic)
- **Expected:** Arabic education/digital documents
- Tests Arabic proximity search

### Evaluation Process
1. Build the index (POST /api/index/build)
2. Run each query (POST /api/search/query)
3. Manually identify relevant documents in the collection
4. Count relevant documents in the returned results
5. Calculate Precision = relevant_retrieved / total_retrieved
6. Calculate Recall = relevant_retrieved / total_relevant

---

## 7. API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/search/test | Health check |
| POST | /api/search/process | Preprocess text |
| POST | /api/search/query | Execute search (Normal or Proximity) |
| GET | /api/search/suggest?term=X | Spelling suggestions |
| POST | /api/index/build | Build inverted + k-gram index |
| GET | /api/index | Get full index JSON |
| GET | /api/index/term/{term} | Lookup term postings |
| GET | /api/documents | List all documents |

---

## 8. Step-by-Step Implementation Guide

### Step 1: Setup Project
```bash
cd "d:\My Work\Semester 8\Ir\Ir Project\IRSearchEngine"
dotnet restore
dotnet build
```

### Step 2: Create Database
Execute `SeedData.sql` on your SQL Server to create the `Documents` table.

### Step 3: Insert Dataset
The SeedData.sql script inserts 20 documents (10 English, 10 Arabic) with 15,000+ total words.

### Step 4: Configure Connection String
Edit `appsettings.json` with your SQL Server credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server; Database=your_db; User Id=...; Password=...; ..."
  }
}
```

### Step 5: Run the Application
```bash
dotnet run
```
Open browser to the URL shown in the console output.

### Step 6: Build Index
Navigate to the **Index** tab → click **Build Index**.
This builds both the Positional Inverted Index and the K-Gram Index.

### Step 7: Execute Queries
Navigate to the **Search** tab:
- Normal query: `machine learning`
- Proximity query: `machine /5 learning`
- Arabic query: `الذكاء الاصطناعي`

### Step 8: Evaluate Results
For each test query:
1. Note the returned documents and their scores
2. Identify which returned documents are truly relevant
3. Calculate Precision = relevant_retrieved / total_retrieved
4. Calculate Recall = relevant_retrieved / total_relevant_in_collection


Presicion and Recall :

simple example from your project.
Imagine you search for "machine learning".

Before you look at the results, you look at your Database (SeedData.sql) and you see there are exactly 6 documents about AI and Machine Learning in the whole system. (Let's call these the "Good" documents).

1. Precision (Accuracy of the results you see)
Question: "Out of all the results shown on my screen, how many are actually good?"

You search: machine learning
The system returns: 8 results total.
You check them: You see that 5 of them are actually about ML, but 3 are just random.
Calculation: 5 (Good results) ÷ 8 (Total results shown) = 0.625
Result: 62.5% Precision
2. Recall (How much of the database you found)
Question: "Out of all the 'Good' documents in the whole database, how many did the search engine find?"

Total Good documents in Database: 6
How many you found: 5
Calculation: 5 (Good results found) ÷ 6 (Total Good docs in DB) = 0.833
Result: 83.3% Recall
🟢 How to do it for your report:
Pick a query, for example: "الذكاء الاصطناعي" (Arabic AI).
Count how many documents in your SeedData.sql are actually about Arabic AI. (Let's say it is 4).
Run the search in the app.
Count how many results the app gave you. (Let's say it gave you 5 results).
Check if all 5 are relevant. (Maybe only 4 are good).
Precision: 4 ÷ 5 = 80%
Recall: 4 ÷ 4 = 100% (You found all of them!)
Summary:

Precision = Quality of the results on the screen.
Recall = Completeness of the search.