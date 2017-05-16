using FluentFTP;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UploadClient.Properties;

namespace UploadClient
{
    public class FtpUploader
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;

        private readonly object _lockObj = new object();
        private static readonly int ChunkSize = 65536;

        public CancellationTokenSource UploadCTS;
        public CancellationTokenSource SendLiveStatusCTS;

        public EventHandler<FtpProgressEventArgs> UploadProgress;

        public FtpUploader(string ip, int port, string userName, string password)
        {
            _ip = ip;
            _port = port;
            _userName = userName;
            _password = password;

        }

        private FtpClient _client = null;

        void InitializeClient()
        {
            lock (_lockObj)
            {
                if (_client == null)
                {
                    _client = new FtpClient
                    {
                        Host = _ip,
                        Port = _port,
                        Credentials = new NetworkCredential(_userName, _password),
                    };
                }
            }
        }

        public async Task<bool> Upload(string localPath, string remotePath, CancellationToken cancelToken, long lastTransferredBytes = 0)
        {
            InitializeClient();
            var task = await Task.Factory.StartNew(() => FtpUpload(localPath, remotePath, cancelToken, lastTransferredBytes));
            return task;
        }

        private bool FtpUpload(string localPath, string remotePath,CancellationToken cancelToken,long transferredBytes)
        {
            // 获取本地文件的Md5值，根据Md5值读取本地文件的断点续传位置
            long total = transferredBytes;

            try
            {
                string ftpDirectoryName = FtpExtensions.GetFtpDirectoryName(remotePath);
                if (!_client.DirectoryExists(ftpDirectoryName))
                {
                    _client.CreateDirectory(ftpDirectoryName);
                }


                using (var fs = File.OpenRead(localPath))
                {
                    if (transferredBytes!=0)
                    {
                        fs.Seek(transferredBytes, SeekOrigin.Begin);
                    }

                    using (var stream = transferredBytes==0? _client.OpenWrite(remotePath):_client.OpenAppend(remotePath))
                    {
                        var reader = new BinaryReader(fs);
                        var writer = new BinaryWriter(stream);
                        var buffer = new byte[ChunkSize];
                        int size;
                        while ((size = reader.Read(buffer, 0, ChunkSize)) != 0)
                        {
                            if (cancelToken.IsCancellationRequested)
                            {
                                FtpBreakpoint ftpBreakpoint = new FtpBreakpoint()
                                {
                                    RemoteFile = remotePath,
                                    TranscodedFile = localPath,
                                    TransferrdBytes = total,
                                    Ip = _ip,
                                    Port = _port,
                                    Uid = _userName,
                                    Pwd = _password,
                                    SaveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                };

                                string ftpBreakpointFile = Path.Combine(Environment.CurrentDirectory, Resources.FtpBreakpointRecord);
                                using (FileStream breakpointFileStream = new FileStream(ftpBreakpointFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    XmlSerializer xs = new XmlSerializer(typeof(FtpBreakpoint));
                                    xs.Serialize(breakpointFileStream, ftpBreakpoint);
                                }
                                break;
                                //save file md5 and total transferred bytes
                            }

                            writer.Write(buffer, 0, size);

                            total += size;

                            double divide = total;
                            double divided = fs.Length;

                            double percent = divide / divided;
                            //Console.WriteLine("*********************    " + percent + "    1**************************");

                            percent = Math.Round(percent, 2);

                            uint uintPercent = (uint)(percent * 100);

                            //Console.WriteLine("*********************    " + total + "/" + fs.Length + "     2*********************");


                            //Console.WriteLine("*********************    " + percent + "    3**************************");
                            //Console.WriteLine("*********************    " + uintPercent + "    4**************************");

                            if (uintPercent == 100 && (total != fs.Length))
                            {
                                uintPercent = 99;
                            }

                            OnUploadProgress(new FtpProgressEventArgs(uintPercent));


                            // 同时记录断点续传位置
                        }

                        writer.Flush();

                        writer.Close();
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(string.Format("【FtpUpload({0},{1})】 exception：{2}", localPath, remotePath, ex));
            }

            return true;
        }

        private void OnUploadProgress(FtpProgressEventArgs args)
        {
            UploadProgress?.Invoke(this, args);
        }

        public void StartSendLiveStatusAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    BmsResult bmsResult = await BmsService.Instance.SendLiveStatus();
                    Console.WriteLine(bmsResult.data);
                    Log.Logger.Information(bmsResult.data.ToString());

                    Thread.Sleep(60000);
                }
            });
        }
    }

}
