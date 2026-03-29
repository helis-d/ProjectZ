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
    public class SerializedColorSet
    {
        public int Species { get; set; }
        public string Name { get; set; }
        public string SourceColorPath { get; set; }
        public string SourceMetallicPath { get; set; }
        public string SourceSmoothnessPath { get; set; }
        public string SourceReflectionPath { get; set; }
        public string SourceEmissionPath { get; set; }
        public string SourceOpacityPath { get; set; }

        public SerializedColorSet()
        {
        }

        public void PopulateFromSidekickColorSet(SidekickColorSet colorSet, SidekickSpecies defaultSpecies)
        {
            Species = colorSet.Species?.ID ?? defaultSpecies.ID;
            Name = colorSet.Name;
            SourceColorPath = colorSet.SourceColorPath;
            SourceMetallicPath = colorSet.SourceMetallicPath;
            SourceSmoothnessPath = colorSet.SourceSmoothnessPath;
            SourceReflectionPath = colorSet.SourceReflectionPath;
            SourceEmissionPath = colorSet.SourceEmissionPath;
            SourceOpacityPath = colorSet.SourceOpacityPath;
        }

        public SidekickColorSet CreateSidekickColorSet(DatabaseManager db)
        {
            SidekickSpecies species = SidekickSpecies.GetByID(db, Species);
            return new SidekickColorSet
            {
                ID = -1,
                Species = species,
                Name = Name,
                SourceColorPath = SourceColorPath,
                SourceMetallicPath = SourceMetallicPath,
                SourceSmoothnessPath = SourceSmoothnessPath,
                SourceReflectionPath = SourceReflectionPath,
                SourceEmissionPath = SourceEmissionPath,
                SourceOpacityPath = SourceOpacityPath
            };
        }
    }
}
