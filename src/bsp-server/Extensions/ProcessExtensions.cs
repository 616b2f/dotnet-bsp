using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dotnet_bsp;

public static class ProcessExtensions
{
    [DllImport ("libc", SetLastError=true, EntryPoint="kill")]
    private static extern int sys_kill (int pid, int sig);

    public static int SIGINT = 2;

    public static void Kill(this Process process, int sig)
    {
        sys_kill(process.Id, sig);
    }
}
