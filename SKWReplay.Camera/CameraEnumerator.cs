using DirectShowLib;
using SKWReplay.Core;
using System.Runtime.InteropServices;

namespace SKWReplay.Camera;

public static class CameraEnumerator
{
    public static IReadOnlyList<CameraDevice> GetAvailableCameras()
    {
        DsDevice[] devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        try
        {
            return devices
                .Select((device, index) => new CameraDevice(index, device.Name))
                .ToArray();
        }
        finally
        {
            foreach (DsDevice device in devices)
            {
                if (device.Mon is not null && Marshal.IsComObject(device.Mon))
                    Marshal.ReleaseComObject(device.Mon);
            }
        }
    }
}
