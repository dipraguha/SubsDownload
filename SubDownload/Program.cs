using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SubDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter directory path: ");
            var dirPath = Console.ReadLine();

            try
            {
                if (!Directory.Exists(dirPath))
                {
                    throw new Exception("Invalid Directory Path");
                }

                ProcessFiles(dirPath);                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);                
            }

            Console.WriteLine("Completed. Press any key to exit...");
            Console.ReadLine();
        }

        private static void ProcessFiles(string dirPath)
        {
            var videoFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly)
                                        .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                                            || f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)
                                            || f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase));

            foreach (var file in videoFiles)
            {
                GetSub(GetMD5Hash(file), dirPath, file);
            }
        }

        private static string GetMD5Hash(string fileName)
        {
            int readSize = 64 * 1024;

            byte[] startData = new byte[readSize];
            byte[] endData = new byte[readSize];

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                fs.Read(startData, 0, startData.Length);

                fs.Seek(-readSize, SeekOrigin.End);
                fs.Read(endData, 0, readSize);

                fs.Close();
            }

            byte[] data = new byte[startData.Length + endData.Length];
            startData.CopyTo(data, 0);
            endData.CopyTo(data, startData.Length);

            return GenerateHash(data);
        }

        private static string GenerateHash(byte[] data)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashResult = md5.ComputeHash(data);

            StringBuilder hashString = new StringBuilder();
            for (int i = 0; i < hashResult.Length; i++)
            {
                hashString.Append(hashResult[i].ToString("x2"));
            }

            return hashString.ToString();
        }

        private static void GetSub(string hash, string dirPath, string fileName)
        {
            var videoFileName = Path.GetFileNameWithoutExtension(fileName);
            var srtFileName = videoFileName + ".srt";
            var targetFilePath = dirPath + @"\" + srtFileName;

            Console.WriteLine("Getting subs for " + videoFileName + "...");

            var url = @"http://api.thesubdb.com/?action=download&hash=" + hash + "&language=en";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            
            //Specify your own user agent below. Do not use the one here.
            req.UserAgent = @"SubDB/1.0 (DipraWin10/0.1; http://diprawin10.in/subdb)";
            req.Timeout = 50000;
            req.KeepAlive = true;

            try
            {
                using (var res = (HttpWebResponse)req.GetResponse())
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream inputStream = res.GetResponseStream())
                        {
                            var streamReader = new StreamReader(inputStream, Encoding.GetEncoding(res.CharacterSet));
                            File.WriteAllText(targetFilePath, streamReader.ReadToEnd());
                        }
                    }
                    else if (res.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception("Subtitle not found for " + videoFileName);
                    }
                    else if (res.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new Exception("Bad Request");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to get subs for " + videoFileName + ". Reason: " + e.Message);
            }
        }
    }
}
