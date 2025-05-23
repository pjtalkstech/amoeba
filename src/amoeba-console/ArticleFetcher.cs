using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using System.Net.Http;

public class ArticleFetcher : IArticleFetcher
{
    public async Task<List<SyndicationItem>> FetchArticlesAsync(string feedUrl, int count)
    {
        return await Task.Run(() =>
        {
            using var reader = XmlReader.Create(feedUrl);
            var feed = SyndicationFeed.Load(reader);
            return feed.Items.Take(count).ToList();
        });
    }

    public async Task<string> FetchArticleContentAsync(string url)
    {
        try
        {
            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 20 // Or whatever you need
            };
            Console.WriteLine($"Fetching article content from {url}");
            using var http = new HttpClient(handler);
            var html = await http.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var paragraphs = doc.DocumentNode.SelectNodes("//p");
            if (paragraphs != null)
            {
                return string.Join("\n", paragraphs.Select(p => p.InnerText.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
            }

            return doc.DocumentNode.InnerText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching article content: {ex.Message} {url}");
            return string.Empty;
        }
    }
}
