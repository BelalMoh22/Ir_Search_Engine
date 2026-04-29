# IR Search Engine — README

## 📌 What Is This Project?

This is a **Bilingual Search Engine** built for an Information Retrieval (IR) course project. It processes and searches through documents written in **English** and **Arabic**. Think of it as a mini Google that:

1. Takes raw documents from a database
2. Cleans and processes the text (removes stop words, stems words to their roots)
3. Builds an index so it can quickly find which documents contain which words
4. Lets you search using different query types and ranks the results by relevance
5. Suggests corrections if you misspell a word

---

## 🚀 How to Run the Project

### Prerequisites
- **.NET 8 SDK** installed on your machine
- **SQL Server** database with the seed data loaded

### Step 1: Load the Database
Open SQL Server Management Studio (or any SQL client) and run the file:
```
SeedData.sql
```
This creates the `Documents` table and inserts 20 documents (10 English + 10 Arabic).

### Step 2: Check the Connection String
Open `IRSearchEngine/appsettings.json` and make sure the `DefaultConnection` points to your database:
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=your_server; Database=your_db; ..."
}
```

### Step 3: Build and Run
Open a terminal in the project folder and run:
```bash
cd IRSearchEngine
dotnet run
```
The console will show a URL like `http://localhost:5000` — open it in your browser.

---

## 🧪 How to Test It

### Step 1: Build the Index
- Go to the **Index** tab in the browser
- Click **"Build Index"**
- You should see a success message like: "Indexed 20 docs, 5000+ terms, 8000+ k-grams"

### Step 2: Try a Normal Search
- Go to the **Search** tab
- Type: `machine learning`
- Click **Search**
- You'll see a list of documents ranked by relevance score (highest first)

### Step 3: Try a Proximity Search
- Type: `machine /3 learning`
- This finds documents where "machine" and "learning" appear within 3 words of each other

### Step 4: Try an Arabic Search
- Type: `الذكاء الاصطناعي`
- Select **Arabic** from the language dropdown (or leave on Auto-detect)
- You'll see Arabic documents about AI ranked by relevance

### Step 5: Try Spelling Correction
- Type a misspelled word in the **Spelling Correction** section (e.g., "lerning")
- Click **Suggest**
- It will show: "Did you mean: learn?"

---

## 📁 Project Files — What Does Each File Do?

### 🗄️ Database
| File | Purpose |
|------|---------|
| `SeedData.sql` | Creates the Documents table and inserts 20 documents (10 English, 10 Arabic) into the database |

### 📦 Models (Data Structures)
| File | Purpose |
|------|---------|
| `Models/Document.cs` | Represents a document from the database (Id, Content, Language) |
| `Models/ProcessRequest.cs` | The request body when you send text to be preprocessed (text + language) |
| `Models/ProcessResponse.cs` | The response containing the processed tokens |
| `Models/SearchRequest.cs` | The request body when you submit a search query |
| `Models/SearchResult.cs` | A single search result (document ID, score, snippet, language) and the full search response |
| `Models/IndexResponse.cs` | The response when looking up a term in the index |

### ⚙️ Services (Business Logic)
| File | What It Does |
|------|-------------|
| `Services/EnglishProcessor.cs` | **English text pipeline:** converts to lowercase → removes punctuation → splits into words → removes stop words (the, is, in...) → applies Porter Stemmer (running→run, learning→learn) |
| `Services/ArabicProcessor.cs` | **Arabic text pipeline:** normalizes letters (أ→ا, ى→ي, ة→ه) → removes diacritics (tashkeel) → splits into words → removes Arabic stop words (من، في، على...) → applies light stemming (removes prefixes like ال and suffixes like ات) |
| `Services/TextProcessorService.cs` | **Router:** receives text + language, sends it to either EnglishProcessor or ArabicProcessor |
| `Services/InvertedIndexService.cs` | **Positional Inverted Index:** processes all documents and builds a dictionary: for each word, it stores which documents contain it and at which positions. Example: "learn" → Doc 1 at positions [1, 5], Doc 2 at position [3] |
| `Services/KGramIndexService.cs` | **K-Gram Index:** breaks each word into 2-character chunks (bigrams) for spelling correction. Example: "learn" → {$l, le, ea, ar, rn, n$}. Used to find similar words via Jaccard similarity |
| `Services/RankingService.cs` | **TF-IDF + Cosine Similarity:** calculates how important each word is in each document, then scores how similar a search query is to each document. Higher score = more relevant |
| `Services/SpellingCorrectionService.cs` | **Spelling correction:** when a word isn't found, it uses Levenshtein Distance (counting letter edits) and Jaccard Similarity (comparing bigram overlap) to suggest the closest matching word |
| `Services/QueryProcessorService.cs` | **Query handler:** detects query type (Normal or Proximity), preprocesses the query, searches the index, ranks results, and checks spelling. This is the brain of the search engine |

