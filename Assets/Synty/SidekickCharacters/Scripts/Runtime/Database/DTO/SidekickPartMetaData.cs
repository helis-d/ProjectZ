// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_pmdata")]
    public class SidekickPartMetaData
    {
        [PrimaryKey, Column("id")]
        public int ID { get; set; }
        [Column("part_guid")]
        public string PartGuid { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("value")]
        public string Value { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("value_type")]
        public string ValueType { get; set; }
        [Column("last_updated")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        ///     Gets a list of part guids from the meta data based on the passed in type and value.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="type">The type of metadata to search for.</param>
        /// <param name="value">The value of the metadata to search for.</param>
        /// <returns>A list of part guids that match the given criteria.</returns>
        public static List<string> GetPartGuidsByMetaDataValue(DatabaseManager dbManager, string type, string value)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickPartMetaData>()
                .Where(meta => meta.Type == type && meta.Value == value)
                .Select(meta => meta.PartGuid)
                .ToList();
        }
    }
}
