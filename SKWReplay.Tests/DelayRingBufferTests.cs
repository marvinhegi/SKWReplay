using SKWReplay.Recording;
using Xunit;

namespace SKWReplay.Tests;

public class DelayRingBufferTests
{
    [Fact]
    public void ZeroDelayReturnsLatestFrame()
    {
        var ring = new DelayRingBuffer();
        ring.Add(new byte[] { 1 });
        ring.Add(new byte[] { 2 });

        BufferedFrame? result = ring.GetDelayedFrame(0);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Jpeg[0]);
    }

    [Fact]
    public void ClearRemovesBufferedFrames()
    {
        var ring = new DelayRingBuffer();
        ring.Add(new byte[] { 1 });
        ring.Clear();

        Assert.Null(ring.GetDelayedFrame(0));
    }

    [Fact]
    public void DelayIsClampedByAvailableFrames()
    {
        var ring = new DelayRingBuffer();
        ring.Add(new byte[] { 1 });

        BufferedFrame? result = ring.GetDelayedFrame(60);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Jpeg[0]);
    }
}
