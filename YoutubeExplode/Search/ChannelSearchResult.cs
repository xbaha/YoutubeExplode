using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using YoutubeExplode.Channels;
using YoutubeExplode.Common;

namespace YoutubeExplode.Search;

/// <summary>
/// Metadata associated with a YouTube channel returned by a search query.
/// </summary>
public class ChannelSearchResult : ISearchResult, IChannel
{
    /// <inheritdoc />
    public ChannelId Id { get; }

    /// <inheritdoc cref="IChannel.Url" />
    public string Url => $"https://www.youtube.com/channel/{Id}";

    /// <inheritdoc cref="IChannel.Title" />
    public string Title { get; }

    /// <inheritdoc />
    public IReadOnlyList<Thumbnail> Thumbnails { get; }

    /// <inheritdoc  cref="SubscriberCount" />
    public ulong? SubscriberCount { get; }

    /// <summary>
    /// Initializes an instance of <see cref="ChannelSearchResult" />.
    /// </summary>
    public ChannelSearchResult(
        ChannelId id,
        string title,
        IReadOnlyList<Thumbnail> thumbnails,
        ulong? subscriberCount
    )
    {
        Id = id;
        Title = title;
        Thumbnails = thumbnails;
        SubscriberCount = subscriberCount;
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString() => $"Channel ({Title})";
}
