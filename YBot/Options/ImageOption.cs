using System.Net;
using System.Text;

namespace YBot.Options;

public class ImageOption
{
    public const    string                Section = "Image";
    public required string                Url          { get; set; }
    public required int                   Score        { get; set; }
    public required string                SearchTag    { get; set; }
    public required IReadOnlyList<string> RandomAppend { get; set; }

    public string GetSearchQuery(ref IReadOnlyCollection<string> query, bool appendRandom = false)
    {
        var seed    = Random.Shared.Next(0, 10000);
        var builder = new StringBuilder($"sort:random:{seed} score:>={Score}");

        if (appendRandom)
        {
            var size = RandomAppend.Count;
            var pick = Random.Shared.Next(-1, size);
            query = pick >= 0 ? [RandomAppend[pick], ..query] : query;
        }

        foreach (var x in query.OrderDescending())
        {
            builder.Insert(0, ' ');
            builder.Insert(0, x);
        }

        var tag = WebUtility.UrlEncode(builder.ToString());
        var url = Url;
        if (url.EndsWith('/')) { url = url[..^1]; }

        return $"{url}?page=dapi&q=index&json=1&s=post&limit=1&tags={tag}";
    }
}