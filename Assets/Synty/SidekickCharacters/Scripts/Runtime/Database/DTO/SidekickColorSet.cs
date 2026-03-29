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
    [Table("sk_color_set")]
    public class SidekickColorSet
    {
        private SidekickSpecies _species;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        // NOTE : cannot make private as the ORM requires it to be visible when calling Save()
        // NOTE : not possible to encapsulate setting _species without a dbManager reference, so always set via Species instead
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("src_color")]
        public string SourceColorPath { get; set; }
        [Column("src_metallic")]
        public string SourceMetallicPath { get; set; }
        [Column("src_smoothness")]
        public string SourceSmoothnessPath { get; set; }
        [Column("src_reflection")]
        public string SourceReflectionPath { get; set; }
        [Column("src_emission")]
        public string SourceEmissionPath { get; set; }
        [Column("src_opacity")]
        public string SourceOpacityPath { get; set; }

        [Ignore]
        public SidekickSpecies Species
        {
            get => _species;
            set
            {
                _species = value;
                PtrSpecies = value.ID;
            }
        }

        /// <summary>
        ///     Gets a list of all the Color Sets in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all color sets in the database.</returns>
        public static List<SidekickColorSet> GetAll(DatabaseManager dbManager)
        {
            List<SidekickColorSet> sets = dbManager.GetCurrentDbConnection().Table<SidekickColorSet>().ToList();

            foreach (SidekickColorSet set in sets)
            {
                Decorate(dbManager, set);
            }

            return sets;
        }

        /// <summary>
        ///     Gets a list of all the Color Sets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get all the color sets for.</param>
        /// <returns>A list of all color sets in the database for the given species.</returns>
        public static List<SidekickColorSet> GetAllBySpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            List<SidekickColorSet> sets = dbManager.GetCurrentDbConnection().Table<SidekickColorSet>()
                .Where(set => set.PtrSpecies == species.ID)
                .ToList();

            foreach (SidekickColorSet set in sets)
            {
                set.Species = species;
                Decorate(dbManager, set);
            }

            return sets;
        }

        /// <summary>
        ///     Gets the default Color Set in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>The default Color Set in the database.</returns>
        public static SidekickColorSet GetDefault(DatabaseManager dbManager)
        {
            SidekickColorSet set = dbManager.GetCurrentDbConnection().Get<SidekickColorSet>(set => set.PtrSpecies == -1);
            Decorate(dbManager, set);
            return set;
        }

        /// <summary>
        ///     Gets count of all the Color Sets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get the color set count for.</param>
        /// <returns>The count of all color sets in the database for the given species.</returns>
        public static int GetCountBySpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickColorSet>().Count(set => set.PtrSpecies == species.ID);
        }

        /// <summary>
        ///     Gets a specific Color Set by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Set.</param>
        /// <returns>The specific Color Set if it exists; otherwise null.</returns>
        public static SidekickColorSet GetByID(DatabaseManager dbManager, int id)
        {
            SidekickColorSet set = dbManager.GetCurrentDbConnection().Find<SidekickColorSet>(id);
            Decorate(dbManager, set);
            return set;
        }

        /// <summary>
        ///     Gets a specific Color Set by its database name.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="name">The name of the required Color Set.</param>
        /// <returns>The specific Color Set if it exists; otherwise null.</returns>
        public static SidekickColorSet GetByName(DatabaseManager dbManager, string name)
        {
            SidekickColorSet set = dbManager.GetCurrentDbConnection().Table<SidekickColorSet>().FirstOrDefault(set => set.Name == name);
            Decorate(dbManager, set);
            return set;
        }

        /// <summary>
        ///     Checks if the given color set name exists in the database or not.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="name">The name to check for.</param>
        /// <returns>True if the name exists in the database; otherwise False.</returns>
        public static bool DoesNameExist(DatabaseManager dbManager, string name)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickColorSet>().Count(set => set.Name == name) > 0;
        }

        /// <summary>
        ///     Ensures that the given set has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="set">The color set to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickColorSet set)
        {
            if (set.Species == null && set.PtrSpecies >= 0)
            {
                set.Species = SidekickSpecies.GetByID(dbManager, set.PtrSpecies);
            }
        }

        /// <summary>
        ///     Delete this color set from the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            int deletedCount = dbManager.GetCurrentDbConnection().Delete<SidekickColorSet>(ID);
            if (deletedCount == 0)
            {
                throw new Exception($"Could not delete color set with ID '{ID}'");
            }
        }

        /// <summary>
        ///     Inserts, or updates the values in the database, depending on this object has been saved before or not.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        public void Save(DatabaseManager dbManager)
        {
            if (ID <= 0)
            {
                SaveToDB(dbManager);
            }
            else
            {
                UpdateDB(dbManager);
            }
        }

        /// <summary>
        ///     Saves this Color Set to the database with the current values.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        private void SaveToDB(DatabaseManager dbManager)
        {
            SQLiteConnection connection = dbManager.GetCurrentDbConnection();
            int insertCount = connection.Insert(this);
            if (insertCount == 0)
            {
                throw new Exception("Unable to save current color set");
            }

            // in theory this could return a different ID, but in practice it's highly unlikely
            ID = (int) SQLite3.LastInsertRowid(connection.Handle);
        }

        /// <summary>
        ///     Updates this Color Set in the database with the current values.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        private void UpdateDB(DatabaseManager dbManager)
        {
            int updatedCount = dbManager.GetCurrentDbConnection().Update(this);
            if (updatedCount == 0)
            {
                throw new Exception($"Could not update color set with ID '{ID}'");
            }
        }
    }
}
