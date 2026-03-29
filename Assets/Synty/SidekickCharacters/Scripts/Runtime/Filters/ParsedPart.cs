// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Synty.SidekickCharacters.Filters
{
    public class ParsedPart
    {
        public string Species;
        public string Outfit1;
        public string Outfit2;
        public string PartArea;
        public string Filename;
        public string Name;

        public ParsedPart(string partName, string name)
        {
            Species = partName.Substring(partName.LastIndexOf('_') + 1, 2);
            Outfit1 = partName.Substring(3, 4);
            Outfit2 = partName.Substring(8, 4);
            PartArea = partName.Substring(18, 4);
            Filename = partName;
            Name = name;
        }
    }
}
