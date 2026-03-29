// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_part_preset")]
    public class SidekickPartPreset
    {
        private SidekickSpecies _species;

        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("part_group")]
        public PartGroup PartGroup { get; set; }
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }
        [Column("outfit")]
        public string Outfit { get; set; }

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
        ///     Gets a specific Preset by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset.</param>
        /// <returns>The specific Preset if it exists; otherwise null.</returns>
        public static SidekickPartPreset GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPartPreset partPreset = dbManager.GetCurrentDbConnection().Find<SidekickPartPreset>(id);
            Decorate(dbManager, partPreset);
            return partPreset;
        }

        /// <summary>
        ///     Gets a list of all the Presets in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all presets in the database.</returns>
        public static List<SidekickPartPreset> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPartPreset> partPresets = dbManager.GetCurrentDbConnection().Table<SidekickPartPreset>().ToList();

            foreach (SidekickPartPreset partPreset in partPresets)
            {
                Decorate(dbManager, partPreset);
            }

            return partPresets;
        }

        /// <summary>
        ///     Gets a list of all the Part Presets in the database that have the matching species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get all the part presets for.</param>
        /// <returns>A list of all part presets in the database for the given species.</returns>
        public static List<SidekickPartPreset> GetAllBySpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            List<SidekickPartPreset> partPresets = dbManager.GetCurrentDbConnection().Table<SidekickPartPreset>()
                .Where(partPreset => partPreset.PtrSpecies == species.ID)
                .ToList();

            foreach (SidekickPartPreset partPreset in partPresets)
            {
                partPreset.Species = species;
            }

            return partPresets;
        }

        /// <summary>
        ///     Gets a Part Presets in the database with the matching name if one exists.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="name">The name of the preset to retrieve.</param>
        /// <returns>Returns a Part Presets in the database with the matching name if one exists; otherwise null.</returns>
        public static SidekickPartPreset GetByName(DatabaseManager dbManager, string name)
        {
            SidekickPartPreset partPreset = dbManager.GetCurrentDbConnection()
                .Table<SidekickPartPreset>()
                .FirstOrDefault(partPreset => partPreset.Name == name);

            if (partPreset != null)
            {
                Decorate(dbManager, partPreset);
            }

            return partPreset;
        }

        /// <summary>
        ///     Gets a list of all the Part Presets in the database that have the matching species and part group.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partGroup">The part group to filter search by.</param>
        /// <returns>A list of all part presets in the database for the given species and part group.</returns>
        public static List<SidekickPartPreset> GetAllByGroup(DatabaseManager dbManager, PartGroup partGroup, bool excludeMissingParts = true)
        {
            List<SidekickPartPreset> partPresets = dbManager.GetCurrentDbConnection().Table<SidekickPartPreset>()
                .Where(partPreset => partPreset.PartGroup == partGroup)
                .ToList();

            foreach (SidekickPartPreset partPreset in partPresets)
            {
                Decorate(dbManager, partPreset);
            }

            if (excludeMissingParts)
            {
                List<SidekickPartPreset> toRemove = new List<SidekickPartPreset>();
                foreach (SidekickPartPreset partPreset in partPresets)
                {
                    if (!partPreset.HasAllPartsAvailable(dbManager))
                    {
                        toRemove.Add(partPreset);
                    }
                }

                partPresets.RemoveAll(preset => toRemove.Contains(preset));
            }

            return partPresets;
        }

        /// <summary>
        ///     Gets a list of all the Part Presets in the database that have the matching species and part group.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get all the part presets for.</param>
        /// <param name="partGroup">The part group to filter search by.</param>
        /// <returns>A list of all part presets in the database for the given species and part group.</returns>
        public static List<SidekickPartPreset> GetAllBySpeciesAndGroup(DatabaseManager dbManager, SidekickSpecies species, PartGroup partGroup)
        {
            List<SidekickPartPreset> partPresets = dbManager.GetCurrentDbConnection().Table<SidekickPartPreset>()
                .Where(partPreset => partPreset.PtrSpecies == species.ID && partPreset.PartGroup == partGroup)
                .ToList();

            foreach (SidekickPartPreset partPreset in partPresets)
            {
                partPreset.Species = species;
            }

            return partPresets;
        }

        /// <summary>
        ///     Ensures that the given set has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partPreset">The color set to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickPartPreset partPreset)
        {
            if (partPreset.Species == null && partPreset.PtrSpecies >= 0)
            {
                partPreset.Species = SidekickSpecies.GetByID(dbManager, partPreset.PtrSpecies);
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
            foreach (SidekickPartPresetRow row in SidekickPartPresetRow.GetAllByPreset(dbManager, this))
            {
                row.Delete(dbManager);
            }

            foreach (SidekickPresetFilterRow row in SidekickPresetFilterRow.GetAllForPreset(dbManager, this))
            {
                row.Delete(dbManager);
            }

            SidekickPartPresetImage image = SidekickPartPresetImage.GetByPresetAndPartGroup(dbManager, this, PartGroup);
            image?.Delete(dbManager);

            dbManager.GetCurrentDbConnection().Delete<SidekickPartPreset>(ID);
        }

        /// <summary>
        ///     Determines if all the parts associated with this preset are valid (has a file in the project).
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <returns>True if all parts are valid; otherwise False.</returns>
        public bool HasAllPartsAvailable(DatabaseManager dbManager)
        {
            List<SidekickPartPresetRow> allRows = SidekickPartPresetRow.GetAllByPreset(dbManager, this);
            return allRows.Count > 0 && allRows.All(row => row.HasValidPart());
        }

        /// <summary>
        ///     Determines if all the parts associated with this preset are valid (has a file in the project).
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <returns>True if all parts are valid; otherwise False.</returns>
        public bool HasOnlyBasePartsAndAllAvailable(DatabaseManager dbManager)
        {
            List<SidekickPartPresetRow> allRows = SidekickPartPresetRow.GetAllByPreset(dbManager, this);
            return allRows.Count > 0 && allRows.All(row => row.HasValidPart() && (row.Part == null || PartUtils.IsBaseSpeciesPart(row.PartName)));
        }

        /// <summary>
        ///     Checks the equality of this preset to the given preset.
        /// </summary>
        /// <param name="other">The preset to check equality with.</param>
        /// <returns>True if the presets are equal, otherwise false.</returns>
        protected bool Equals(SidekickPartPreset other)
        {
            return ID == other.ID
                && Name == other.Name
                && PartGroup == other.PartGroup
                && PtrSpecies == other.PtrSpecies;
        }

        /// <inheritdoc cref="Equals"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((SidekickPartPreset) obj);
        }

        /// <inheritdoc cref="GetHashCode"/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ID, Name, (int) PartGroup, PtrSpecies);
        }
    }
}
