namespace SKWReplay.Recording;

public sealed record BufferedFrame(byte[] Jpeg, DateTimeOffset Timestamp);

public sealed class DelayRingBuffer
{
    private readonly Queue<BufferedFrame> _frames = new();
    private readonly object _sync = new();

    public void Clear()
    {
        lock (_sync) _frames.Clear();
    }

    public void Add(byte[] jpeg)
    {
        lock (_sync)
        {
            _frames.Enqueue(new BufferedFrame(jpeg, DateTimeOffset.UtcNow));
        }
    }

    public BufferedFrame? GetDelayedFrame(int delaySeconds)
    {
        lock (_sync)
        {
            if (_frames.Count == 0)
                return null;
            if (delaySeconds == 0)
                return _frames.Last();

            var target = DateTimeOffset.UtcNow.AddSeconds(-delaySeconds);
            BufferedFrame? selected = null;
            while (_frames.Count > 1 && _frames.Peek().Timestamp < target)
            {
                selected = _frames.Dequeue();
                if (_frames.Peek().Timestamp >= target)
                    break;
            }
            return selected ?? _frames.Peek();
        }
    }
}
