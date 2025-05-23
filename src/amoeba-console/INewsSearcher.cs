public interface INewsSearcher
{
    Task<IEnumerable<SearxNewsSearchResult>> SearchNewsAsync(string query);
}
