public interface IArticleSummarizer
{
    Task<string> SummarizeAsync(string content);
    Task<string> ExtractEntitiesAsync(string content);
    Task<bool> IsRelatedAsync(string summary, string articleContent);
    Task<string> SummarizeRelatedArticlesAsync(IEnumerable<(string Title, string Content)> articles);
}
