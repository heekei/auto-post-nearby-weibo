using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.StubHelpers;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.Script.Serialization;
using System.Configuration;
using System.Reflection;
using System.Text.RegularExpressions;

namespace nsuZhonglou
{
    public partial class Default : System.Web.UI.Page
    {

        const string access_token = "2.00XoGYyC0fjnxi67e82f9350ft9dQE";
        const string appsecret = "a9e232881d466ec9a10d638e26b227e9";
        const string appkey = "664411323";
        string singleWeiboUrl = "http://api.weibo.com/2/statuses/go?access_token=2.00XoGYyC0fjnxi67e82f9350ft9dQE&id=";
        string getImageUrl = "http%3A%2F%2Fupload.api.weibo.com%2F2%2Fmss%2Fmsget%3Faccess_token%3D2.00XoGYyC0fjnxi67e82f9350ft9dQE%26fid%3D";
        string json;
        string errCode;
        string errorStr;
        protected void Page_Load(object sender, EventArgs e)
        {
            string postStr = "";
            Valid();
            //if (Request.HttpMethod.ToLower() == "post")
            //{
            Stream s = System.Web.HttpContext.Current.Request.InputStream;
            byte[] b = new byte[s.Length];
            s.Read(b, 0, (int)s.Length);
            postStr = Encoding.UTF8.GetString(b);
            if (!string.IsNullOrEmpty(postStr))
            {
                ResponseMsg(postStr);
            }

            //}
            //oauth.GetAccessTokenByRefreshToken(access_token);
            //string token = oauth.AccessToken.ToString();
            //WriteLog( oauth.VerifierAccessToken().ToString());
        }
        private bool CheckSignature()
        {
            string signature = Request.QueryString["signature"];
            string timestamp = Request.QueryString["timestamp"];
            string nonce = Request.QueryString["nonce"];
            string[] ArrTmp = { appsecret, timestamp, nonce };
            Array.Sort(ArrTmp);     //字典排序
            string tmpStr = string.Join("", ArrTmp);
            //SHA1 sha = new SHA1CryptoServiceProvider(); 
            //byte[] data = new byte[tmpStr.Length];
            //byte[] result;
            tmpStr = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(tmpStr, "SHA1");
            //result = sha.ComputeHash(data);
            //tmpStr = result.ToString();
            tmpStr = tmpStr.ToLower();
            if (tmpStr == signature)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 验证
        /// </summary>
        private void Valid()
        {
            string echoStr = Request.QueryString["echostr"];
            if (CheckSignature())
            {
                if (!string.IsNullOrEmpty(echoStr))
                {
                    Response.Write(echoStr);
                    //Response.End();
                }
            }
        }
        /// <summary>
        /// 发布文字微博
        /// </summary>
        /// <param name="content">微博内容</param>
        /// <returns></returns>
        private bool postWeibo(string content)
        {
            //string[] keyWords = new string{"#东软钟楼#","@" };
            //Regex regex = new Regex("((?<=^#东软钟楼#).*)|((?<=^@).*)");
            //regex.Match(content).Value
            try
            {
                string str2 = System.Web.HttpUtility.UrlEncode("#东软钟楼#" + content, Encoding.UTF8);
                string url = "https://api.weibo.com/2/statuses/update.json";
                string data = "source=664411323&access_token=" + access_token + "&status=" + str2;
                json = HttpPost(url, data);//自动发布微博
                return true;
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
                return false;
            }

        }
        /// <summary>
        /// 发布图片微博
        /// </summary>
        /// <param name="content">文本</param>
        /// <param name="tovfid">图片ID</param>
        /// <param name="pic_ids">图片IDS</param>
        /// <returns></returns>
        private bool postPicWeibo(string content, string tovfid="",string pic_ids="")
        {
            try
            {
                content = HttpUtility.UrlEncode(content, Encoding.GetEncoding("UTF-8"));
                string str = "source=664411323&access_token=" + access_token + "&status=" + content + "&url=" + getImageUrl + tovfid;
                if (pic_ids != "")
                {
                    str= "source=664411323&access_token=" + access_token + "&status=" + content + "&pic_id=" + pic_ids;
                }
                json = HttpPost("https://api.weibo.com/2/statuses/upload_url_text.json", str);
                return true;
            }
            catch(Exception e)
            {
                WriteLog(e.ToString());
                return false;
            }
        }

        private void resMsg(string zhonglou_id, string user_id, string MsgType, string content)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            returnObject rOb = new returnObject();
            rOb.result = true;
            rOb.sender_id = zhonglou_id.Trim().ToString();
            rOb.receiver_id = user_id.Trim().ToString();
            rOb.type = MsgType;
            try
            {
                byte[] utf8 = Encoding.UTF8.GetBytes("{\"text\":\"" + content + "\"}");
                rOb.data = Encoding.UTF8.GetString(utf8);
                string result = serializer.Serialize(rOb);
                Response.Write(result);

            }
            catch(Exception e)
            {
                WriteLog(e.ToString());
            }
        }
        /// <summary>
        /// 返回信息结果(信息返回)
        /// </summary>
        /// <param name="weiboJson"></param>
        private void ResponseMsg(string weiboJson)
        {
            //string result = "";
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Rootobject wbjsonObj = (Rootobject)serializer.Deserialize(weiboJson, typeof(Rootobject));
            string userStr = HttpGet("https://api.weibo.com/2/users/show.json", "source=664411323&access_token=2.00XoGYyC0fjnxi67e82f9350ft9dQE&uid=" + wbjsonObj.sender_id.ToString());
            UserInfo userinfo = (UserInfo)serializer.Deserialize(userStr, typeof(UserInfo));
            if (wbjsonObj.type == "text")//文本消息
            {
                string result;
                bool haskey;
                IsKeyword(wbjsonObj.text, out haskey, out result);
                if (haskey)//是否包含AutoPost关键字
                {
                //是，自动发布
                    if (postWeibo(result))
                    {
                        //发布成功后，自动回复
                        resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "你的秘密已发布~请查看~");
                    }
                    else
                    {
                        //发布失败后，自动回复
                        resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "可能是服务器死机咯~请稍后再试");
                    }

                }//否，判断是否为AutoUpdate关键字
                else if (wbjsonObj.text == "更新")//其他文字的回复
                {
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", get_poi_timeline());
                }
                else if (wbjsonObj.text=="昵称")
                {
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", userinfo.screen_name);
                }
                else
                {
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "使用方式：@加上你想说的话；或者：#东软钟楼#加上你想说的话。");
                }
                //Response.Write(result);
            }
            else if (wbjsonObj.type == "image")
            {
                //if (wbjsonObj.text.Length >= 6 && wbjsonObj.text.Substring(0, 6) == "#东软钟楼#")//#东软钟楼#关键字自动发布
                //{
                if (postPicWeibo("#东软钟楼#" + wbjsonObj.text, wbjsonObj.data.tovfid.ToString()))
                {
                    //发布成功后，自动回复
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "你的秘密已发布~请查看~");
                }
                else
                {
                    //发布失败后，自动回复
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "可能是服务器死机咯~请稍后再试");
                }

                //}
            }
            else if (wbjsonObj.type == "event")
            {
                if (wbjsonObj.data.subtype == "follow")
                {
                    resMsg(wbjsonObj.receiver_id.ToString(), wbjsonObj.sender_id.ToString(), "text", "感谢你陪伴钟楼一起成长，把你的青春烙印在钟楼上。回复：@加上你想说的话，会将它自动发布在钟楼上，伴随东软一起成长");
                }
            }
            //Response.Write(result);
            
            WriteLog("@" + userinfo.screen_name + "：" + wbjsonObj.sender_id + "：" + wbjsonObj.type + "：" + wbjsonObj.text + "：" + wbjsonObj.data.tovfid.ToString() + "：" + wbjsonObj.created_at);
        }

        public string get_poi_timeline()
        {
            try
            {
                string since_id = readSince_id();
                string poiid = "B2094757D06AA1FE469D";
                string str = HttpGet("https://api.weibo.com/2/place/poi_timeline.json", "source=664411323&access_token=2.00XoGYyC0fjnxi67e82f9350ft9dQE&poiid="+poiid+"&since_id=" + since_id + "&count=50&page=1");
                if (str.Length <= 2)
                {
                    WriteLog("null");
                    return "最近没有什么新的微博可以更新的";
                }
                else
                {
                    JavaScriptSerializer seri = new JavaScriptSerializer();
                    poi_timeline timeline = (poi_timeline)seri.Deserialize(str, typeof(poi_timeline));
                    WriteLog(since_id);
                    bool multi;
                    if (timeline.statuses.Length > 1)
                        multi = true;
                    else
                        multi = false;
                    writeSince_id(timeline.states.First().id);
                    int n=0;int f = 0;
                    foreach (poi_timeline.Status c in timeline.statuses)
                    {
                        string pic_ids = "";
                        //pic_ids = c.pic_ids[0];
                        for (int len = 0; len < c.pic_ids.Length; len++)
                        {
                            pic_ids += c.pic_ids[len] + ",";
                        }
                        if (postPicWeibo("#东软钟楼#" + c.text, pic_ids: pic_ids))
                            n++;
                        else
                            f++;
                        WriteLog(c.id + "：" + c.user.screen_name + "：" + c.text + "<br />图片数量：" + c.pic_ids.Length.ToString());
                    }
                    return "发布成功"+ n.ToString() + "条，失败"+f.ToString()+"条，请查看。";
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
                return "error:" + e.ToString();
            }

        }
        public string readSince_id()
        {
            FileStream file = new FileStream(Server.MapPath("/weibo/shudong/since_id.txt"), FileMode.Open);
            byte[] c = new byte[file.Length];
            file.Read(c, 0, c.Length);
            string result = Encoding.Default.GetString(c);
            file.Close();
            return result;
        }
        public void writeSince_id(string since_id)
        {
            string filename = Server.MapPath("/weibo/shudong/since_id.txt");
            StreamWriter sr = null;
            try
            {
                if (!File.Exists(filename))
                {
                    sr = File.CreateText(filename);
                }
                else
                {
                    string alltext = since_id;
                    File.WriteAllText(filename, alltext);
                }
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }
        }

        /// <summary>  
        /// POST请求与获取结果  
        /// </summary>  
        public string HttpPost(string Url, string postDataStr)
        {
            ServicePointManager.DefaultConnectionLimit = 120;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postDataStr.Length;
            StreamWriter writer = new StreamWriter(request.GetRequestStream(), Encoding.ASCII);
            writer.Write(postDataStr);
            writer.Flush();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string encoding = response.ContentEncoding;
            if (encoding == null || encoding.Length < 1)
            {
                encoding = "UTF-8"; //默认编码  
            }
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
            //string retString = reader.ReadToEnd();
            string retString =reader.ReadToEnd();
            return retString;
        }
        /// <summary>  
        /// GET请求与获取结果  
        /// </summary>  
        public string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
        
        /// <summary>
        /// 响应消息结构
        /// </summary>
        public class returnObject
        {

            public bool result { get; set; }
            public string sender_id { get; set; }
            public string receiver_id { get; set; }
            public string type { get; set; }
            public string data { get; set; }

        }

        /// <summary>
        /// 常规消息结构
        /// </summary>
        public class Rootobject
        {
            public string text { get; set; }
            public string type { get; set; }
            public Int64 receiver_id { get; set; }
            public Int64 sender_id { get; set; }
            public string created_at { get; set; }
            public Data data { get; set; }
            public class Data
            {
                public long vfid { get; set; }
                public long tovfid { get; set; }
                public string subtype { get; set; }
            }
        }

        /// <summary>
        /// 用户信息结构
        /// </summary>
        public class UserInfo
        {
            public long id { get; set; }
            public string idstr { get; set; }
            public int _class { get; set; }
            public string screen_name { get; set; }
            public string name { get; set; }
            public string province { get; set; }
            public string city { get; set; }
            public string location { get; set; }
            public string description { get; set; }
            public string url { get; set; }
            public string profile_image_url { get; set; }
            public string cover_image_phone { get; set; }
            public string profile_url { get; set; }
            public string domain { get; set; }
            public string weihao { get; set; }
            public string gender { get; set; }
            public int followers_count { get; set; }
            public int friends_count { get; set; }
            public int pagefriends_count { get; set; }
            public int statuses_count { get; set; }
            public int favourites_count { get; set; }
            public string created_at { get; set; }
            public bool following { get; set; }
            public bool allow_all_act_msg { get; set; }
            public bool geo_enabled { get; set; }
            public bool verified { get; set; }
            public int verified_type { get; set; }
            public string remark { get; set; }
            public Status status { get; set; }
            public int ptype { get; set; }
            public bool allow_all_comment { get; set; }
            public string avatar_large { get; set; }
            public string avatar_hd { get; set; }
            public string verified_reason { get; set; }
            public string verified_trade { get; set; }
            public string verified_reason_url { get; set; }
            public string verified_source { get; set; }
            public string verified_source_url { get; set; }
            public int verified_state { get; set; }
            public int verified_level { get; set; }
            public string verified_reason_modified { get; set; }
            public string verified_contact_name { get; set; }
            public string verified_contact_email { get; set; }
            public string verified_contact_mobile { get; set; }
            public bool follow_me { get; set; }
            public int online_status { get; set; }
            public int bi_followers_count { get; set; }
            public string lang { get; set; }
            public int star { get; set; }
            public int mbtype { get; set; }
            public int mbrank { get; set; }
            public int block_word { get; set; }
            public int block_app { get; set; }
            public int credit_score { get; set; }
            public int user_ability { get; set; }
            public int urank { get; set; }
            public class Status
            {
            }
        }

        /// <summary>
        /// 微博发布后返回结构
        /// </summary>
        public class returnPosted
        {
            public string created_at { get; set; }
            public long id { get; set; }
            public string mid { get; set; }
            public string idstr { get; set; }
            public string text { get; set; }
            public string source { get; set; }
            public bool favorited { get; set; }
            public bool truncated { get; set; }
            public string in_reply_to_status_id { get; set; }
            public string in_reply_to_user_id { get; set; }
            public string in_reply_to_screen_name { get; set; }
            public Geo geo { get; set; }
            public User user { get; set; }
            public Annotation[] annotations { get; set; }
            public int reposts_count { get; set; }
            public int comments_count { get; set; }
            public int attitudes_count { get; set; }
            public int mlevel { get; set; }
            public Visible visible { get; set; }
            //地理信息字段
            public class Geo
            {
                public string type { get; set; }
                public float[] coordinates { get; set; }
            }
            //微博作者的用户信息字段
            public class User
            {
                public long id { get; set; }
                public string idstr { get; set; }
                public string screen_name { get; set; }
                public string name { get; set; }
                public string province { get; set; }
                public string city { get; set; }
                public string location { get; set; }
                public string description { get; set; }
                public string url { get; set; }
                public string profile_image_url { get; set; }
                public string profile_url { get; set; }
                public string domain { get; set; }
                public string weihao { get; set; }
                public string gender { get; set; }
                public int followers_count { get; set; }
                public int friends_count { get; set; }
                public int statuses_count { get; set; }
                public int favourites_count { get; set; }
                public string created_at { get; set; }
                public bool following { get; set; }
                public bool allow_all_act_msg { get; set; }
                public bool geo_enabled { get; set; }
                public bool verified { get; set; }
                public int verified_type { get; set; }
                public bool allow_all_comment { get; set; }
                public string avatar_large { get; set; }
                public string verified_reason { get; set; }
                public bool follow_me { get; set; }
                public int online_status { get; set; }
                public int bi_followers_count { get; set; }
                public string lang { get; set; }
                public int level { get; set; }
                public int type { get; set; }
                public int ulevel { get; set; }
                public Badge badge { get; set; }
                public class Badge
                {
                    public Kuainv kuainv { get; set; }
                    public int uc_domain { get; set; }
                    public int enterprise { get; set; }
                    public int anniversary { get; set; }
                    public class Kuainv
                    {
                        public int level { get; set; }
                    }
                }
            }
            //微博的可见性及指定可见分组信息。
            public class Visible
            {
                public int type { get; set; }
                public long list_id { get; set; }
            }

            public class Annotation
            {
                public string aa { get; set; }
            }
        }


        /// <summary>
        /// 某个位置地点的动态的结构
        /// </summary>
        public class poi_timeline
        {
            public Status[] statuses { get; set; }
            public int total_number { get; set; }
            public State[] states { get; set; }
            public class Status
            {
                public string created_at { get; set; }
                public long id { get; set; }
                public string mid { get; set; }
                public string idstr { get; set; }
                public string text { get; set; }
                public int textLength { get; set; }
                public int source_allowclick { get; set; }
                public int source_type { get; set; }
                public string source { get; set; }
                public bool favorited { get; set; }
                public bool truncated { get; set; }
                public string in_reply_to_status_id { get; set; }
                public string in_reply_to_user_id { get; set; }
                public string in_reply_to_screen_name { get; set; }
                public string[] pic_ids { get; set; }
                public string thumbnail_pic { get; set; }
                public string bmiddle_pic { get; set; }
                public string original_pic { get; set; }
                public Geo geo { get; set; }
                public User user { get; set; }
                public Annotation[] annotations { get; set; }
                public int reposts_count { get; set; }
                public int comments_count { get; set; }
                public int attitudes_count { get; set; }
                public bool isLongText { get; set; }
                public int mlevel { get; set; }
                public Visible visible { get; set; }
                public int[] biz_ids { get; set; }
                public long biz_feature { get; set; }
                public Url_Objects[] url_objects { get; set; }
                public object[] darwin_tags { get; set; }
                public object[] hot_weibo_tags { get; set; }
                public string rid { get; set; }
                public int userType { get; set; }
                public class Geo
                {
                    public string type { get; set; }
                    public float[] coordinates { get; set; }
                }

                public class User
                {
                    public long id { get; set; }
                    public string idstr { get; set; }
                    public int _class { get; set; }
                    public string screen_name { get; set; }
                    public string name { get; set; }
                    public string province { get; set; }
                    public string city { get; set; }
                    public string location { get; set; }
                    public string description { get; set; }
                    public string url { get; set; }
                    public string profile_image_url { get; set; }
                    public string profile_url { get; set; }
                    public string domain { get; set; }
                    public string weihao { get; set; }
                    public string gender { get; set; }
                    public int followers_count { get; set; }
                    public int friends_count { get; set; }
                    public int pagefriends_count { get; set; }
                    public int statuses_count { get; set; }
                    public int favourites_count { get; set; }
                    public string created_at { get; set; }
                    public bool following { get; set; }
                    public bool allow_all_act_msg { get; set; }
                    public bool geo_enabled { get; set; }
                    public bool verified { get; set; }
                    public int verified_type { get; set; }
                    public string remark { get; set; }
                    public int ptype { get; set; }
                    public bool allow_all_comment { get; set; }
                    public string avatar_large { get; set; }
                    public string avatar_hd { get; set; }
                    public string verified_reason { get; set; }
                    public string verified_trade { get; set; }
                    public string verified_reason_url { get; set; }
                    public string verified_source { get; set; }
                    public string verified_source_url { get; set; }
                    public bool follow_me { get; set; }
                    public int online_status { get; set; }
                    public int bi_followers_count { get; set; }
                    public string lang { get; set; }
                    public int star { get; set; }
                    public int mbtype { get; set; }
                    public int mbrank { get; set; }
                    public int block_word { get; set; }
                    public int block_app { get; set; }
                    public int level { get; set; }
                    public int type { get; set; }
                    public int ulevel { get; set; }
                    public Badge badge { get; set; }
                    public string badge_top { get; set; }
                    public int has_ability_tag { get; set; }
                    public Extend extend { get; set; }
                    public int credit_score { get; set; }
                    public int user_ability { get; set; }
                    public int urank { get; set; }
                    public string cover_image_phone { get; set; }
                }

                public class Badge
                {
                    public int uc_domain { get; set; }
                    public int enterprise { get; set; }
                    public int anniversary { get; set; }
                    public int taobao { get; set; }
                    public int travel2013 { get; set; }
                    public int gongyi { get; set; }
                    public int gongyi_level { get; set; }
                    public int bind_taobao { get; set; }
                    public int hongbao_2014 { get; set; }
                    public int suishoupai_2014 { get; set; }
                    public int dailv { get; set; }
                    public int zongyiji { get; set; }
                    public int vip_activity1 { get; set; }
                    public int hongbao_2016 { get; set; }
                    public int unread_pool { get; set; }
                    public int daiyan { get; set; }
                    public int ali_1688 { get; set; }
                }

                public class Extend
                {
                    public Privacy privacy { get; set; }
                    public string mbprivilege { get; set; }
                }

                public class Privacy
                {
                    public int mobile { get; set; }
                }

                public class Visible
                {
                    public int type { get; set; }
                    public int list_id { get; set; }
                }

                public class Annotation
                {
                    public Place place { get; set; }
                    public string client_mblogid { get; set; }
                    public bool mapi_request { get; set; }
                    public int shooting { get; set; }
                }

                public class Place
                {
                    public float lon { get; set; }
                    public string poiid { get; set; }
                    public string title { get; set; }
                    public string type { get; set; }
                    public float lat { get; set; }
                }

                public class Url_Objects
                {
                    public string url_ori { get; set; }
                    public string object_id { get; set; }
                    public Info info { get; set; }
                    public Object _object { get; set; }
                    public int like_count { get; set; }
                    public int follower_count { get; set; }
                    public int asso_like_count { get; set; }
                    public bool card_info_un_integrity { get; set; }
                    public class Info
                    {
                        public string url_short { get; set; }
                        public string url_long { get; set; }
                        public int type { get; set; }
                        public bool result { get; set; }
                        public string title { get; set; }
                        public string description { get; set; }
                        public int last_modified { get; set; }
                        public int transcode { get; set; }
                    }
                    public class Object
                    {
                        public string object_id { get; set; }
                        public string containerid { get; set; }
                        public string object_domain_id { get; set; }
                        public string object_type { get; set; }
                        public int safe_status { get; set; }
                        public string show_status { get; set; }
                        public string act_status { get; set; }
                        public string last_modified { get; set; }
                        public long timestamp { get; set; }
                        public long uuid { get; set; }
                        public string activate_status { get; set; }
                        public Object1 _object { get; set; }
                        public Action[] actions { get; set; }
                        public class Object1
                        {
                            public string object_type { get; set; }
                            public string id { get; set; }
                            public string summary { get; set; }
                            public string position { get; set; }
                            public string display_name { get; set; }
                            public string checkin_num { get; set; }
                            public Address address { get; set; }
                            public string keyword { get; set; }
                            public string[] action { get; set; }
                            public Image image { get; set; }
                            public string target_url { get; set; }
                            public string url { get; set; }
                            public Biz biz { get; set; }
                            public Mobile mobile { get; set; }
                            public class Address
                            {
                                public string region { get; set; }
                                public string formatted { get; set; }
                                public string fax { get; set; }
                                public string email { get; set; }
                                public string postal_code { get; set; }
                                public string locality { get; set; }
                                public string street_address { get; set; }
                                public string telephone { get; set; }
                                public string country { get; set; }
                            }

                            public class Image
                            {
                                public string height { get; set; }
                                public string width { get; set; }
                                public string url { get; set; }
                            }

                            public class Biz
                            {
                                public string biz_id { get; set; }
                                public string containerid { get; set; }
                            }

                            public class Mobile
                            {
                                public Card card { get; set; }
                                public string page_id { get; set; }
                                public Url url { get; set; }
                                public class Card
                                {
                                    public string scheme { get; set; }
                                    public string[] contents { get; set; }
                                    public int status { get; set; }
                                    public int is_asyn { get; set; }
                                    public string pic { get; set; }
                                    public int type { get; set; }
                                }

                                public class Url
                                {
                                    public string scheme { get; set; }
                                    public int status { get; set; }
                                    public string name { get; set; }
                                }

                            }
                        }
                        public class Action
                        {
                            public string name { get; set; }
                            public Params _params { get; set; }
                            public string pic { get; set; }
                            public string type { get; set; }
                            public class Params
                            {
                                public string uid { get; set; }
                                public string type { get; set; }
                            }

                        }
                    }
                }
            }
            public class State
            {
                public string id { get; set; }
                public int state { get; set; }
            }
        }

        /// <summary>
        /// 错误返回值格式
        /// </summary>
        public class errorCode
        {
            public string request { get; set; }
            public string error_code { get; set; }
            public string error { get; set; }
        }

        //public class updateParam
        //{
        //    public string source { get; set; }
        //    public string acc_token = access_token;
        //    public string status { get; set; }
        //    public string visible { get; set; }
        //    public string list_id { get; set; }
        //    public string lat { get; set; }
        //    public string lon { get; set; }
        //    public string annotations { get; set; }
        //    public string rip { get; set; }
        //}
        /// <summary>
        /// 写日志(用于跟踪)
        /// </summary>
        /// <param name="strMemo"></param>
        public void WriteLog(string strMemo)
        {
            string filename = Server.MapPath("/weibo/shudong/log.html");
            //if (!Directory.Exists(Server.MapPath("//logs//")))
            //    Directory.CreateDirectory("//logs//");
            StreamWriter sr = null;
            try
            {
                if (!File.Exists(filename))
                {
                    sr = File.CreateText(filename);
                }
                else
                {
                    //sr = File.AppendText(filename);
                    string alltext = strMemo + "<br /><br />" + File.ReadAllText(filename);
                    File.WriteAllText(filename, alltext);
                }
                //string alltext = strMemo + File.ReadAllText(filename);
                //File.WriteAllText(filename, alltext);
                //sr.WriteLine(strMemo);
                //sr.WriteLine("\n");

            }
            catch
            {
            }
            finally
            {
                if (sr != null)
                    sr.Close();
            }
        }

        /// <summary>
        /// 判断是否包含关键字
        /// </summary>
        /// <param name="content">原文</param>
        /// <param name="HasKeys">是否包含关键字</param>
        /// <param name="result">截取关键字后的内容</param>
        public void IsKeyword(string content, out bool HasKeys, out string result)
        {
            Regex regex = new Regex("((?<=^#东软钟楼#).*)|((?<=^@).*)");
            result = regex.Match(content).Value;
            if (result != "")
                HasKeys = true;
            else
                HasKeys = false;
        }
    }
}