// See https://aka.ms/new-console-template for more information
using System.Xml;
using System.ServiceModel.Syndication;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Net.Http;
using HtmlAgilityPack;

Console.WriteLine("Hello, World!");

var builder = Kernel.CreateBuilder();
builder.AddOllamaChatCompletion(
    modelId: "llama3",
    endpoint: new Uri("http://localhost:11434")
);
// Build the kernel
var kernel = builder.Build();

//Get Articles
using var reader = XmlReader.Create("https://feeds.feedburner.com/ndtvnews-trending-news");
var feed = SyndicationFeed.Load(reader);
var articles = feed.Items.Take(5).ToList();


for (int i = 0; i < articles.Count; i++)
    Console.WriteLine($"{i}: {articles[i].Title.Text}");

//Selected article
Console.Write("Pick an article (0-4): ");
if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 0 || idx >= articles.Count)
{
    Console.WriteLine("Invalid selection.");
    return;
}

var selected = articles[idx];
Console.WriteLine($"\nYou selected: {selected.Title.Text}\n");

// Summarize the article
//var extractEntities = kernel.CreateFunctionFromPrompt("Summarize the article in 20 words and extract all names of entities, people, countries, organizations, and event dates from this news article as search keywords:\n{{$input}}");
var extractEntities = kernel.CreateFunctionFromPrompt("Summarize the article in 20 words and extract all names of entities, people, countries, organizations, and event dates from this news article as search keywords:\n{{$input}}");
var entityResult = await kernel.InvokeAsync(extractEntities, new() { ["input"] = selected.Summary.Text });
var outPut = entityResult.ToString();

// Extract summary from output string
string ExtractSummary(string output)
{
    // Try to find a line starting with "Summary:" (case-insensitive)
    var lines = output.Split('\n');
    foreach (var line in lines)
    {
        if (line.Trim().StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
        {
            return line.Substring("Summary:".Length).Trim();
        }
    }
    // If not found, assume the first non-empty line is the summary
    foreach (var line in lines)
    {
        if (!string.IsNullOrWhiteSpace(line))
            return line.Trim();
    }
    return string.Empty;
}

var summary = ExtractSummary(outPut);
Console.WriteLine($"\nSummary:\n{summary}\n");

// Call Searx API
SearxNewsSearch search = new("http://localhost:8081");
var results = await search.SearchNewsAsync(summary);


Console.WriteLine($"\nEntities found:\n{entityResult}\n");

// Helper to fetch and extract main text content from a URL
async Task<string> FetchArticleContentAsync(string url)
{
    try
    {
        using var http = new HttpClient();
        var html = await http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        // Try to extract main content heuristically
        var paragraphs = doc.DocumentNode.SelectNodes("//p");
        if (paragraphs != null)
        {
            return string.Join("\n", paragraphs.Select(p => p.InnerText.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
        }
        return doc.DocumentNode.InnerText;
    }
    catch
    {
        return string.Empty;
    }
}

// For each article in results, check if related to summary and summarize content
if (results != null)
{
    var relatedSummaries = new List<string>();
    foreach (var article in results)
    {
        // Fetch content from article URL
        var content = await FetchArticleContentAsync(article.Url);
        if (string.IsNullOrWhiteSpace(content)) continue;

        // Use LLM to check if article is related to summary
        var isRelatedPrompt = $"Is the following article content related to this summary?\nSummary: {summary}\nArticle: {content}\nRespond with only 'yes' or 'no'.";
        var isRelatedFunc = kernel.CreateFunctionFromPrompt(isRelatedPrompt);
        var isRelatedResult = await kernel.InvokeAsync(isRelatedFunc);
        var isRelated = isRelatedResult.ToString().Trim().ToLower().StartsWith("y");
        if (!isRelated) continue;

        // Summarize the article content
        var summarizeFunc = kernel.CreateFunctionFromPrompt("Summarize the following article in 100 words:\n{{$input}}");
        var summaryResult = await kernel.InvokeAsync(summarizeFunc, new() { ["input"] = content });
        relatedSummaries.Add($"- {article.Title}: {summaryResult.ToString().Trim()}");
    }
    var joinedSummaries = string.Join("\n", relatedSummaries);
    var finalSummary = kernel.CreateFunctionFromPrompt("Can you create coherent summary based on these articles and create a possible time line:\n{{$joinedSummaries}}");
    var finalSummaryResult = await kernel.InvokeAsync(finalSummary, new() { ["joinedSummaries"] = joinedSummaries });

    Console.WriteLine("\nSummaries of related articles:\n" + string.Join("\n", finalSummaryResult.ToString().Trim()));
}