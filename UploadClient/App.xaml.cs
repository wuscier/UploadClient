using Serilog;
using System.Threading;
using System.Windows;

namespace UploadClient
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public static bool Logouted = false;
        public static  FtpUploader FtpUploader;
        public static string UploadMode;
        public static string UserName;
        public static string ServerIp;

        private static Mutex _mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            UploadMode = UploadPattern.Http;
            bool isNewInstance = false;

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.RollingFile("LOG\\log-{Date}.txt").CreateLogger();

            _mutex = new Mutex(true, "UploadClient", out isNewInstance);

            if (!isNewInstance)
            {
                MessageBox.Show("程序已经在运行中！");
                Current.Shutdown();
            }
        }
    }
}
