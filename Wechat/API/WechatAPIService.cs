using System;
using System.Collections.Generic;
using System.Text;
using Wechat.API.RPC;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using Wechat.tools;
using System.Collections;
using System.Threading.Tasks;
using DotNet4.Utilities.UtilHttp;
namespace Wechat.API
{
    public class WechatAPIService
    {
		private HttpClient http;
        public WechatAPIService() {
            InitHttpClient();
        }

        private void InitHttpClient()
        {
			http = new HttpClient();
		}


        /// <summary>
        /// 获得二维码登录SessionID,使用此ID可以获得登录二维码
        /// </summary>
        public void GetNewQRLoginSessionID(Action<string>CallBack)
        {
			//respone like this => window.QRLogin.code = 200; window.QRLogin.uuid = "Qa_GBH_IqA==";

			http.GetHtml("https://login.wx2.qq.com/jslogin?appid=wx782c26e4c19acffb", referer: "https://wx2.qq.com/",callBack:(x)=> {
				var doc = x.response.DataString();
				var pairs = doc.Split(new string[] { "\"" }, StringSplitOptions.None);
				if (pairs.Length >= 2)
				{
					string sessionID = pairs[1];
					CallBack?.Invoke(sessionID);
				}
			});
        }

        /// <summary>
        /// 获得登录二维码图片
        /// </summary>
        /// <param name="QRLoginSessionID"></param>
        public void GetQRCodeImage(string QRLoginSessionID,Action<Image>CallBack)
        {
            string url = "https://login.weixin.qq.com/qrcode/" + QRLoginSessionID;
			//SetHttpHeader("Accept", "image/webp,image/*,*/*;q=0.8");
			http.GetHtml(url, referer: "https://wx2.qq.com/",callBack:(x)=> {
				if(x.Data!=null&& x.Data.Length > 0)
				{
					Image img = Image.FromStream(new MemoryStream(x.Data));
					CallBack?.Invoke(img);
				}
			});


        }

        /// <summary>
        /// 登录检查
        /// </summary>
        /// <param name="QRLoginSessionID"></param>
        public void Login(string QRLoginSessionID,long t,Action<LoginResult>CallBack)
        {
            string url = string.Format("https://login.wx2.qq.com/cgi-bin/mmwebwx-bin/login?loginicon=true&uuid={0}&tip={1}&r={2}&_={3}",
                QRLoginSessionID,"0",t/1579,t);

			http.GetHtml(url, referer: "https://wx.qq.com/",callBack:(x)=> {
				string login_result = x.response.DataString();
				if (login_result == null) CallBack?.Invoke(null);

				LoginResult result = new LoginResult
				{
					code = 408
				};
				if (login_result.Contains("window.code=201")) //已扫描 未登录
				{
					string base64_image = login_result.Split(new string[] { "\'" }, StringSplitOptions.None)[1].Split(',')[1];
					result.code = 201;
					result.UserAvatar = base64_image;
				}
				else if (login_result.Contains("window.code=200"))  //已扫描 已登录
				{
					string login_redirect_url = login_result.Split(new string[] { "\"" }, StringSplitOptions.None)[1];
					result.code = 200;
					result.redirect_uri = login_redirect_url;
				}
				CallBack?.Invoke(result);
			});
        }

