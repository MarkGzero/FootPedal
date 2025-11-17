using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FootPedalApp
{
    public class RawInputFootPedalListener : IDisposable
    {
        public event Action<string> PedalPressed;  // Changed to pass button name
        public event Action<bool> DeviceStatusChanged; // True = connected, False = disconnected

        private HwndSource hwndSource;
        private IntPtr hwnd;
        private IntPtr deviceNotificationHandle = IntPtr.Zero;
        
        // Debouncing: track last state to only fire on changes
        private bool lastLeft = false;
        private bool lastMiddle = false;
        private bool lastRight = false;
        private bool lastTop = false;
        
        private bool isDeviceConnected = false;
        private DateTime lastInputTime = DateTime.MinValue;
        private bool isFirstStatusUpdate = true;

        public RawInputFootPedalListener()
        {
            Logger.Log("FootPedalListener initialized");
            try
            {
                // Create a hidden window to receive raw input messages
                var window = new Window
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    ShowActivated = false
                };
                window.Show();
                window.Hide();

                hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                if (hwndSource != null)
                {
                    hwnd = hwndSource.Handle;
                    hwndSource.AddHook(WndProc);
                    RegisterRawInput();
                    RegisterForDeviceNotifications();
                    CheckDeviceConnection();
                }
                else
                {
                    Logger.Log("ERROR: Failed to get HwndSource");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("RawInputFootPedalListener Constructor", ex);
                throw;
            }
        }

        private void RegisterRawInput()
        {
            // Register for Consumer Devices (0x0C, 0x01) - this is what the foot pedal uses!
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x0C; // HID_USAGE_PAGE_CONSUMER
            rid[0].usUsage = 0x01;     // HID_USAGE_CONSUMER_CONTROL
            rid[0].dwFlags = 0x00000100; // RIDEV_INPUTSINK
            rid[0].hwndTarget = hwnd;

            bool result = RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0]));
            
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"ERROR: Failed to register raw input device. Error code: {error}");
                throw new ApplicationException($"Failed to register raw input device(s). Error: {error}");
            }
            
            Logger.Log("Raw input registration successful (Consumer Control 0x0C/0x01)");
        }

        private void RegisterForDeviceNotifications()
        {
            Logger.Log("RegisterForDeviceNotifications: Starting");
            try
            {
                // GUID for HID device class
                Guid hidGuid = new Guid(0x4d1e55b2, 0xf16f, 0x11cf, 0x88, 0xcb, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);
                Logger.Log($"RegisterForDeviceNotifications: HID GUID = {hidGuid}");
                
                DEV_BROADCAST_DEVICEINTERFACE dbi = new DEV_BROADCAST_DEVICEINTERFACE
                {
                    dbcc_size = Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                    dbcc_devicetype = 0x00000005, // DBT_DEVTYP_DEVICEINTERFACE
                    dbcc_reserved = 0,
                    dbcc_classguid = hidGuid,
                    dbcc_name = ""
                };
                
                Logger.Log($"RegisterForDeviceNotifications: Structure size = {dbi.dbcc_size}");
                
                IntPtr buffer = Marshal.AllocHGlobal(dbi.dbcc_size);
                try
                {
                    Marshal.StructureToPtr(dbi, buffer, false);
                    
                    const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
                    Logger.Log($"RegisterForDeviceNotifications: Calling RegisterDeviceNotification with hwnd=0x{hwnd.ToString("X")}");
                    deviceNotificationHandle = RegisterDeviceNotification(hwnd, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);
                    
                    if (deviceNotificationHandle == IntPtr.Zero)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Log($"ERROR: Failed to register device notifications. Error code: {error}");
                    }
                    else
                    {
                        Logger.Log($"Device notification registration successful (handle=0x{deviceNotificationHandle.ToString("X")})");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("RegisterForDeviceNotifications", ex);
            }
        }

        private void CheckDeviceConnection()
        {
            try
            {
                // Optimistically assume device is connected
                // Status will be confirmed when first input is received
                lastInputTime = DateTime.Now;
                UpdateDeviceStatus(true);
            }
            catch (Exception ex)
            {
                Logger.LogException("CheckDeviceConnection", ex);
            }
        }

        private void UpdateDeviceStatus(bool connected)
        {
            if (connected)
            {
                lastInputTime = DateTime.Now;
            }
            
            // On first call during initialization, just set state without firing event
            if (isFirstStatusUpdate)
            {
                isFirstStatusUpdate = false;
                isDeviceConnected = connected;
                return;
            }
            
            if (connected && !isDeviceConnected)
            {
                isDeviceConnected = true;
                Logger.Log("Device connected");
                DeviceStatusChanged?.Invoke(true);
            }
            else if (!connected && isDeviceConnected)
            {
                isDeviceConnected = false;
                Logger.Log("Device disconnected");
                DeviceStatusChanged?.Invoke(false);
            }
        }

        public bool IsDeviceConnected()
        {
            // Consider device disconnected if no input for 10 seconds
            if (lastInputTime != DateTime.MinValue && (DateTime.Now - lastInputTime).TotalSeconds > 10)
            {
                if (isDeviceConnected)
                {
                    UpdateDeviceStatus(false);
                }
            }
            return isDeviceConnected;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;
            const int WM_DEVICECHANGE = 0x0219;
            const int DBT_DEVICEARRIVAL = 0x8000;
            const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
            const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

            if (msg == WM_INPUT)
            {
                ProcessRawInput(lParam);
            }
            else if (msg == WM_DEVICECHANGE)
            {
                try
                {
                    if (lParam != IntPtr.Zero)
                    {
                        DEV_BROADCAST_DEVICEINTERFACE deviceInfo = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(lParam);
                        
                        if (deviceInfo.dbcc_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
                        {
                            string devicePath = deviceInfo.dbcc_name?.ToUpper() ?? "";
                            
                            // Check if this is our foot pedal (VID_07B4&PID_025F)
                            bool isFootPedal = devicePath.Contains("VID_07B4") && devicePath.Contains("PID_025F");
                            
                            if (isFootPedal)
                            {
                                if (wParam.ToInt32() == DBT_DEVICEARRIVAL)
                                {
                                    Logger.Log($"Foot pedal connected: {devicePath}");
                                    UpdateDeviceStatus(true);
                                }
                                else if (wParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE)
                                {
                                    Logger.Log($"Foot pedal disconnected: {devicePath}");
                                    UpdateDeviceStatus(false);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("WndProc:WM_DEVICECHANGE", ex);
                }
            }

            return IntPtr.Zero;
        }

        private void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, 0x10000003, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    // Copy raw data to byte array for easier processing
                    byte[] rawData = new byte[dwSize];
                    Marshal.Copy(buffer, rawData, 0, (int)dwSize);
                    
                    RAWINPUT raw = Marshal.PtrToStructure<RAWINPUT>(buffer);

                    if (raw.header.dwType == 2 && rawData.Length >= 36) // RIM_TYPEHID
                    {
                        // Read button codes from the raw data
                        int buttonCode32 = BitConverter.ToInt32(rawData, 32);
                        int buttonCode28 = BitConverter.ToInt32(rawData, 28);
                        int buttonCode = buttonCode32 != 0 ? buttonCode32 : buttonCode28;
                        
                        // Map button codes to pedals (matching your data)
                        bool newLeft = (buttonCode & 0x04000000) != 0;   // Left
                        bool newMiddle = (buttonCode & 0x02000000) != 0; // Middle
                        bool newRight = (buttonCode & 0x01000000) != 0;  // Right
                        bool newTop = (buttonCode == 2);                  // Top
                        bool isReleased = (buttonCode == 0 || buttonCode == 1);
                        
                        // Update device connection status
                        UpdateDeviceStatus(true);
                        
                        if (isReleased)
                        {
                            // Update state on release
                            lastLeft = false;
                            lastMiddle = false;
                            lastRight = false;
                            lastTop = false;
                        }
                        else
                        {
                            // Only trigger on transition from released to pressed
                            bool leftPressed = newLeft && !lastLeft;
                            bool middlePressed = newMiddle && !lastMiddle;
                            bool rightPressed = newRight && !lastRight;
                            bool topPressed = newTop && !lastTop;
                            
                            if (leftPressed || middlePressed || rightPressed || topPressed)
                            {
                                Logger.Log($"Pedal pressed - Left={leftPressed}, Middle={middlePressed}, Right={rightPressed}, Top={topPressed}");
                                
                                // Fire event for each newly pressed button
                                if (leftPressed && PedalPressed != null)
                                {
                                    PedalPressed.Invoke("Left");
                                }
                                if (middlePressed && PedalPressed != null)
                                {
                                    PedalPressed.Invoke("Middle");
                                }
                                if (rightPressed && PedalPressed != null)
                                {
                                    PedalPressed.Invoke("Right");
                                }
                                if (topPressed && PedalPressed != null)
                                {
                                    PedalPressed.Invoke("Top");
                                }
                                
                                lastLeft = newLeft;
                                lastMiddle = newMiddle;
                                lastRight = newRight;
                                lastTop = newTop;
                            }
                        }
                    }
                    else
                    {
                        Logger.Log($"ProcessRawInput: Not HID device or data too small, type={raw.header.dwType}, size={rawData.Length}");
                    }
                }
                else
                {
                    Logger.Log("ProcessRawInput: GetRawInputData failed to read data");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("ProcessRawInput", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            if (deviceNotificationHandle != IntPtr.Zero)
            {
                UnregisterDeviceNotification(deviceNotificationHandle);
                deviceNotificationHandle = IntPtr.Zero;
            }
            
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
                hwndSource.Dispose();
            }
        }

        #region Win32 Interop

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr Handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            [FieldOffset(16)]
            public RAWKEYBOARD data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public uint VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        #endregion
    }
}
