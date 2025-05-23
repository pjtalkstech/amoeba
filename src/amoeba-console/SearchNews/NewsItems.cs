using System.Text.Json;

public class SearxNewsSearchResult
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Snippet { get; set; }
    public string Category { get; set; }
}

public class SearxNewsSearch
{
    private readonly string _searxUrl;

    public SearxNewsSearch(string searxUrl)
    {
        _searxUrl = searxUrl.TrimEnd('/');
    }

    public async Task<List<SearxNewsSearchResult>> SearchNewsAsync(string query, int count = 10)
    {
        using var http = new HttpClient();
        // You can change the instance if this one is slow or down
        var apiUrl = $"{_searxUrl}/search?q={Uri.EscapeDataString(query)}&categories=general,news&format=json&language=en";
        var resp = await http.GetAsync(apiUrl);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        var results = new List<SearxNewsSearchResult>();
        if (doc.RootElement.TryGetProperty("results", out var newsArray))
        {
            foreach (var item in newsArray.EnumerateArray())
            {
                if (results.Count >= count) break;
                results.Add(new SearxNewsSearchResult
                {
                    Title = item.GetProperty("title").GetString() ?? string.Empty,
                    Url = item.GetProperty("url").GetString() ?? string.Empty,
                    Snippet = item.TryGetProperty("content", out var content) ? content.GetString() : "",
                    Category = item.TryGetProperty("category", out var category) ? category.GetString() : ""
                });
            }
        }

        return results;
    }
}