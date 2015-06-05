using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using MvcSignal.Models;
using Microsoft.AspNet.SignalR.Hubs;

namespace MvcSignal
{
    public class MyHub : Hub
    {
        static List<UserInfo> UsersList = new List<UserInfo>();
        static List<MessageInfo> MessageList = new List<MessageInfo>();

        //-->>>>> ***** Receive Request From Client [  Connect  ] *****
        public void Connect(string userName, string password)
        {
            var id = Context.ConnectionId;
            string userGroup = "";
            //Manage Hub Class
            //if freeflag==0 ==> Busy
            //if freeflag==1 ==> Free

            //if tpflag==0 ==> User
            //if tpflag==1 ==> Admin


            var ctx = new TestEntities();

            var userInfo =
                 (from m in ctx.tbl_User
                  where m.UserName == userName && m.Password == password
                  select new { m.UserID, m.UserName, m.AdminCode, m.ImagePath, userGroup = m.tbl_Dep.DepName }).FirstOrDefault();


            try
            {
                //You can check if user or admin did not login before by below line which is an if condition
                //if (UsersList.Count(x => x.ConnectionId == id) == 0)

                //Here you check if there is no userGroup which is same DepID --> this is User otherwise this is Admin
                //userGroup = DepID


                if (true) //((int)userInfo.AdminCode == 0)
                {
                    //now we encounter ordinary user which needs userGroup and at this step, system assigns the first of free Admin among UsersList

                    /*I don't need this here
                     var strg = (from s in UsersList where (s.tpflag == "1") && (s.freeflag == "1") select s).First();
                     userGroup = strg.UserGroup;
                     * strg.freeflag = "0";
                     * 
                     */

                    //Admin becomes busy so we assign zero to freeflag which is shown admin is busy

                    /*Changed userInfo.userGroup=>userGroup*/
                    //now add USER to UsersList

                    //var id = Context.ConnectionId;


                    if (UsersList.Count(x => x.ConnectionId == id) == 0)
                    {
                        var UserNew = (from s in UsersList where (s.UserID == userInfo.UserID) select s).ToList();
                        if (UserNew.Count()>0) { }
                        else
                        {


                            UsersList.Add(new UserInfo { ConnectionId = id, UserID = userInfo.UserID, UserName = userName, UserGroup = userInfo.userGroup, freeflag = "0", tpflag = "0", ImagePath = userInfo.ImagePath, });
                            //whether it is Admin or User now both of them has userGroup and I Join this user or admin to specific group 
                            //  Groups.Add(Context.ConnectionId, userGroup);
                            // Clients.Caller.onConnected(id, userName, userInfo.UserID, userGroup, userInfo.ImagePath, UsersList);
                            // send to caller
                            Clients.Caller.onConnected(id, userName, userInfo.UserID, userGroup, userInfo.ImagePath, UsersList, MessageList);

                            // send to all except caller client
                            Clients.AllExcept(id).onNewUserConnected(id, userName);
                        }
                    }

                }
                else
                {
                    //If user has admin code so admin code is same userGroup
                    //now add ADMIN to UsersList
                    UsersList.Add(new UserInfo { ConnectionId = id, AdminID = userInfo.UserID, UserName = userName, UserGroup = userInfo.userGroup, freeflag = "1", tpflag = "1", ImagePath = userInfo.ImagePath });
                    //whether it is Admin or User now both of them has userGroup and I Join this user or admin to specific group 
                    Groups.Add(Context.ConnectionId, userInfo.userGroup);
                    Clients.Caller.onConnected(id, userName, userInfo.UserID, userInfo.userGroup, userInfo.ImagePath);

                }




            }

            catch
            {
                string msg = "All Administrators are busy, please be patient and try again";
                //***** Return to Client *****
                Clients.Caller.NoExistAdmin();

            }


        }
        // <<<<<-- ***** Return to Client [  NoExist  ] *****
        /// <summary>
        /// Send Messege To Specific Person
        /// </summary>
        /// <param name="toUserId"></param>
        /// <param name="message"></param>
        public void SendPrivateMessage(string toUserId, string message)
        {

            string fromUserId = Context.ConnectionId;

            var toUser = UsersList.FirstOrDefault(x => x.ConnectionId == toUserId);
            var fromUser = UsersList.FirstOrDefault(x => x.ConnectionId == fromUserId);

            if (toUser != null && fromUser != null)
            {
                // send to 
                Clients.Client(toUserId).sendPrivateMessage(fromUserId, fromUser.UserName, message);

                // send to caller user
                Clients.Caller.sendPrivateMessage(toUserId, fromUser.UserName, message);
            }

        }

        //--group ***** Receive Request From Client [  SendMessageToGroup  ] *****
        public void SendMessageToGroup(string userName, string message)
        {

            if (UsersList.Count != 0)
            {
                var strg = (from s in UsersList where (s.UserName == userName) select s).First();
                MessageList.Add(new MessageInfo { UserName = userName, Message = message, UserGroup = strg.UserGroup, ImagePath = strg.ImagePath });
                string strgroup = strg.UserGroup;
                // If you want to Broadcast message to all UsersList use below line
                // Clients.All.getMessages(userName, message);

                //If you want to establish peer to peer connection use below line so message will be send just for user and admin who are in same group
                //***** Return to Client *****
                Clients.Group(strgroup).getMessages(userName, message, strg.ImagePath);
            }

        }
        // <<<<<-- ***** Return to Client [  getMessages  ] *****


        //--group ***** Receive Request From Client ***** { Whenever User close session then OnDisconneced will be occurs }
        public override System.Threading.Tasks.Task OnDisconnected()
        {

            var item = UsersList.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                UsersList.Remove(item);

                var id = Context.ConnectionId;

                Clients.All.onUserDisconnected(id, item.UserName);

                if (item.tpflag == "0")
                {
                    //user logged off == user
                    try
                    {
                        var stradmin = (from s in UsersList where (s.UserGroup == item.UserGroup) && (s.tpflag == "1") select s).First();
                        //become free
                        stradmin.freeflag = "1";
                    }
                    catch
                    {
                        //***** Return to Client *****
                        Clients.Caller.NoExistAdmin();
                    }

                }

                //save conversation to dat abase


            }

            return base.OnDisconnected();
        }
        #region private Messages

        private void AddMessageinCache(string userName, string message)
        {
            MessageList.Add(new MessageInfo { UserName = userName, Message = message });

            if (MessageList.Count > 100)
                MessageList.RemoveAt(0);
        }

        #endregion
    }
}