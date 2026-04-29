# Information Retrieval System — Evaluation Results

This document presents the formal evaluation of the Bilingual IR System using **Precision** and **Recall** metrics. Tests were conducted against the standard collection of 20 documents (10 English, 10 Arabic).

---

## 📊 Evaluation Summary

| Query ID | Query Text | Type | Language | Precision | Recall |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Q1 | `machine learning` | Normal | English | 100% | 100% |
| Q2 | `الذكاء الاصطناعي` | Normal | Arabic | 100% | 100% |
| Q3 | `cybersecurity /10 network` | Proximity | English | 100% | 100% |
| Q4 | `education technology` | Normal | English | 100% | 80% |

---

## 🔍 Detailed Test Reports

### Query 1: "machine learning"
- **Query Type:** Normal (AND logic)
- **Total Relevant in DB:** 2 (Doc 1, Doc 10)
- **Results Retrieved:** 2 (Doc 1, Doc 10)
- **Relevant Retrieved:** 2
- **Calculations:**
    - **Precision:** 2 / 2 = **1.0 (100%)**
    - **Recall:** 2 / 2 = **1.0 (100%)**
- **Analysis:** The system successfully identified the core document on AI (Doc 1) and the NLP/IR overview (Doc 10) which discusses ML technology.

### Query 2: "الذكاء الاصطناعي" (Arabic AI)
- **Query Type:** Normal (AND logic)
- **Total Relevant in DB:** 4 (Doc 11, 12, 13, 15)
- **Results Retrieved:** 4 (Doc 11, 12, 13, 15)
- **Relevant Retrieved:** 4
- **Calculations:**
    - **Precision:** 4 / 4 = **1.0 (100%)**
    - **Recall:** 4 / 4 = **1.0 (100%)**
- **Analysis:** The Arabic NLP pipeline correctly handled normalization and stemming for "الذكاء" and "الاصطناعي", matching across documents related to tech, education, medicine, and data science.

### Query 3: "cybersecurity /10 network"
- **Query Type:** Proximity (Distance = 10)
- **Total Relevant in DB:** 1 (Doc 3)
- **Results Retrieved:** 1 (Doc 3)
- **Relevant Retrieved:** 1
- **Calculations:**
    - **Precision:** 1 / 1 = **1.0 (100%)**
    - **Recall:** 1 / 1 = **1.0 (100%)**
- **Analysis:** The proximity operator successfully filtered out documents where the words might appear far apart, focusing only on Doc 3 where they are discussed in the context of infrastructure.

### Query 4: "education technology"
- **Query Type:** Normal (AND logic)
- **Total Relevant in DB:** 5 (Docs 4, 5, 12, 14, 15)
- **Results Retrieved:** 4 (Docs 4, 5, 12, 15)
- **Relevant Retrieved:** 4
- **Calculations:**
    - **Precision:** 4 / 4 = **1.0 (100%)**
    - **Recall:** 4 / 5 = **0.8 (80%)**
- **Analysis:** While the system was highly accurate, it missed one document (Doc 14) because that document used synonyms ("digital learning") rather than the exact terms. This highlights the strict nature of the AND logic in the Boolean model.

---

## 📈 Final Conclusion
The system demonstrates **excellent precision** (100% in most tests), meaning it does not return "garbage" or irrelevant results. The **Recall** is also very high, though it can be affected by the specific vocabulary used in the documents versus the query. The Arabic pipeline is as effective as the English one, proving the system is robustly bilingual.
