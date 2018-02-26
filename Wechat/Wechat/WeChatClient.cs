using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Wechat.API;
using Wechat.API.RPC;
using System.Collections.Generic;
using Wechat.tools;
using System.Text;

namespace Wechat
{

    public enum ClientStatusType
    {
        GetUUID,
        GetQRCode,
        Login,
        QRCodeScaned,
        WeixinInit,
        SyncCheck,
        WeixinSync,
        None,
    }


    public class WeChatClient:IDisposable
    {

		private User currentUser;
		public User CurrentUser { get => currentUser; set => currentUser = value; }


		public Action<object, GetQRCodeImageEvent> OnGetQRCodeImage;
		public Action<object, UserScanQRCodeEvent> OnUserScanQRCode;
		public Action<object, LoginSucessEvent> OnLoginSucess;
		public Action<object, InitedEvent> OnInitComplate;
		public Action<object, RecvMessageEvent> OnRecvMsg;
		public Action<object, AddUserEvent> OnAddUser;


		#region StatusHandle
		private void HandleStatus()
		{
			switch (CurrentStatus)
			{
				case ClientStatusType.GetUUID:
					Console.WriteLine("ClientStatusType.GetUUID");
					HandleGetLoginSession();
					break;
				case ClientStatusType.GetQRCode:
					Console.WriteLine("ClientStatusType.GetQRCode");
					HandleGetQRCode();
					break;
				case ClientStatusType.Login:
					Console.WriteLine("ClientStatusType.Login");
					HandleLogin();
					break;
				case ClientStatusType.QRCodeScaned:
					Console.WriteLine("ClientStatusType.QRCodeScaned");
					HandleQRCodeScaned();
					break;
				case ClientStatusType.WeixinInit:
					Console.WriteLine("ClientStatusType.WeixinInit");
					HandleInit();
					break;

				case ClientStatusType.WeixinSync:
					Console.WriteLine("ClientStatusType.WeixinSync");
					HandleSync();
					break;
			}
		}
		private void HandleGetLoginSession()
		{
			IsLogin = false;
			mLoginSession = mAPIService.GetNewQRLoginSessionID();
			if (!string.IsNullOrWhiteSpace(mLoginSession))
			{
				CurrentStatus = ClientStatusType.GetQRCode;
			}
		}
		private void HandleGetQRCode()
		{
			var QRCodeImg = mAPIService.GetQRCodeImage(mLoginSession);
			if (QRCodeImg != null)
			{
				CurrentStatus = ClientStatusType.Login;
				var wce = new GetQRCodeImageEvent(QRCodeImg);
				OnGetQRCodeImage?.Invoke(this, wce);

			}
			else
			{
				CurrentStatus = ClientStatusType.GetUUID;
			}
		}
		private void HandleLogin()
		{
			var loginResult = mAPIService.Login(mLoginSession, Util.GetTimeStamp());
			if (loginResult != null && loginResult.code == 201)
			{
				// 已扫描,但是未确认登录
				// convert base64 to image
				byte[] base64_image_bytes = Convert.FromBase64String(loginResult.UserAvatar);
				MemoryStream memoryStream = new MemoryStream(base64_image_bytes, 0, base64_image_bytes.Length);
				memoryStream.Write(base64_image_bytes, 0, base64_image_bytes.Length);
				var image = Image.FromStream(memoryStream);

				OnUserScanQRCode?.Invoke(this, new UserScanQRCodeEvent(image));

				CurrentStatus = ClientStatusType.QRCodeScaned;
			}
			else
			{
				CurrentStatus = ClientStatusType.GetUUID;
			}
		}
		private long mSyncCheckTimes = 0;
		private void HandleQRCodeScaned()
		{
			mSyncCheckTimes = Util.GetTimeStamp();
			var loginResult = mAPIService.Login(mLoginSession, mSyncCheckTimes);
			if (loginResult != null && loginResult.code == 200)
			{
				// 登录成功
				var redirectResult = mAPIService.LoginRedirect(loginResult.redirect_uri);
				if (redirectResult == null) {
					CurrentStatus = ClientStatusType.GetUUID;
					return;
				};
				mBaseReq = new BaseRequest
				{
					Skey = redirectResult.skey,
					Sid = redirectResult.wxsid,
					Uin = redirectResult.wxuin,
					DeviceID = CreateNewDeviceID()
				};
				mPass_ticket = redirectResult.pass_ticket;
				CurrentStatus = ClientStatusType.WeixinInit;

				OnLoginSucess?.Invoke(this, new LoginSucessEvent());
			}
			else
			{
				CurrentStatus = ClientStatusType.GetUUID;
			}
		}
		private void HandleInit()
		{
			var initResult = mAPIService.Init(mPass_ticket, mBaseReq);
			if (initResult != null && initResult.BaseResponse.ret == 0)
			{
				Self = initResult.User;
				CurrentUser = Self;
				mSyncKey = initResult.SyncKey;
				// 开启系统通知
				var statusNotifyRep = mAPIService.Statusnotify(Self.UserName, Self.UserName, mPass_ticket, mBaseReq);
				if (statusNotifyRep != null && statusNotifyRep.BaseResponse != null && statusNotifyRep.BaseResponse.ret == 0)
				{
					CurrentStatus = ClientStatusType.WeixinSync;
					IsLogin = true;
				}
				else
				{
					CurrentStatus = ClientStatusType.GetUUID;
					return;
				}
			}
			else
			{
				CurrentStatus = ClientStatusType.GetUUID;
				return;
			}
			if (!InitContactAndGroups())
			{
				CurrentStatus = ClientStatusType.WeixinInit;
				IsLogin = false;
				return;
			}


			OnInitComplate?.Invoke(this, new InitedEvent());
		}


