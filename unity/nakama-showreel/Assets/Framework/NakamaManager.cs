/**
 * Copyright 2017 The Nakama Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Nakama;
using Nakama.TinyJson;
using UnityEngine;

namespace Framework
{
    public class NakamaManager : Singleton<NakamaManager>
    {
        //internal const string HostIp = "10.10.10.112";
        internal const string HostIp = "165.227.128.175";
        internal const int Port = 7350;
        internal const bool UseSsl = false;
        internal const string ServerKey = "defaultkey";
        private string deviceId;
        private ISocket _socket;
        
        
        // TODO : remove this? do i need this?
        private const int MaxReconnectAttempts = 5;

        private readonly Queue<Action> _dispatchQueue = new Queue<Action>();
        private readonly Client _client;
        private readonly Action<ISession> _sessionHandler;

        public static event EventHandler AfterConnected = (sender, evt) => { };
        public static event EventHandler AfterDisconnected = (sender, evt) => { };

        // TODO : need to rewrite this i guess , there is no INError anymore
        /*private static readonly Action<INError> ErrorHandler = err =>
        {
            if (err.Code == ErrorCode.GroupNameInuse)
            {
                // This is caused by the FakeDataLoader and is expected. We can ignore this safely in this case.
                return;
            }
            Logger.LogErrorFormat("Error: code '{0}' with '{1}'.", err.Code, err.Message);
        };*/

        // Flag to tell us whether the socket was closed intentially or not and whether to attempt reconnect.
        private bool _doReconnect = true;

        private uint _reconnectCount;

        public ISession Session { get; private set; }

        protected override async void Awake()
        {
            base.Awake();
            deviceId = SystemInfo.deviceUniqueIdentifier;                        
        }

        private NakamaManager()
        {            
            _client = new Client(ServerKey , HostIp , Port , UseSsl);        
            _socket = _client.CreateWebSocket();                          
            _socket.OnConnect += OnSocketConnected;
            _socket.OnDisconnect += OnSocketDisconnected;
            _socket.OnChannelMessage += OnChannelMessage;
            // TODO : this callback must be implemented for sockets i guess now                           
            //_client.OnError = error => ErrorHandler(error);
        }

        public void OnChannelMessage(object sender , IApiChannelMessage message)
        {
            var chatMessages = StateManager.Instance.ChatMessages;
            foreach (var topic in chatMessages.Keys)
            {               
                if (topic.Equals(message.ChannelId))
                {
                    chatMessages[topic].Add(message.MessageId , message);
                }                
            }
        }
        public void OnSocketConnected(object sender, object evt)
        {
            // cache session for quick reconnects
            Debug.LogFormat($"On Socket Connected");
            _reconnectCount = 0;            
            _dispatchQueue.Enqueue(() =>
            {
                PlayerPrefs.SetString("nk.session", Session.AuthToken);
                AfterConnected(this, EventArgs.Empty);
            });
        }

        public void OnSocketDisconnected(object sender, object evt)
        {
            if (_doReconnect && _reconnectCount < MaxReconnectAttempts)
            {
                _reconnectCount++;
                _dispatchQueue.Enqueue(() =>
                {
                    var enumerator = Reconnect();
                });
            }
            else
            {
                _dispatchQueue.Clear();
                _dispatchQueue.Enqueue(() => { AfterDisconnected(this, EventArgs.Empty); });
            }
        }
        

        public async void Authenticate()
        {
            Session = await _client.AuthenticateDeviceAsync(deviceId);
            await _socket.ConnectAsync(Session);
            Logger.LogFormat("Session: '{0}'.", Session.AuthToken);                                  
        }
        // Restore serialised session token from PlayerPrefs
        // If the token doesn't exist or is expired `null` is returned.
        private ISession RestoreSession()
        {
            if (Session == null)
            {
                var cachedSession = PlayerPrefs.GetString("nk.session");
                if (string.IsNullOrEmpty(cachedSession))
                {
                    Logger.Log("No Session in PlayerPrefs.");
                    return null;
                }
                Session = Nakama.Session.Restore(cachedSession);
                if (!Session.HasExpired(DateTime.UtcNow))
                {
                    return Session;
                }                
            }
            else
            {
                if (Session.HasExpired(DateTime.UtcNow))
                {
                    Session = Nakama.Session.Restore(Session.AuthToken);
                    return Session;
                }                
            }
            Logger.Log("Session expired.");
            return null;
            
        }
        // This method connects the client to the server and
        // if neccessary authenticates with the server
        public async void Connect()
        {
            
            Authenticate();
            // Check to see if we have a valid session token we can restore
            /*var session = RestoreSession();            
            if (session != null)
            {
                // Session is valid, let's connect.
                _sessionHandler(session);                
            }
            else
            {
                // Session is not valid or we dont have any session right now , reauthenticate
                Authenticate();                
            }*/
        }
        private IEnumerator Reconnect()
        {
            // if it's the first time disconnected, then attempt to reconnect immediately
            // every other time, wait 10,20,30,40,50 seconds each time 
            var reconnectTime = ((_reconnectCount - 1) + 10) * 60;
            yield return new WaitForSeconds(reconnectTime);
            _sessionHandler(Session);
        }        

        private void Update()
        {
            for (int i = 0, l = _dispatchQueue.Count; i < l; i++)
            {
                _dispatchQueue.Dequeue()();
            }
        }

        private async void OnApplicationQuit()
        {
            _doReconnect = false;
            await _socket.DisconnectAsync();
        }

        private async void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                _doReconnect = false;
                await _socket.DisconnectAsync();
                return;
            }

            // let's re-authenticate (if neccessary) and reconnect to the server.
            _doReconnect = true;
            Connect();
        }
       
        public async void SelfFetch()
        {
            var userAccount = await _client.GetAccountAsync(Session);

            StateManager.Instance.SelfInfo = userAccount;
        }

        public async void FriendAdd(string userId , bool refreshList)
        {
            List<string> items;
            items = new List<string>{userId};
            await _client.AddFriendsAsync(Session, items);
            if (refreshList)
            {
                FriendsList();
            }
        }
        public async void FriendAdd(string[] usernames , string[] ids , bool refreshList)
        {
            await _client.AddFriendsAsync(Session, ids, usernames);
            if (refreshList)
            {
                FriendsList();   
            }            
        }

        public async void FriendRemove(string userId)
        {
            List<string> items;
            items = new List<string> {userId};
            await _client.DeleteFriendsAsync(Session, items);
            FriendsList();
            
        }
        public async void FriendRemove(string[] usernames , string[] ids)
        {
            await _client.DeleteFriendsAsync(Session, ids, usernames);
            FriendsList();            
        }
        /// <summary>
        /// This Method Will Update FriendList in StateManager 
        /// </summary>
        public async void FriendsList()
        {
            StateManager.Instance.Friends.Clear();
            var friendListResult = await _client.ListFriendsAsync(Session);
            StateManager.Instance.Friends.AddRange(friendListResult.Friends);                     
        }

        public async void GroupsList(string nameFilter , int limit , bool appendList)
        {

            
            var groupListResult = await _client.ListGroupsAsync(Session, nameFilter, limit);
            var groupsList = groupListResult.Groups;            
            if (!appendList)
            {
                StateManager.Instance.SearchedGroups.Clear();                
            }            
            foreach (var group in groupsList)
            {
                StateManager.Instance.SearchedGroups.Add(group);   
            }
            if (groupListResult.Cursor != null && groupListResult.Cursor.Equals(""))
            {                
                GroupsList(nameFilter , limit, true , groupListResult.Cursor);
            }        
        }
        public async void GroupsList(string nameFilter, int limit, bool appendList, string cursor)
        {
            var groupListResult = await _client.ListGroupsAsync(Session, nameFilter, limit, cursor);
            var groupList = groupListResult.Groups;
            if (!appendList)
            {
                StateManager.Instance.SearchedGroups.Clear();
            }

            foreach (var group in groupList)
            {
                StateManager.Instance.SearchedGroups.Add(group);
            }
            // Recursively fetch the next set of groups and append
            if (groupListResult.Cursor != null && groupListResult.Cursor.Equals(""))
            {
                GroupsList(nameFilter , limit  , true , groupListResult.Cursor);
            }
            
        }

        public async void GroupJoin(string groupId, bool refreshList = true)
        {
            await _client.JoinGroupAsync(Session, groupId);
            if (refreshList)
            {
                JoinedGroupsList();
            }            
        }

        public async void JoinedGroupsList()
        {
            SelfFetch();
            var userId = StateManager.Instance.SelfInfo.User.Id;
            StateManager.Instance.JoinedGroups.Clear();
            var userGroupList = await _client.ListUserGroupsAsync(Session, userId);
            StateManager.Instance.JoinedGroups.AddRange(userGroupList.UserGroups);            
        }

        public async void GroupCreate(string groupName , string groupDesc)
        {
            var group = await _client.CreateGroupAsync(Session, groupName, groupDesc);
            Debug.LogFormat($"New Group Created : {group.Id}");
        }

        public async void TopicJoin(string roomNameOrUserId , ChannelType channelType)
        {
            var channel = await _socket.JoinChatAsync(roomNameOrUserId, channelType, true, false);
            Debug.LogFormat($"You Can now send messages to channel id : {channel.Id}");
            StateManager.Instance.Topics.Add(roomNameOrUserId , channel.Id);
            StateManager.Instance.ChatMessages.Add(channel.Id , new Dictionary<string, IApiChannelMessage>());
        }

        public async void TopicMessageList(string channelId, int messageCount, bool forward , bool appendList , int maxMessages)
        {
            var result = await _client.ListChannelMessagesAsync(Session, channelId, messageCount, forward);
            if (!appendList)
            {
                StateManager.Instance.ChatMessages[channelId].Clear();                    
            }

            foreach (var message in result.Messages)
            {
                // check to see if ChatMessages has 'maxMessages' messages.
                if (StateManager.Instance.ChatMessages[channelId].Count >= maxMessages)
                {
                    return;
                }                                     
                StateManager.Instance.ChatMessages[channelId].Add(message.MessageId , message);
            }

            if (!string.IsNullOrEmpty(result.NextCursor))
            {
                TopicMessageList(channelId , messageCount , forward , appendList , maxMessages , result.NextCursor);
            }
        }

        public async void TopicMessageList(string channelId, int messageCount, bool forward, bool appendList,
            int maxMessages, string cursor)
        {
            var result = await _client.ListChannelMessagesAsync(Session, channelId, messageCount, forward, cursor);
            if (!appendList)
            {
                StateManager.Instance.ChatMessages[channelId].Clear();
            }

            foreach (var message in result.Messages)
            {
                // check to see if ChatMessages has 'maxMessages' messages.
                if (StateManager.Instance.ChatMessages[channelId].Count >= maxMessages)
                {
                    return;
                }
                StateManager.Instance.ChatMessages[channelId].Add(message.MessageId , message);
            }

            if (!string.IsNullOrEmpty(result.NextCursor))
            {
                TopicMessageList(channelId , messageCount , forward , appendList , maxMessages , result.NextCursor);
            }
            
        }

        public async void TopicSendMessage(string channelId , string messageJson)
        {
            
            IChannelMessageAck messageAck = await _socket.WriteChatMessageAsync(channelId, messageJson);
            Debug.LogFormat($"message sent to {channelId} , messageAck : {messageAck}");
        }

        public async void NotificationsList(int notificationCount , bool appendList)
        {
            
            var result = await _client.ListNotificationsAsync(Session, notificationCount);
            if (!appendList)
            {
                StateManager.Instance.Notifications.Clear();
            }
            foreach (var notification in result.Notifications)
            {
                StateManager.Instance.Notifications.Add(notification);
            }

            if (!string.IsNullOrEmpty(result.CacheableCursor))
            {
                NotificationsList(notificationCount , appendList , result.CacheableCursor);
            }
        }

        public async void NotificationsList(int notificationCount, bool appendList, string cursor)
        {
            var result = await _client.ListNotificationsAsync(Session, notificationCount, cursor);
            if (!appendList)
            {
                StateManager.Instance.Notifications.Clear();
            }
            foreach (var notification in result.Notifications)
            {
                StateManager.Instance.Notifications.Add(notification);
            }

            if (!string.IsNullOrEmpty(result.CacheableCursor))
            {
                NotificationsList(notificationCount , appendList , result.CacheableCursor);
            }
        }              
    }
}