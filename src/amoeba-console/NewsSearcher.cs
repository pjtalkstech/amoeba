using System.Collections.Generic;
using System.Threading.Tasks;

public class NewsSearcher : INewsSearcher
{
    private readonly SearxNewsSearch _search;
    public NewsSearcher(string apiUrl)
    {
        _search = new SearxNewsSearch(apiUrl);
    }
    public async Task<IEnumerable<SearxNewsSearchResult>> SearchNewsAsync(string query, int c = 10)
    {
        return await _search.SearchNewsAsync(query, c);
    }
}
