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
    [Table("sk_color_filter")]
    public class SidekickColorFilter
    {
        [PrimaryKey, Column("id")]
        public int ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        ///     Gets a list of all the Color Filters in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Color Filters in the database.</returns>
        public static List<SidekickColorFilter> GetAll(DatabaseManager dbManager)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickColorFilter>().ToList();
        }

        /// <summary>
        ///     Gets a specific Color Filter by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Filter.</param>
        /// <returns>The specific Color Filter if it exists; otherwise null.</returns>
        public static SidekickColorFilter GetByID(DatabaseManager dbManager, int id)
        {
            return dbManager.GetCurrentDbConnection().Get<SidekickColorFilter>(id);
        }
    }
}
