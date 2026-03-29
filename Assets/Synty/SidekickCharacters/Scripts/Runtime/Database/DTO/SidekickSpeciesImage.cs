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
    [Table("sk_species_image")]
    public class SidekickSpeciesImage
    {
        private SidekickSpecies _species;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }
        [Column("img_data")]
        public byte[] ImageData { get; set; }
        [Column("img_width")]
        public int Width { get; set; }
        [Column("img_height")]
        public int Height { get; set; }

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
        ///     Gets a specific Species Image by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Species Image.</param>
        /// <returns>The specific Species Image if it exists; otherwise null.</returns>
        public static SidekickSpeciesImage GetByID(DatabaseManager dbManager, int id)
        {
            SidekickSpeciesImage speciesImage = dbManager.GetCurrentDbConnection().Find<SidekickSpeciesImage>(id);
            Decorate(dbManager, speciesImage);
            return speciesImage;
        }

        /// <summary>
        ///     Gets a list of all the Species Image in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all species images in the database.</returns>
        public static List<SidekickSpeciesImage> GetAll(DatabaseManager dbManager)
        {
            List<SidekickSpeciesImage> speciesImages = dbManager.GetCurrentDbConnection().Table<SidekickSpeciesImage>().ToList();

            foreach (SidekickSpeciesImage speciesImage in speciesImages)
            {
                Decorate(dbManager, speciesImage);
            }

            return speciesImages;
        }

        /// <summary>
        ///     Gets the Species Image in the database that has the matching Species.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The species to get the species image for.</param>
        /// <returns>The species image in the database for the given species.</returns>
        public static SidekickSpeciesImage GetBySpecies(DatabaseManager dbManager, SidekickSpecies species)
        {
            SidekickSpeciesImage partPresetImage = dbManager.GetCurrentDbConnection().Table<SidekickSpeciesImage>()
                .FirstOrDefault(presetPart => presetPart.PtrSpecies == species.ID);

            if (partPresetImage != null)
            {
                Decorate(dbManager, partPresetImage);
            }

            return partPresetImage;
        }

        /// <summary>
        ///     Ensures that the given preset has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="bodyShapePresetImage">The color preset to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickSpeciesImage bodyShapePresetImage)
        {
            bodyShapePresetImage.Species ??= SidekickSpecies.GetByID(dbManager, bodyShapePresetImage.PtrSpecies);
        }

        /// <summary>
        ///     Updates or Inserts this item in the Database.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public void Save(DatabaseManager dbManager)
        {
            if (ID <= 0)
            {
                dbManager.GetCurrentDbConnection().Insert(this);
                // in theory this could return a different ID, but in practice it's highly unlikely
                ID = (int) SQLite3.LastInsertRowid(dbManager.GetCurrentDbConnection().Handle);
            }
            dbManager.GetCurrentDbConnection().Update(this);
        }
    }
}