		private bool InitContactAndGroups()
		{
			mContacts = new List<User>();
			mGroups = new List<Group>();

			var contactResult = mAPIService.GetContact(mPass_ticket, mBaseReq.Skey);
			if (contactResult == null || contactResult.BaseResponse == null || contactResult.BaseResponse.ret != 0)
			{
				return false;
			}

			List<string> groupIDs = new List<string>();
			foreach (var user in contactResult.MemberList)
			{
				if (user.UserName.StartsWith("@@"))
				{
					groupIDs.Add(user.UserName);
				}
				else
				{
					
					mContacts.Add(user);
					OnAddUser?.Invoke(this, new AddUserEvent() { User=user});
				}
			}

			if (groupIDs.Count <= 0) return true;
			// 批量获得群成员详细信息
			var batchResult = mAPIService.BatchGetContact(groupIDs.ToArray(), mPass_ticket, mBaseReq);
			if (batchResult == null || batchResult.BaseResponse.ret != 0) return false;

			foreach (var user in batchResult.ContactList)
			{
				if (!user.UserName.StartsWith("@@") || user.MemberCount <= 0) continue;
				Group group = new Group
				{
					ID = user.UserName,
					NickName = user.NickName,
					RemarkName = user.RemarkName
				};
				List<Contact> groupMembers = new List<Contact>();
				foreach (User member in user.MemberList)
				{
					groupMembers.Add(CreateContact(member));
				}
				group.Members = groupMembers.ToArray();
				mGroups.Add(group);
			}

			return true;
		}


		private SyncKey mSyncKey;
		private void HandleSync()
		{
			if (mSyncKey == null)
			{
				CurrentStatus = ClientStatusType.GetUUID;
				return;
			}
			if (mSyncKey.Count <= 0) return;

			var checkResult = mAPIService.SyncCheck(mSyncKey.List, mBaseReq, ++mSyncCheckTimes);
			if (checkResult == null) return;


			if (checkResult.retcode != null && checkResult.retcode != "0")
			{
				CurrentStatus = ClientStatusType.GetUUID;
				return;
			}
			if (checkResult.selector == "0") return;
			var syncResult = mAPIService.Sync(mSyncKey, mPass_ticket, mBaseReq);
			if (syncResult == null) return;
			mSyncKey = syncResult.SyncKey;

			// 处理同步
			ProcessSyncResult(syncResult);

		}
		private void ProcessSyncResult(SyncResponse result)
		{
			// 处理消息
			if (result.AddMsgCount > 0)
			{
				foreach (AddMsg msg in result.AddMsgList)
				{
					var message = MessageFactory.CreateMessage(msg);
					OnRecvMsg?.Invoke(this, new RecvMessageEvent(msg) );
				}
			}
		}
		#endregion
		public User Self { get; private set; }

