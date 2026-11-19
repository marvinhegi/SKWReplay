using System;
using SKWReplay.Recording;
using Xunit;

namespace SKWReplay.Tests;

public class DelayRingBufferTests
{
    [Fact]
    public void ZeroDelayReturnsLatestFrame()
    {
        var ring = new DelayRingBuffer();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ring.Add(new byte[] { 1 }, now.AddMilliseconds(-20));
        ring.Add(new byte[] { 2 }, now);

        BufferedFrame? result = ring.GetDelayedFrame(0);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Jpeg[0]);
    }

    [Fact]
    public void ClearRemovesBufferedFrames()
    {
        var ring = new DelayRingBuffer();
        ring.Add(new byte[] { 1 }, DateTimeOffset.UtcNow);
        ring.Clear();

        Assert.Null(ring.GetDelayedFrame(0));
    }

    [Fact]
    public void DelayedFrameIsUnavailableUntilTheBufferHasEnoughHistory()
    {
        var ring = new DelayRingBuffer();
        ring.Add(new byte[] { 1 }, DateTimeOffset.UtcNow);

        BufferedFrame? result = ring.GetDelayedFrame(60);

        Assert.Null(result);
    }

    [Fact]
    public void ChangingDelaySelectsTheFrameAtTheRequestedOffset()
    {
        var ring = new DelayRingBuffer();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ring.Add(new byte[] { 1 }, now.AddSeconds(-10));
        ring.Add(new byte[] { 2 }, now.AddSeconds(-6));

        BufferedFrame? result = ring.GetDelayedFrame(5);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Jpeg[0]);
    }
}
