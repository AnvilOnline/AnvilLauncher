using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace AnvilLauncher.Core
{
    class Injector
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
             ProcessAccessFlags p_ProcessAccess,
             bool p_BInheritHandle,
             uint p_ProcessId
        );

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr p_HModule, string p_ProcName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr p_HProcess, IntPtr p_LpAddress,
           uint p_DwSize, AllocationType p_FlAllocationType, MemoryProtection p_FlProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr p_HProcess,
            IntPtr p_LpBaseAddress,
            byte[] p_LpBuffer,
            int p_NSize,
            out IntPtr p_LpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr p_HProcess,
           IntPtr p_LpThreadAttributes, uint p_DwStackSize, IntPtr
           p_LpStartAddress, IntPtr p_LpParameter, uint p_DwCreationFlags, IntPtr p_LpThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string p_LpModuleName);

        public bool InjectDll(uint p_ProcessId, string p_DllPath)
        {
            var s_Handle = OpenProcess(ProcessAccessFlags.All, false, p_ProcessId);
            if (s_Handle == IntPtr.Zero)
                return false;

            var s_Address = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (s_Address == IntPtr.Zero)
                return false;

            var s_Arg = VirtualAllocEx(s_Handle, IntPtr.Zero, (uint)p_DllPath.Length,
                AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);

            if (s_Arg == IntPtr.Zero)
                return false;

            IntPtr s_Written;
            var s_WriteSuccess = WriteProcessMemory(s_Handle, s_Arg, Encoding.Default.GetBytes(p_DllPath), p_DllPath.Length,
                out s_Written);
            if (!s_WriteSuccess)
                return false;

            var s_ThreadId = CreateRemoteThread(s_Handle, IntPtr.Zero, 0, s_Address, s_Arg, 0, IntPtr.Zero);
            return s_ThreadId != IntPtr.Zero;
        }

        public bool SetPermissions(string p_DllPath)
        {
            if (string.IsNullOrWhiteSpace(p_DllPath))
                return false;

            if (!File.Exists(p_DllPath))
                return false;

            var s_Security = File.GetAccessControl(p_DllPath);
            s_Security.AddAccessRule(new FileSystemAccessRule("ALL APPLICATION PACKAGES", FileSystemRights.ReadAndExecute, AccessControlType.Allow));
            File.SetAccessControl(p_DllPath, s_Security);

            return true;
        }
    }
}
