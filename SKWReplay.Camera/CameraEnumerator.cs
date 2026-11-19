using SKWReplay.Core;
using System.Runtime.InteropServices;
using System.Text;
using OpenCvSharp;

namespace SKWReplay.Camera;

public static class CameraEnumerator
{
    private const uint DIGCF_PRESENT = 0x2;
    private const uint DIGCF_DEVICEINTERFACE = 0x10;
    private static readonly Guid KSCATEGORY_VIDEO = new("65E8773D-8F56-11D0-A3B9-00A0C9223196");

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData,
        ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet,
        ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr detailData, uint detailDataSize,
        out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll")]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public UIntPtr Reserved;
    }

    public static IReadOnlyList<CameraDevice> GetAvailableCameras()
    {
        var devices = new List<CameraDevice>();
        Guid category = KSCATEGORY_VIDEO;
        IntPtr set = SetupDiGetClassDevs(ref category, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == new IntPtr(-1))
            return devices;

        try
        {
            var data = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
            for (uint i = 0; i < 32; i++)
            {
                data.cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref category, i, ref data))
                    break;

                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out uint required, IntPtr.Zero);
                IntPtr buffer = Marshal.AllocHGlobal((int)required);
                try
                {
                    Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
                    if (SetupDiGetDeviceInterfaceDetail(set, ref data, buffer, required, out _, IntPtr.Zero))
                    {
                        string? path = Marshal.PtrToStringUni(buffer + 4);
                        string name = path is null ? $"Kamera {i + 1}" : $"Kamera {i + 1}";
                        devices.Add(new CameraDevice(devices.Count, name));
                    }
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }

        if (devices.Count > 0)
            return devices;

        var detected = new List<CameraDevice>();
        for (int i = 0; i < 10; i++)
        {
            using var capture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
            if (capture.IsOpened())
            {
                detected.Add(new CameraDevice(i, $"Kamera {i + 1}"));
                capture.Release();
            }
        }
        return detected;
    }
}