        public void LoginRedirect(string redirect_uri,Action<LoginRedirectResult>CallBack=null)
        {
            string url = redirect_uri + "&fun=new&version=v2&lang=zh_CN";
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.GetHtml(url,referer: "https://wx2.qq.com/",callBack:(x)=> {
				var rep = x.response.DataString();
				string ret = rep.Split(new string[] { "ret" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
				if (ret != "0")
				{
					Console.WriteLine("登录失败:\n" + rep);
					InitHttpClient();
				}
				LoginRedirectResult result = new LoginRedirectResult
				{
					pass_ticket = rep.Split(new string[] { "pass_ticket" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
					skey = rep.Split(new string[] { "skey" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
					wxsid = rep.Split(new string[] { "wxsid" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
					wxuin = rep.Split(new string[] { "wxuin" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/'),
					isgrayscale = rep.Split(new string[] { "isgrayscale" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/')
				};
				CallBack?.Invoke(result);
			});
			

        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="pass_ticket"></param>
        /// <param name="uin"></param>
        /// <param name="sid"></param>
        /// <param name="skey"></param>
        /// <param name="deviceID"></param>
        /// <returns></returns>
        public void  Init(string pass_ticket,BaseRequest baseReq,Action<InitResponse>CallBack)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r={0}&pass_ticket={1}";
            url = string.Format(url, GetR(), pass_ticket);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.Item.Request.HeadersDic["Connection"] = "keep-alive";
			InitRequest initReq = new InitRequest
			{
				BaseRequest = baseReq
			};
			string requestJson = JsonConvert.SerializeObject(initReq);
			http.GetHtml(url,"post", referer: "https://wx2.qq.com/", postData: requestJson, callBack: (x) => {
				var repJsonStr = x.response.DataString();
				if (repJsonStr == null) CallBack?.Invoke(null);
				var rep = JsonConvert.DeserializeObject<InitResponse>(repJsonStr);
				CallBack?.Invoke(rep);
			});

        }

        /// <summary>
        /// 获得联系人列表
        /// </summary>
        /// <param name="pass_ticket"></param>
        /// <param name="skey"></param>
        public void GetContact(string pass_ticket,string skey,Action<GetContactResponse>CallBack)
        {
			string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact?pass_ticket={0}&r={1}&seq=0&skey={2}";
			url = string.Format(url, pass_ticket, GetR(), skey);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.GetHtml(url, referer: "https://wx2.qq.com/", callBack: (x) => {
				var rep = JsonConvert.DeserializeObject<GetContactResponse>(x.response.DataString());
				CallBack?.Invoke(rep);
			});


        }

        /// <summary>
        /// 批量获取联系人详细信息
        /// </summary>
        /// <param name="requestContacts"></param>
        /// <param name="pass_ticket"></param>
        /// <param name="uin"></param>
        /// <param name="sid"></param>
        /// <param name="skey"></param>
        /// <param name="deviceID"></param>
        public void BatchGetContact(string[] requestContacts,string pass_ticket,BaseRequest baseReq,Action<BatchGetContactResponse>CallBack)
        {
			BatchGetContactRequest req = new BatchGetContactRequest
			{
				BaseRequest = baseReq,
				Count = requestContacts.Length
			};

			List<BatchUser> requestUsers = new List<BatchUser>();
			for (int i = 0; i < req.Count; i++)
			{
				var tmp = new BatchUser
				{
					UserName = requestContacts[i],
				};
				requestUsers.Add(tmp);
			}

			req.List = requestUsers.ToArray();
			string requestJson = JsonConvert.SerializeObject(req);

			string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxbatchgetcontact?type=ex&r={0}&lang=zh_CN&pass_ticket={1}";
            url = string.Format(url, GetR(), pass_ticket);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.GetHtml(url,"post", requestJson, referer: "https://wx2.qq.com/", callBack: (x) =>
			{
				string repJsonStr = x.response.DataString();
				var rep = JsonConvert.DeserializeObject<BatchGetContactResponse>(repJsonStr);
				CallBack?.Invoke(rep);
			});
        }


        public void SyncCheck(SyncItem[] syncItems,BaseRequest baseReq,long syncCheckTimes,Action<SyncCheckResponse>CallBack)
        {

				string synckey = "";
            for (int i = 0; i < syncItems.Length; i++) {
                if (i != 0) {
                    synckey += "|";
                }
                synckey += syncItems[i].Key + "_" + syncItems[i].Val;
            }
            string url = "https://webpush.wx2.qq.com/cgi-bin/mmwebwx-bin/synccheck?skey={0}&sid={1}&uin={2}&deviceid={3}&synckey={4}&_={5}&r={6}";
            url = string.Format(url,UrlEncode(baseReq.Skey), UrlEncode(baseReq.Sid), baseReq.Uin, baseReq.DeviceID,synckey,syncCheckTimes,Util.GetTimeStamp());
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.GetHtml(url, referer: "https://wx2.qq.com/", callBack: (x) => {
				string repStr = x.response.DataString();
				if (repStr == null)CallBack?.Invoke(null);
				SyncCheckResponse rep = new SyncCheckResponse();
				if (repStr.StartsWith("window.synccheck="))
				{
					repStr = repStr.Substring("window.synccheck=".Length);
					rep = JsonConvert.DeserializeObject<SyncCheckResponse>(repStr);
				}
				CallBack?.Invoke(rep);
			});
        }

        static long GetR() {
            return Util.GetTimeStamp();
        }

        public void Sync(SyncKey syncKey,string pass_ticket,BaseRequest baseReq,Action<SyncResponse>CallBack)
        {
			string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid={0}&skey={1}&lang=zh_CN&pass_ticket={2}";
			url = string.Format(url, baseReq.Sid, baseReq.Skey, pass_ticket);
			SyncRequest req = new SyncRequest
			{
				BaseRequest = baseReq,
				SyncKey = syncKey,
				rr = ~GetR()
			};
			string requestJson = JsonConvert.SerializeObject(req);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.Item.Request.HeadersDic["Origin"] = "https://wx2.qq.com";
			http.GetHtml(url,"post", requestJson, referer: "https://wx2.qq.com/", callBack: (x) => {
				var repJsonStr = x.response.DataString();
				if (repJsonStr == null) CallBack?.Invoke( null);
				var rep = JsonConvert.DeserializeObject<SyncResponse>(repJsonStr);
				CallBack?.Invoke(rep);
			});
        }

        public void Statusnotify(string formUser,string toUser,string pass_ticket,BaseRequest baseReq,Action<StatusnotifyResponse>CallBack)
        {

            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxstatusnotify?lang=zh_CN&pass_ticket=" + pass_ticket;
			StatusnotifyRequest req = new StatusnotifyRequest
			{
				BaseRequest = baseReq,
				ClientMsgId = GetR(),
				FromUserName = formUser,
				ToUserName = toUser,
				Code = 3
			};
			string requestJson = JsonConvert.SerializeObject(req);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.Item.Request.HeadersDic["Origin"] = "https://wx2.qq.com";
			http.GetHtml(url, "post", requestJson, referer: "https://wx2.qq.com/", callBack: (x) => {
				var repJsonStr = x.response.DataString();
				if (repJsonStr == null) CallBack?.Invoke(null);
				var rep = JsonConvert.DeserializeObject<StatusnotifyResponse>(repJsonStr);//此处掉线时会报格式不正确
				CallBack?.Invoke(rep);
			});
        }
		//public List<SendMsgResponse> SendMsgs(IEnumerable<Msg> msgs, string pass_ticket, BaseRequest baseReq)
		//{
		//	var results = new List<SendMsgResponse>();
		//	Func<Msg, string, BaseRequest, SendMsgResponse> sendmsg = SendMsg;
		//	foreach(var msg in msgs)
		//	{
		//		var task = new Task<SendMsgResponse>(sendmsg(msg, pass_ticket, baseReq));
		//	}
		//	Task.WaitAll();
		//	return results;
		//}
		////private Func<SendMsgResponse> SendMsgAsyn(Msg msg,string pass_ticket,BaseRequest baseReq)
		//{
		//	return SendMsg;
		//}
		public void SendMsg(Msg msg, string pass_ticket,BaseRequest baseReq,Action<SendMsgResponse>CallBack)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg?sid={0}&r={1}&lang=zh_CN&pass_ticket={2}";
            url = string.Format(url, baseReq.Sid, GetR(), pass_ticket);
			SendMsgRequest req = new SendMsgRequest
			{
				BaseRequest = baseReq,
				Msg = msg,
				rr = DateTime.Now.Millisecond
			};
			string requestJson = JsonConvert.SerializeObject(req);
			http.Item.Request.HeadersDic["Accept"] = "application/json, text/plain, */*";
			http.Item.Request.HeadersDic["Origin"] = "https://wx2.qq.com";
			http.GetHtml(url, "post", requestJson, referer: "https://wx2.qq.com/", callBack: (x) =>
			{
				var repJsonStr = x.response.DataString();
				if (repJsonStr == null) CallBack?.Invoke( null);
				var rep = JsonConvert.DeserializeObject<SendMsgResponse>(repJsonStr);
				CallBack?.Invoke(rep);
			});

        }
		private void ReportForWeb(Action CallBack)
		{
			string url = "https://support.weixin.qq.com/cgi-bin/mmsupport-bin/reportforweb?rid={0}&rkey={1}&rvalue={2}";
			http.GetHtml(string.Format(url, 69373, 9, 1),callBack:(x)=> { });
			http.GetHtml(string.Format(url, 63637, 72, 80), callBack: (x) => { });
			http.GetHtml(string.Format(url, 63637, 76, 1), callBack: (x) => { });
		}

        public void Uploadmedia(string fromUserName,string toUserName,string id,string mime_type, int uploadType,int mediaType,byte[] buffer,string fileName,string pass_ticket,BaseRequest baseReq,Action<UploadmediaResponse> CallBack) {
			//TODO 暂未实现图片上传
			ReportForWeb(()=> {
				UploadmediaRequest req = new UploadmediaRequest
				{
					UploadType = uploadType,
					BaseRequest = baseReq,
					ClientMediaId = GetR(),
					TotalLen = buffer.Length,
					StartPos = 0,
					DataLen = buffer.Length,
					MediaType = mediaType,
					FromUserName = fromUserName,
					ToUserName = toUserName,

					FileMd5 = Util.GetMD5(buffer)
				};

				string url = "https://file.wx2.qq.com/cgi-bin/mmwebwx-bin/webwxuploadmedia?f=json";
				http.GetHtml(url, "option");

				string requestJson = JsonConvert.SerializeObject(req);
				string mt = "doc";
				if (mime_type.StartsWith("image/"))
				{
					mt = "pic";
				}
				//var dataTicketCookie = GetCookie("webwx_data_ticket");

			//	var dataContent = new MultipartFormDataContent
			//{
			//	{ new StringContent(id), "id" },
			//	{ new StringContent(fileName), "name" },
			//	{ new StringContent(mime_type), "type" },
			//	{ new StringContent("2018/2/23 下午11:23:33"), "lastModifiedDate" },
			//	{ new StringContent(buffer.Length.ToString()), "size" },
			//	{ new StringContent(mt), "mediatype" },
			//	{ new StringContent(requestJson), "uploadmediarequest" },
			//	{ new StringContent(dataTicketCookie.Value), "webwx_data_ticket" },
			//	{ new StringContent("undefined"), "pass_ticket" },
			//	{ new ByteArrayContent(buffer) },
			//	{ new StringContent( fileName + "\r\n Content - Type: " + mime_type) ,"filename"}
			//};

				//try
				//{
				//	var response = mHttpClient.PostAsync(url, dataContent).Result;
				//	string repJsonStr = response.Content.ReadAsStringAsync().Result;
				//	var rep = JsonConvert.DeserializeObject<UploadmediaResponse>(repJsonStr);
				//	return rep;
				//}
				//catch (Exception ex)
				//{
				//	Console.WriteLine("uploadmedia()" + ex.Message);
				//	return null;
				//}


			});
	

        }


        public void SendMsgImg(ImgMsg msg, string pass_ticket,BaseRequest baseReq,Action<SendMsgImgResponse>CallBack)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsgimg?fun=async&f=json&lang=zh_CN&pass_ticket={0}";
            url = string.Format(url,pass_ticket);
			SendMsgImgRequest req = new SendMsgImgRequest
			{
				BaseRequest = baseReq,
				Msg = msg,
				Scene = 0
			};
			string requestJson = JsonConvert.SerializeObject(req);
			http.GetHtml(url, "post", requestJson,callBack:(x)=> {
				var repJsonStr = x.response.DataString();
				if (repJsonStr == null) CallBack?.Invoke(null);
				var rep = JsonConvert.DeserializeObject<SendMsgImgResponse>(repJsonStr);
				CallBack?.Invoke(rep);
			});

        }


  
        public void Oplog(string userName,int cmdID,int op,string RemarkName, string pass_ticket,BaseRequest baseReq,Action<OplogResponse>CallBack)
        {
            string url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxoplog?pass_ticket={0}";
            url = string.Format(url, pass_ticket);
			OplogRequest req = new OplogRequest
			{
				BaseRequest = baseReq,
				UserName = userName,
				CmdId = cmdID,
				OP = op,
				RemarkName = RemarkName
			};
			string requestJson = JsonConvert.SerializeObject(req);
			http.GetHtml(url, "post", requestJson, callBack: (x) => {
				string repJsonStr = x.response.DataString();
				if (repJsonStr == null)
				{
					CallBack?.Invoke(null);
					return;
				}
				var rep = JsonConvert.DeserializeObject<OplogResponse>(repJsonStr);
				CallBack?.Invoke(rep);

			});
  
        }

        public void Logout(string skey,string sid,string uin,Action CallBack)
        {

            string url = string.Format("https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=0&skey={0}", System.Web.HttpUtility.UrlEncode(skey));
            string requestStr = string.Format("sid={0}&uin={1}",sid,uin);
   
			http.Item.Request.HeadersDic["Cache-Control"]= "max-age=0";
			http.Item.Request.HeadersDic["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
			http.GetHtml(url, "post", requestStr,referer: "https://wx.qq.com/", callBack: (x) => {
				url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=1&skey=" + System.Web.HttpUtility.UrlEncode(skey);
				http.GetHtml(url, "post", requestStr, referer: "https://wx.qq.com/", callBack: (xx) => { });
			});

        }
        string UrlEncode(string url)
        {
            return System.Web.HttpUtility.UrlEncode(url);
        }




	}


}
