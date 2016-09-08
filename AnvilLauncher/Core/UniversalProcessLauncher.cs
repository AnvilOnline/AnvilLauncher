
using Windows.Management.Deployment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AnvilLauncher.Core
{
    class UniversalProcessLauncher
    {
        public enum ActivateOptions
        {
            /// <summary>
            /// No flags set
            /// </summary>
            None = 0x00000000,

            /// <summary>
            /// The application is being activated for design mode, and thus will not be able to
            /// create an immersive window. Window creation must be done by design tools which
            /// load the necessary components by communicating with a designer-specified service on
            /// the site chain established on the activation manager.  The splash screen normally
            /// shown when an application is activated will also not appear.  Most activations
            /// will not use this flag.
            /// </summary>
            DesignMode = 0x00000001,

            /// <summary>
            /// Do not show an error dialog if the app fails to activate.
            /// </summary>
            NoErrorUi = 0x00000002,

            /// <summary>
            /// Do not show the splash screen when activating the app.
            /// </summary>
            NoSplashScreen = 0x00000004,
        }

        [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IApplicationActivationManager
        {
            // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
            // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
            IntPtr ActivateApplication([In] string p_AppUserModelId, [In] string p_Arguments, [In] ActivateOptions p_Options, [Out] out UInt32 p_ProcessId);
            IntPtr ActivateForFile([In] string p_AppUserModelId, [In] IntPtr /*IShellItemArray* */ p_ItemArray, [In] string p_Verb, [Out] out UInt32 p_ProcessId);
            IntPtr ActivateForProtocol([In] string p_AppUserModelId, [In] IntPtr /* IShellItemArray* */p_ItemArray, [Out] out UInt32 p_ProcessId);

        }
        [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
        class ApplicationActivationManager : IApplicationActivationManager
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
            public extern IntPtr ActivateApplication([In] string p_AppUserModelId, [In] string p_Arguments, [In] ActivateOptions p_Options, [Out] out UInt32 p_ProcessId);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForFile([In] string p_AppUserModelId, [In] IntPtr /*IShellItemArray* */ p_ItemArray, [In] string p_Verb, [Out] out UInt32 p_ProcessId);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForProtocol([In] string p_AppUserModelId, [In] IntPtr /* IShellItemArray* */p_ItemArray, [Out] out UInt32 p_ProcessId);
        }

        private readonly PackageManager m_PackageManager;

        private readonly List<UniversalPackage> m_Packages;

        public UniversalProcessLauncher()
        {
            m_PackageManager = new PackageManager();
            m_Packages = new List<UniversalPackage>();
        }

        private int EnumeratePackages()
        {
            m_Packages.Clear();
            
            try
            {
                var s_Packages = m_PackageManager.FindPackages();
                foreach (var l_Package in s_Packages)
                    m_Packages.Add(new UniversalPackage(l_Package, m_PackageManager));
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Could not enumerate packages, you will have to run as administrator.");
                return 0;
            }

            return m_Packages.Count;
        }

        private bool FindHalo(out UniversalPackage p_Package)
        {
            p_Package = null;

            var s_Count = EnumeratePackages();
            if (s_Count == 0)
                return false;

#if DEBUG
            Console.WriteLine($"Searching for Halo in {s_Count} packages.");
#endif
            var s_HaloPackage = m_Packages.FirstOrDefault(p_SearchPackage => p_SearchPackage.Name.Contains("Halo5Forge"));
            if (s_HaloPackage == null)
            {
                Console.WriteLine($"Could not find Halo in {s_Count} packages.");
                return false;
            }

            p_Package = s_HaloPackage;
            return true;
        }

        private bool LaunchProcess(UniversalPackage p_Package, out uint p_ProcessId, bool p_Suspend = false)
        {
            // Initialize our output process id to 0
            p_ProcessId = 0;

            // Create a new instance of ApplicationActivationManager
            var s_ActivationManager = new ApplicationActivationManager();//Class not registered

            var s_AppUserModelId = GetAppUserModelId(p_Package);

            // Try to activate our application
            var s_Result = s_ActivationManager.ActivateApplication(s_AppUserModelId, null, ActivateOptions.None, out p_ProcessId);
            if (s_Result != IntPtr.Zero || p_ProcessId == 0)
            {
#if DEBUG
                Console.WriteLine($"Could not launch {p_Package.Name}.");
#endif
                return false;
            }

            // If we are set to suspend our process, do so now
            if (p_Suspend && !SuspendProcess((int)p_ProcessId))
            {
#if DEBUG
                Console.WriteLine($"Could not suspend process {p_ProcessId}.");
#endif
                return false;
            }

            return true;
        }

        // xbox7887 modified by kiwidog
        private static string GetAppUserModelId(UniversalPackage p_Package)
        {
            var s_AppUserModelId = string.Empty;
            using (var s_Key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\"))
            {
                if (s_Key == null)
                    return s_AppUserModelId;

                var s_ClassKeys = s_Key.GetSubKeyNames().Where(p_Key => p_Key.StartsWith("AppX"));
                foreach (var l_KeyName in s_ClassKeys)
                {
                    using (var l_Key = s_Key.OpenSubKey($"{l_KeyName}\\Application\\"))
                    {
                        var l_ModelId = l_Key?.GetValue("AppUserModelID") as string;
                        if (string.IsNullOrWhiteSpace(l_ModelId))
                            continue;

                        if (!l_ModelId.StartsWith(p_Package.FamilyName))
                            continue;

                        s_AppUserModelId = l_ModelId;
                        break;
                    }
                }
            }

            return s_AppUserModelId;
        }

        public bool LaunchHalo(string p_InjectDll = "")
        {
            UniversalPackage s_HaloPackage;
            if (!FindHalo(out s_HaloPackage))
                return false;

            uint s_ProcessId = 0;
            if (!LaunchProcess(s_HaloPackage, out s_ProcessId))
                return false;

            if (string.IsNullOrWhiteSpace(p_InjectDll))
                return true;
            
            var s_Injector = new Injector();

            if (!s_Injector.SetPermissions(p_InjectDll))
                return false;

            return s_Injector.InjectDll(s_ProcessId, p_InjectDll);
        }

        private bool SuspendProcess(int p_ProcessId)
        {
            try
            {
                var s_Process = Process.GetProcessById(p_ProcessId);
                s_Process.Suspend();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
