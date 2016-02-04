using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnvilLauncher.Core
{
    public class ManifestBuilder
    {
        private readonly string m_BaseDirectory;
        public ManifestBuilder(string p_BaseDirectory)
        {
            m_BaseDirectory = p_BaseDirectory;
        }

        public async Task<AnvilManifest> GenerateManifest(uint p_Build, string p_Commit = "", string p_BaseUrl = "")
        {
            // Create a new manifest to hold all of our information
            var s_Manifest = new AnvilManifest
            {
                BaseUrl = p_BaseUrl,
                Build = p_Build,
                Commit = p_Commit
            };

            // This will take some time on large directories, but should be fairly instant in any other regards
            var s_Files = await Task.Run(() => Directory.GetFiles(m_BaseDirectory, "*.*", SearchOption.AllDirectories));
            var s_ManifestEntries = new List<AnvilManifest.ManifestEntry>();

            foreach (var l_File in s_Files)
            {
                var l_FileInfo = new FileInfo(l_File);
                var l_Hash = await Task.Run(() => BitConverter.ToString(new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(l_File))).Replace("-", ""));

                var l_Entry = new AnvilManifest.ManifestEntry
                {
                    Hash = l_Hash,
                    Path = l_File.Replace(m_BaseDirectory, ""),
                    Size = l_FileInfo.Length
                };

                s_ManifestEntries.Add(l_Entry);
            }

            s_Manifest.Entries = s_ManifestEntries.ToArray();

            return s_Manifest;
        }

        public async Task<bool> GenerateUpdate(string p_PackageDirectory, uint p_Build, string p_Commit = "", string p_BaseUrl = "")
        {
            var s_Manifest = await GenerateManifest(p_Build, p_Commit, p_BaseUrl);

            foreach (var l_Entry in s_Manifest.Entries)
            {
                var l_FilePath = Path.GetFullPath(p_PackageDirectory + l_Entry.Path);
                if (!File.Exists(l_FilePath))
                    continue;

                File.WriteAllBytes(l_FilePath, ZLib.Compress(l_FilePath));
            }

            var s_ManifestPath = Path.Combine(p_PackageDirectory, "manifest.json");

            File.WriteAllText(s_ManifestPath, s_Manifest.Serialize());

            return true;
        }
    }
}
