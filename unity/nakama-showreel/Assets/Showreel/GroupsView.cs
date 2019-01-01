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
    public class GroupsView : MonoBehaviour
    {
        private Text _groupInfoLabel;
        private Button _joinGroupButton;
        private Dropdown _joinedGroupsSelectorDropdown;
        private Dropdown _allGroupsDropdown;
        private InputField _langField;

        private string _groupInfo = "";

        // Use this for initialization
        private void Start()
        {
            NakamaManager.Instance.JoinedGroupsList();

            _groupInfoLabel = GameObject.Find("GroupInfoLabel").GetComponent<Text>();
            _langField = GameObject.Find("LangField").GetComponent<InputField>();
            _joinGroupButton = GameObject.Find("JoinGroupButton").GetComponent<Button>();
            _joinedGroupsSelectorDropdown = GameObject.Find("JoinedGroupsDropdown").GetComponent<Dropdown>();
            _joinedGroupsSelectorDropdown.onValueChanged.AddListener(delegate { JoinedGroupsDropdownOnChange(); });
            _allGroupsDropdown = GameObject.Find("AllGroupsDropdown").GetComponent<Dropdown>();
            _allGroupsDropdown.onValueChanged.AddListener(delegate { AllGroupsDropdownOnChange(); });

            _groupInfo = "";
            _joinGroupButton.interactable = false;
            _joinedGroupsSelectorDropdown.options.Clear();
            _joinedGroupsSelectorDropdown.interactable = false;
            _allGroupsDropdown.options.Clear();
            _allGroupsDropdown.interactable = false;
        }

        private void Update()
        {
            // check if dropdown is already populated
            if (_joinedGroupsSelectorDropdown.options.Count != StateManager.Instance.JoinedGroups.Count)
            {
                // lets add group names to the dropdown
                List<Dropdown.OptionData> groupList = new List<Dropdown.OptionData>();
                for (var i = 0; i < StateManager.Instance.JoinedGroups.Count; i++)
                {
                    groupList.Add(new Dropdown.OptionData(StateManager.Instance.JoinedGroups[i].Group.Name));
                }

                _joinedGroupsSelectorDropdown.interactable = true;
                _joinedGroupsSelectorDropdown.options.Clear();
                _joinedGroupsSelectorDropdown.options.AddRange(groupList);
                _joinedGroupsSelectorDropdown.value = 0;
                _joinedGroupsSelectorDropdown.RefreshShownValue();
                JoinedGroupsDropdownOnChange();
            }

            // check if dropdown is already populated
            if (_allGroupsDropdown.options.Count != StateManager.Instance.SearchedGroups.Count)
            {
                // lets add group names to the dropdown
                List<Dropdown.OptionData> groupList = new List<Dropdown.OptionData>();
                for (var i = 0; i < StateManager.Instance.SearchedGroups.Count; i++)
                {
                    groupList.Add(new Dropdown.OptionData(StateManager.Instance.SearchedGroups[i].Name));
                }

                _allGroupsDropdown.interactable = true;
                _allGroupsDropdown.options.Clear();
                _allGroupsDropdown.options.AddRange(groupList);
                _allGroupsDropdown.value = 0;
                _allGroupsDropdown.RefreshShownValue();
                AllGroupsDropdownOnChange();
            }

            _groupInfoLabel.text = _groupInfo;
        }

        private void JoinedGroupsDropdownOnChange()
        {
            if (_joinedGroupsSelectorDropdown.options.Count == 0)
            {
                _joinedGroupsSelectorDropdown.interactable = false;
                _groupInfo = "";
                return;
            }

            var group = StateManager.Instance.JoinedGroups[_joinedGroupsSelectorDropdown.value];
            var state = "";
            switch (group.State)
            {
                case 0:
                    state = "Super Admin";
                    break;
                case 1:
                    state = "Admin";
                    break;                
                case 2:
                    state = "Member";
                    break;
                case 3:
                    state = "Join request sent";
                    break;
            }

            _groupInfo = string.Format(@"
Id: {0}
Name: {1}
Description: {2}
Language: {3}
Private Group: {4}
Member count: {5}
State: {6}
			", group.Group.Id, group.Group.Name, group.Group.Description, group.Group.LangTag, group.Group.Open ? "Yes" : "No", group.Group.EdgeCount, state);

            _joinGroupButton.interactable = false;
        }

        private void AllGroupsDropdownOnChange()
        {
            if (_allGroupsDropdown.options.Count == 0)
            {
                _allGroupsDropdown.interactable = false;
                _groupInfo = "";
                return;
            }

            var group = StateManager.Instance.SearchedGroups[_allGroupsDropdown.value];
            _groupInfo = string.Format(@"
Id: {0}
Name: {1}
Description: {2}
Language: {3}
Private Group: {4}
Member count: {5}
			", group.Id, group.Name, group.Description, group.LangTag, group.Open? "Yes" : "No", group.EdgeCount);

            // check to see if we've already joined this group, 
            // and if so, disable the join button
            bool alreadyJoinedGroup = false;
            for (var i = 0; i < StateManager.Instance.JoinedGroups.Count; i++)
            {
                if (StateManager.Instance.JoinedGroups[i].Group.Id.Equals(group.Id))
                {
                    alreadyJoinedGroup = true;
                    break;
                }
            }

            _joinGroupButton.interactable = !alreadyJoinedGroup;
        }

        public void SearchGroups()
        {
            var lang = _langField.text;            
            NakamaManager.Instance.GroupsList(lang , 10 , true);
        }

        public void JoinGroup()
        {
            var group = StateManager.Instance.SearchedGroups[_allGroupsDropdown.value];            
            NakamaManager.Instance.GroupJoin(group.Id);
        }
    }
}