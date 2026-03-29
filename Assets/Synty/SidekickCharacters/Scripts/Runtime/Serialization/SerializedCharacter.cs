// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using System;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Serialization
{
    [Serializable]
    public class SerializedCharacter
    {
        public string Name { get; set; }
        public int Species { get; set; }
        public List<SerializedPart> Parts { get; set; }
        public SerializedColorSet ColorSet { get; set; }
        public List<SerializedColorRow> ColorRows { get; set; }
        public SerializedBlendShapeValues BlendShapes { get; set; } = new SerializedBlendShapeValues();

        public SerializedCharacter()
        {
        }
    }
}
