using System.Collections.Generic;
using System.Linq;
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
}
