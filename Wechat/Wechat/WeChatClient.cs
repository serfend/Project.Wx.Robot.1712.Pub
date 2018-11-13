using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Wechat.API;
using Wechat.API.RPC;
using System.Collections.Generic;
using Wechat.tools;
using System.Text;
using DotNet4.Utilities.UtilHttp;
using System.Threading;

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


    public class WeChatClient
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
		{;
			if (mIsQuit) return;
			switch (CurrentStatus)
			{
				case ClientStatusType.GetUUID:
					Console.WriteLine("ClientStatusType.GetUUID");
					HandleGetLoginSession(() => {
						HandleStatus();
						Console.WriteLine("ClientStatusType.GetUUIDFinish");
					});
					break;
				case ClientStatusType.GetQRCode:
					Console.WriteLine("ClientStatusType.GetQRCode");
					HandleGetQRCode(() => {
						HandleStatus();
						Console.WriteLine("ClientStatusType.GetQRCodeFinish");
					});
					break;
				case ClientStatusType.Login:
					Console.WriteLine("ClientStatusType.Login");
					HandleLogin(() => {
						HandleStatus();
						Console.WriteLine("ClientStatusType.LoginFinish");
					});
					break;
				case ClientStatusType.QRCodeScaned:
					Console.WriteLine("ClientStatusType.QRCodeScaned");
					HandleQRCodeScaned(() => {
						HandleStatus();
						Console.WriteLine("ClientStatusType.QRCodeScanedFinish");
					});
					break;
				case ClientStatusType.WeixinInit:
					Console.WriteLine("ClientStatusType.WeixinInit");
					HandleInit(() => {
						HandleStatus();
						Console.WriteLine("ClientStatusType.WeixinInitFinish");
					});
					break;

				case ClientStatusType.WeixinSync:
					Console.WriteLine("ClientStatusType.WeixinSync");
					HandleSync(()=> {
						HandleStatus();
						Console.WriteLine("ClientStatusType.WeixinSyncFinish");
					});
					break;
			}
		}
		private void HandleGetLoginSession(Action CallBack)
		{
			IsLogin = false;
			mAPIService.GetNewQRLoginSessionID((mLoginSession)=> {
				this.mLoginSession = mLoginSession;
				if (!string.IsNullOrWhiteSpace(mLoginSession))
				{
					CurrentStatus = ClientStatusType.GetQRCode;
				}
				CallBack.Invoke();
			});

		}
		private void HandleGetQRCode(Action CallBack)
		{
			mAPIService.GetQRCodeImage(mLoginSession,(QRCodeImg)=> {
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
				CallBack.Invoke();
			});

		}
		private void HandleLogin(Action CallBack)
		{
			mAPIService.Login(mLoginSession, Util.GetTimeStamp(),(loginResult)=> {
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
				
				CallBack.Invoke();
			});

		}
		private long mSyncCheckTimes = 0;
		private void HandleQRCodeScaned(Action CallBack)
		{
			mSyncCheckTimes = Util.GetTimeStamp();
			mAPIService.Login(mLoginSession, mSyncCheckTimes,(loginResult)=> {
				if (loginResult != null && loginResult.code == 200)
				{
					// 登录成功
					mAPIService.LoginRedirect(loginResult.redirect_uri, (redirectResult) =>
					{
						if (redirectResult == null)
						{
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
						CallBack.Invoke();
					});

				}
				else
				{
					CurrentStatus = ClientStatusType.GetUUID;
					CallBack.Invoke();
				}
				
			});

		}
		private void HandleInit(Action CallBack)
		{
			mAPIService.Init(mPass_ticket, mBaseReq,(initResult)=> {
				if (initResult != null && initResult.BaseResponse.ret == 0)
				{
					Self = initResult.User;
					CurrentUser = Self;
					mSyncKey = initResult.SyncKey;
					// 开启系统通知
					mAPIService.Statusnotify(Self.UserName, Self.UserName, mPass_ticket, mBaseReq,(statusNotifyRep)=> {
						if (statusNotifyRep != null && statusNotifyRep.BaseResponse != null && statusNotifyRep.BaseResponse.ret == 0)
						{
							CurrentStatus = ClientStatusType.WeixinSync;
							IsLogin = true;
						}
						else
						{
							CurrentStatus = ClientStatusType.GetUUID;
							CallBack.Invoke();
							return;
						}
					});

				}
				else
				{
					CurrentStatus = ClientStatusType.GetUUID;
					CallBack.Invoke();
					return;
				}
				InitContactAndGroups((x) =>
				{
					if (x)
					{
						OnInitComplate?.Invoke(this, new InitedEvent());
						CallBack.Invoke();
					}
					else
					{
						CurrentStatus = ClientStatusType.WeixinInit;
						IsLogin = false;
						CallBack.Invoke();
						return;
					}

					
				});
				CallBack.Invoke();
			});

		}


		private void InitContactAndGroups(Action<bool>CallBack)
		{
			mContacts = new List<User>();
			mGroups = new List<Group>();

			mAPIService.GetContact(mPass_ticket, mBaseReq.Skey,(contactResult)=> {
				if (contactResult == null || contactResult.BaseResponse == null || contactResult.BaseResponse.ret != 0)
				{
					CallBack?.Invoke(false);
					return;
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
						OnAddUser?.Invoke(this, new AddUserEvent() { User = user });
					}
				}

				if (groupIDs.Count <= 0) { CallBack?.Invoke(false);return; }
				// 批量获得群成员详细信息
				mAPIService.BatchGetContact(groupIDs.ToArray(), mPass_ticket, mBaseReq,(batchResult)=> {
					if (batchResult == null || batchResult.BaseResponse.ret != 0) {CallBack?.Invoke(false); return; }

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

					CallBack?.Invoke(true);

				});

			});


		}


		private SyncKey mSyncKey;
		private void HandleSync(Action CallBack)
		{
			if (mSyncKey == null)
			{
				CurrentStatus = ClientStatusType.GetUUID;
				return;
			}
			if (mSyncKey.Count <= 0) return;

			mAPIService.SyncCheck(mSyncKey.List, mBaseReq, ++mSyncCheckTimes,(checkResult)=> {
				if (checkResult == null) return;

				if (checkResult.retcode != null && checkResult.retcode != "0")
				{
					CurrentStatus = ClientStatusType.GetUUID;
					return;
				}
				if (checkResult.selector == "0") return;
				mAPIService.Sync(mSyncKey, mPass_ticket, mBaseReq,(syncResult)=> {
					if (syncResult == null) return; 
					mSyncKey = syncResult.SyncKey;
					// 处理同步
					ProcessSyncResult(syncResult);
				});
				CallBack.Invoke();
			});
			



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
			//HttpClient.UsedFidder = true;
            HandleStatus();
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

        public void GetGroupMembers(string groupID,Action<Contact[]>CallBack)
        {
            //获取群聊成员
            mAPIService.BatchGetContact(new string[] { groupID}, mPass_ticket, mBaseReq,(batchResult)=> {
				if (batchResult == null || batchResult.BaseResponse.ret != 0) { CallBack?.Invoke(null);return; }

				List<Contact> members = new List<Contact>();
				foreach (var contact in batchResult.ContactList)
				{
					if (contact.UserName.StartsWith("@@")) continue;
					members.Add(CreateContact(contact));
				}
				CallBack?.Invoke(members.ToArray());
			});
        }



        /// <summary>
        /// 置顶群聊
        /// </summary>
        /// <param name="groupUserName"></param>
        public void StickyPost(string groupUserName,Action<bool>CallBack)
        {
            mAPIService.Oplog(groupUserName, 3, 0, null, mPass_ticket, mBaseReq,(rep)=> {
				CallBack?.Invoke(rep.BaseResponse.ret == 0);
			});
        }

        public void SetRemarkName(string id, string remarkName,Action<bool>CallBack)
        {
            var contact = GetContact(id);
			if (contact == null) { CallBack?.Invoke(false);return; }
			mAPIService.Oplog(id, 2, 0, remarkName, mPass_ticket, mBaseReq, (rep) => {
				if (rep.BaseResponse.ret == 0)
				{
					contact.RemarkName = remarkName;
					CallBack?.Invoke(true);
				}
				else
					CallBack?.Invoke(false);

			});


        }

		/// <summary>
		/// 发送文本消息
		/// </summary>
		/// <param name="toUserName">接收者用户名</param>
		/// <param name="content">内容</param>
		/// <returns></returns>
        public void SendMsg(string toUserName, string content,Action<bool>CallBack)
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
			mAPIService.SendMsg(msg, mPass_ticket, mBaseReq,(response) => {
				CallBack?.Invoke(response != null && response.BaseResponse != null && response.BaseResponse.ret == 0);
			});
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
        public void SendMsg(string toUserName, Image img, ImageFormat format = null, string imageName = null,Action<bool>CallBack=null)
        {
			if (img == null) { CallBack?.Invoke(false);return; }

			string fileName = imageName ?? "img_" + upLoadMediaCount;
            var imgFormat = format ?? ImageFormat.Png;

            fileName += "." + imgFormat.ToString().ToLower();

            MemoryStream ms = new MemoryStream();
            img.Save(ms, imgFormat);
            ms.Seek(0, SeekOrigin.Begin);
            byte[] data = new byte[ms.Length];
            int readCount = ms.Read(data, 0, data.Length);
			if (readCount != data.Length) { CallBack.Invoke(false);return; }


			string mimetype = "image/" + imgFormat.ToString().ToLower();
			mAPIService.Uploadmedia(Self.UserName, toUserName, "WU_FILE_" + upLoadMediaCount, mimetype, 2, 4, data, fileName, mPass_ticket, mBaseReq,(response) =>{
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
					mAPIService.SendMsgImg(msg, mPass_ticket, mBaseReq,(sendImgRep) => {
						if (sendImgRep != null && sendImgRep.BaseResponse != null && sendImgRep.BaseResponse.ret == 0)
						{
							CallBack?.Invoke(true);
						}
						CallBack?.Invoke(false);
						return;

					});
					return;
				}
				CallBack?.Invoke(false);
			});

        }

		/// <summary>
		/// 退出登录
		/// </summary>
        public void Logout()
        {
            if (!IsLogin || mMainLoopThread==null || !mMainLoopThread.IsAlive) return;
            mAPIService.Logout(mBaseReq.Skey, mBaseReq.Sid, mBaseReq.Uin,()=> {
				IsLogin = false;
				mContacts = null;
				mGroups = null;
				CurrentStatus = ClientStatusType.GetUUID;
			});

        }

	}
}
