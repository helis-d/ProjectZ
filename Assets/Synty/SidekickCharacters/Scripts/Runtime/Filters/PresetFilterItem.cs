// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Synty.SidekickCharacters.Filters
{
    public class PresetFilterItem
    {
        public DatabaseManager DbManager;
        public SidekickPresetFilter Filter;
        public FilterCombineType CombineType;

        private List<SidekickPartPreset> _filteredPresets;

        public PresetFilterItem(DatabaseManager dbManager, SidekickPresetFilter filter, FilterCombineType combineType)
        {
            DbManager = dbManager;
            Filter = filter;
            CombineType = combineType;
        }

        /// <summary>
        ///     Gets a list of all the presets for this filter item.
        /// </summary>
        /// <returns>A list of all presets for this filter item.</returns>
        public List<SidekickPartPreset> GetFilteredPresets()
        {
            if (_filteredPresets == null || _filteredPresets.Count < 1)
            {
                _filteredPresets = SidekickPresetFilterRow.GetAllForFilter(DbManager, Filter).Select(row => row.Preset).ToList();
            }

            return _filteredPresets;
        }
    }
}
