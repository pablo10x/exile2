using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace core.ApiModels {

	public enum chatChannel {

		Global, Friends

	}

	public class User {

		[ JsonProperty ] public int      PlayerId     { get; set; }
		[ JsonProperty ] public string   Name         { get; set; }
		[ JsonProperty ] public Activity Activity     { get; set; }
		[ JsonProperty ] public string   AuthId       { get; set; }
		[ JsonProperty ] public string   DeviceId     { get; set; }
		[ JsonProperty ] public int      AccountLevel { get; set; } = 1;
		[ JsonProperty ] public bool     Vip          { get; set; }
		[ JsonProperty ] public string   Cash         { get; set; }
		[ JsonProperty ] public DateTime RegistredOn  { get; set; } = DateTime.Now;

		[ JsonProperty ] public List <Item> Items { get; set; }

		[ JsonProperty ] public bool AllowSearch { get; set; } = true;

		[ JsonProperty ] public List <UserInfo> Friends   { get; set; }
		[ JsonProperty ] public List <UserInfo> Blocklist { get; set; }

		[ JsonProperty ("outfriendrequests") ] public List <UserInfo> OutFriendRequests { get; set; }
		[ JsonProperty ("incfriendrequests") ] public List <UserInfo> IncFriendRequests { get; set; }


		public List <MessageSchema> PrivateMessages = new ();

		public UserInfo ToUserInfo ()
		{
			return new UserInfo
			       {
					       PlayerId = PlayerId, Name = Name, AccountLevel = AccountLevel, Vip = Vip, Activity  = Activity
			       };
		}

	}


	public class Friend {

		[ JsonProperty ] public UserInfo user;

	}

	public class Activity  {

		[ JsonProperty ("isOnline") ]        public bool   IsOnline        { get; set; }
		[ JsonProperty ("currentActivity") ] public string CurrentActivity { get; set; } = "offline";
		[ JsonProperty ("lastSeen") ]        public string LastSeen        { get; set; } = "def";
		
	}

	public class OutgoingFriendRequest {

		[ JsonProperty ("to") ] public UserInfo To { get; set; }

	}

	public class IncommingFriendRequest {

		[ JsonProperty ("from") ] public UserInfo From { get; set; }

	}

	public class UserInfo {

		[ JsonProperty ("playerid") ] public int PlayerId { get; set; }

		[ JsonProperty ("name") ]         public string Name         { get; set; }
		[ JsonProperty ("accountlevel") ] public int    AccountLevel { get; set; }
		[ JsonProperty ("vip") ]          public bool   Vip          { get; set; }

		[ JsonProperty ("activity") ] public Activity Activity { get; set; }
		

	}


	public class ApiCodeResponse
	{
		[JsonProperty("code")] public int code { get; set; }
		[JsonProperty("message")] public string message { get; set; }
	}

	public class InitResponse {

		[ JsonProperty ("allowed") ] public bool Allowed { get; set; }

		[ JsonProperty ("message") ] public string Message { get; set; }

		[ JsonProperty ("key") ] public string Key { get; set; }

	}

	public class Item {

		[ JsonProperty ] public int    ID       { get; set; } = - 1;
		[ JsonProperty ] public string Name     { get; set; } = "undefined";
		[ JsonProperty ] public int    Quantity { get; set; }

	}


	public class MessageSchema {

		[ JsonProperty ] public int         senderID   { get; set; }
		[ JsonProperty ] public int         ReciverID  { get; set; }
		[ JsonProperty ] public string      senderName { get; set; }
		[ JsonProperty ] public string      content    { get; set; }
		[ JsonProperty ] public bool        isRead     { get; set; }
		[ JsonProperty ] public DateTime    sentAt     { get; set; }
		[ JsonProperty ] public chatChannel channel    { get; set; }

	}

}
