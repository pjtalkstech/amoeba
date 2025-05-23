using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

public class ArticleSummary
{
    public string Summary { get; set; }
    public List<string> Keywords { get; set; }
    public List<string> Urls { get; set; }
}
public class ArticleSummarizer : IArticleSummarizer
{
    private readonly Kernel _kernel;
    public ArticleSummarizer(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<ArticleSummary> SummarizeAsync(string content)
    {
        Console.WriteLine("Summarizing selected news item");
        var summarizeFunc = _kernel.CreateFunctionFromPrompt(@" You are news agent and history expert.
   Respond ONLY in valid JSON with this format (no prose, no comments):
    {
       // This is the summary of the article in 30 words
      ""summary"": ""..."",
        // This is the list of keywords like people, entities, organizations etc extracted from the article maximum of 10
      ""keywords"": [""..."", ""..."", ""...""]
    }
    Article: {{$input}}");
        var result = await _kernel.InvokeAsync(summarizeFunc, new() { ["input"] = content });
        var doc = System.Text.Json.JsonDocument.Parse(result.ToString());
        return new ArticleSummary
        {
            Summary = doc.RootElement.GetProperty("summary").GetString() ?? string.Empty,
            Keywords = doc.RootElement.GetProperty("keywords").EnumerateArray().Select(k => k.GetString() ?? string.Empty).ToList(),
            Urls = new List<string>() // Placeholder for URLs, if needed
        };  
    }

    public async Task<ArticleSummary> ExtractEntitiesAsync(string content)
    {
        // For now, reuse SummarizeAsync output
        return await SummarizeAsync(content);
    }

    public async Task<bool> IsRelatedAsync(string summary, string articleContent)
    {
       try
       {
         Console.WriteLine("IsRelatedAsync called");
         var isRelatedPrompt = $"As you are news agent and hisotry expert, Is the following article content related to this summary?\nSummary: {summary}\nArticle: {articleContent}\nRespond with only 'yes' or 'no'.";
         var isRelatedFunc = _kernel.CreateFunctionFromPrompt(isRelatedPrompt);
         var isRelatedResult = await _kernel.InvokeAsync(isRelatedFunc);
         return isRelatedResult.ToString().Trim().ToLower().StartsWith("y");
       }
       catch (System.Exception ex)
       {
        Console.WriteLine("Error in IsRelatedAsync: " + ex.Message + articleContent);
        return false; // Handle exceptions gracefully
       }
    }

    public async Task<string> SummarizeRelatedArticlesAsync(IEnumerable<(string Title, string Content)> articles)
    {
        var relatedSummaries = new List<string>();
        foreach (var article in articles)
        {
            Console.WriteLine($"Summarizing related article: {article.Title}");
            var summarizeFunc = _kernel.CreateFunctionFromPrompt("Summarize the following article in 100 words:\n{{$input}}");
            var summaryResult = await _kernel.InvokeAsync(summarizeFunc, new() { ["input"] = article.Content });
            relatedSummaries.Add($"- {article.Title}: {summaryResult.ToString().Trim()}");
        }
        var joinedSummaries = string.Join("\n", relatedSummaries);
        Console.WriteLine($"Summarizing related articles");
        var finalSummary = _kernel.CreateFunctionFromPrompt("Can you create coherent summary of 300 words based on these articles. Give a bit of background as 1 Paragraph and summaried content as another. Also Create a time line where possible:\n{{$joinedSummaries}}");
        var finalSummaryResult = await _kernel.InvokeAsync(finalSummary, new() { ["joinedSummaries"] = joinedSummaries });
        return finalSummaryResult.ToString().Trim();
    }
}
