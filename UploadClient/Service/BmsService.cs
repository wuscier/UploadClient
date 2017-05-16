using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace UploadClient
{
    public class BmsService
    {
        private BmsService() { }

        private static object _syncRoot = new object();

        private static volatile BmsService _instance;
        public static BmsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new BmsService();
                        }
                    }
                }

                return _instance;
            }
        }

        public string AccessToken { get; set; }

        public async Task<BmsResult> ApplyForToken(string userName, string password)
        {
            JObject json = new JObject();
            json.Add("UserName", userName);
            json.Add("Password", password);

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            BmsResult bmsResult = await Request("/api/Token/RequestToken", content);

            Log.Logger.Information(string.Format("【RequestToken】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status, bmsResult.message));

            return bmsResult;
        }

        public async Task<BmsResult> GetCourses()
        {
            BmsResult bmsResult = await Request("/api/Teach/GetCourses");

            Log.Logger.Information(string.Format("【GetCourses】 data：{0}, status：{1}, message：{2}", bmsResult.data, bmsResult.status, bmsResult.message));


            if (bmsResult.data != null)
            {
                JObject data = bmsResult.data as JObject;

                string jsonItems = data.SelectToken("items").ToString();
                List<LessonInfo> lessons = JsonConvert.DeserializeObject<List<LessonInfo>>(jsonItems);

                bmsResult.data = lessons;
            }

            return bmsResult;
        }

        public async Task<BmsResult> GetActivityInfo()
        {
            BmsResult bmsResult = await Request("/api/Teach/GetActivity");

            if (bmsResult.data != null)
            {
                ActivityInfo activityInfo = JsonConvert.DeserializeObject<ActivityInfo>(bmsResult.data.ToString());

                bmsResult.data = activityInfo;
            }

            Log.Logger.Information(string.Format("【GetActivity】 data：{0}, status：{1}, message：{2}", bmsResult.data, bmsResult.status, bmsResult.message));

            return bmsResult;
        }

        public async Task<BmsResult> UpdateCourseStatus(string lessonId)
        {
            JObject json = new JObject();
            JArray jarray = new JArray();

            JObject jsonCollection = new JObject();
            jsonCollection.Add("name", "PreProcessStage");
            jsonCollection.Add("value", 2);

            jarray.Add(jsonCollection);

            json.Add("id", lessonId);
            json.Add("properties", jarray);

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            BmsResult bmsResult = await Request("/api/Teach/UpdateCourse", content);

            Log.Logger.Information(string.Format("【UpdateCourse】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status, bmsResult.message));
            return bmsResult;
        }

        public async Task<BmsResult> CreateCourseMedia(string lessonId, string guid)
        {
            JObject json = new JObject();

            JArray jarray = new JArray();
            jarray.Add(guid);

            json.Add("id", lessonId);
            json.Add("mediaId", jarray);

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            BmsResult bmsResult = await Request("/api/Teach/CreateCourseMedia", content);

            Log.Logger.Information(string.Format("【CreateCourseMedia】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status,bmsResult.message));

            return bmsResult;
        }

        public async Task<BmsResult> SendLiveStatus()
        {
            JObject json = new JObject();

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            BmsResult bmsResult = await Request("api/ftp/ActiveUpload");

            Log.Logger.Information(string.Format("【ActiveUpload】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status, bmsResult.message));
            return bmsResult;
        }

        public async Task<BmsResult> FinishFtpUpload(string md5)
        {
            JObject json = new JObject();
            json.Add("md5", md5);

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            BmsResult bmsResult = await Request("/api/ftp/NotifyUpload", content);

            if (bmsResult.data!=null)
            {
                JObject jObj = bmsResult.data as JObject;

                string fileId = jObj.SelectToken("fileId").ToString();

                bmsResult.data = fileId;
            }
            Log.Logger.Information(string.Format("【NotifyUpload】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status, bmsResult.message));


            return bmsResult;
        }

        public async Task<BmsResult> StartFtpUpload(string fileName,long length, string md5)
        {
            JObject json = new JObject();
            json.Add("fileName", fileName);
            json.Add("length", length);
            json.Add("md5", md5);

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            BmsResult bmsResult = await Request("/api/ftp/RequestUpload", content);

            if (bmsResult.data != null)
            {

                JObject data = bmsResult.data as JObject;

                FtpItem ftpItem = JsonConvert.DeserializeObject<FtpItem>(data.ToString());

                bmsResult.data = ftpItem;
            }

            Log.Logger.Information(string.Format("【RequestUpload】 post：{0}, data：{1}, status：{2}, message：{3}", json, bmsResult.data, bmsResult.status, bmsResult.message));

            return bmsResult;
        }

        public async Task<BmsResult> Request(string url, HttpContent content = null)
        {
            BmsResult bmsResult = new BmsResult();

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(App.ServerIp);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                var response = content == null ? await httpClient.GetAsync(url) : await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    object obj = JsonConvert.DeserializeObject(result, typeof(BmsResult));
                    bmsResult = obj as BmsResult;
                }
                else
                {
                    bmsResult.message = response.ReasonPhrase;
                    bmsResult.status = response.StatusCode.ToString();
                }

                return bmsResult;
            }
        }
    }
}
