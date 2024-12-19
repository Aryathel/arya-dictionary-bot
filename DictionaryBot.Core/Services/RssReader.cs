using System.Xml;
using System.ServiceModel.Syndication;

namespace DictionaryBot.Core.Services;

public class RssReaderService
{
    public IEnumerable<SyndicationItem> ReadRss(string url)
    {
        XmlReader reader = XmlReader.Create(url);
        SyndicationFeed feed = SyndicationFeed.Load(reader);
        reader.Close();
        return feed.Items;
    }

    public IEnumerable<SyndicationItem> ReadRssAfter(string url, DateTime dateTime)
    {
        return ReadRss(url).Where(i => i.PublishDate.UtcDateTime > dateTime.ToUniversalTime());
    }

    public SyndicationItem ReadRssMostRecent(string url)
    {
        return ReadRss(url).OrderByDescending(i => i.PublishDate.UtcDateTime).First();
    }
}