using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AnvilLauncher.Core
{
    public static class ProcessExtension
    {
        [Flags]
        public enum ThreadAccess : int
        {
            Terminate = (0x0001),
            SuspendResume = (0x0002),
            GetContext = (0x0008),
            SetContext = (0x0010),
            SetInformation = (0x0020),
            QueryInformation = (0x0040),
            SetThreadToken = (0x0080),
            Impersonate = (0x0100),
            DirectImpersonation = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess p_DwDesiredAccess, bool p_BInheritHandle, uint p_DwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr p_HThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr p_HThread);

        public static void Suspend(this Process p_Process)
        {
            foreach (ProcessThread thread in p_Process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                SuspendThread(pOpenThread);
            }
        }
        public static void Resume(this Process p_Process)
        {
            foreach (ProcessThread thread in p_Process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                ResumeThread(pOpenThread);
            }
        }
    }
}
