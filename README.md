# C# MCP Server - Date, Weather & RAG Document Search

This project implements a fully functional **MCP (Model Context Protocol) server** in C#, exposing three tools:

1. **weekday-date** — returns weekday + ordinal occurrence  
2. **weather-forecast** — mock weather lookup for a given city and date  
3. **search_documents** — a RAG-style free‑text search tool over a remote knowledge base  

The server is compatible with any MCP‑enabled AI client such as Claude Code, Cursor, or GitHub Copilot in VS Code.

---

## 🚀 Features

### ✔ MCP-compliant JSON-RPC server  
The server exposes a single endpoint:

#### POST /mcp

It handles:

- `initialize`
- `tools/list`
- `tools/call`

### ✔ Tool 1: weekday-date  
Returns weekday name and ordinal occurrence for a given ISO date.

Example output:

Monday, 2nd


### ✔ Tool 2: weather-forecast  
Mocked weather response for verification purposes.

Example output:

Weather in Paris on 2025-07-12: Sunny, max 21°C


### ✔ Tool 3: search_documents (RAG)  
A Retrieval-Augmented Generation tool that:

- Fetches a knowledge base from  
  `https://tribetrot.ngrok.app/api/knowledge-base`
- Caches documents in memory
- Performs keyword scoring
- Returns a JSON array (as text) with:
  - `id`
  - `title`
  - `content`
  - `score` (relevance)

Example output:

```json
[
  {
    "id": "sev-001",
    "title": "Severity Levels & Response SLAs",
    "content": "...",
    "score": 2
  }
]
```
## 📦 Project Structure
Program.cs        # MCP server with all tools
skills.md         # (optional) CAIN rule engine instructions
README.md         # Project documentation


## 🧠 How RAG Works in This Server
The search_documents tool implements a minimal RAG pipeline:
1. Retrieve  
Free‑text query → keyword match against all documents.
2. Augment  
The AI assistant receives the returned JSON array as context.
3. Generate  
The assistant uses the retrieved content as the source of truth.

This allows the AI to answer questions about the knowledge base without embedding the entire dataset into the prompt.

## 📘 Skills File (Optional)
If your AI assistant supports Skills files, you can add a skills.md to define:

- How to parse complaint logs
- When to call weekday-date
- When to call weather-forecast
- When to SKIP or HANDLE entries
- Saturn bonus rule
- Deterministic logic for CAIN

## ▶ Running the Server
dotnet run






