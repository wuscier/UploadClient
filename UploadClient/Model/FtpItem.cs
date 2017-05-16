namespace UploadClient
{
    public class FtpItem
    {
        public string ip { get; set; }

        public string port { get; set; }

        public string uid { get; set; }

        public string pwd { get; set; }

        public string path { get; set; }

        public long transferredBytes { get; set; }
    }
}
