using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode.Utils;

namespace YoutubeExplode.Videos.Streams;

// Works around YouTube's rate throttling, provides seeking support, and some resiliency
internal class MediaStream(HttpClient http, IStreamInfo streamInfo) : Stream
{
    private readonly long _segmentLength = streamInfo.IsThrottled()
        ? 9_898_989
        : streamInfo.Size.Bytes;

    private Stream? _segmentStream;
    private long _actualPosition;

    [ExcludeFromCodeCoverage]
    public override bool CanRead => true;

    [ExcludeFromCodeCoverage]
    public override bool CanSeek => true;

    [ExcludeFromCodeCoverage]
    public override bool CanWrite => false;

    public override long Length => streamInfo.Size.Bytes;

    public override long Position { get; set; }

    // For most streams, YouTube limits transfer speed to match the video playback rate.
    // This helps them avoid unnecessary bandwidth, but for us it's a hindrance because
    // we want to download the stream as fast as possible.
    // To solve this, we divide the logical stream up into multiple segments and download
    // them all separately.

    private void ResetSegment()
    {
        _segmentStream?.Dispose();
        _segmentStream = null;
    }

    private async ValueTask<Stream> ResolveSegmentAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_segmentStream is not null)
            return _segmentStream;

        var from = Position;
        var to = Position + _segmentLength - 1;
        var url = UrlEx.SetQueryParameter(streamInfo.Url, "range", $"{from}-{to}");

        var stream = await http.GetStreamAsync(url, cancellationToken);

        return _segmentStream = stream;
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default) =>
        await ResolveSegmentAsync(cancellationToken);

    private async ValueTask<int> ReadSegmentAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken = default
    )
    {
        for (var retriesRemaining = 5; ; retriesRemaining--)
        {
            try
            {
                var stream = await ResolveSegmentAsync(cancellationToken);
                return await stream.ReadAsync(buffer, offset, count, cancellationToken);
            }
            // Retry on connectivity issues
            catch (IOException) when (retriesRemaining > 0)
            {
                ResetSegment();
            }
        }
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var requestedPosition = Position;

            // If the consumer changed position since the last read, reset the segment
            // to get the correct data.
            if (_actualPosition != requestedPosition)
                ResetSegment();

            // Exit if we reached the end of the stream
            if (requestedPosition >= Length)
                return 0;

            var bytesRead = await ReadSegmentAsync(buffer, offset, count, cancellationToken);
            Position = _actualPosition = requestedPosition + bytesRead;

            if (bytesRead > 0)
                return bytesRead;

            // Reached the end of the segment, load the next one and loop around
            ResetSegment();
        }
    }

    [ExcludeFromCodeCoverage]
    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

    [ExcludeFromCodeCoverage]
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override void SetLength(long value) => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override long Seek(long offset, SeekOrigin origin) =>
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

    [ExcludeFromCodeCoverage]
    public override void Flush() => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ResetSegment();

        base.Dispose(disposing);
    }
}
