using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AnvilLauncher.Core;

namespace AnvilLauncher
{
    public partial class FrmMain : Form
    {
        private readonly string m_ManifestUrl = Properties.Settings.Default.ManifestUrl;
        private readonly string m_CurrentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private readonly string m_BinDir = Path.GetFullPath(Properties.Settings.Default.BinDirectory);
        private readonly string m_ModuleName = Properties.Settings.Default.ModuleName;
        private readonly string m_ProcessLocation = Path.GetFullPath(Properties.Settings.Default.ExecutableLocation);
        private readonly string m_DefaultArgs = Properties.Settings.Default.DefaultArgs;
        private readonly bool m_SkipUpdate = Properties.Settings.Default.SkipUpdate;

        private bool m_SelfUpdated;

        public FrmMain()
        {
            InitializeComponent();
        }

        private async void frmMain_Load(object p_Sender, EventArgs p_EventArgs)
        {
            var s_UpdateResult = await RunUpdate();

            // Proceed to launch the game
            if (s_UpdateResult)
                LaunchProcess();
        }

        private void LaunchProcess()
        {
            UpdateStatus(100, "Launching Process...");

            var s_Launcher = new ProcessLauncher();

            // Set our new path up so we can load dll's from a different location
            var s_Ret = s_Launcher.SetDllSearchLocation(m_BinDir);
            if (!s_Ret)
            {
                UpdateStatus(0, "Could not add bin folder to search path.");
                return;
            }

            // Get the command line arguments and forward them.
            var s_CommandLineArgs = Environment.GetCommandLineArgs();
            var s_CommandLine = (string.IsNullOrWhiteSpace(m_DefaultArgs) ? string.Empty : m_DefaultArgs + " ");

            if (s_CommandLineArgs.Length > 0)
            {
                for (var i = 1; i < s_CommandLineArgs.Length; ++i)
                    s_CommandLine += s_CommandLineArgs[i] + " ";
            }

            // Create the suspended process, which will give us ample time to inject (hue hue hue, it will never execute unless we tell it too)
            var s_LaunchSuccess = s_Launcher.CreateSuspendedProcess(m_ProcessLocation, s_CommandLine, m_BinDir);

            // Ensure that the launch was a success
            if (!s_LaunchSuccess)
            {
                UpdateStatus(0, "Could not launch Halo Online.");
                return;
            }

            // Get the process and thread if the launch was a success
            var s_ProcessId = s_Launcher.SpawnedProcessId;
            var s_ProcessThread = s_Launcher.SpawnedThread;

#if DEBUG
            WriteLog($"ProcessId: {s_ProcessId} ProcessThread: {s_ProcessThread}");
#endif

            // Inject our dll
            var s_Final = Path.GetFullPath(Path.Combine(m_BinDir, m_ModuleName));
            WriteLog($"Injecting module: {s_Final}.");
            if (!s_Launcher.InjectDll(s_ProcessId, s_Final))
            {
                WriteLog("Failed injection");
                UpdateStatus(0, "Could not load module.");
                return;
            }

            // UI Update ;)
            UpdateStatus(100, "Launched process...");

            Thread.Sleep(500);

            // Resume the suspended process, that way all hook code should already be injected at this time.
            s_Launcher.ResumeProcess(s_ProcessThread);

            // Sob quietly before leaving.
            Application.Exit();
        }

        private async Task<bool> RunUpdate()
        {
            if (m_SkipUpdate)
                return true;

            UpdateStatus(0, "Contacting download server");

            // TODO: Make configurable
            var s_UpdateDirectory = m_CurrentDir;

            // Ensure that our update's directory actually exists
            if (!Directory.Exists(s_UpdateDirectory))
                Directory.CreateDirectory(s_UpdateDirectory);

            // Download the manifest
            var s_ManifestData = await DownloadManifest();

            // Ensure that we got good data back
            if (string.IsNullOrWhiteSpace(s_ManifestData))
                return false;

            // Parse the manifest data and create our manifest
            var s_Manifest = new AnvilManifest(s_ManifestData);

            // Get the manifest entries that we need to download
            var s_EntriesToDownload = await CompareManifest(s_Manifest);

            // UI tracking
            var s_Current = 0.0f;
            var s_Length = (float)s_EntriesToDownload.Length;

            // Hold our current running executable
            var s_UpdateExecutablePath = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            // Loop through and download every entry
            foreach (var l_Entry in s_EntriesToDownload)
            {
                s_Current++;

                UpdateStatus((int)(100 * (s_Current / s_Length)), "Downloading " + l_Entry.Path + ".");

                var l_DownloadPath = s_Manifest.BaseUrl + l_Entry.Path;

                try
                {
                    using (var l_Client = new HttpClient())
                    {
                        var l_Data = await l_Client.GetByteArrayAsync(new Uri(l_DownloadPath));

                        var l_Path = Path.GetFullPath(s_UpdateDirectory + l_Entry.Path);

                        var l_DirectoryPath = Path.GetDirectoryName(l_Path);
                        if (l_DirectoryPath != null && !Directory.Exists(l_DirectoryPath))
                            Directory.CreateDirectory(l_DirectoryPath);

                        // Handle updating the updater.
                        if (Path.GetFileName(l_Entry.Path) == s_UpdateExecutablePath)
                        {
                            // If a previous old updater already exists, delete it
                            var l_OldUpdaterPath = s_UpdateExecutablePath + ".old";
                            if (File.Exists(l_OldUpdaterPath))
                                File.Delete(l_OldUpdaterPath);

                            // Rename the currently running executable.
                            File.Move(s_UpdateExecutablePath, l_OldUpdaterPath);
                            m_SelfUpdated = true;
                        }

                        // Automatically decompress zlib files
                        File.WriteAllBytes(l_Path, ZLib.Decompress(l_Data));
                    }
                }
                catch (Exception s_Exception)
                {
                    Console.WriteLine(s_Exception.Message);
                    UpdateStatus(0, $"Error downloading {l_Entry.Path}.");
                }
            }

            // Create a post-update manifest to see if there were any files that didn't process correctly
            var s_PostManifest = await CompareManifest(s_Manifest);
            if (s_PostManifest.Length > 0)
            {
                UpdateStatus(0, "Error extracting some files, restart updater, or seek help");
                return false;
            }

            if (!m_SelfUpdated)
                return true;

            // Get the command line arguments and forward them.
            var s_CommandLineArgs = Environment.GetCommandLineArgs();
            var s_CommandLine = "";

            // Append all of the arguments to one long string
            if (s_CommandLineArgs.Length > 0)
            {
                for (var i = 1; i < s_CommandLineArgs.Length; ++i)
                    s_CommandLine += s_CommandLineArgs[i] + " ";
            }

            // Re-launch the updater with the same arguments
            var s_ProcessInfo = new ProcessStartInfo(s_UpdateExecutablePath, s_CommandLine);
            Process.Start(s_ProcessInfo);

            // Exit this copy of the updater
            Application.Exit();

            return true;
        }

