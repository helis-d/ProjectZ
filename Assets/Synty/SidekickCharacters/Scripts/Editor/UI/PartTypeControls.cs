// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synty.SidekickCharacters.Synty.SidekickCharacters.Scripts.Editor.UI
{
    public class PartTypeControls
    {
        public CharacterPartType PartType;
        public Button ClearButton;
        public Button PreviousButton;
        public Button NextButton;
        public Button RandomButton;
        public PopupField<string> PartDropdown;

        public PartTypeControls()
        {
        }

        public PartTypeControls(CharacterPartType partType, Button clearButton, Button previousButton, Button nextButton, Button randomButton, PopupField<string> partDropdown)
        {
            PartType = partType;
            ClearButton = clearButton;
            PreviousButton = previousButton;
            NextButton = nextButton;
            RandomButton = randomButton;
            PartDropdown = partDropdown;
        }

        public void UpdateControls()
        {
            PartDropdown.SetEnabled(PartDropdown.choices.Count > 0);

            if (PartType != CharacterPartType.Wrap)
            {
                ClearButton.SetEnabled(PartDropdown.value != "None");
            }
            else
            {
                ClearButton.SetEnabled(false);
                if (string.IsNullOrEmpty(PartDropdown.value))
                {
                    PartDropdown.SetEnabled(false);
                }
            }

            if (PartDropdown.choices.Count > 1)
            {
                PreviousButton.SetEnabled(PartDropdown.index >= 1);

                NextButton.SetEnabled(PartDropdown.index < PartDropdown.choices.Count - 1);
            }
            else
            {
                PreviousButton.SetEnabled(false);
                NextButton.SetEnabled(false);
            }

            RandomButton.SetEnabled(PartDropdown.choices.Count > 2);
        }

        public void UpdateDropdownValues(List<string> newValues)
        {
            PartDropdown.choices = newValues;
            PartDropdown.MarkDirtyRepaint();
        }

        public void SetPartDropdownValue(string value)
        {
            PartDropdown.value = value;
        }

        public void RandomisePartDropdownValue()
        {
            int lowValue = PartType == CharacterPartType.Wrap ? 0 : 1;
            PartDropdown.index = Random.Range(lowValue, PartDropdown.choices.Count);
        }
    }
}
