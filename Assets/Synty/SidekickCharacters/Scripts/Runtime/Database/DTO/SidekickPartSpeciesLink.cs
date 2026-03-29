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
    [Table("sk_part_species_link")]
    public class SidekickPartSpeciesLink
    {
        private SidekickSpecies _species;
        private SidekickPart _part;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }
        [Column("ptr_part")]
        public int PtrPart { get; set; }

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
        ///     Gets a specific Part Species Link by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Part Species Link.</param>
        /// <returns>The specific Part Species Link if it exists; otherwise null.</returns>
        public static SidekickPartSpeciesLink GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPartSpeciesLink part = dbManager.GetCurrentDbConnection().Find<SidekickPartSpeciesLink>(id);
            Decorate(dbManager, part);
            return part;
        }

        /// <summary>
        ///     Gets a list of all the Part Species Links in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Part Species Links in the database.</returns>
        public static List<SidekickPartSpeciesLink> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPartSpeciesLink> partSpeciesLinks = dbManager.GetCurrentDbConnection().Table<SidekickPartSpeciesLink>().ToList();

            foreach (SidekickPartSpeciesLink partSpeciesLink in partSpeciesLinks)
            {
                Decorate(dbManager, partSpeciesLink);
            }

            return partSpeciesLinks;
        }

        /// <summary>
        ///     Gets a list of all the Part Species Links in the database for the specific species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to search for all parts species links for.</param>
        /// <returns>A list of all Part Species Links in the database for the specified species.</returns>
        public static List<SidekickPartSpeciesLink> GetAllForSpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            List<SidekickPartSpeciesLink> partSpeciesLinks = dbManager.GetCurrentDbConnection().Table<SidekickPartSpeciesLink>()
                .Where(psl => psl.PtrSpecies == species.ID)
                .ToList();

            foreach (SidekickPartSpeciesLink partSpeciesLink in partSpeciesLinks)
            {
                Decorate(dbManager, partSpeciesLink);
            }

            return partSpeciesLinks;
        }

        /// <summary>
        ///     Gets a list of all the Part Species Links in the database for the specific part.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="part">The part to search for all parts species links for.</param>
        /// <returns>A list of all Part Species Links in the database for the specified part.</returns>
        public static List<SidekickPartSpeciesLink> GetAllForPart(DatabaseManager dbManager, SidekickPart part)
        {
            List<SidekickPartSpeciesLink> partSpeciesLinks = dbManager.GetCurrentDbConnection().Table<SidekickPartSpeciesLink>()
                .Where(psl => psl.PtrPart == part.ID)
                .ToList();

            foreach (SidekickPartSpeciesLink partSpeciesLink in partSpeciesLinks)
            {
                Decorate(dbManager, partSpeciesLink);
            }

            return partSpeciesLinks;
        }

        /// <summary>
        ///     Gets a list of all the Part Species Links in the database for the specific part.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to search for all parts species links for.</param>
        /// <param name="part">The part to search for all parts species links for.</param>
        /// <returns>A list of all Part Species Links in the database for the specified part.</returns>
        public static SidekickPartSpeciesLink GetForSpeciesAndPart(DatabaseManager dbManager, SidekickSpecies species, SidekickPart part)
        {
            SidekickPartSpeciesLink partSpeciesLink = dbManager.GetCurrentDbConnection().Table<SidekickPartSpeciesLink>()
                .FirstOrDefault(psl => psl.PtrSpecies == species.ID && psl.PtrPart == part.ID);

            Decorate(dbManager, partSpeciesLink);

            return partSpeciesLink;
        }

        /// <summary>
        ///     Ensures that the given part has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partSpeciesLink">The part to decorate</param>
        private static void Decorate(DatabaseManager dbManager, SidekickPartSpeciesLink partSpeciesLink)
        {
            if (partSpeciesLink != null)
            {
                if (partSpeciesLink.Species == null && partSpeciesLink.PtrSpecies >= 0)
                {
                    partSpeciesLink.Species = SidekickSpecies.GetByID(dbManager, partSpeciesLink.PtrSpecies);
                }

                if (partSpeciesLink.Part == null && partSpeciesLink.PtrPart >= 0)
                {
                    partSpeciesLink.Part = SidekickPart.GetByID(dbManager, partSpeciesLink.PtrPart);
                }
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
            dbManager.GetCurrentDbConnection().Delete<SidekickPartSpeciesLink>(ID);
        }
    }
}
