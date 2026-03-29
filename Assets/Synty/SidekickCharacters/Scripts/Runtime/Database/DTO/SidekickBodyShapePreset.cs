// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_body_shape_preset")]
    public class SidekickBodyShapePreset
    {
        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("body_type")]
        public int BodyType { get; set; }
        [Column("body_size")]
        public int BodySize { get; set; }
        [Column("musculature")]
        public int Musculature { get; set; }

        /// <summary>
        ///     Gets a list of all the Preset Body Shape in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Preset Body Shapes in the database.</returns>
        public static List<SidekickBodyShapePreset> GetAll(DatabaseManager dbManager)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickBodyShapePreset>().ToList();
        }

        /// <summary>
        ///     Gets a specific Preset Body Shape by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Body Shape.</param>
        /// <returns>The specific Preset Body Shape if it exists; otherwise null.</returns>
        public static SidekickBodyShapePreset GetByID(DatabaseManager dbManager, int id)
        {
            return dbManager.GetCurrentDbConnection().Get<SidekickBodyShapePreset>(id);
        }

        /// <summary>
        ///     Updates or Inserts this item in the Database.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public int Save(DatabaseManager dbManager)
        {
            if (ID < 0)
            {
                dbManager.GetCurrentDbConnection().Insert(this);
                // in theory this could return a different ID, but in practice it's highly unlikely
                ID = (int) SQLite3.LastInsertRowid(dbManager.GetCurrentDbConnection().Handle);
            }
            dbManager.GetCurrentDbConnection().Update(this);
            return ID;
        }

        /// <summary>
        ///     Deletes this item from the database
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            dbManager.GetCurrentDbConnection().Delete<SidekickBodyShapePreset>(ID);
        }
    }
}
