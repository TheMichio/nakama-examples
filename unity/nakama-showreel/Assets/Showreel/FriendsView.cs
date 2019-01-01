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
using Framework;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

namespace Showreel
{
    public class FriendsView : MonoBehaviour
    {
        private Text _friendInfoLabel;
        private Button _acceptFriendButton;
        private Button _deleteFriendButton;
        private Dropdown _friendSelectorDropdown;

        private string _friendInfo = "";

        // Use this for initialization
        private void Start()
        {
            NakamaManager.Instance.FriendsList();

            _friendInfoLabel = GameObject.Find("FriendInfoLabel").GetComponent<Text>();
            _acceptFriendButton = GameObject.Find("AcceptFriendButton").GetComponent<Button>();
            _deleteFriendButton = GameObject.Find("DeleteFriendButton").GetComponent<Button>();
            _friendSelectorDropdown = GameObject.Find("FriendSelectorDropdown").GetComponent<Dropdown>();
            _friendSelectorDropdown.onValueChanged.AddListener(delegate { FriendSelectorDropdownOnChange(); });

            _friendInfo = "";
            _acceptFriendButton.interactable = false;
            _deleteFriendButton.interactable = false;
            _friendSelectorDropdown.options.Clear();
            _friendSelectorDropdown.interactable = false;
        }

        private void Update()
        {
            // check if dropdown is already populated
            if (_friendSelectorDropdown.options.Count != StateManager.Instance.Friends.Count)
            {
                // lets add friend handles to the dropdown
                List<Dropdown.OptionData> friendList = new List<Dropdown.OptionData>();
                for (var i = 0; i < StateManager.Instance.Friends.Count; i++)
                {
                    friendList.Add(new Dropdown.OptionData(StateManager.Instance.Friends[i].User.Username));
                }

                _friendSelectorDropdown.interactable = true;
                _friendSelectorDropdown.options.Clear();
                _friendSelectorDropdown.options.AddRange(friendList);
                _friendSelectorDropdown.value = 0;
                _friendSelectorDropdown.RefreshShownValue();
                FriendSelectorDropdownOnChange();
            }

            _friendInfoLabel.text = _friendInfo;
        }

        private void FriendSelectorDropdownOnChange()
        {
            if (_friendSelectorDropdown.options.Count == 0)
            {
                _friendSelectorDropdown.interactable = false;
                _friendInfo = "";
                return;
            }

            var friend = StateManager.Instance.Friends[_friendSelectorDropdown.value];
            var state = "";
            switch (friend.State)
            {                
                case 0:
                    state = "Mutual friends";
                    _acceptFriendButton.interactable = false;
                    _deleteFriendButton.interactable = true;
                    break;
                case 1:
                    state = "Sent friend invitation";
                    _acceptFriendButton.interactable = false;
                    _deleteFriendButton.interactable = true;
                    break;
                case 2:
                    state = "Received friend invitation";
                    _acceptFriendButton.interactable = true;
                    _deleteFriendButton.interactable = true;
                    break;                
                case 3:
                    state = "Blocked user";
                    _acceptFriendButton.interactable = false;
                    _deleteFriendButton.interactable = true;
                    break;
            }

            _friendInfo = string.Format(@"
Id: {0}
Display Name: {1}
Currently Online: {2}
State: {3}
			", friend.User.Id, friend.User.DisplayName, friend.User.Online ? "Yes" : "No", state);
        }

        public void DeleteFriend()
        {
            var friend = StateManager.Instance.Friends[_friendSelectorDropdown.value];
            NakamaManager.Instance.FriendRemove(friend.User.Id);
        }

        public void AcceptFriend()
        {
            var friend = StateManager.Instance.Friends[_friendSelectorDropdown.value];
            NakamaManager.Instance.FriendAdd(friend.User.Id , true);
        }
    }
}