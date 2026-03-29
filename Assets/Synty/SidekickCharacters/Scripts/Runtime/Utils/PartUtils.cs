using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.Utils
{
    public static class PartUtils
    {
        /// <summary>
        ///     Checks if a given part is a base part or not.
        /// </summary>
        /// <param name="partName">The name of the part to check.</param>
        /// <returns>true if it is a base part; otherwise false</returns>
        public static bool IsBaseSpeciesPart(string partName)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return false;
            }

            return partName.Contains("_BASE_");
        }
    }
}
