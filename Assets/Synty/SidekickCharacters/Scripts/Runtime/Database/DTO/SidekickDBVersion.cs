// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_vdata")]
    public class SidekickDBVersion
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("semantic_version")]
        public string SemanticVersion { get; set; }
        [Column("update_time")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        ///     Updates or Inserts this item in the Database.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public int Save(DatabaseManager dbManager)
        {
            if (ID <= 0)
            {
                dbManager.GetCurrentDbConnection().Insert(this);
                // in theory this could return a different ID, but in practice it's highly unlikely
                ID = (int) SQLite3.LastInsertRowid(dbManager.GetCurrentDbConnection().Handle);
            }
            dbManager.GetCurrentDbConnection().Update(this);
            return ID;
        }
    }
}
