using System;
using AngleSharp.Html.Dom;
using Lazy;
using YoutubeExplode.Utils;
using YoutubeExplode.Utils.Extensions;

namespace YoutubeExplode.Bridge;

internal partial class ChannelPage(IHtmlDocument content)
{
    [Lazy]
    public string? Url =>
        content.QuerySelector("meta[property=\"og:url\"]")?.GetAttribute("content");

    [Lazy]
    public string? Id => Url?.SubstringAfter("channel/", StringComparison.OrdinalIgnoreCase);

    [Lazy]
    public string? Title =>
        content.QuerySelector("meta[property=\"og:title\"]")?.GetAttribute("content");

    [Lazy]
    public string? LogoUrl =>
        content.QuerySelector("meta[property=\"og:image\"]")?.GetAttribute("content");

    [Lazy]
    public ulong? SubscriberCount
    {
        get
        {
            var metaTag = content.QuerySelector("meta[property=\"og:subscriberCount\"]");
            if (metaTag != null)
            {
                var attributeContent = metaTag.GetAttribute("content");
                if (
                    attributeContent != null
                    && ulong.TryParse(attributeContent, out var subscriberCount)
                )
                {
                    return subscriberCount;
                }
            }
            return null;
        }
    }
}

internal partial class ChannelPage
{
    public static ChannelPage? TryParse(string raw)
    {
        var content = Html.Parse(raw);

        if (content.QuerySelector("meta[property=\"og:url\"]") is null)
            return null;

        return new ChannelPage(content);
    }
}
