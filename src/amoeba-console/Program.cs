// See https://aka.ms/new-console-template for more information
using System.Xml;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Diagnostics;

class Program
{
    private const string RssFeedUrl = "https://feeds.feedburner.com/ndtvnews-trending-news";
    private const string OllamaEndpoint = "http://localhost:11434";
    private const string SearxEndpoint = "http://localhost:8081";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var kernel = BuildKernel();
        IArticleFetcher articleFetcher = new ArticleFetcher();
        IArticleSummarizer summarizer = new ArticleSummarizer(kernel);
        INewsSearcher newsSearcher = new NewsSearcher(SearxEndpoint);

        var articles = await FetchAndDisplayArticles(articleFetcher);
        var selected = PromptUserToSelectArticle(articles);
        if (selected == null) return;

        var stopwatch = Stopwatch.StartNew();
        var summary = await SummarizeAndDisplay(summarizer, selected);
        var results = await SearchRelatedNews(newsSearcher, summary);
        await ProcessAndSummarizeRelatedArticles(articleFetcher, summarizer, summary, results);
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("Press any key to exit...");
    }

    private static Kernel BuildKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(
            modelId: "llama3",
            endpoint: new Uri(OllamaEndpoint)
        );
        return builder.Build();
    }

    private static async Task<List<SyndicationItem>> FetchAndDisplayArticles(IArticleFetcher fetcher)
    {
        var articles = await fetcher.FetchArticlesAsync(RssFeedUrl, 10);
        for (int i = 0; i < articles.Count; i++)
            Console.WriteLine($"{i}: {articles[i].Title.Text}");
        return articles;
    }

    private static SyndicationItem? PromptUserToSelectArticle(List<SyndicationItem> articles)
    {
        Console.Write($"Pick an article (0-{articles.Count}): ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= articles.Count)
        {
            Console.WriteLine("Invalid selection.");
            return null;
        }
        var selected = articles[idx];
        Console.WriteLine($"\nYou selected: {selected.Title.Text}\n");
        return selected;
    }

    private static async Task<ArticleSummary?> SummarizeAndDisplay(IArticleSummarizer summarizer, SyndicationItem selected)
    {
        var summary = await summarizer.SummarizeAsync(selected.Summary.Text);
        Console.WriteLine($"\nSummary:\n{summary.Summary}\n");
        return summary;
    }

    private static async Task<List<SearxNewsSearchResult>> SearchRelatedNews(INewsSearcher searcher, ArticleSummary summary)
    {
        IEnumerable<SearxNewsSearchResult> results = await searcher.SearchNewsAsync(summary.Summary, 10);
        string keywordSearch = string.Join(" ", summary.Keywords);
        IEnumerable<SearxNewsSearchResult> keywordResult = await searcher.SearchNewsAsync(keywordSearch, 5);
        return results.Concat(keywordResult).Distinct().ToList();
    }

    private static async Task ProcessAndSummarizeRelatedArticles(
        IArticleFetcher fetcher,
        IArticleSummarizer summarizer,
        ArticleSummary summary,
        List<SearxNewsSearchResult> results)
    {
        if (results == null || results.Count == 0) return;

        // Start all fetches in parallel, keep track of their tasks and associated articles
        var fetchTasks = results
            .Select(article => (Task: fetcher.FetchArticleContentAsync(article.Url), Article: article))
            .ToList();

        var relatedArticles = new List<(string Title, string Content)>();
        while (fetchTasks.Count > 0)
        {
            // Wait for any fetch to complete
            var completed = await Task.WhenAny(fetchTasks.Select(ft => (Task)ft.Task));
            var finishedTuple = fetchTasks.First(ft => ft.Task == completed);
            fetchTasks.Remove(finishedTuple);

            var content = await finishedTuple.Task;
            var article = finishedTuple.Article;
            if (string.IsNullOrWhiteSpace(content)) continue;

            var isRelated = await summarizer.IsRelatedAsync(summary.Summary, content);
            if (isRelated)
            {
                relatedArticles.Add((article.Title, content));
            }
        }
        var finalSummary = await summarizer.SummarizeRelatedArticlesAsync(relatedArticles);
        Console.WriteLine("\nSummaries of related articles:\n" + finalSummary);
    }
}