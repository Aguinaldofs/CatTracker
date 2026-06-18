using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BongoCatTracker;

public sealed class NativeInputHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;

    private readonly LowLevelProc _keyboardProc;
    private readonly LowLevelProc _mouseProc;
    private readonly HashSet<int> _pressedKeys = new();
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private bool _disposed;

    public event Action? KeyPressed;
    public event Action<string>? MousePressed;

    public NativeInputHook()
    {
        _keyboardProc = KeyboardCallback;
        _mouseProc = MouseCallback;
    }

    public void Start()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule currentModule = currentProcess.MainModule!;
        IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);

        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);

        if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Nao foi possivel iniciar os hooks globais do Windows.");
        }
    }

    private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();
            int vkCode = Marshal.ReadInt32(lParam);

            if (message is WM_KEYDOWN or WM_SYSKEYDOWN)
            {
                if (_pressedKeys.Add(vkCode))
                {
                    KeyPressed?.Invoke();
                }
            }
            else if (message is WM_KEYUP or WM_SYSKEYUP)
            {
                _pressedKeys.Remove(vkCode);
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            string? button = wParam.ToInt32() switch
            {
                WM_LBUTTONDOWN => "Esquerdo",
                WM_RBUTTONDOWN => "Direito",
                WM_MBUTTONDOWN => "Meio",
                WM_XBUTTONDOWN => "Extra",
                _ => null
            };

            if (button is not null)
            {
                MousePressed?.Invoke(button);
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
        }

        _disposed = true;
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
