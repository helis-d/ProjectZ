// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Enums;
using System;

namespace Synty.SidekickCharacters.Serialization
{
    [Serializable]
    public class SerializedPart
    {
        public string Name { get; set; }
        public CharacterPartType PartType { get; set; }
        public string PartVersion { get; set; }

        public SerializedPart()
        {
        }

        public SerializedPart(string name, CharacterPartType partType, string partVersion)
        {
            Name = name;
            PartType = partType;
            PartVersion = partVersion;
        }
    }
}

