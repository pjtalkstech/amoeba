public interface IArticleSummarizer
{
    Task<ArticleSummary> SummarizeAsync(string content);
    Task<ArticleSummary> ExtractEntitiesAsync(string content);
    Task<bool> IsRelatedAsync(string summary, string articleContent);
    Task<string> SummarizeRelatedArticlesAsync(IEnumerable<(string Title, string Content)> articles);
}
