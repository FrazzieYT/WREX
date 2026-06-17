using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace SystemManager.Services
{
    public static class KeySimulator
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bKey, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        private static readonly Dictionary<string, byte> KeyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "l", 0x4C }, { "d", 0x44 }, { "e", 0x45 }, { "r", 0x52 },
            { "i", 0x49 }, { "x", 0x58 }, { "tab", 0x09 }, { "v", 0x56 },
            { "s", 0x53 }, { "f2", 0x71 }, { "f5", 0x74 }, { "f4", 0x73 },
            { "esc", 0x1B }, { "space", 0x20 }, { "enter", 0x0D },
            { "delete", 0x2E }, { "backspace", 0x08 }, { "home", 0x24 },
            { "end", 0x23 }, { "pause", 0x13 },
            { "lwin", 0x5B }, { "rwin", 0x5C },
            { "lctrl", 0xA2 }, { "rctrl", 0xA3 },
            { "lalt", 0xA4 }, { "ralt", 0xA5 },
            { "lshift", 0xA0 }, { "rshift", 0xA1 },
        };

        public static void SimulateHotkey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var parts = key.ToLower().Replace("+", " ").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (KeyMap.TryGetValue(part, out var vk))
                    keybd_event(vk, 0, 0, UIntPtr.Zero);
            }

            System.Threading.Thread.Sleep(50);

            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (KeyMap.TryGetValue(parts[i], out var vk))
                    keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        public static void SimulateKeys(string text)
        {
            System.Windows.Forms.SendKeys.SendWait(text);
        }
    }
}
