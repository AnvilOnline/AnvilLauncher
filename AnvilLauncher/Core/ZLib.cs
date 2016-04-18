using System.IO;
using System.IO.Compression;

namespace AnvilLauncher.Core
{
    public class ZLib
    {
        public static byte[] Decompress(string p_File)
        {
            byte[] s_Data;
            using (var l_FileReader = new BinaryReader(new FileStream(p_File, FileMode.Open, FileAccess.Read)))
                s_Data = l_FileReader.ReadBytes((int)l_FileReader.BaseStream.Length);
            return Decompress(s_Data);
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
                    {
                        s_DecompressionStream.CopyTo(s_OutputStream);

                        s_DecompressionStream.Close();

                        // Get our final data
                        s_Data = s_OutputStream.ToArray();
                    }
                }
            }

            return s_Data;
        }

        public static byte[] Compress(string p_File)
        {
            byte[] s_Data;
            using (var l_FileReader = new BinaryReader(new FileStream(p_File, FileMode.Open, FileAccess.Read)))
                s_Data = l_FileReader.ReadBytes((int)l_FileReader.BaseStream.Length);
            return Compress(s_Data);
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
                {
                    s_CompressStream.Write(p_Data, 0, p_Data.Length);
                    s_CompressStream.Close();

                    // zlib compatible compressed query
                    s_Data = s_OutputStream.ToArray();
                }
            }

            return s_Data;
        }
    }
}
