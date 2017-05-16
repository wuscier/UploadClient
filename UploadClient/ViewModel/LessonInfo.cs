using Prism.Mvvm;
using System.Windows;
using System.Windows.Input;

namespace UploadClient
{
    public class LessonInfo:BindableBase
    {
        public LessonInfo()
        {
            Status = UploadLiteralStatus.FileNotSelected;
            UploadStatus = UploadStatus.FileNotSelected;
            CanOperate = true;
            SelectedForeground = "Black";
            SelectedBackground = "Transparent";
        }


        public string Id { get; set; }
        public string DisplayCourseId { get; set; }
        public string ActivityId { get; set; }
        public string CreateTime { get; set; }
        public string CourseName { get; set; }
        public string RemoteFile { get; set; }

        private string status;
        public string Status
        {
            get { return status; }
            set { SetProperty(ref status, value); }
        }

        public UploadStatus UploadStatus { get; set; }

        private ICommand selectedCommand;
        public ICommand SelectedCommand
        {
            get { return selectedCommand; }
            set { SetProperty(ref selectedCommand, value); }
        }


        private string selectedForeground;
        public string SelectedForeground
        {
            get
            {
                return selectedForeground;
            }

            set
            {
                SetProperty(ref selectedForeground, value);
            }
        }
        private string selectedBackground;
        public string SelectedBackground
        {
            get
            {
                return selectedBackground;
            }

            set
            {
                SetProperty(ref selectedBackground, value);
            }
        }

        //private ICommand selectFileCommand;
        //public ICommand SelectFileCommand
        //{
        //    get { return selectFileCommand; }
        //    set { SetProperty(ref selectFileCommand, value); }
        //}

        //private bool canSelectFile;
        //public bool CanSelectFile
        //{
        //    get { return canSelectFile; }
        //    set { SetProperty(ref canSelectFile, value); }
        //}


        //private ICommand transcodeUploadCommand;
        //public ICommand TranscodeUploadCommand
        //{
        //    get { return transcodeUploadCommand; }
        //    set { SetProperty(ref transcodeUploadCommand, value); }
        //}

        //private bool canTranscodeUpload;
        //public bool CanTranscodeUpload
        //{
        //    get { return canTranscodeUpload; }
        //    set { SetProperty(ref canTranscodeUpload, value); }
        //}

        //private string selectFileOperation;
        //public string SelectFileOperation
        //{
        //    get { return selectFileOperation; }
        //    set { SetProperty(ref selectFileOperation, value); }
        //}

        //private string transcodeUploadOperation;
        //public string TranscodeUploadOperation
        //{
        //    get { return transcodeUploadOperation; }
        //    set { SetProperty(ref transcodeUploadOperation, value); }
        //}

        //private Visibility showProgressingStatus;
        //public Visibility ShowProgressingStatus
        //{
        //    get { return showProgressingStatus; }
        //    set { SetProperty(ref showProgressingStatus, value); }
        //}

        private bool canOperate;
        public bool CanOperate
        {
            get { return canOperate; }
            set { SetProperty(ref canOperate, value); }
        }

        //private double progressValue;
        //public double ProgressValue
        //{
        //    get { return progressValue; }
        //    set { SetProperty(ref progressValue, value); }
        //}

        //private string progressPercentage;
        //public string ProgressPercentage
        //{
        //    get { return progressPercentage; }
        //    set { SetProperty(ref progressPercentage, value); }
        //}


        //private string timer;
        //public string Timer
        //{
        //    get { return timer; }
        //    set { SetProperty(ref timer, value); }
        //}

        private string sourceFile;
        public string SourceFile
        {
            get { return sourceFile; }
            set { SetProperty(ref sourceFile, value); }
        }

        private string outputFile;
        public string OutputFile
        {
            get { return outputFile; }
            set { SetProperty(ref outputFile, value); }
        }


        private string payFlag;
        public string PayFlag
        {
            get { return payFlag; }
            set { SetProperty(ref payFlag, value); }
        }

        private string reportFlag;
        public string ReportFlag
        {
            get { return reportFlag; }
            set { SetProperty(ref reportFlag, value); }
        }

        //public void SetStatus(UploadStatus uploadStatus, ICommand selectFileCommand, ICommand transcodeUploadCommand)
        //{
        //    switch (uploadStatus)
        //    {
        //        case UploadStatus.FileNotSelected:
        //            CanSelectFile = true;
        //            Status = UploadLiteralStatus.FileNotSelected;
        //            SelectFileOperation = Operations.SelectFile;
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = false;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.FileSelected:
        //            CanSelectFile = true;
        //            Status = UploadLiteralStatus.FileSelected;
        //            SelectFileOperation = Operations.SelectFile;
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.StartTranscoding:
        //            CanSelectFile = false;
        //            Status = UploadLiteralStatus.Transcoding;
        //            SelectFileOperation = Operations.SelectFile;
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StopTranscoding;

        //            Timer = "00:00:00";
        //            break;
        //        case UploadStatus.StopTranscoding:
        //            CanSelectFile = true;
        //            Status = UploadLiteralStatus.FileSelected;
        //            SelectFileOperation = Operations.SelectFile;
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StartTranscoding;
        //            break;
        //        case UploadStatus.StartUploading:
        //            CanSelectFile = false;
        //            Status = UploadLiteralStatus.Uploading;
        //            SelectFileOperation = Operations.SelectFile;
        //            SelectFileCommand = selectFileCommand;
        //            CanTranscodeUpload = true;
        //            ShowProgressingStatus = Visibility.Visible;
        //            TranscodeUploadCommand = transcodeUploadCommand;
        //            TranscodeUploadOperation = Operations.StopUploading;
        //            Timer = "00:00:00";
        //            break;
        //        case UploadStatus.StopUploading:
        //            CanSelectFile = true;
        //            Status = UploadLiteralStatus.FileSelected;
        //            SelectFileOperation = Operations.SelectFile;
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

    }
}