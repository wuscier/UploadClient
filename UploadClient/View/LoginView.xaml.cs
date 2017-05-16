namespace UploadClient
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView
    {
        public LoginView()
        {
            InitializeComponent();
            DataContext = new LoginViewModel(this);
        }
    }
}
