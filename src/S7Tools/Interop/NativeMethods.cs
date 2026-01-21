using System.Runtime.InteropServices;

namespace S7Tools.Interop;

internal static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, int dwProcessGroupId);

    internal enum CtrlTypes : uint
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1
    }
}
