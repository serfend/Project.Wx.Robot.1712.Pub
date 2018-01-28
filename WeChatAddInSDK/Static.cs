using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wechat.API;
namespace WeChatAddInSDK
{
    public static class Static
    {
		#region  一般消息
				//MsgType
				//1   文本消息
				//3   图片消息
				//34  语音消息
				//37  VERIFYMSG
				//40  POSSIBLEFRIEND_MSG
				//42  共享名片
				//43  视频通话消息
				//47  动画表情
				//48  位置消息
				//49  分享链接
				//50  VOIPMSG
				//51  微信初始化消息
				//52  VOIPNOTIFY
				//53  VOIPINVITE
				//62  小视频
				//9999    SYSNOTICE
				//10000   系统消息/红包
				//10002   撤回消息

				/// <summary>
				/// 当接收到私聊消息时会被调用
				/// </summary>
				/// <param name="from">消息来源用户名</param>
				/// <param name="content">内容</param>
				/// <param name="type">消息类型</param>
				/// <returns>返回1时会阻止本插件之后的插件的调用</returns>
				public static Func<string, string, int, int> ReceivingPrivateMsg;
				/// <summary>
				/// 当接收到群组的消息时会被调用
				/// </summary>
				/// <param name="from">消息来源用户名</param>
				/// <param name="content">内容</param>
				/// <param name="type">消息类型</param>
				/// <returns>返回1时会阻止本插件之后的插件的调用</returns>
				public static Func<string, string, int, int> ReceivingGroupMsg;
				/// <summary>
				/// 当消息发送完毕时会被调用
				/// </summary>
				/// <param name="to">发送的目标</param>
				/// <param name="content">内容</param>
				/// <param name="type">消息类型</param>
				/// <param name="result">成功发送返回0,否则返回1</param>
				/// <returns>返回1时会阻止本插件之后的插件的调用</returns>
				public static Func<string, string, int, int> MessageSent;
				/// <summary>
				/// 当有好友申请时会被调用
				/// </summary>
				/// <param name="from">好友申请来源的用户名</param>
				/// <param name="content">备注</param>
				/// <returns>
				/// 0:不处理,1:同意,2:拒绝,3:不处理并截断（配合 FriendResponse()使用)
				/// </returns>
				public static Func<string, string, int, int> FriendRequest;
		#endregion 
		#region 系统信息
		/// <summary>
		/// 当微信登录完成后会被调用
		/// </summary>
		/// <returns></returns>
		public static Func<int> WxLogin;
		/// <summary>
		/// 当微信退出登录后会被调用
		/// </summary>
		/// <returns></returns>
		public static Func<int> WxLogOut;
		#endregion
	}
}