        /// <summary>
        /// Downloads the manifest content (json)
        /// </summary>
        /// <returns>string with the manifest in json format.</returns>
        private async Task<string> DownloadManifest()
        {
            var s_Manfiest = string.Empty;
            try
            {
                using (var s_Client = new HttpClient())
                {
                    var s_Data = await s_Client.GetStringAsync(m_ManifestUrl);
                    if (!s_Data.StartsWith("{"))
                        return string.Empty;

                    s_Manfiest = s_Data;
                }
            }
            catch (HttpRequestException s_Exception)
            {
                Console.WriteLine($"Could not get the manifest, error {s_Exception.Message}.");
                UpdateStatus(0, "There was an error downloading the manifest.");
            }

            return s_Manfiest;
        }

        /// <summary>
        /// Compare information that is in the manifest to local files
        /// </summary>
        /// <param name="p_Manifest"></param>
        /// <returns></returns>
        private async Task<AnvilManifest.ManifestEntry[]> CompareManifest(AnvilManifest p_Manifest)
        {
            // Start by changing our status
            UpdateStatus(0, $"Verifying {p_Manifest.Entries.Length} files.");

            // Create a new list of entries
            var s_EntryList = new List<AnvilManifest.ManifestEntry>();

            // If we have the setting set to skip updates then bail out without verifying or downloading
            if (m_SkipUpdate)
                return s_EntryList.ToArray();
            
            // Information tracking
            var s_Count = (float)p_Manifest.Entries.Length;
            var s_Current = 0.0f;

            // Loop through each entry that got added and verify the SHA1 if not add it to the list to be downloaded later
            foreach (var l_Entry in p_Manifest.Entries)
            {
                s_Current++;

                // Update the status
                UpdateStatus((int)(100 * (s_Current / s_Count)), $"Verifying {l_Entry.Path}");


                // If the file does not exist, set to download it
                var l_Path = Path.GetFullPath(m_CurrentDir + l_Entry.Path);
                if (!File.Exists(l_Path))
                {
                    s_EntryList.Add(l_Entry);
                    continue;
                }

                // If the file does exist, and the file size does not match, set to download
                var l_Info = new FileInfo(l_Path);
                if (l_Info.Length != l_Entry.Size)
                {
                    s_EntryList.Add(l_Entry);
                    continue;
                }

                // If the file exists, and it's the correct size, verify the hash finally.
                var l_Hash =
                    await Task.Run(() => BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(l_Path))).Replace("-", ""));
                if (l_Hash == l_Entry.Hash)
                    continue;

                s_EntryList.Add(l_Entry);
            }

            return s_EntryList.ToArray();
        }

        /// <summary>
        /// Update's the user interface with some extra information
        /// </summary>
        /// <param name="p_Percentage"></param>
        /// <param name="p_Status"></param>
        public void UpdateStatus(int p_Percentage, string p_Status)
        {
            // Cross thread safe
            if (lblUpdateInfo.InvokeRequired)
                lblUpdateInfo.Invoke((MethodInvoker)delegate { lblUpdateInfo.Text = p_Status; });
            else
                lblUpdateInfo.Text = p_Status;

            if (pbPercentage.InvokeRequired)
                pbPercentage.Invoke((MethodInvoker)delegate { pbPercentage.Value = p_Percentage; });
            else
                pbPercentage.Value = p_Percentage;
        }

        private void WriteLog(string p_Message)
        {
            using (var l_Log = File.AppendText("availlauncher.log"))
                l_Log.WriteLine(p_Message);
        }
    }
}
