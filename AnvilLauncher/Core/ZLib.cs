using System.IO;
using System.IO.Compression;

namespace AnvilLauncher.Core
{
    public class ZLib
    {
        public static byte[] Decompress(string p_File)
        {
            return Decompress(File.ReadAllBytes(p_File));
        }

        /// <summary>
        /// Decompress zlib compressed data
        /// </summary>
        /// <param name="p_Data"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] p_Data)
        {
            byte[] s_Data;

            using (var s_InputStream = new MemoryStream(p_Data))
            {
                using (var s_OutputStream = new MemoryStream())
                {
                    using (var s_DecompressionStream = new DeflateStream(s_InputStream, CompressionMode.Decompress, true))
                        s_DecompressionStream.CopyTo(s_OutputStream);

                    // Get our final data
                    s_Data = s_OutputStream.ToArray();
                }
            }

            return s_Data;
        }

        public static byte[] Compress(string p_File)
        {
            return Compress(File.ReadAllBytes(p_File));
        }
        /// <summary>
        /// Compress uncompressed data
        /// </summary>
        /// <param name="p_Data"></param>
        /// <returns></returns>
        public static byte[] Compress(byte[] p_Data)
        {
            byte[] s_Data;

            using (var s_OutputStream = new MemoryStream())
            {
                using (var s_CompressStream = new DeflateStream(s_OutputStream, CompressionMode.Compress, true))
                    s_CompressStream.Write(p_Data, 0, p_Data.Length);

                // zlib compatible compressed query
                s_Data = s_OutputStream.ToArray();
            }

            return s_Data;
        }
    }
}
