// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_species")]
    public class SidekickSpecies
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("code")]
        public string Code { get; set; }

        /// <summary>
        ///     Gets a list of all the Species in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="excludeSpeciesWithNoParts">Whether to excludes species that have no active parts</param>
        /// <returns>A list of all species in the database.</returns>
        public static List<SidekickSpecies> GetAll(DatabaseManager dbManager, bool excludeSpeciesWithNoParts = true)
        {
            List<SidekickSpecies> allSpecies = dbManager.GetCurrentDbConnection().Table<SidekickSpecies>().ToList();

            List<SidekickSpecies> filteredSpecies = allSpecies;
            if (excludeSpeciesWithNoParts)
            {
                filteredSpecies = new List<SidekickSpecies>();
                foreach (SidekickSpecies species in allSpecies)
                {
                    List<SidekickPart> allParts = SidekickPart.GetAllForSpecies(dbManager, species);
                    if (allParts.Count > 0 || species.Name.Equals("Unrestricted", StringComparison.OrdinalIgnoreCase))
                    {
                        filteredSpecies.Add(species);
                    }
                }
            }

            return filteredSpecies;
        }

        /// <summary>
        ///     Gets a specific Species by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Species.</param>
        /// <returns>The specific Species if it exists; otherwise null.</returns>
        public static SidekickSpecies GetByID(DatabaseManager dbManager, int id)
        {
            return dbManager.GetCurrentDbConnection().Get<SidekickSpecies>(id);
        }

        /// <summary>
        ///     Gets a specific Species by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="name">The name of the required Species.</param>
        /// <returns>The specific Species if it exists; otherwise null.</returns>
        public static SidekickSpecies GetByName(DatabaseManager dbManager, string name)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickSpecies>().FirstOrDefault(species => species.Name.ToLower() == name.ToLower());
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
            dbManager.GetCurrentDbConnection().Delete<SidekickSpecies>(ID);
        }

        /// <inheritdoc cref="Equals"/>
        public override bool Equals(object obj)
        {
            SidekickSpecies species = (SidekickSpecies) obj;
            if (ID > 0 && species?.ID > 0)
            {
                return ID == species?.ID;
            }

            return Name.Equals(species?.Name);
        }

        /// <inheritdoc cref="GetHashCode"/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ID, Name, Code);
        }
    }
}
