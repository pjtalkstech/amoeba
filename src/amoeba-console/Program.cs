// See https://aka.ms/new-console-template for more information
using System.Xml;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(
            modelId: "llama3",
            endpoint: new Uri("http://localhost:11434")
        );
        var kernel = builder.Build();

        IArticleFetcher articleFetcher = new ArticleFetcher();
        IArticleSummarizer summarizer = new ArticleSummarizer(kernel);
        INewsSearcher newsSearcher = new NewsSearcher("http://localhost:8081");

        // Fetch articles
        var articles = await articleFetcher.FetchArticlesAsync("https://feeds.feedburner.com/ndtvnews-trending-news", 10);
        for (int i = 0; i < articles.Count; i++)
            Console.WriteLine($"{i}: {articles[i].Title.Text}");

        // Select article
        Console.Write("Pick an article (0-4): ");
        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= articles.Count)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }
        var selected = articles[idx];
        Console.WriteLine($"\nYou selected: {selected.Title.Text}\n");
        // Fetch article content
        Stopwatch stopwatch = Stopwatch.StartNew();
        // Summarize and extract entities
        var summary = await summarizer.SummarizeAsync(selected.Summary.Text);


        Console.WriteLine($"\nSummary:\n{summary.Summary}\n");

        // Search news
        var results = await newsSearcher.SearchNewsAsync(summary.Summary, 10);
        var keywordSearch = string.Join(" ", summary.Keywords);
        var keywordResult = await newsSearcher.SearchNewsAsync(keywordSearch, 5);
        results = results.Concat(keywordResult).Distinct().ToList();

        // For each article in results, check if related and summarize
        if (results != null)
        {
            // Start all fetches in parallel, keep track of their tasks and associated articles
            var fetchTasks = results
                .Select(article => (Task: articleFetcher.FetchArticleContentAsync(article.Url), Article: article))
                .ToList();

            var relatedArticles = new List<(string Title, string Content)>();
            while (fetchTasks.Count > 0)
            {
                // Wait for any fetch to complete
                var completed = await Task.WhenAny(fetchTasks.Select(ft => ft.Task));
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
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("Press any key to exit...");
    }
}