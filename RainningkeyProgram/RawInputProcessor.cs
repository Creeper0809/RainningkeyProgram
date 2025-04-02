using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace RainningkeyProgram
{
    public class RawInputProcessor
    {
        private RawInputProcessor() { }
        private static RawInputProcessor Instance;
        public event Action<string>? KeyDown;
        public event Action<string>? KeyUp;

        const int WM_INPUT = 0x00FF;
        const uint RID_INPUT = 0x10000003;

        [DllImport("User32.dll")]
        extern static bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        extern static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER
        {
            public uint Type;
            public uint Size;
            public IntPtr Device;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWKEYBOARD keyboard;
        }
        public static RawInputProcessor GetInstance()
        {
            if (Instance == null)
                Instance = new RawInputProcessor();
            return Instance;
        }
        public void RegisterRawInputDevices(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].UsagePage = 0x01;
            rid[0].Usage = 0x06;
            rid[0].Flags = 0x00000100; // RIDEV_INPUTSINK
            rid[0].Target = hwnd;
            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
            {
                MessageBox.Show("Raw Input 등록 실패");
            }
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_INPUT)
            {
                uint size = 0;
                GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
                IntPtr buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    if (GetRawInputData(lParam, RID_INPUT, buffer, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == size)
                    {
                        RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);
                        if (raw.header.Type == 1) // Keyboard
                        {
                            string keyName = KeyInterop.KeyFromVirtualKey(raw.keyboard.VKey).ToString();
                            if (raw.keyboard.Message == 0x0100 || raw.keyboard.Message == 0x0104)
                                KeyDown?.Invoke(keyName);
                            else if (raw.keyboard.Message == 0x0101 || raw.keyboard.Message == 0x0105)
                                KeyUp?.Invoke(keyName);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
