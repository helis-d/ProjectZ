// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_color_preset_image")]
    public class SidekickColorPresetImage
    {
        private SidekickColorPreset _colorPreset;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_color_preset")]
        public int PtrPreset { get; set; }
        [Column("color_group")]
        public ColorGroup ColorGroup { get; set; }
        [Column("img_data")]
        public byte[] ImageData { get; set; }
        [Column("img_width")]
        public int Width { get; set; }
        [Column("img_height")]
        public int Height { get; set; }

        [Ignore]
        public SidekickColorPreset ColorPreset
        {
            get => _colorPreset;
            set
            {
                _colorPreset = value;
                PtrPreset = value.ID;
            }
        }

        /// <summary>
        ///     Gets a specific Preset Part by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Part.</param>
        /// <returns>The specific Preset Part if it exists; otherwise null.</returns>
        public static SidekickColorPresetImage GetByID(DatabaseManager dbManager, int id)
        {
            SidekickColorPresetImage partPresetImage = dbManager.GetCurrentDbConnection().Find<SidekickColorPresetImage>(id);
            Decorate(dbManager, partPresetImage);
            return partPresetImage;
        }

        /// <summary>
        ///     Gets a list of all the Preset Parts in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all preset parts in the database.</returns>
        public static List<SidekickColorPresetImage> GetAll(DatabaseManager dbManager)
        {
            List<SidekickColorPresetImage> partPresetImages = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetImage>().ToList();

            foreach (SidekickColorPresetImage partPresetImage in partPresetImages)
            {
                Decorate(dbManager, partPresetImage);
            }

            return partPresetImages;
        }

        /// <summary>
        ///     Gets a list of all the Preset Part Images in the database that have the matching Part Preset.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partPreset">The preset to get all the preset part images for.</param>
        /// <returns>A list of all preset part images in the database for the given preset.</returns>
        public static List<SidekickColorPresetImage> GetAllByPreset(DatabaseManager dbManager, SidekickPartPreset partPreset)
        {
            List<SidekickColorPresetImage> partPresetImages = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetImage>()
                .Where(presetPart => presetPart.PtrPreset == partPreset.ID)
                .ToList();

            foreach (SidekickColorPresetImage partPresetImage in partPresetImages)
            {
                Decorate(dbManager, partPresetImage);
            }

            return partPresetImages;
        }

        /// <summary>
        ///     Gets the Preset Part Image in the database that have the matching Part Preset and Part Group.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partPreset">The preset to get the preset part image for.</param>
        /// <param name="partGroup">The part group to filter the results by.</param>
        /// <returns>The preset part image in the database for the given preset and part group.</returns>
        public static SidekickColorPresetImage GetByPresetAndColorGroup(
            DatabaseManager dbManager,
            SidekickColorPreset partPreset,
            ColorGroup partGroup
            )
        {
            SidekickColorPresetImage partPresetImage = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetImage>()
                .FirstOrDefault(presetPart => presetPart.PtrPreset == partPreset.ID && presetPart.ColorGroup == partGroup);

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
        /// <param name="partPresetImage">The color preset to decorate</param>
        /// <returns>A color set with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickColorPresetImage partPresetImage)
        {
            if (partPresetImage != null)
            {
                if (partPresetImage.ColorPreset == null && partPresetImage.PtrPreset >= 0)
                {
                    partPresetImage.ColorPreset = SidekickColorPreset.GetByID(dbManager, partPresetImage.PtrPreset);
                }
            }
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

        /// <summary>
        ///     Deletes this item from the database
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            dbManager.GetCurrentDbConnection().Delete<SidekickColorPresetImage>(ID);
        }
    }
}
