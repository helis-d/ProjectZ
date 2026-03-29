// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using System;

namespace Synty.SidekickCharacters.Serialization
{
    [Serializable]
    public class SerializedColorRow
    {
        public int ColorProperty { get; set; }
        public string MainColor { get; set; }
        public string Metallic { get; set; }
        public string Smoothness { get; set; }
        public string Reflection { get; set; }
        public string Emission { get; set; }
        public string Opacity { get; set; }

        // Empty constructor for serialization purposes.
        public SerializedColorRow()
        {

        }

        public SerializedColorRow(SidekickColorRow row)
        {
            ColorProperty = row.PtrColorProperty;
            MainColor = row.MainColor;
            Metallic = row.Metallic;
            Smoothness = row.Smoothness;
            Reflection = row.Reflection;
            Emission = row.Emission;
            Opacity = row.Opacity;
        }

        public SidekickColorRow CreateSidekickColorRow(DatabaseManager db, SidekickColorSet set)
        {
            SidekickColorProperty property = SidekickColorProperty.GetByID(db, ColorProperty);
            return new SidekickColorRow
            {
                ID = -1,
                ColorSet = set,
                ColorProperty = property,
                MainColor = MainColor,
                Metallic = Metallic,
                Smoothness = Smoothness,
                Reflection = Reflection,
                Emission = Emission,
                Opacity = Opacity
            };
        }
    }
}