### 🌐 Controllers (API Endpoints)
| File | Endpoints |
|------|-----------|
| `Controllers/SearchController.cs` | `POST /api/search/process` — preprocess text; `POST /api/search/query` — execute a search; `GET /api/search/suggest?term=X` — get spelling suggestions; `GET /api/search/test` — health check |
| `Controllers/IndexController.cs` | `POST /api/index/build` — build the index from database; `GET /api/index` — view full index as JSON; `GET /api/index/term/{term}` — look up a specific term |
| `Controllers/DocumentsController.cs` | `GET /api/documents` — list all documents from the database |

### 🖥️ Frontend
| File | Purpose |
|------|---------|
| `wwwroot/index.html` | The web page you see in the browser. Has 3 tabs: **Search** (run queries, see ranked results, spelling suggestions), **Preprocessing** (test the text processing pipeline), **Index** (build index, look up terms) |

### ⚙️ Configuration
| File | Purpose |
|------|---------|
| `Program.cs` | Registers all services with dependency injection and configures the web server |
| `appsettings.json` | Stores the database connection string |

---

## 🔍 Query Types Explained

### Normal Query
**Example:** `machine learning`
- The system finds documents that contain **both** "machine" AND "learning" (after stemming)
- Results are ranked by TF-IDF cosine similarity score

### Proximity Query
**Example:** `machine /3 learning`
- The system finds documents where "machine" and "learning" appear **within 3 words of each other**
- The `/3` means the maximum allowed distance between the two words
- This is stricter than a normal search — the words must be close together in the text

---

## 📊 How Ranking Works

1. **TF (Term Frequency):** How many times a word appears in a document. More occurrences = higher weight.
   - Formula: `TF = 1 + log₁₀(count)`

2. **IDF (Inverse Document Frequency):** How rare a word is across all documents. Rare words are more important.
   - Formula: `IDF = log₁₀(total_docs / docs_with_term)`

3. **TF-IDF Weight:** `TF × IDF` — combines both factors

4. **Cosine Similarity:** Measures the angle between the query vector and each document vector. Score ranges from 0 (not relevant) to 1 (perfect match).

---

## ✏️ How Spelling Correction Works

When you search for a word that doesn't exist in the index:

1. **Levenshtein Distance:** Counts the minimum number of letter changes (insert, delete, replace) to turn your word into a known word. Example: "lerning" → "learn" (2 edits)

2. **Jaccard Similarity:** Compares the bigram sets of your word with vocabulary words. More shared bigrams = more similar.

3. **Combined Score:** `0.4 × Jaccard + 0.6 × EditSimilarity` — the best match is suggested as "Did you mean...?"

---

## 📏 Evaluation (Precision & Recall)

After running a search:

- **Precision** = How many of the returned results are actually relevant?
  - `Precision = relevant_retrieved / total_retrieved`

- **Recall** = How many of the relevant documents in the database were found?
  - `Recall = relevant_retrieved / total_relevant`

**Example:** If you search "machine learning" and get 8 results, but only 5 are truly about ML, and there are 6 ML documents total:
- Precision = 5/8 = 62.5%
- Recall = 5/6 = 83.3%
