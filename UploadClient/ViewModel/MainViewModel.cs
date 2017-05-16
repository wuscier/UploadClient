using Prism.Commands;
using System.Windows.Input;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Linq;
using System.IO;
using UploadClient.Properties;
using System.Xml.Serialization;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using Serilog;
using System.Collections.Generic;
using UploaderClient;
using System.Windows;

namespace UploadClient
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(MainView mainView)
        {
            _mainView = mainView;

            Lessons = new ObservableCollection<LessonInfo>();
            UploadItemsResult = new UploadItemsResult();
            UploadConfig.Instance.Endpoint = App.ServerIp;
            UploadManager = new UploadManager() { CanSelectFile = false, CurLessonName = Tips.Info_PleaseSelectLesson };

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            SelectFileCommand = DelegateCommand.FromAsyncHandler(SelectFileAsync);
            TranscodeCommand = DelegateCommand.FromAsyncHandler(TranscodeAsync);
            UploadCommand = DelegateCommand.FromAsyncHandler(UploadAsync);
            StopTranscodeCommand = DelegateCommand.FromAsyncHandler(StopTranscodeAsync);
            StopUploadCommand = DelegateCommand.FromAsyncHandler(StopUploadAsync);

            LogoutCommand = DelegateCommand.FromAsyncHandler(LogoutAsync);
            RefreshCommand = DelegateCommand.FromAsyncHandler(RefreshAsync);
        }

        //private void _mainView_Closed(object sender, EventArgs e)
        //{
        //    //_sendingStatus = false;
        //    //_counting = false;
        //}

        //private fields
        //private bool _counting;
        //private bool _sendingStatus;
        private MainView _mainView;

        //properties
        public ObservableCollection<LessonInfo> Lessons { get; set; }
        public UploadItemsResult UploadItemsResult { get; set; }
        public string OutputFileFolder
        {
            get
            {
                string outputFileFolder = Path.Combine(Environment.CurrentDirectory, Resources.TranscodingFolder);

                if (!Directory.Exists(outputFileFolder))
                {
                    Directory.CreateDirectory(outputFileFolder);
                }

                return outputFileFolder;
            }
        }

        //private SessionInfo sessionInfo;
        //public SessionInfo SessionInfo
        //{
        //    get { return sessionInfo; }
        //    set
        //    {
        //        if (!value.Equals(sessionInfo))
        //        {
        //            sessionInfo = value;
        //            SessionDetails = string.Format("用户：{0}, 上传方式：{1}", sessionInfo.UserName, sessionInfo.UploadMode);
        //        }
        //    }
        //}

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }
        private string uploadMode;
        public string UploadMode
        {
            get { return uploadMode; }
            set { SetProperty(ref uploadMode, value); }
        }

        private UploadManager uploadManager;
        public UploadManager UploadManager
        {
            get { return uploadManager; }
            set { SetProperty(ref uploadManager, value); }
        }

        //commands
        public ICommand LoadCommand { get; set; }
        public ICommand SelectFileCommand { get; set; }
        public ICommand TranscodeCommand { get; set; }
        public ICommand UploadCommand { get; set; }
        public ICommand StopTranscodeCommand { get; set; }
        public ICommand StopUploadCommand { get; set; }
        public ICommand SelectedCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        //command actions
        private async Task LoadAsync()
        {
            if (!App.Logouted)
            {
                //_mainView.Closed += _mainView_Closed;

                try
                {
                    int result = Transcoder.OpenTranscoder();
                    Log.Logger.Information(string.Format("【OpenTranscoder】 returns：{0}", result));

                    if (ShowErrorMsg(result.ToString(), Tips.Error_FailedToOpenTranscoder)) { return; }
                }
                catch (Exception ex)
                {
                    ShowErrorMsg("-1", ex.InnerException == null ? ex.Message.Replace("\r\n", "") : ex.InnerException.Message.Replace("\r\n", ""));
                }
            }

            UserName = App.UserName;
            UploadMode = App.UploadMode;

            //BmsResult bmsResult = await BmsService.Instance.GetActivityInfo();

            //ShowErrorMsg(bmsResult.status, bmsResult.message);

            //ActivityInfo = bmsResult.data as ActivityInfo;

            await GetLessonListAsync();

        }
        private async Task SelectFileAsync()
        {
            await Task.Run(() =>
            {
                // many dialogs can be opened in different threads!!!
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = Resources.VideoFormats;
                bool? result = openFileDialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    var lesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();
                    lesson.SourceFile = openFileDialog.FileName;

                    UpdateUploadItemsXmlAsync(lesson);
                    lesson.Status = UploadLiteralStatus.FileSelected;
                    lesson.UploadStatus = UploadStatus.FileSelected;
                    SetStatus(lesson);
                }
            });
        }
        private async Task TranscodeAsync()
        {
            await Task.Run(async () =>
            {
                var lesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

                string cachedTranscodedFile = CheckTranscodingFile(lesson);

                if (!string.IsNullOrEmpty(cachedTranscodedFile))
                {
                    SnackBarMsg = Tips.Info_FoundLocalOutputFile;
                    ShowSnackBar = true;

                    lesson.OutputFile = cachedTranscodedFile;
                    lesson.UploadStatus = UploadStatus.Transcoded;
                    lesson.Status = UploadLiteralStatus.Transcoded;

                    SetStatus(lesson);

                    //upload automatically

                    await UploadAsync();

                    //there is already an output file associated with source file
                    return;
                }


                try
                {
                    int result = Transcoder.Transcode(lesson.SourceFile, OutputFileFolder, int.Parse(lesson.Id));

                    Log.Logger.Information(string.Format("【Transcode】 returns：{0}", result));

                    if (ShowErrorMsg(result.ToString(), Tips.Error_FailedToTranscode))
                    {
                        return;
                    }

                    lesson.Status = UploadLiteralStatus.Transcoding;
                    lesson.UploadStatus = UploadStatus.StartTranscoding;
                    SetStatus(lesson);
                    SetOtherLessonsCanOperateFlag(false);
                    GetTranscodingProgress(lesson);

                }
                catch (Exception ex)
                {
                    ShowErrorMsg("-1", ex.Message);
                    Log.Logger.Error(string.Format("【Transcode】 exception：{0}", ex));
                }
            });
        }
        private async Task StopTranscodeAsync()
        {
            await Task.Run(() =>
            {
                var lesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

                SetOtherLessonsCanOperateFlag(true);

                int result = Transcoder.StopTranscode();

                Log.Logger.Information(string.Format("【StopTranscode】 returns：{0}", result));

                if (ShowErrorMsg(result.ToString(), Tips.Error_FailedToStopTranscode)) { return; }

                lesson.UploadStatus = UploadStatus.FileSelected;
                lesson.Status = UploadLiteralStatus.FileSelected;
                SetStatus(lesson);
            });
        }
        private async Task UploadAsync()
        {
            var lesson = Lessons.Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

            lesson.UploadStatus = UploadStatus.StartUploading;
            lesson.Status = UploadLiteralStatus.Uploading;
            SetStatus(lesson);

            SetOtherLessonsCanOperateFlag(false);

            //Thread thread = new Thread(new ThreadStart(CountUploadTime));
            //thread.IsBackground = true;

            UploadManager.TimerCancellationTokenSource = new CancellationTokenSource();

            BmsResult bmsResult;
            switch (App.UploadMode)
            {
                case UploadPattern.Ftp:
                    object[] fileInfos = GetFileInfos(lesson.OutputFile);
                    string fileName = Path.GetFileName(lesson.OutputFile);

                    FtpItem ftpItem = null;
                    string ftpBreakpointFile = Path.Combine(Environment.CurrentDirectory, Resources.FtpBreakpointRecord);
                    if (File.Exists(ftpBreakpointFile))
                    {
                        using (FileStream ftpBreakpointFileStream = new FileStream(ftpBreakpointFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            XmlSerializer xs = new XmlSerializer(typeof(FtpBreakpoint));

                            FtpBreakpoint ftpBreakpoint = xs.Deserialize(ftpBreakpointFileStream) as FtpBreakpoint;

                            DateTime saveTime = DateTime.Parse(ftpBreakpoint.SaveTimestamp);
                            double timeSpanMinutes = DateTime.Now.Subtract(saveTime).TotalMinutes;

                            if (timeSpanMinutes < 10 && ftpBreakpoint.TranscodedFile == lesson.OutputFile)
                            {
                                //breakpoint doesn't timeout
                                ftpItem = new FtpItem();
                                ftpItem.transferredBytes = ftpBreakpoint.TransferrdBytes;
                                ftpItem.path = ftpBreakpoint.RemoteFile;
                                ftpItem.ip = ftpBreakpoint.Ip;
                                ftpItem.port = ftpBreakpoint.Port.ToString();
                                ftpItem.uid = ftpBreakpoint.Uid;
                                ftpItem.pwd = ftpBreakpoint.Pwd;
                            }

                        }
                    }

                    if (ftpItem == null)
                    {
                        bmsResult = await BmsService.Instance.StartFtpUpload(fileName, (long)fileInfos[1], fileInfos[0].ToString());

                        if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

                        ftpItem = bmsResult.data as FtpItem;
                    }

                    App.FtpUploader = new FtpUploader(ftpItem.ip, int.Parse(ftpItem.port), ftpItem.uid, ftpItem.pwd);

                    App.FtpUploader.UploadProgress = new EventHandler<FtpProgressEventArgs>(FtpUploader_UploadProgress);
                    //thread.Start();

                    //Thread sendLiveThread = new Thread(new ThreadStart(SendLiveStatus));
                    //sendLiveThread.IsBackground = true;
                    //sendLiveThread.Start();


                    UploadManager.StartTimerAsync(UploadManager.TimerCancellationTokenSource.Token);

                    App.FtpUploader.SendLiveStatusCTS = new CancellationTokenSource();
                    App.FtpUploader.StartSendLiveStatusAsync(App.FtpUploader.SendLiveStatusCTS.Token);

                    App.FtpUploader.UploadCTS = new CancellationTokenSource();
                    bool result = await App.FtpUploader.Upload(lesson.OutputFile, ftpItem.path, App.FtpUploader.UploadCTS.Token, ftpItem.transferredBytes);


                    break;



                case UploadPattern.Http:
                default:

                    if (!File.Exists(lesson.OutputFile))
                    {
                        ShowErrorMsg("-1", Tips.Error_UploadFileNotExisted);
                        return;
                    }

                    Uploader uploader = new Uploader(lesson.OutputFile, BmsService.Instance.AccessToken, true);
                    uploader.UploadProgress = new EventHandler<UploadProgressEventArgs>(GetHttpUoloadProgress);

                    //thread.Start();

                    UploadManager.StartTimerAsync(UploadManager.TimerCancellationTokenSource.Token);
                    UploadManager.UploadCancellationTokenSource = new CancellationTokenSource();

                    UploadResult uploadResult = await uploader.Upload(UploadManager.UploadCancellationTokenSource.Token);


                    if (uploadResult.Success)
                    {
                        bmsResult = await BmsService.Instance.CreateCourseMedia(UploadManager.CurLessonId, uploadResult.FileId);

                        if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

                        bmsResult = await BmsService.Instance.UpdateCourseStatus(UploadManager.CurLessonId);

                        if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

                        await RefreshAsync();
                    }
                    else
                    {
                        ShowErrorMsg("-1", uploadResult.Error.Message);
                        Log.Logger.Error(string.Format("【http upload】 exception：{0},{1}", uploadResult.Error, uploadResult.Error.Message));
                        var curLesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

                        Log.Logger.Information(string.Format("【http upload】 lessonId：{0}, diaplayId：{1}, lessonName：{2}, sourceFile：{3}, outputFile：{4}", curLesson.Id, curLesson.DisplayCourseId, curLesson.CourseName, curLesson.SourceFile, curLesson.OutputFile));

                        if (!(uploadResult.Error is TaskCanceledException))
                        {
                            await RefreshAsync();
                        }
                    }
                    break;
            }
        }

        private async Task StopUploadAsync()
        {
            await Task.Run(() =>
            {

                try
                {
                    switch (App.UploadMode)
                    {
                        case UploadPattern.Ftp:
                            App.FtpUploader.UploadCTS.Cancel();
                            break;
                        case UploadPattern.Http:
                            UploadManager.UploadCancellationTokenSource.Cancel();
                            break;
                    }

                    UploadManager.TimerCancellationTokenSource.Cancel();

                    var lesson = Lessons.Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

                    lesson.UploadStatus = UploadStatus.Transcoded;
                    lesson.Status = UploadLiteralStatus.Transcoded;
                    SetStatus(lesson);

                    SetOtherLessonsCanOperateFlag(true);

                }
                catch (Exception ex)
                {
                    ShowErrorMsg("-1", ex.Message);
                }
            });
        }
        private async Task SelectLessonAsync(string lessonId)
        {
            var lesson = Lessons.ToList().Where(l => l.Id == lessonId).FirstOrDefault();
            SetSelectedBackground(lessonId);
            await UpdateUploadManager(lesson);
        }
        private async Task RefreshAsync()
        {
            UploadManager = new UploadManager() { CanSelectFile = false, CurLessonName = Tips.Info_PleaseSelectLesson };

            await GetLessonListAsync();

            string uploadJsonFile = Path.Combine(Environment.CurrentDirectory, "upload.json");
            if (File.Exists(uploadJsonFile))
            {
                try
                {
                    File.Delete(uploadJsonFile);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(string.Format("【delete upload.json】 exception：{0}", ex));
                    ShowErrorMsg("-1", ex.Message);
                }
            }
        }
        private async Task LogoutAsync()
        {
            await _mainView.Dispatcher.BeginInvoke(new Action(() =>
            {
                LoginView loginView = new LoginView();

                loginView.Show();

                App.Logouted = true;
                _mainView.Close();
            }));

        }

        //methods
        private async Task GetLessonListAsync()
        {
            BmsResult bmsResult = await BmsService.Instance.GetCourses();

            if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

            List<LessonInfo> lessons = bmsResult.data as List<LessonInfo>;

            await App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CloneLessons(lessons);
            }));
        }

        private void CloneLessons(List<LessonInfo> lessons)
        {
            Lessons.Clear();

            lessons.ForEach(lesson =>
            {
                //lesson.SetStatus(UploadStatus.FileNotSelected, SelectFileCommand, TranscodeCommand);

                lesson.SelectedCommand = DelegateCommand<string>.FromAsyncHandler(SelectLessonAsync);
                Lessons.Add(lesson);
            });
        }

        private void UpdateUploadItemsXmlAsync(LessonInfo newLessonInfo)
        {
            try
            {
                string uploadItemsXmlPath = Path.Combine(Environment.CurrentDirectory, Resources.UploadList);


                if (File.Exists(uploadItemsXmlPath))
                {
                    using (Stream stream = new FileStream(uploadItemsXmlPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(UploadItemsResult));
                        UploadItemsResult = xs.Deserialize(stream) as UploadItemsResult;
                    }
                }
                else
                {
                    UploadItemsResult.UploadItems.Clear();
                }

                var targetUploadItem = UploadItemsResult.UploadItems.Where(uploaditem => uploaditem.Id == newLessonInfo.Id).FirstOrDefault();

                if (targetUploadItem == null)
                {
                    UploadItemsResult.UploadItems.Add(new UploadItem()
                    {
                        Id = newLessonInfo.Id,
                        CourseName = newLessonInfo.CourseName,
                        OutPutFile = newLessonInfo.OutputFile,
                        RemoteFile = newLessonInfo.RemoteFile,
                        SourceFile = newLessonInfo.SourceFile,
                    });
                }
                else
                {
                    targetUploadItem.OutPutFile = newLessonInfo.OutputFile;
                    targetUploadItem.RemoteFile = newLessonInfo.RemoteFile;
                    targetUploadItem.SourceFile = newLessonInfo.SourceFile;
                }


                using (Stream stream = new FileStream(uploadItemsXmlPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(UploadItemsResult));
                    xs.Serialize(stream, UploadItemsResult);
                }
                //newLessonInfo.SetStatus(UploadStatus.FileSelected, SelectFileCommand, TranscodeCommand);
            }
            catch (Exception ex)
            {
                //newLessonInfo.SetStatus(UploadStatus.FileNotSelected, SelectFileCommand, TranscodeCommand);
            }
        }
        private void SetOtherLessonsCanOperateFlag(bool canOperate)
        {
            var lessons = Lessons.ToList().Where(l => l.Id != UploadManager.CurLessonId);
            lessons.ToList().ForEach(l => l.CanOperate = canOperate);
        }
        private unsafe void GetTranscodingProgress(LessonInfo lesson)
        {
            int progress, status;

            while (true)
            {
                if (UploadManager.TranscodeUploadOperation != Operations.StopTranscoding)
                {
                    break;
                }

                Transcoder.GetState(&progress, &status);

                UploadManager.ProgressValue = progress;
                UploadManager.ProgressPercentage = string.Format("{0}%", progress);

                if (status == 1)
                {
                    lesson.UploadStatus = UploadStatus.Transcoded;
                    lesson.Status = UploadLiteralStatus.Transcoded;
                    SetStatus(lesson);
                    SetOtherLessonsCanOperateFlag(true);

                    StringBuilder outputName = new StringBuilder(1024);
                    Transcoder.GetOutputFileName(outputName);

                    RenameOutputFile(lesson, outputName.ToString());

                    UpdateUploadItemsXmlAsync(lesson);

                    StartATaskDelegate autoUploadDelegate = new StartATaskDelegate(UploadAsync);

                    autoUploadDelegate();

                    break;
                }

                if (status == -1)
                {
                    ShowErrorMsg(status.ToString(), Tips.Error_FailedToTranscode);
                    break;
                }

                Thread.Sleep(1000);
                //Console.WriteLine("**************************************   GetTranscodingProgress *******************************************");

                DateTime timer;
                if (!DateTime.TryParse(UploadManager.Timer, out timer))
                {
                    break;
                }
                else
                {
                    UploadManager.Timer = Convert.ToDateTime(UploadManager.Timer).AddSeconds(1).ToString("HH:mm:ss");
                }
            }
        }


        private static string ByteArrayToHexString(byte[] bytes)
        {
            int length = bytes.Length;

            StringBuilder sb = new StringBuilder();
            foreach (var data in bytes)
            {
                sb.Append(data.ToString("x2"));
            }

            return sb.ToString();
        }

        private void RenameOutputFile(LessonInfo lesson, string fileName)
        {
            DirectoryInfo di = new DirectoryInfo(OutputFileFolder);
            FileInfo[] files = di.GetFiles();

            FileInfo fi = files.Where(file => file.Name == fileName).FirstOrDefault();

            string hasdCode = FileHelper.GetFileHash(lesson.SourceFile);

            string newFileName = Path.Combine(OutputFileFolder, string.Format("{0}{1}", hasdCode, fi.Extension));

            fi.MoveTo(newFileName);
            lesson.OutputFile = newFileName;
        }

        private string CheckTranscodingFile(LessonInfo lesson)
        {
            string cachedFile = string.Empty;
            try
            {
                string sourceFileHashCode = FileHelper.GetFileHash(lesson.SourceFile);

                DirectoryInfo di = new DirectoryInfo(OutputFileFolder);

                var files = di.GetFiles();

                var fileInfo = files.Where(file => file.Name.Contains(sourceFileHashCode)).FirstOrDefault();

                cachedFile =  fileInfo == null ? string.Empty : fileInfo.FullName;
            }
            catch (Exception ex)
            {
                ShowErrorMsg("-1", ex.Message);
                Log.Logger.Error(string.Format("【check cached transcoded file】 exception：{0}", ex));
            }

            return cachedFile;
        }

        private async Task UpdateUploadManager(LessonInfo lesson)
        {
            await Task.Run(() =>
            {
                UploadManager.CurDisplayId = lesson.DisplayCourseId;
                UploadManager.CurLessonId = lesson.Id;
                UploadManager.CurLessonName = lesson.CourseName;

                SetStatus(lesson);
            });
        }

        private void SetSelectedBackground(string lessonId)
        {
            Lessons.ToList().ForEach(lesson =>
            {
                if (lesson.Id == lessonId)
                {
                    lesson.SelectedBackground = "#673AB7";
                    lesson.SelectedForeground = "White";
                }
                else
                {
                    lesson.SelectedForeground = "Black";
                    lesson.SelectedBackground = "Transparent";
                }
            });
        }

        private void SetStatus(LessonInfo lesson)
        {
            UploadManager.SelectFileCommand = SelectFileCommand;
            UploadManager.CanSelectFile = true;
            switch (lesson.UploadStatus)
            {

                case UploadStatus.FileNotSelected:
                    UploadManager.CanTranscodeUpload = false;
                    UploadManager.TranscodeUploadCommand = TranscodeCommand;
                    UploadManager.TranscodeUploadOperation = Operations.StartTranscoding;
                    UploadManager.ShowProgressingStatus = Visibility.Hidden;
                    UploadManager.ProgressPercentage = string.Empty;
                    UploadManager.ProgressValue = 0D;
                    UploadManager.Timer = string.Empty;
                    break;
                case UploadStatus.FileSelected:
                    
                    UploadManager.CanTranscodeUpload = true;
                    UploadManager.TranscodeUploadCommand = TranscodeCommand;
                    UploadManager.ShowProgressingStatus = Visibility.Hidden;
                    UploadManager.ProgressPercentage = string.Empty;
                    UploadManager.ProgressValue = 0D;
                    UploadManager.Timer = string.Empty;
                    UploadManager.TranscodeUploadOperation = Operations.StartTranscoding;
                    break;
                case UploadStatus.StartTranscoding:
                    
                    UploadManager.CanTranscodeUpload = true;
                    UploadManager.ShowProgressingStatus = Visibility.Visible;
                    UploadManager.TranscodeUploadCommand = StopTranscodeCommand;
                    UploadManager.TranscodeUploadOperation = Operations.StopTranscoding;

                    UploadManager.Timer = "00:00:00";
                    break;
                case UploadStatus.StopTranscoding:
                    
                    UploadManager.CanTranscodeUpload = true;
                    UploadManager.ShowProgressingStatus = Visibility.Visible;
                    UploadManager.TranscodeUploadCommand = TranscodeCommand;
                    UploadManager.TranscodeUploadOperation = Operations.StartTranscoding;
                    break;
                case UploadStatus.Transcoded:
                    
                    UploadManager.CanTranscodeUpload = true;
                    UploadManager.ShowProgressingStatus = Visibility.Hidden;
                    UploadManager.ProgressPercentage = string.Empty;
                    UploadManager.ProgressValue = 0D;
                    UploadManager.Timer = string.Empty;
                    UploadManager.TranscodeUploadCommand = UploadCommand;
                    UploadManager.TranscodeUploadOperation = Operations.StartUploading;
                    break;
                case UploadStatus.StartUploading:
                    
                    UploadManager.CanTranscodeUpload = true;
                    UploadManager.ShowProgressingStatus = Visibility.Visible;
                    UploadManager.TranscodeUploadCommand = StopUploadCommand;
                    UploadManager.TranscodeUploadOperation = Operations.StopUploading;
                    UploadManager.Timer = "00:00:00";
                    break;
                case UploadStatus.Finished:
                    UploadManager.ShowProgressingStatus = Visibility.Hidden;
                    UploadManager.ProgressPercentage = string.Empty;
                    UploadManager.ProgressValue = 0D;
                    UploadManager.Timer = string.Empty;

                    UploadManager.TranscodeUploadOperation = Operations.Finished;
                    UploadManager.CanTranscodeUpload = false;

                    break;
                default:
                    break;
            }
        }

        private void GetHttpUoloadProgress(object sender, UploadProgressEventArgs e)
        {
            uint percentage =  UpdateProgress(e.ProgressPercentage);

            if (percentage == 100)
            {
                UpdateUploadStatus();
            }
        }

        private uint UpdateProgress(long percent)
        {
            if (UploadManager.ShowProgressingStatus != Visibility.Visible)
            {
                UploadManager.ShowProgressingStatus = Visibility.Hidden;
                UploadManager.ProgressPercentage = string.Empty;
                UploadManager.ProgressValue = 0D;
                UploadManager.Timer = string.Empty;
            }

            var percentage = (uint)percent;
            percentage = percentage > 100 ? 100 : percentage;

            UploadManager.ProgressPercentage = string.Format("{0}%", percentage);
            UploadManager.ProgressValue = percentage;

            return percentage;
        }

        private void UpdateUploadStatus()
        {
            UploadManager.TimerCancellationTokenSource.Cancel();

            var lesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();
            lesson.Status = UploadLiteralStatus.Finished;
            lesson.UploadStatus = UploadStatus.Finished;

            SetStatus(lesson);
            SetOtherLessonsCanOperateFlag(true);

            if (App.UploadMode == UploadPattern.Ftp)
            {

                string ftpBreakpointFile = Path.Combine(Environment.CurrentDirectory, Resources.FtpBreakpointRecord);
                if (File.Exists(ftpBreakpointFile))
                {
                    bool deleteFile = false;
                    using (FileStream ftpBreakpointFileStream = new FileStream(ftpBreakpointFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(FtpBreakpoint));

                        FtpBreakpoint ftpBreakpoint = xs.Deserialize(ftpBreakpointFileStream) as FtpBreakpoint;

                        if (ftpBreakpoint.TranscodedFile == lesson.OutputFile)
                        {
                            deleteFile = true;
                        }

                    }
                    if (deleteFile)
                    {
                        try
                        {
                            File.Delete(ftpBreakpointFile);
                        }
                        catch (Exception ex)
                        {
                            ShowErrorMsg("-1", ex.Message);
                            Log.Logger.Error(string.Format("【delete FtpBreakpoint.xml】 exception：{0}", ex));
                        }
                    }
                }

                UpdateCourseStatusDelegate updateCourseDelegate = new UpdateCourseStatusDelegate(UpdateCourse);
                updateCourseDelegate(lesson.Id);
            }
        }

        private void FtpUploader_UploadProgress(object sender, FtpProgressEventArgs e)
        {
            uint percentage = UpdateProgress(e.Percentage);

            if (percentage == 100)
            {
                App.FtpUploader.SendLiveStatusCTS.Cancel();

                //_sendingStatus = false;
                UpdateUploadStatus();
            }
        }

        private delegate Task UpdateCourseStatusDelegate(string lessonId);

        private async Task UpdateCourse(string lessonId)
        {

            var lesson = Lessons.ToList().Where(l => l.Id == UploadManager.CurLessonId).FirstOrDefault();

            object[] fileInfos = GetFileInfos(lesson.OutputFile);

            BmsResult bmsResult = await BmsService.Instance.FinishFtpUpload(fileInfos[0].ToString());

            if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

            string fileId = bmsResult.data.ToString();

            bmsResult = await BmsService.Instance.CreateCourseMedia(lessonId, fileId);

            if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

            bmsResult = await BmsService.Instance.UpdateCourseStatus(lessonId);


            if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

            await RefreshAsync();
        }

        //private void CountUploadTime()
        //{
        //    _counting = true;
        //    while (_counting)
        //    {
        //        if (UploadManager.ProgressValue == 100D)
        //        {
        //            break;
        //        }

        //        if (UploadManager.TranscodeUploadOperation != Operations.StopUploading)
        //        {
        //            break;
        //        }
        //        Thread.Sleep(1000);
        //        DateTime timer;
        //        if (!DateTime.TryParse(UploadManager.Timer, out timer))
        //        {
        //            break;
        //        }
        //        else
        //        {
        //            UploadManager.Timer = Convert.ToDateTime(UploadManager.Timer).AddSeconds(1).ToString("HH:mm:ss");
        //        }
        //    }
        //}

        private delegate Task StartATaskDelegate();

        private async Task SendLiveStatusAsync()
        {
            BmsResult bmsResult = await BmsService.Instance.SendLiveStatus();

            ShowErrorMsg(bmsResult.status, bmsResult.message);
        }

        //private void SendLiveStatus()
        //{
        //    _sendingStatus = true;
        //    while (_sendingStatus)
        //    {
        //        Thread.Sleep(60000);

        //        StartATaskDelegate sendLiveDelegate = new StartATaskDelegate(SendLiveStatusAsync);

        //        sendLiveDelegate();
        //    }
        //}

        public object[] GetFileInfos(string fileName)
        {
            object[] fileInfos = new object[2];

            try
            {
                StringBuilder sb = new StringBuilder();

                using (FileStream file = new FileStream(fileName, FileMode.Open,FileAccess.Read,FileShare.Read))
                {
                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] retVal = md5.ComputeHash(file);


                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }

                    fileInfos[0] = sb.ToString();
                    fileInfos[1] = file.Length;

                    file.Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMsg("-1", ex.Message);
                Log.Logger.Error(string.Format("【get file md5 and length】 exception：{0}", ex));
            }

            return fileInfos;
        }
    }
}