using System.Collections.Generic;

namespace UploadClient
{
    public class UploadItem
    {
        public string Id { get; set; }
        public string CourseName { get; set; }
        public string SourceFile { get; set; }
        public string OutPutFile { get; set; }
        public string RemoteFile { get; set; }
    }

    public class UploadItemsResult
    {
        public UploadItemsResult()
        {
            UploadItems = new List<UploadItem>();
        }

        public List<UploadItem> UploadItems { get; set; }
    }
}
