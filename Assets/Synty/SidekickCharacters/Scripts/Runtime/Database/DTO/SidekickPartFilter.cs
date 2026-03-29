// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_part_filter")]
    public class SidekickPartFilter
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("filter_type")]
        public FilterType Type { get; set; }
        [Column("filter_term")]
        public string Term { get; set; }

        /// <summary>
        ///     Gets a specific Part Filter by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Part Filter.</param>
        /// <returns>The specific Part Filter if it exists; otherwise null.</returns>
        public static SidekickPartFilter GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPartFilter filter = dbManager.GetCurrentDbConnection().Find<SidekickPartFilter>(id);
            return filter;
        }

        /// <summary>
        ///     Gets a list of all the part filters in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all part filters in the database.</returns>
        public static List<SidekickPartFilter> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPartFilter> filters = dbManager.GetCurrentDbConnection().Table<SidekickPartFilter>().ToList();

            return filters;
        }

        /// <summary>
        ///     Gets a list of all the filters in the database for a given Character Part Type.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="filterType">The Character Part Type to get all the filters for.</param>
        /// <param name="excludeFiltersWithNoParts">Whether to exclude filters that have no parts associated</param>
        /// <returns>A list of all filters for the given Character Part Type in the database.</returns>
        public static List<SidekickPartFilter> GetAllForFilterType(DatabaseManager dbManager, FilterType filterType, bool
                excludeFiltersWithNoParts = true)
        {
            List<SidekickPartFilter> filters = dbManager.GetCurrentDbConnection().Table<SidekickPartFilter>().Where(filter => filter.Type == filterType).ToList();

            if (excludeFiltersWithNoParts)
            {
                List<SidekickPartFilter> toRemove = new List<SidekickPartFilter>();
                foreach (SidekickPartFilter filter in filters)
                {
                    List<SidekickPartFilterRow> rows = SidekickPartFilterRow.GetAllForFilter(dbManager, filter);
                    rows = rows.Where(row => !row.Part.FileName.Contains("_BASE_")).ToList();
                    if (rows.Count < 1)
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
        /// <param name="filterType">The Character Part Type to search for.</param>
        /// <returns>The filter with the given term and given Character Part Type in the database if it exist; otherwise null.</returns>
        public static SidekickPartFilter GetByTermAndFilterType(DatabaseManager dbManager, string filterTerm, FilterType filterType)
        {
            SidekickPartFilter filter = dbManager.GetCurrentDbConnection().Table<SidekickPartFilter>()
                .FirstOrDefault(filter => filter.Term == filterTerm && filter.Type == filterType);

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
            foreach (SidekickPartFilterRow row in SidekickPartFilterRow.GetAllForFilter(dbManager, this))
            {
                row.Delete(dbManager);
            }

            dbManager.GetCurrentDbConnection().Delete<SidekickPartFilter>(ID);
        }
    }
}
