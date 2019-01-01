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

using System.Linq;
using Framework;
using Nakama;
using UnityEngine;
using UnityEngine.UI;

namespace Showreel
{
    public class UserAccountView : MonoBehaviour
    {
        private Text _selfInfoText;

        private void Start()
        {
            _selfInfoText = GameObject.Find("SelfInfoText").GetComponent<Text>();

            NakamaManager.Instance.SelfFetch();
        }
        // NOTE : there was a handle property here , but i couldn't find it 
        private void Update()
        {
            var self = StateManager.Instance.SelfInfo;
            if (self == null)
            {
                return;
            }
            var selfText = $"Id : {self.User.Id} , Display Name : {self.User.DisplayName} , Device ID : {self.Devices.FirstOrDefault()?.Id}";            
            _selfInfoText.text = selfText;
        }
    }
}