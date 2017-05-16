namespace UploadClient
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsAutoLogin { get; set; }

        public void CloneUserInfo(UserInfo newUserInfo)
        {
            UserName = newUserInfo.UserName;
            Password = newUserInfo.Password;
            IsAutoLogin = newUserInfo.IsAutoLogin;
        }
    }
}
