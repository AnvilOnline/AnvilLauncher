using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;


namespace AnvilLauncher.Core
{
    [DataContract]
    public class AnvilManifest
    {
        [DataContract]
        public class ManifestEntry
        {
            [DataMember]
            public string Path { get; set; } // Relative path
            [DataMember]
            public string Hash { get; set; }
            [DataMember]
            public long Size { get; set; }
        }

        [DataMember]
        public uint Build { get; set; }
        [DataMember]
        public string Commit { get; set; }
        [DataMember]
        public string BaseUrl { get; set; }

        [DataMember]
        public ManifestEntry[] Entries { get; set; }

        public AnvilManifest()
        {

        }

        public AnvilManifest(string p_JsonData)
        {
            var s_Success = Deserialize(p_JsonData);

            if (!s_Success)
                Debug.WriteLine("Deserialization failed.");
        }

        public string Serialize()
        {
            var s_Serializer = new DataContractJsonSerializer(typeof(AnvilManifest));
            var s_Json = "";

            try
            {
                using (var s_Stream = new MemoryStream())
                {
                    s_Serializer.WriteObject(s_Stream, this);

                    s_Stream.Position = 0;

                    s_Json = new StreamReader(s_Stream).ReadToEnd();
                }
            }
            catch (Exception p_Exception)
            {
                Debug.WriteLine("Exception: {0}", p_Exception.Message);
            }


            return string.IsNullOrWhiteSpace(s_Json) ? "" : s_Json;
        }

        public bool Deserialize(string p_JsonData)
        {
            // Hold our incoming manifest
            AnvilManifest s_Manifest = null;

            try
            {
                // Create our serializer and try to parse the manifest
                var s_Serializer = new DataContractJsonSerializer(typeof(AnvilManifest));
                using (var s_Stream = new MemoryStream(Encoding.UTF8.GetBytes(p_JsonData)))
                    s_Manifest = (AnvilManifest)s_Serializer.ReadObject(s_Stream);
            }
            catch (Exception p_Exception)
            {
                Debug.WriteLine("Exception: {0}", p_Exception.Message);
            }

            // See if we successfully got a manifest
            if (s_Manifest == null)
                return false;

            // Copy pasta
            Build = s_Manifest.Build;
            Commit = s_Manifest.Commit;
            BaseUrl = s_Manifest.BaseUrl;
            Entries = s_Manifest.Entries;

            return true;
        }
    }
}
