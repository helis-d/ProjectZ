// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_color_property")]
    public class SidekickColorProperty
    {
        [PrimaryKey, Column("id")]
        public int ID { get; set; }
        // NOTE : automatically converts to the integer mapping of the enum
        [Column("color_group")]
        public ColorGroup Group { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("u")]
        public int U { get; set; }
        [Column("v")]
        public int V { get; set; }

        /// <summary>
        ///     Gets a list of all the Color Properties in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Color Properties in the database.</returns>
        public static List<SidekickColorProperty> GetAll(DatabaseManager dbManager)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickColorProperty>().ToList();
        }

        /// <summary>
        ///     Gets a list of all the Color Properties in the database that have the matching group.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="group">The color group to get all the color properties for.</param>
        /// <returns>A list of all color groups in the database for the given group.</returns>
        public static List<SidekickColorProperty> GetAllByGroup(DatabaseManager dbManager, ColorGroup group)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickColorProperty>().Where(prop => prop.Group == group).ToList();
        }

        /// <summary>
        ///     Gets a specific Color Property by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Property.</param>
        /// <returns>The specific Color Property if it exists; otherwise null.</returns>
        public static SidekickColorProperty GetByID(DatabaseManager dbManager, int id)
        {
            return dbManager.GetCurrentDbConnection().Find<SidekickColorProperty>(id);
        }

        /// <summary>
        ///     Gets a list of properties that match any of the UVs from the given list.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="uVs">The list of UVs to get the properties for.</param>
        /// <returns>A list of properties that match any of the UVs from the given list.</returns>
        public static List<SidekickColorProperty> GetByUVs(DatabaseManager dbManager, List<Vector2> uVs)
        {
            List<SidekickColorProperty> properties = new List<SidekickColorProperty>();
            foreach (Vector2 uv in uVs)
            {
                properties.AddRange(
                    dbManager.GetCurrentDbConnection().Table<SidekickColorProperty>()
                        .Where(prop => prop.U == uv.x && prop.V == uv.y)
                        .ToList()
                );
            }

            return properties;
        }
    }
}
