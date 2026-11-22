using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using SKWReplay.Camera;
using Xunit;

namespace SKWReplay.Tests;

public class CameraEnumeratorTests
{
    [Fact]
    public void EnumeratedCamerasHaveSystemNamesAndSequentialCaptureIndices()
    {
        IReadOnlyList<SKWReplay.Core.CameraDevice> cameras = CameraEnumerator.GetAvailableCameras();

        Assert.All(cameras, camera => Assert.False(string.IsNullOrWhiteSpace(camera.Name)));
        Assert.Equal(Enumerable.Range(0, cameras.Count), cameras.Select(camera => camera.Index));
    }

    [Fact]
    public void ApplyingZoomCropsFromTheCenterWithoutChangingFrameSize()
    {
        using var source = new Mat(4, 4, MatType.CV_8UC1);
        for (int y = 0; y < source.Rows; y++)
        for (int x = 0; x < source.Cols; x++)
            source.Set(y, x, (byte)(y * source.Cols + x));

        using Mat zoomed = CameraCapture.ApplyZoom(source, 2);

        Assert.Equal(source.Size(), zoomed.Size());
        Assert.Equal(source.At<byte>(1, 1), zoomed.At<byte>(0, 0));
    }
}
