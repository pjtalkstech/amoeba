public interface INewsSearcher
{
    Task<IEnumerable<SearxNewsSearchResult>> SearchNewsAsync(string query, int count = 10);
}
