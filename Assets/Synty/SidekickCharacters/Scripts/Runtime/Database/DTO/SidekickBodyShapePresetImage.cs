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
    [Table("sk_body_shape_preset_image")]
    public class SidekickBodyShapePresetImage
    {
        private SidekickBodyShapePreset _bodyShapePreset;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_body_shape_preset")]
        public int PtrPreset { get; set; }
        [Column("img_data")]
        public byte[] ImageData { get; set; }
        [Column("img_width")]
        public int Width { get; set; }
        [Column("img_height")]
        public int Height { get; set; }

        [Ignore]
        public SidekickBodyShapePreset BodyShapePreset
        {
            get => _bodyShapePreset;
            set
            {
                _bodyShapePreset = value;
                PtrPreset = value.ID;
            }
        }

        /// <summary>
        ///     Gets a specific Preset Part by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Part.</param>
        /// <returns>The specific Preset Part if it exists; otherwise null.</returns>
        public static SidekickBodyShapePresetImage GetByID(DatabaseManager dbManager, int id)
        {
            SidekickBodyShapePresetImage bodyShapePresetImage = dbManager.GetCurrentDbConnection().Find<SidekickBodyShapePresetImage>(id);
            Decorate(dbManager, bodyShapePresetImage);
            return bodyShapePresetImage;
        }

        /// <summary>
        ///     Gets a list of all the Preset Parts in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all preset parts in the database.</returns>
        public static List<SidekickBodyShapePresetImage> GetAll(DatabaseManager dbManager)
        {
            List<SidekickBodyShapePresetImage> bodyShapePresetImages = dbManager.GetCurrentDbConnection().Table<SidekickBodyShapePresetImage>().ToList();

            foreach (SidekickBodyShapePresetImage bodyShapePresetImage in bodyShapePresetImages)
            {
                Decorate(dbManager, bodyShapePresetImage);
            }

            return bodyShapePresetImages;
        }

        /// <summary>
        ///     Gets a list of all the Preset Part Images in the database that have the matching Part Preset.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="bodyShapePreset">The preset to get all the preset part images for.</param>
        /// <returns>A list of all preset part images in the database for the given preset.</returns>
        public static SidekickBodyShapePresetImage GetByPreset(DatabaseManager dbManager, SidekickBodyShapePreset bodyShapePreset)
        {
            SidekickBodyShapePresetImage bodyShapePresetImage = dbManager.GetCurrentDbConnection().Table<SidekickBodyShapePresetImage>()
                .FirstOrDefault(bodyShapePresetImage => bodyShapePresetImage.PtrPreset == bodyShapePreset.ID);

            if (bodyShapePresetImage != null)
            {
                Decorate(dbManager, bodyShapePresetImage);
            }

            return bodyShapePresetImage;
        }

        /// <summary>
        ///     Ensures that the given preset has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="bodyShapePresetImage">The color preset to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickBodyShapePresetImage bodyShapePresetImage)
        {
            bodyShapePresetImage.BodyShapePreset ??= SidekickBodyShapePreset.GetByID(dbManager, bodyShapePresetImage.PtrPreset);
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
