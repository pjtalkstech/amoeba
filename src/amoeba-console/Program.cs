// See https://aka.ms/new-console-template for more information
using System.Xml;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

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
        var articles = await articleFetcher.FetchArticlesAsync("https://feeds.feedburner.com/ndtvnews-trending-news", 5);
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

        // Summarize and extract entities
        var summary = await summarizer.SummarizeAsync(selected.Summary.Text);

      
        Console.WriteLine($"\nSummary:\n{summary.Summary}\n");

        // Search news
        var results = await newsSearcher.SearchNewsAsync(summary.Summary);
        
        
        // For each article in results, check if related and summarize
        if (results != null)
        {
            // Fire all fetches in parallel
            var fetchTasks = results.Select(article => articleFetcher.FetchArticleContentAsync(article.Url)).ToList();
            var contents = await Task.WhenAll(fetchTasks);

            var relatedArticles = new List<(string Title, string Content)>();
            var checkTasks = new List<Task>();
            for (int i = 0; i < results.Count(); i++)
            {
                var article = results.ElementAt(i);
                var content = contents[i];
                if (string.IsNullOrWhiteSpace(content)) continue;
                // Optionally, parallelize this too:
                checkTasks.Add(Task.Run(async () =>
                {
                    var isRelated = await summarizer.IsRelatedAsync(summary.Summary, content);
                    if (isRelated)
                    {
                        lock (relatedArticles)
                        {
                            relatedArticles.Add((article.Title, content));
                        }
                    }
                }));
            }
            await Task.WhenAll(checkTasks);
            var finalSummary = await summarizer.SummarizeRelatedArticlesAsync(relatedArticles);
            Console.WriteLine("\nSummaries of related articles:\n" + finalSummary);
        }
    }
}