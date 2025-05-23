# amoeba

# Amoeba Console

Amoeba Console is a C# .NET console application that summarizes news articles and finds related news using LLMs and web search. It demonstrates modern async orchestration, parallel web requests, and LLM-based summarization.

## Features
- Fetches trending news articles from an RSS feed
- Lets the user select an article to summarize
- Uses an LLM (Ollama/llama3 via Semantic Kernel) to extract a summary and keywords
- Searches for related news using Searx
- Fetches and processes related articles in parallel, processing each as soon as it is available
- Summarizes and combines related articles into a coherent timeline

## Architecture
- **Program.cs**: Orchestrates the workflow
- **ArticleFetcher**: Fetches RSS articles and extracts main content from URLs
- **ArticleSummarizer**: Summarizes articles and checks relatedness using LLM
- **NewsSearcher**: Searches news using Searx API
- **Interfaces**: For dependency inversion and testability

## Business Process Flow
```mermaid
flowchart LR
    A[1.Display News Items] --> B[2.User Selects News]
    B --> C[3.Summarize & Generate Keywords]
    C --> D[4.Search Web for Related News]
    D --> E[5.Check Relevance to Selected News]
    E --> F[6.Summarize Relevant Articles]
    F --> G[7.Present Full Summary with Background & Timeline]
```

## Sequence Diagram
```mermaid
sequenceDiagram
    participant User
    participant Program
    participant ArticleFetcher
    participant ArticleSummarizer
    participant NewsSearcher
    User->>Program: Start
    Program->>ArticleFetcher: FetchArticlesAsync
    Program->>User: Show articles, prompt selection
    User->>Program: Select article
    Program->>ArticleSummarizer: SummarizeAsync
    ArticleSummarizer-->>Program: Summary, Keywords
    Program->>NewsSearcher: SearchNewsAsync (summary/keywords)
    NewsSearcher-->>Program: News results
    loop For each news article (async)
        Program->>ArticleFetcher: FetchArticleContentAsync
        ArticleFetcher-->>Program: Content
        Program->>ArticleSummarizer: IsRelatedAsync
        ArticleSummarizer-->>Program: Related?
    end
    Program->>ArticleSummarizer: SummarizeRelatedArticlesAsync
    ArticleSummarizer-->>Program: Final summary
    Program->>User: Show summary
```

## Requirements
- .NET 9.0+
- [Ollama](https://ollama.com/) running locally (for LLM)
- [Searx](https://searx.github.io/searx/) instance for news search

## How to Run
1. Clone the repo
2. Start Ollama and Searx locally
3. Build and run the project:
   ```sh
   dotnet run --project amoeba-console/amoeba-console.csproj
   ```
4. Follow the prompts in the console

## Customization
- Change the RSS feed URL in `Program.cs` to target different news sources
- Adjust the LLM prompt in `ArticleSummarizer` for different summarization styles
- Tune parallelism in `ArticleFetcher` as needed
- Add the model of your choice
- Add the URLs for Ollama & Searx

## License
MIT
