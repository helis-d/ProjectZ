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
    [Table("sk_preset_filter_row")]
    public class SidekickPresetFilterRow
    {
        private SidekickPresetFilter _filter;
        private SidekickPartPreset _preset;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_filter")]
        public int PtrFilter { get; set; }
        [Column("ptr_preset")]
        public int PtrPreset { get; set; }

        [Ignore]
        public SidekickPresetFilter Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                PtrFilter = value.ID;
            }
        }

        [Ignore]
        public SidekickPartPreset Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                PtrPreset = value.ID;
            }
        }

        /// <summary>
        ///     Gets a specific Preset Filter by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Filter.</param>
        /// <returns>The specific Preset Filter if it exists; otherwise null.</returns>
        public static SidekickPresetFilterRow GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPresetFilterRow filterRow = dbManager.GetCurrentDbConnection().Find<SidekickPresetFilterRow>(id);
            Decorate(dbManager, filterRow);
            return filterRow;
        }

        /// <summary>
        ///     Gets a list of all the preset filters in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all preset filters in the database.</returns>
        public static List<SidekickPresetFilterRow> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPresetFilterRow> filterRows = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilterRow>().ToList();

            foreach (SidekickPresetFilterRow row in filterRows)
            {
                Decorate(dbManager, row);
            }

            return filterRows;
        }

        /// <summary>
        ///     Gets a list of all the preset filter rows in the database for a given preset filter.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="filter">The Preset Filter to get all the preset filter rows for.</param>
        /// <returns>A list of all preset filter rows for the given Preset Filter in the database.</returns>
        public static List<SidekickPresetFilterRow> GetAllForFilter(DatabaseManager dbManager, SidekickPresetFilter filter, bool excludeMissingParts = true)
        {
            List<SidekickPresetFilterRow> filterRows = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilterRow>().Where(filterRow => filterRow
                .PtrFilter == filter.ID).ToList();

            List<SidekickPresetFilterRow> toRemove = new List<SidekickPresetFilterRow>();
            foreach (SidekickPresetFilterRow row in filterRows)
            {
                Decorate(dbManager, row);
                if (row.Preset == null)
                {
                    toRemove.Add(row);
                }
            }

            if (excludeMissingParts)
            {
                foreach (SidekickPresetFilterRow row in filterRows)
                {
                    if (!row.Preset.HasAllPartsAvailable(dbManager))
                    {
                        toRemove.Add(row);
                    }
                }
            }

            filterRows.RemoveAll(row => toRemove.Contains(row));

            return filterRows;
        }

        /// <summary>
        ///     Gets a preset filter row in the database for a given preset filter and preset.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="filter">The Preset Filter to get all the preset filter row for.</param>
        /// <param name="preset">The preset to get the row for.</param>
        /// <returns>The preset filter row for the given Preset Filter and Preset in the database.</returns>
        public static SidekickPresetFilterRow GetForFilterAndPreset(DatabaseManager dbManager, SidekickPresetFilter filter, SidekickPartPreset preset)
        {
            SidekickPresetFilterRow row = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilterRow>().FirstOrDefault(filterRow => filterRow
                    .PtrFilter == filter.ID && filterRow.PtrPreset == preset.ID
            );

            Decorate(dbManager, row);

            return row;
        }

        /// <summary>
        ///     Gets a list of all the preset filter rows in the database for a given part.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="preset">The preset to get the rows for.</param>
        /// <returns>The preset filter rows for the given Preset in the database.</returns>
        public static List<SidekickPresetFilterRow> GetAllForPreset(DatabaseManager dbManager, SidekickPartPreset preset)
        {
            List<SidekickPresetFilterRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilterRow>()
                .Where(filterRow => filterRow.PtrPreset == preset.ID).ToList();

            foreach (SidekickPresetFilterRow row in rows)
            {
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Ensures that the given preset filter row has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="filterRow">The preset filter row to decorate</param>
        private static void Decorate(DatabaseManager dbManager, SidekickPresetFilterRow filterRow)
        {
            SidekickPartPreset preset = SidekickPartPreset.GetByID(dbManager, filterRow.PtrPreset);

            if (preset == null)
            {
                filterRow.Delete(dbManager);
            }
            else
            {
                filterRow.Filter ??= SidekickPresetFilter.GetByID(dbManager, filterRow.PtrFilter);
                filterRow.Preset ??= preset;
            }
        }

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

        /// <summary>
        ///     Deletes this item from the database
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            dbManager.GetCurrentDbConnection().Delete<SidekickPresetFilterRow>(ID);
        }
    }
}
