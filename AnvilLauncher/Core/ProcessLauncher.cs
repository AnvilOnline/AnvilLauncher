using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AnvilLauncher.Core
{
    public class ProcessLauncher
    {
        [Flags]
        public enum ProcessCreationFlags : uint
        {
            ZeroFlag = 0x00000000,
            CreateBreakawayFromJob = 0x01000000,
            CreateDefaultErrorMode = 0x04000000,
            CreateNewConsole = 0x00000010,
            CreateNewProcessGroup = 0x00000200,
            CreateNoWindow = 0x08000000,
            CreateProtectedProcess = 0x00040000,
            CreatePreserveCodeAuthzLevel = 0x02000000,
            CreateSeparateWowVdm = 0x00001000,
            CreateSharedWowVdm = 0x00001000,
            CreateSuspended = 0x00000004,
            CreateUnicodeEnvironment = 0x00000400,
            DebugOnlyThisProcess = 0x00000002,
            DebugProcess = 0x00000001,
            DetachedProcess = 0x00000008,
            ExtendedStartupinfoPresent = 0x00080000,
            InheritParentAffinity = 0x00010000
        }

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

        public struct Startupinfo
        {
            public uint Cb;
            public string LpReserved;
            public string LpDesktop;
            public string LpTitle;
            public uint DwX;
            public uint DwY;
            public uint DwXSize;
            public uint DwYSize;
            public uint DwXCountChars;
            public uint DwYCountChars;
            public uint DwFillAttribute;
            public uint DwFlags;
            public short WShowWindow;
            public short CbReserved2;
            public IntPtr LpReserved2;
            public IntPtr HStdInput;
            public IntPtr HStdOutput;
            public IntPtr HStdError;
        }

        public struct ProcessInformation
        {
            public IntPtr HProcess;
            public IntPtr HThread;
            public uint DwProcessId;
            public uint DwThreadId;
        }

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string p_LpApplicationName,
               string p_LpCommandLine, IntPtr p_LpProcessAttributes,
               IntPtr p_LpThreadAttributes,
               bool p_BInheritHandles, ProcessCreationFlags p_DwCreationFlags,
               IntPtr p_LpEnvironment, string p_LpCurrentDirectory,
               ref Startupinfo p_LpStartupInfo,
               out ProcessInformation p_LpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr p_HThread);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr p_HThread);

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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string p_LpPathName);

        public uint SpawnedProcessId { get; private set; }
        public IntPtr SpawnedThread { get; private set; }

        public bool CreateSuspendedProcess(string p_FilePath, string p_Arguments = "", string p_BinDirectory = "")
        {
            if (!string.IsNullOrWhiteSpace(p_BinDirectory))
            {
                var s_Path = Environment.GetEnvironmentVariable("PATH") + $";{p_BinDirectory}";
                Environment.SetEnvironmentVariable("PATH", s_Path);
            }

            var s_StartupInfo = new Startupinfo();
            
            ProcessInformation s_ProcessInfo;
            var s_Success = CreateProcess(null, p_FilePath + " " + p_Arguments, IntPtr.Zero, IntPtr.Zero, false,
                ProcessCreationFlags.CreateSuspended, IntPtr.Zero, null, ref s_StartupInfo, out s_ProcessInfo);

            if (!s_Success)
                return false;

            SpawnedProcessId = s_ProcessInfo.DwProcessId;
            SpawnedThread = s_ProcessInfo.HThread;
            return true;
        }

        public bool InjectDll(uint p_processId, string p_dllPath)
        {
            var s_Handle = OpenProcess(ProcessAccessFlags.All, false, p_processId);
            if (s_Handle == IntPtr.Zero)
                return false;

            var s_Address = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (s_Address == IntPtr.Zero)
                return false;

            var s_Arg = VirtualAllocEx(s_Handle, IntPtr.Zero, (uint)p_dllPath.Length,
                AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);

            if (s_Arg == IntPtr.Zero)
                return false;

            IntPtr s_Written;
            var s_WriteSuccess = WriteProcessMemory(s_Handle, s_Arg, Encoding.Default.GetBytes(p_dllPath), p_dllPath.Length,
                out s_Written);
            if (!s_WriteSuccess)
                return false;

            var s_ThreadId = CreateRemoteThread(s_Handle, IntPtr.Zero, 0, s_Address, s_Arg, 0, IntPtr.Zero);
            return s_ThreadId != IntPtr.Zero;
        }

        public bool ResumeProcess(IntPtr p_Thread)
        {
            var s_Result = ResumeThread(p_Thread);
            return (int)s_Result != -1;
        }

        public bool SetDllSearchLocation(string p_Path)
        {
            return SetDllDirectory(p_Path);
        }
    }
}