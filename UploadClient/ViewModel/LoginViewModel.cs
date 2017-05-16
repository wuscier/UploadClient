using Prism.Commands;
using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace UploadClient
{
    public class LoginViewModel : ViewModelBase
    {
        public LoginViewModel(Window loginView)
        {
            _loginView = loginView;
            _loginView.Closing += _loginView_Closing;

            LoginCommand = DelegateCommand.FromAsyncHandler(LoginAsync);
            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
        }

        //private fields
        private Window _loginView;
        private ConfigResult _ConfigResult;

        //public fields
        public bool IsLoginSucceeded;

        //properties
        private string userName;
        public string UserName
        {
            get { return userName; }
            set
            {
                SetProperty(ref userName, value);
            }
        }
        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                SetProperty(ref password, value);
            }
        }
        private bool isLoginEnabled = true;
        public bool IsLoginEnabled
        {
            get { return isLoginEnabled; }
            set { SetProperty(ref isLoginEnabled, value); }
        }
        private bool showProgressBar;
        public bool ShowProgressBar
        {
            get { return showProgressBar; }
            set { SetProperty(ref showProgressBar, value); }
        }
        private bool rememberMe;
        public bool RememberMe
        {
            get { return rememberMe; }
            set
            {
                if (SetProperty(ref rememberMe, value) && !value)
                {
                    AutoLogin = false;
                }
            }
        }
        private bool autoLogin;
        public bool AutoLogin
        {
            get { return autoLogin; }
            set
            {
                if (SetProperty(ref autoLogin, value) && value)
                {
                    RememberMe = true;
                }
            }
        }

        //commands
        public ICommand LoginCommand { get; set; }
        public ICommand LoadCommand { get; set; }

        //command handlers
        private async Task LoginAsync()
        {
            ResetStatus();

            if (!ValidateInputs())
            {
                return;
            }

            await Login();
            await WriteCachedFlagsAsync();
        }
        private async Task LoadAsync()
        {
            //load local setting
            await ReadCachedFlags();
            if (AutoLogin && !App.Logouted)
            {
                await Login();
            }
        }


        //event handlers
        private void _loginView_Closing(object sender, CancelEventArgs e)
        {
            if (IsLoginSucceeded)
            {
                MainView mainView = new MainView();
                mainView.Show();
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        //methods
        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                SnackBarMsg = Tips.Warning_UserNameEmpty;
                ShowSnackBar = true;
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                SnackBarMsg = Tips.Warning_PasswordEmpty;
                ShowSnackBar = true;
                return false;
            }

            return true;
        }
        private void ResetStatus()
        {
            ShowSnackBar = false;
            ShowProgressBar = false;
        }
        private void SetBeginLoginStatus()
        {
            IsLoginEnabled = false;
            ShowProgressBar = true;
        }
        private void SetEndLoginStatus()
        {
            IsLoginEnabled = true;
            ShowProgressBar = false;
        }
        private async Task Login()
        {
            SetBeginLoginStatus();

            try
            {
                if (string.IsNullOrEmpty(App.ServerIp))
                {
                    ShowErrorMsg("-1", Tips.Error_ServerIpNotConfigured);
                    return;
                }

                BmsResult bmsResult = await BmsService.Instance.ApplyForToken(UserName, Password);

                if (ShowErrorMsg(bmsResult.status, bmsResult.message)) { return; }

                App.UserName = UserName;

                BmsService.Instance.AccessToken = bmsResult.data.ToString();

                await _loginView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsLoginSucceeded = true;

                    _loginView.Close();
                }));
            }
            catch (Exception ex)
            {
                ShowErrorMsg("-1", ex.InnerException == null ? ex.Message.Replace("\r\n", "") : ex.InnerException.Message.Replace("\r\n", ""));
            }
            finally
            {
                SetEndLoginStatus();
            }
        }

        private async Task ReadCachedFlags()
        {
            await Task.Run(() =>
            {
                string configResultPath = Path.Combine(Environment.CurrentDirectory, "ConfigResult.xml");
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigResult));

                if (File.Exists(configResultPath))
                {
                    try
                    {
                        using (Stream stream = new FileStream(configResultPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            _ConfigResult = xmlSerializer.Deserialize(stream) as ConfigResult;
                        }

                        UserName = _ConfigResult.UserInfo.UserName;
                        Password = _ConfigResult.UserInfo.Password;
                        AutoLogin = _ConfigResult.UserInfo.IsAutoLogin;
                        RememberMe = !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password);

                        string uploadMode = _ConfigResult.UploadMode;

                        App.UploadMode = (!string.IsNullOrEmpty(uploadMode) && (uploadMode.ToLower() == UploadPattern.Http || uploadMode.ToLower() == UploadPattern.Ftp)) ? uploadMode : UploadPattern.Http;
                        App.ServerIp = _ConfigResult.ServerIp;
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMsg("-1", ex.Message);
                        Log.Logger.Error(string.Format("【read ConfigResult.xml】 exception：{0}", ex));
                    }
                }
                else
                {
                    _ConfigResult = new ConfigResult();
                    //using (FileStream fs = File.Create(configResultPath))
                    //{

                    //}

                    try
                    {
                        using (Stream stream = new FileStream(configResultPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            xmlSerializer.Serialize(stream, _ConfigResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMsg("-1", ex.Message);
                        Log.Logger.Error(string.Format("【create ConfigResult.xml】 exception：{0}", ex));
                    }
                }

                //If config result does not exist, assume that user has never logined before. Will do nothing in this case.
            });
        }
        private async Task WriteCachedFlagsAsync()
        {
            await Task.Run(() =>
            {
                //AutoLogin: cache account, password and autologin flag
                //RememberMe: just cache account and password

                string configResultPath = Path.Combine(Environment.CurrentDirectory, "ConfigResult.xml");
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigResult));


                if (_ConfigResult == null)
                {
                    _ConfigResult = new ConfigResult();
                }

                _ConfigResult.UserInfo.IsAutoLogin = AutoLogin;
                if (RememberMe)
                {
                    _ConfigResult.UserInfo.UserName = UserName;
                    _ConfigResult.UserInfo.Password = Password;
                }

                try
                {
                    using (Stream stream = new FileStream(configResultPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        xmlSerializer.Serialize(stream, _ConfigResult);
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorMsg("-1", ex.Message);
                    Log.Logger.Error(string.Format("【write ConfigResult.xml】 exception：{0}", ex));
                }
            });
        }
    }
}
