namespace UploadClient
{
    public class FtpBreakpoint
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Uid { get; set; }
        public string Pwd { get; set; }
        public string TranscodedFile { get; set; }
        public string RemoteFile { get; set; }
        public long TransferrdBytes { get; set; }
        public string SaveTimestamp { get; set; }
    }
}
