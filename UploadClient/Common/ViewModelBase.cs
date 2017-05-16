using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Input;

namespace UploadClient
{
    public class ViewModelBase : BindableBase
    {
        protected ViewModelBase()
        {
            HideSnackBarCommand = new DelegateCommand(() => { ShowSnackBar = false; });
        }

        private bool showSnackBar;
        public bool ShowSnackBar
        {
            get { return showSnackBar; }
            set { SetProperty(ref showSnackBar, value); }
        }

        private string snackBarMsg;
        public string SnackBarMsg
        {
            get { return snackBarMsg; }
            set { SetProperty(ref snackBarMsg, value); }
        }

        public ICommand HideSnackBarCommand { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <returns>返回true表示有错误，返回falseb表示成功</returns>
        protected bool ShowErrorMsg(string status, string msg)
        {
            if (status != "0")
            {
                SnackBarMsg = msg;
                if (!ShowSnackBar) { ShowSnackBar = true; }
                return true;
            }

            return false;
        }
    }
}
