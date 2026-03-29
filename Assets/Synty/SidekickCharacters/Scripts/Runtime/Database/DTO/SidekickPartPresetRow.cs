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
    [Table("sk_part_preset_row")]
    public class SidekickPartPresetRow
    {
        private SidekickPartPreset _partPreset;
        private SidekickPart _part;

        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int ID { get; set; }
        [Column("part_name")]
        public string PartName { get; set; }
        [Column("ptr_part_preset")]
        public int PtrPreset { get; set; }

        [Column("ptr_part")]
        public int PtrPart { get; set; } = -1;
        [Column("part_type")]
        public string PartType { get; set; }

        [Ignore]
        public SidekickPartPreset PartPreset
        {
            get => _partPreset;
            set
            {
                _partPreset = value;
                PtrPreset = value.ID;
            }
        }

        [Ignore]
        public SidekickPart Part
        {
            get => _part;
            set
            {
                _part = value;
                PtrPart = value.ID;
            }
        }

        /// <summary>
        ///     Gets a specific Preset Part by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Part.</param>
        /// <returns>The specific Preset Part if it exists; otherwise null.</returns>
        public static SidekickPartPresetRow GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPartPresetRow partPreset = dbManager.GetCurrentDbConnection().Find<SidekickPartPresetRow>(id);
            Decorate(dbManager, partPreset);
            return partPreset;
        }

        /// <summary>
        ///     Gets a list of all the Preset Parts in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all preset parts in the database.</returns>
        public static List<SidekickPartPresetRow> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPartPresetRow> presetParts = dbManager.GetCurrentDbConnection().Table<SidekickPartPresetRow>().ToList();

            foreach (SidekickPartPresetRow preset in presetParts)
            {
                Decorate(dbManager, preset);
            }

            return presetParts;
        }

        /// <summary>
        ///     Gets a list of all the Preset Part rows in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partPreset">The preset to get all the preset part rows for.</param>
        /// <returns>A list of all preset part rows in the database for the given preset.</returns>
        public static List<SidekickPartPresetRow> GetAllByPreset(DatabaseManager dbManager, SidekickPartPreset partPreset)
        {
            List<SidekickPartPresetRow> presetParts = dbManager.GetCurrentDbConnection().Table<SidekickPartPresetRow>()
                .Where(presetPart => presetPart.PtrPreset == partPreset.ID)
                .ToList();

            foreach (SidekickPartPresetRow presetPart in presetParts)
            {
                Decorate(dbManager, presetPart);
            }

            return presetParts;
        }

        /// <summary>
        ///     Gets a list of all the Preset Part rows in the database that have the matching part.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="part">The part to get all the preset part rows for.</param>
        /// <returns>A list of all preset part rows in the database for the given part.</returns>
        public static List<SidekickPartPresetRow> GetAllByPart(DatabaseManager dbManager, SidekickPart part)
        {
            List<SidekickPartPresetRow> presetParts = dbManager.GetCurrentDbConnection().Table<SidekickPartPresetRow>()
                .Where(presetPart => presetPart.PtrPart == part.ID)
                .ToList();

            foreach (SidekickPartPresetRow presetPart in presetParts)
            {
                Decorate(dbManager, presetPart);
            }

            return presetParts;
        }

        /// <summary>
        ///     Ensures that the given preset has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partPreset">The color preset to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickPartPresetRow partPreset)
        {
            if (partPreset != null)
            {
                if (partPreset.PartPreset == null && partPreset.PtrPreset >= 0)
                {
                    partPreset.PartPreset = SidekickPartPreset.GetByID(dbManager, partPreset.PtrPreset);
                }

                if (partPreset.Part == null && partPreset.PtrPart >= 0)
                {
                    partPreset.Part = SidekickPart.GetByID(dbManager, partPreset.PtrPart);
                }
                else if (partPreset.Part == null && partPreset.PtrPart < 0)
                {
                    SidekickPart part = SidekickPart.SearchForByName(dbManager, partPreset.PartName);
                    if (part != null)
                    {
                        partPreset.Part = part;
                    }
                }
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
            dbManager.GetCurrentDbConnection().Delete<SidekickPartPresetRow>(ID);
        }

        /// <summary>
        ///     Checks whether the associated part has a file that exists in the project.
        /// </summary>
        /// <returns>True if the part has a file in the project; otherwise False.</returns>
        public bool HasValidPart()
        {
            return Part == null || Part.FileExists;
        }
    }
}