        public bool IsLogin { get; private set; }

        public ClientStatusType CurrentStatus
        {
            get
            {
                return mStatus;
            }
            private set
            {
                if (mStatus != value)
                {
                    var changedEvent = new StatusChangedEvent()
                    {
                        FromStatus = mStatus,
                        ToStatus = value
                    };
                    mStatus = value;
                    //OnEvent?.Invoke(this, changedEvent);

                }
            }
        }
        private List<User> mContacts;
		public List<User> Contacts
        {
            get{
                return mContacts;
            }
            private set {
                mContacts = new List<User>(value);
            }
        }
        private List<Group> mGroups;
        public Group[] Groups
        {
            get {
                return mGroups.ToArray();
            }
            private set
            {
                mGroups = new List<Group>();
                mGroups.AddRange(value);
            }
        }
        public Group GetGroup(string ID)
        {
            if (mGroups == null) return null;
            return mGroups.FindLast((group) => {
                return group.ID == ID;
            });
        }
        public User GetContact(string ID)
        {
            if (ID == Self.UserName) return Self;
            if (mContacts == null) return null;
            return mContacts.FindLast((user) =>
            {
                return user.UserName == ID;
            });
        }

        /*
         * Web Weixin Pipeline
			   +--------------+     +---------------+   +---------------+
               |              |     |               |   |               |
               |   Get UUID   |     |  Get Contact  |   | Status Notify |
               |              |     |               |   |               |
               +-------+------+     +-------^-------+   +-------^-------+
                       |                    |                   |
                       |                    +-------+  +--------+
                       |                            |  |
               +-------v------+               +-----+--+------+      +--------------+
               |              |               |               |      |              |
               |  Get QRCode  |               |  Weixin Init  +------>  Sync Check<----+
               |              |               |               |      |              |    |
               +-------+------+               +-------^-------+      +-------+------+    |
                       |                              |                      |           |
                       |                              |                      +-----------+
                       |                              |                      |
               +-------v------+               +-------+--------+     +-------v-------+
               |              | Confirm Login |                |     |               |
        +------>    Login     +---------------> New Login Page |     |  Weixin Sync  |
        |      |              |               |                |     |               |
        |      +------+-------+               +----------------+     +---------------+
        |             |
        |QRCode Scaned|
        +-------------+
        */

        private System.Threading.Thread mMainLoopThread;
        private ClientStatusType mStatus = ClientStatusType.None;
        public void Run()
        {
            Quit();
            mIsQuit = false;
            IsLogin = false;
            CurrentStatus = ClientStatusType.GetUUID;
            mAPIService = new WechatAPIService();
            mMainLoopThread = new System.Threading.Thread(MainLoop) { IsBackground=true};
            mMainLoopThread.Start();
        }

        public void Quit(bool force = false)
        {
            mIsQuit = true;
            Logout();
            if (force) {
                if (mMainLoopThread != null && mMainLoopThread.IsAlive) {
                    mMainLoopThread.Abort();
                }       
            }
        }



        private bool mIsQuit = false;
        private WechatAPIService mAPIService = null;
        private string mPass_ticket;
        private BaseRequest mBaseReq;
        private string mLoginSession;
        private void MainLoop()
        {
            while (!mIsQuit)
            {
                 HandleStatus();
            }
        }




        private static string CreateNewDeviceID()
        {
            Random ran = new Random();
			StringBuilder tmp = new StringBuilder();
			for (int i = 0; i < 15; i++) tmp.Append(ran.Next(0, 9));
            return string.Format("e{0}", tmp);
        }


        public Contact CreateContact(Wechat.API.User user)
        {

			Contact contact = new Contact
			{
				ID = user.UserName,
				NickName = user.NickName,
				RemarkName = user.RemarkName
			};
			return contact;
        }

        public Contact[] GetGroupMembers(string groupID)
        {

            //获取群聊成员
            var batchResult = mAPIService.BatchGetContact(new string[] { groupID}, mPass_ticket, mBaseReq);
            if (batchResult == null || batchResult.BaseResponse.ret != 0) return null;

            List<Contact> members = new List<Contact>();
            foreach(var contact in batchResult.ContactList)
            {
                if (contact.UserName.StartsWith("@@")) continue;
                members.Add(CreateContact(contact));
            }

            return members.ToArray();

        }



