namespace SKWReplay.Recording;

public sealed record BufferedFrame(byte[] Jpeg, DateTimeOffset Timestamp);

public sealed class DelayRingBuffer
{
    private const int MaxDelaySeconds = 60;
    private readonly Queue<BufferedFrame> _frames = new();
    private readonly object _sync = new();

    public void Clear()
    {
        lock (_sync) _frames.Clear();
    }

    public void Add(byte[] jpeg, DateTimeOffset timestamp)
    {
        lock (_sync)
        {
            _frames.Enqueue(new BufferedFrame(jpeg, timestamp));
            DateTimeOffset cutoff = timestamp.AddSeconds(-MaxDelaySeconds);
            while (_frames.Count > 1 && _frames.ElementAt(1).Timestamp <= cutoff)
                _frames.Dequeue();
        }
    }

    public BufferedFrame? GetDelayedFrame(int delaySeconds)
    {
        lock (_sync)
        {
            if (_frames.Count == 0)
                return null;
            delaySeconds = Math.Clamp(delaySeconds, 0, MaxDelaySeconds);
            if (delaySeconds == 0)
                return _frames.Last();

            DateTimeOffset target = DateTimeOffset.UtcNow.AddSeconds(-delaySeconds);
            if (_frames.Peek().Timestamp > target)
                return null;

            BufferedFrame selected = _frames.Peek();
            foreach (BufferedFrame frame in _frames)
            {
                if (frame.Timestamp > target)
                    break;
                selected = frame;
            }
            return selected;
        }
    }
}
