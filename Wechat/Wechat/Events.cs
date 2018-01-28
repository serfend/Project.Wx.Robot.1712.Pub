using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wechat.API;

namespace Wechat
{

    public class WeChatClientEvent : EventArgs { }


    public class GetQRCodeImageEvent : WeChatClientEvent
    {
        public Image QRImage;

		public GetQRCodeImageEvent(Image qRImage)
		{
			QRImage = qRImage;
		}
	}

    public class UserScanQRCodeEvent : WeChatClientEvent
    {
        public Image UserAvatarImage;

		public UserScanQRCodeEvent(Image userAvatarImage)
		{
			UserAvatarImage = userAvatarImage;
		}
	}

    public class LoginSucessEvent : WeChatClientEvent
    {

    }

    public class InitedEvent : WeChatClientEvent
    {

    }
	public class RecvMessageEvent : WeChatClientEvent
	{
		public Wechat.API.AddMsg Msg;

		public RecvMessageEvent(AddMsg msg)
		{
			Msg = msg;
		}
	}
	public class AddUserEvent : WeChatClientEvent
	{
		public Wechat.API.User User;
	}
    public class StatusChangedEvent:WeChatClientEvent
    {
        public ClientStatusType FromStatus;
        public ClientStatusType ToStatus;
    }
}
