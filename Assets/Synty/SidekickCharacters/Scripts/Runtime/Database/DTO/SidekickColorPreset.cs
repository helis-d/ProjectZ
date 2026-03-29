// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_color_preset")]
    public class SidekickColorPreset
    {
        private SidekickSpecies _species;

        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("color_group")]
        public ColorGroup ColorGroup { get; set; }
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }

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
        ///     Gets a list of all the Color Presets in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all color presets in the database.</returns>
        public static List<SidekickColorPreset> GetAll(DatabaseManager dbManager)
        {
            List<SidekickColorPreset> presets = dbManager.GetCurrentDbConnection().Table<SidekickColorPreset>().ToList();

            foreach (SidekickColorPreset preset in presets)
            {
                Decorate(dbManager, preset);
            }

            return presets;
        }

        /// <summary>
        ///     Gets a list of all the Color Presets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get all the color presets for.</param>
        /// <returns>A list of all color presets in the database for the given species.</returns>
        public static List<SidekickColorPreset> GetAllBySpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            List<SidekickColorPreset> presets = dbManager.GetCurrentDbConnection().Table<SidekickColorPreset>()
                .Where(set => set.PtrSpecies == species.ID)
                .ToList();

            foreach (SidekickColorPreset preset in presets)
            {
                preset.Species = species;
            }

            return presets;
        }

        /// <summary>
        ///     Gets a Color Presets in the database with the matching name if one exists.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="name">The name of the preset to retrieve.</param>
        /// <returns>Returns a Color Presets in the database with the matching name if one exists; otherwise null.</returns>
        public static SidekickColorPreset GetByName(DatabaseManager dbManager, string name)
        {
            SidekickColorPreset colorPreset = dbManager.GetCurrentDbConnection()
                .Table<SidekickColorPreset>()
                .FirstOrDefault(colorPreset => colorPreset.Name == name);

            if (colorPreset != null)
            {
                Decorate(dbManager, colorPreset);
            }

            return colorPreset;
        }

        /// <summary>
        ///     Gets a list of all the Color Presets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="colorGroup">The color group to get all the color presets for.</param>
        /// <returns>A list of all color presets in the database for the given color group.</returns>
        public static List<SidekickColorPreset> GetAllByColorGroup(DatabaseManager dbManager, ColorGroup colorGroup)
        {
            List<SidekickColorPreset> presets = dbManager.GetCurrentDbConnection().Table<SidekickColorPreset>()
                .Where(preset => preset.ColorGroup == colorGroup)
                .ToList();

            foreach (SidekickColorPreset preset in presets)
            {
                Decorate(dbManager, preset);
            }

            return presets;
        }

        /// <summary>
        ///     Gets a list of all the Color Presets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="colorGroup">The color group to get all the color presets for.</param>
        /// <param name="species">The species to get the color presets for.</param>
        /// <returns>A list of all color presets in the database for the given color group and species.</returns>
        public static List<SidekickColorPreset> GetAllByColorGroupAndSpecies(
            DatabaseManager dbManager,
            ColorGroup colorGroup,
            SidekickSpecies species
        )
        {
            List<SidekickColorPreset> presets = dbManager.GetCurrentDbConnection()
                .Table<SidekickColorPreset>()
                .Where(preset => preset.ColorGroup == colorGroup && preset.PtrSpecies == species.ID)
                .ToList();

            foreach (SidekickColorPreset preset in presets)
            {
                preset.Species = species;
            }

            return presets;
        }

        /// <summary>
        ///     Gets a specific Color Preset by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Preset.</param>
        /// <returns>The specific Color Preset if it exists; otherwise null.</returns>
        public static SidekickColorPreset GetByID(DatabaseManager dbManager, int id)
        {
            SidekickColorPreset preset = dbManager.GetCurrentDbConnection().Find<SidekickColorPreset>(id);
            Decorate(dbManager, preset);
            return preset;
        }

        /// <summary>
        ///     Ensures that the given preset has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="preset">The color preset to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickColorPreset preset)
        {
            if (preset.Species == null && preset.PtrSpecies >= 0)
            {
                preset.Species = SidekickSpecies.GetByID(dbManager, preset.PtrSpecies);
            }
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
            foreach (SidekickColorPresetRow row in SidekickColorPresetRow.GetAllByPreset(dbManager, this))
            {
                row.Delete(dbManager);
            }

            SidekickColorPresetImage image = SidekickColorPresetImage.GetByPresetAndColorGroup(dbManager, this, ColorGroup);
            image?.Delete(dbManager);

            dbManager.GetCurrentDbConnection().Delete<SidekickColorPreset>(ID);
        }
    }
}
