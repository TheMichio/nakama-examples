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

using System.Collections.Generic;
using Nakama;

namespace Framework
{
    public class StateManager : Singleton<StateManager>
    {
        public IUserPresence SelfInfo { get; internal set; }
       
        public readonly List<IApiFriend> Friends = new List<IApiFriend>();
        public readonly List<IApiGroup> SearchedGroups = new List<IApiGroup>();
        //public readonly List<INGroupSelf> JoinedGroups = new List<INGroupSelf>();

        // Map of User ID/Room Name to <TopicId, List of messages> for Chat Message
        public readonly Dictionary<string, string> Topics = new Dictionary<string, string>();

        public readonly Dictionary<string , Dictionary<string, IApiChannelMessage>> ChatMessages =
            new Dictionary<string , Dictionary<string, IApiChannelMessage>>();

        public readonly List<IApiNotification> Notifications = new List<IApiNotification>();
    }

    public class TopicMessageComparer : IComparer<IApiChannelMessage>
    {
        public int Compare(IApiChannelMessage x, IApiChannelMessage y)
        {
            if (x == null || y == null)
            {
                return 0;
            }
            return y.CreateTime.CompareTo(x.CreateTime) != 0 ? y.CreateTime.CompareTo(x.CreateTime) : 0;            
        }
    }
}