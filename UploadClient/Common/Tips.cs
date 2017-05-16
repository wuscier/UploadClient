namespace UploadClient
{
    public static class Tips
    {
        public const string Error_UploadFileNotExisted = "上传文件不存在！";
        public const string Error_ServerIpNotConfigured = "服务器地址未配置！";
        public const string Info_PleaseSelectLesson = "无";
        public const string Info_FoundLocalOutputFile = "本地存在缓存转码文件，将直接上传！";

        public const string Warning_UserNameEmpty = "用户名不能为空！";
        public const string Warning_PasswordEmpty = "密码不能为空！";
        public const string Warning_AuthenticationFailure = "用户名或密码错误！";

        public const string Error_FailedToOpenTranscoder = "开启转码器失败！";
        public const string Error_FailedToTranscode = "转码失败！";
        public const string Error_FailedToStopTranscode = "停止转码失败！";
    }
}