        /// <summary>
        /// 置顶群聊
        /// </summary>
        /// <param name="groupUserName"></param>
        public bool StickyPost(string groupUserName)
        {
            var rep = mAPIService.Oplog(groupUserName, 3, 0, null, mPass_ticket, mBaseReq);
            return rep.BaseResponse.ret == 0;
        }

        public bool SetRemarkName(string id, string remarkName)
        {
            var contact = GetContact(id);
            if (contact == null) return false;
            var rep = mAPIService.Oplog(id, 2, 0, remarkName, mPass_ticket, mBaseReq);
            if (rep.BaseResponse.ret == 0) {
                contact.RemarkName = remarkName;
                return true;
            }
            return false;
        }

		/// <summary>
		/// 发送文本消息
		/// </summary>
		/// <param name="toUserName">接收者用户名</param>
		/// <param name="content">内容</param>
		/// <returns></returns>
        public bool SendMsg(string toUserName, string content)
        {
			Msg msg = new Msg
			{
				FromUserName = Self.UserName,
				ToUserName = toUserName,
				Content = content,
				ClientMsgId = DateTime.Now.Millisecond,
				LocalID = DateTime.Now.Millisecond,
				Type = 1//type 1 文本消息
			};
			var response = mAPIService.SendMsg(msg, mPass_ticket, mBaseReq);
            if (response != null && response.BaseResponse != null && response.BaseResponse.ret == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        int upLoadMediaCount = 0;
		/// <summary>
		/// 发送图片消息
		/// </summary>
		/// <param name="toUserName">接收者用户名</param>
		/// <param name="img">图像</param>
		/// <param name="format">格式化，默认为png</param>
		/// <param name="imageName">图片名称，默认为img_{MediaId}</param>
		/// <returns>发送成功</returns>
        public bool SendMsg(string toUserName, Image img, ImageFormat format = null, string imageName = null)
        {
            if (img == null) return false;
            string fileName = imageName ?? "img_" + upLoadMediaCount;
            var imgFormat = format ?? ImageFormat.Png;

            fileName += "." + imgFormat.ToString().ToLower();

            MemoryStream ms = new MemoryStream();
            img.Save(ms, imgFormat);
            ms.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[ms.Length];
            int readCount = ms.Read(data, 0, data.Length);
            if (readCount != data.Length) return false;

            string mimetype = "image/" + imgFormat.ToString().ToLower();
            var response = mAPIService.Uploadmedia(Self.UserName, toUserName, "WU_FILE_" + upLoadMediaCount, mimetype, 2, 4, data, fileName, mPass_ticket, mBaseReq);
            if (response != null && response.BaseResponse != null && response.BaseResponse.ret == 0)
            {
                upLoadMediaCount++;
                string mediaId = response.MediaId;
				ImgMsg msg = new ImgMsg
				{
					FromUserName = Self.UserName,
					ToUserName = toUserName,
					MediaId = mediaId,
					ClientMsgId = DateTime.Now.Millisecond,
					LocalID = DateTime.Now.Millisecond,
					Type = 3
				};
				var sendImgRep = mAPIService.SendMsgImg(msg, mPass_ticket, mBaseReq);
                if (sendImgRep != null && sendImgRep.BaseResponse != null && sendImgRep.BaseResponse.ret == 0)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

		/// <summary>
		/// 退出登录
		/// </summary>
        public void Logout()
        {
            if (!IsLogin || mMainLoopThread==null || !mMainLoopThread.IsAlive) return;
            mAPIService.Logout(mBaseReq.Skey, mBaseReq.Sid, mBaseReq.Uin);
            IsLogin = false;
            mContacts = null;
            mGroups = null;
            CurrentStatus = ClientStatusType.GetUUID;
        }

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					mAPIService.Dispose();
					// TODO: 释放托管状态(托管对象)。
				}

				// TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
				// TODO: 将大型字段设置为 null。

				disposedValue = true;
			}
		}

		// TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
		// ~WeChatClient() {
		//   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
		//   Dispose(false);
		// }

		// 添加此代码以正确实现可处置模式。
		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
			// TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
