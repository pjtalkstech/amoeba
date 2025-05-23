using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;

public interface IArticleFetcher
{
    Task<List<SyndicationItem>> FetchArticlesAsync(string feedUrl, int count);
    Task<string> FetchArticleContentAsync(string url);
}
