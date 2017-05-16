using System;

namespace UploadClient
{
    public class FtpProgressEventArgs : EventArgs
    {
        private uint percentage;
        public FtpProgressEventArgs(uint percentage)
        {
            this.percentage = percentage;
        }

        public uint Percentage { get { return percentage; } }
    }
}
