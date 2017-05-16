using Prism.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace UploadClient
{
    public class UploadManager:BindableBase
    {
        public UploadManager()
        {
            TranscodeUploadOperation = Operations.StartTranscoding;
            ShowProgressingStatus = Visibility.Hidden;
        }

        private bool canSelectFile;
        public bool CanSelectFile
        {
            get { return canSelectFile; }
            set { SetProperty(ref canSelectFile, value); }
        }

        private string curDisplayId;
        public string CurDisplayId
        {
            get { return curDisplayId; }
            set { SetProperty(ref curDisplayId, value); }
        }

        private string curLessonName;
        public string CurLessonName
        {
            get { return curLessonName; }
            set { SetProperty(ref curLessonName, value); }
        }

        private string curLessonId;
        public string CurLessonId
        {
            get { return curLessonId; }
            set { SetProperty(ref curLessonId, value); }
        }

        private ICommand selectFileCommand;
        public ICommand SelectFileCommand
        {
            get { return selectFileCommand; }
            set { SetProperty(ref selectFileCommand, value); }
        }

        private ICommand transcodeUploadCommand;
        public ICommand TranscodeUploadCommand
        {
            get { return transcodeUploadCommand; }
            set { SetProperty(ref transcodeUploadCommand, value); }
        }

        //private bool canSelectFile;
        //public bool CanSelectFile
        //{
        //    get { return canSelectFile; }
        //    set { SetProperty(ref canSelectFile, value); }
        //}

        private bool canTranscodeUpload;
        public bool CanTranscodeUpload
        {
            get { return canTranscodeUpload; }
            set { SetProperty(ref canTranscodeUpload, value); }
        }

        //private string selectFileOperation;
        //public string SelectFileOperation
        //{
        //    get { return selectFileOperation; }
        //    set { SetProperty(ref selectFileOperation, value); }
        //}

        private string transcodeUploadOperation;
        public string TranscodeUploadOperation
        {
            get { return transcodeUploadOperation; }
            set { SetProperty(ref transcodeUploadOperation, value); }
        }

        private Visibility showProgressingStatus;
        public Visibility ShowProgressingStatus
        {
            get { return showProgressingStatus; }
            set { SetProperty(ref showProgressingStatus, value); }
        }

        //private bool canOperate;
        //public bool CanOperate
        //{
        //    get { return canOperate; }
        //    set { SetProperty(ref canOperate, value); }
        //}

        private double progressValue;
        public double ProgressValue
        {
            get { return progressValue; }
            set { SetProperty(ref progressValue, value); }
        }

        private string progressPercentage;
        public string ProgressPercentage
        {
            get { return progressPercentage; }
            set { SetProperty(ref progressPercentage, value); }
        }

        private string timer;
        public string Timer
        {
            get { return timer; }
            set { SetProperty(ref timer, value); }
        }

        //public void SetStatus(UploadStatus uploadStatus, ICommand selectFileCommand, ICommand transcodeUploadCommand)
        //{
        //    switch (uploadStatus)
        //    {
        //        case UploadStatus.FileNotSelected:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = false;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.FileSelected:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.StartTranscoding:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StopTranscoding;

        //            Timer = "00:00:00";
        //            break;
        //        case UploadStatus.StopTranscoding:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.StartUploading:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StopUploading;
        //            Timer = "00:00:00";
        //            break;
        //        case UploadStatus.StopUploading:
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartUploading;
        //            break;
        //        default:
        //            break;
        //    }
        //}

        public CancellationTokenSource TimerCancellationTokenSource { get; set; }
        public CancellationTokenSource UploadCancellationTokenSource { get; set; }

        public void StartTimerAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1000);

                    DateTime display;

                    if (DateTime.TryParse(Timer, out display))
                    {
                        Timer = Convert.ToDateTime(Timer).AddSeconds(1).ToString("HH:mm:ss");
                    }
                    Console.WriteLine(Timer);
                }
            });
        }

    }
}
