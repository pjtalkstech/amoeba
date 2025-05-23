using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

public class ArticleSummarizer : IArticleSummarizer
{
    private readonly Kernel _kernel;
    public ArticleSummarizer(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> SummarizeAsync(string content)
    {
        var summarizeFunc = _kernel.CreateFunctionFromPrompt("Summarize the article in 20 words and extract all names of entities, people, countries, organizations, and event dates from this news article as search keywords. Put the Summary under <Summary> tag entity and <Entity> tag:\n{{$input}}");
        var result = await _kernel.InvokeAsync(summarizeFunc, new() { ["input"] = content });
        return result.ToString();
    }

    public async Task<string> ExtractEntitiesAsync(string content)
    {
        // For now, reuse SummarizeAsync output
        return await SummarizeAsync(content);
    }

    public async Task<bool> IsRelatedAsync(string summary, string articleContent)
    {
        var isRelatedPrompt = $"Is the following article content related to this summary?\nSummary: {summary}\nArticle: {articleContent}\nRespond with only 'yes' or 'no'.";
        var isRelatedFunc = _kernel.CreateFunctionFromPrompt(isRelatedPrompt);
        var isRelatedResult = await _kernel.InvokeAsync(isRelatedFunc);
        return isRelatedResult.ToString().Trim().ToLower().StartsWith("y");
    }

    public async Task<string> SummarizeRelatedArticlesAsync(IEnumerable<(string Title, string Content)> articles)
    {
        var relatedSummaries = new List<string>();
        foreach (var article in articles)
        {
            var summarizeFunc = _kernel.CreateFunctionFromPrompt("Summarize the following article in 100 words:\n{{$input}}");
            var summaryResult = await _kernel.InvokeAsync(summarizeFunc, new() { ["input"] = article.Content });
            relatedSummaries.Add($"- {article.Title}: {summaryResult.ToString().Trim()}");
        }
        var joinedSummaries = string.Join("\n", relatedSummaries);
        var finalSummary = _kernel.CreateFunctionFromPrompt("Can you create coherent summary based on these articles and create a possible time line:\n{{$joinedSummaries}}");
        var finalSummaryResult = await _kernel.InvokeAsync(finalSummary, new() { ["joinedSummaries"] = joinedSummaries });
        return finalSummaryResult.ToString().Trim();
    }
}
