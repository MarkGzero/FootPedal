using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FootPedalApp
{
    public class HotkeySender
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void Send(string hotkey)
        {
            Logger.Log($"HotkeySender.Send: Received hotkey string: '{hotkey}'");
            
            if (string.IsNullOrWhiteSpace(hotkey))
            {
                Logger.Log("HotkeySender.Send: Hotkey is null or empty, returning");
                return;
            }

            var parts = hotkey.Split('+');
            var modifiers = new System.Collections.Generic.List<Keys>();
            Keys mainKey = Keys.None;

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                
                if (trimmed.Equals("Win", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers.Add(Keys.LWin);
                    Logger.Log($"HotkeySender.Send: Added modifier LWin (0x{((byte)Keys.LWin):X})");
                }
                else if (trimmed.Length == 1 && char.IsDigit(trimmed[0]))
                {
                    mainKey = (Keys)Enum.Parse(typeof(Keys), "D" + trimmed);
                    Logger.Log($"HotkeySender.Send: Parsed digit '{trimmed}' as {mainKey} (0x{((byte)mainKey):X})");
                }
                else if (Enum.TryParse<Keys>(trimmed, true, out var key))
                {
                    if (IsModifier(key))
                    {
                        modifiers.Add(key);
                        Logger.Log($"HotkeySender.Send: Added modifier {key} (0x{((byte)key):X})");
                    }
                    else
                    {
                        mainKey = key;
                        Logger.Log($"HotkeySender.Send: Set main key {key} (0x{((byte)key):X})");
                    }
                }
                else
                {
                    Logger.Log($"HotkeySender.Send: Could not parse '{trimmed}'");
                }
            }

            Logger.Log($"HotkeySender.Send: Sending {modifiers.Count} modifiers + main key {mainKey}");

            foreach (var mod in modifiers)
            {
                keybd_event((byte)mod, 0, 0, UIntPtr.Zero);
                Logger.Log($"HotkeySender.Send: keybd_event down for {mod}");
            }

            if (mainKey != Keys.None)
            {
                keybd_event((byte)mainKey, 0, 0, UIntPtr.Zero);
                keybd_event((byte)mainKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Logger.Log($"HotkeySender.Send: keybd_event down/up for {mainKey}");
            }

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                keybd_event((byte)modifiers[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                Logger.Log($"HotkeySender.Send: keybd_event up for {modifiers[i]}");
            }
        }

        private static bool IsModifier(Keys key)
        {
            return key == Keys.LWin || key == Keys.RWin ||
                   key == Keys.Control || key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey ||
                   key == Keys.Shift || key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                   key == Keys.Alt || key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu;
        }
    }
}
