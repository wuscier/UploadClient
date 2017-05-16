using System;
using System.IO;
using System.Security.Cryptography;

namespace UploadClient
{
    public class FileHelper
    {
        public static string GetFileHash(string file)
        {
            string hashString = string.Empty;

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                MD5 algorithm = MD5.Create();
                byte[] hashData = algorithm.ComputeHash(fs);

                hashString = BitConverter.ToString(hashData);
                fs.Close();
            }

            return hashString;
        }
    }
}
