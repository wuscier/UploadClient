namespace UploadClient
{
    public class ConfigResult
    {
        public UserInfo UserInfo { get; set; }

        public string UploadMode { get; set; }

        public string ServerIp { get; set; }

        public ConfigResult()
        {
            UserInfo = new UserInfo()
            {
            };

            UploadMode = UploadPattern.Http;
            ServerIp = string.Empty;
        }
    }
}
