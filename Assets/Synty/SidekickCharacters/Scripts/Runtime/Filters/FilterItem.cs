// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using System;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Filters
{
    public class FilterItem
    {
        public SidekickRuntime Runtime;
        public SidekickPartFilter Filter;
        public FilterCombineType CombineType;

        private Dictionary<CharacterPartType, List<string>> _filteredParts = new Dictionary<CharacterPartType, List<string>>();

        public FilterItem(SidekickRuntime runtime, SidekickPartFilter filter, FilterCombineType combineType)
        {
            Runtime = runtime;
            Filter = filter;
            CombineType = combineType;
        }

        /// <summary>
        ///     Resets the part dictionary when the species is changed.
        /// </summary>
        public void ResetPartsForSpeciesChange()
        {
            _filteredParts = new Dictionary<CharacterPartType, List<string>>();
        }

        /// <summary>
        ///     Gets a list of all the parts for this filter item.
        /// </summary>
        /// <returns>A list of all parts for this filter item.</returns>
        public Dictionary<CharacterPartType, List<string>> GetFilteredParts()
        {
            if (_filteredParts == null || _filteredParts.Count < 1)
            {
                _filteredParts = new Dictionary<CharacterPartType, List<string>>();

                foreach (CharacterPartType type in Enum.GetValues(typeof(CharacterPartType)))
                {
                    List<string> parts = SidekickPartFilterRow.GetAllPartNamesForFilterSpeciesAndType(Runtime.DBManager, Filter, Runtime.CurrentSpecies, type);
                    _filteredParts[type] = parts;
                }
            }

            return _filteredParts;
        }
    }
}
