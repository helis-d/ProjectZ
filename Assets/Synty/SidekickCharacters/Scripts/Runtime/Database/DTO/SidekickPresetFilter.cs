// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System.Collections.Generic;
using System.Linq;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_preset_filter")]
    public class SidekickPresetFilter
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("filter_term")]
        public string Term { get; set; }

        private List<SidekickPartPreset> _allPresets = new List<SidekickPartPreset>();

        /// <summary>
        ///     Gets a specific Preset Filter by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Filter.</param>
        /// <returns>The specific Preset Filter if it exists; otherwise null.</returns>
        public static SidekickPresetFilter GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPresetFilter filter = dbManager.GetCurrentDbConnection().Find<SidekickPresetFilter>(id);
            return filter;
        }

        /// <summary>
        ///     Gets a list of all the Preset filters in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="excludeFiltersWithNoParts">Whether to exclude filters without any parts from the list or not.</param>
        /// <returns>A list of all Preset filters in the database.</returns>
        public static List<SidekickPresetFilter> GetAll(DatabaseManager dbManager, bool excludeFiltersWithNoParts = true)
        {
            List<SidekickPresetFilter> filters = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilter>().ToList();

            if (excludeFiltersWithNoParts)
            {
                List<SidekickPresetFilter> toRemove = new List<SidekickPresetFilter>();
                foreach (SidekickPresetFilter filter in filters)
                {
                    if (filter.GetAllPresetsForFilter(dbManager).Count < 1)
                    {
                        toRemove.Add(filter);
                    }

                }

                filters.RemoveAll(filter => toRemove.Contains(filter));
            }

            return filters;
        }

        /// <summary>
        ///     Searches for a filter in the database with the given term and filter type.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="filterTerm">The term to search for.</param>
        /// <returns>The filter with the given term in the database if it exist; otherwise null.</returns>
        public static SidekickPresetFilter GetByTerm(DatabaseManager dbManager, string filterTerm)
        {
            SidekickPresetFilter filter = dbManager.GetCurrentDbConnection().Table<SidekickPresetFilter>()
                .FirstOrDefault(filter => filter.Term == filterTerm);

            return filter;
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
            foreach (SidekickPresetFilterRow row in SidekickPresetFilterRow.GetAllForFilter(dbManager, this))
            {
                row.Delete(dbManager);
            }

            dbManager.GetCurrentDbConnection().Delete<SidekickPresetFilter>(ID);
        }

        /// <summary>
        ///     Gets all of the presets for this filter.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <param name="excludeMissingParts">Exclude presets where parts are missing or not.</param>
        /// <param name="refreshList">Whether to refresh the list from the database or not.</param>
        /// <returns>All of the presets for this filter.</returns>
        public List<SidekickPartPreset> GetAllPresetsForFilter(DatabaseManager dbManager, bool excludeMissingParts = true, bool refreshList = false)
        {
            if (refreshList || _allPresets.Count < 1)
            {
                List<SidekickPresetFilterRow> filterRows = SidekickPresetFilterRow.GetAllForFilter(dbManager, this, excludeMissingParts);
                _allPresets = filterRows.Select(row => row.Preset).ToList();
            }

            return _allPresets;
        }

        /// <summary>
        ///     Checks if this filter and the given filter are the same filter.
        /// </summary>
        /// <param name="other">The filter to check.</param>
        /// <returns>True, if they are the same filter (IDs match); otherwise false.</returns>
        protected bool Equals(SidekickPresetFilter other)
        {
            return ID == other.ID;
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

            return Equals((SidekickPresetFilter) obj);
        }

        /// <inheritdoc cref="GetHashCode"/>
        public override int GetHashCode()
        {
            return ID;
        }
    }
}
