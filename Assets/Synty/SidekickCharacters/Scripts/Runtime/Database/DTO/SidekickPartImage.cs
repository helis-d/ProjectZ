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
    [Table("sk_part_image")]
    public class SidekickPartImage
    {
        private SidekickPart _part;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_part")]
        public int PtrPart { get; set; }
        [Column("img_data")]
        public byte[] ImageData { get; set; }
        [Column("img_width")]
        public int Width { get; set; }
        [Column("img_height")]
        public int Height { get; set; }

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
        ///     Gets a specific Part Image by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Part Image.</param>
        /// <returns>The specific Part Image if it exists; otherwise null.</returns>
        public static SidekickPartImage GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPartImage partImage = dbManager.GetCurrentDbConnection().Find<SidekickPartImage>(id);
            Decorate(dbManager, partImage);
            return partImage;
        }

        /// <summary>
        ///     Gets a specific Part Image for a specific part.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="part">The Part to get the image for.</param>
        /// <returns>The specific Part Image for a specific part if it exists; otherwise null.</returns>
        public static SidekickPartImage GetByPart(DatabaseManager dbManager, SidekickPart part)
        {
            SidekickPartImage partImage = dbManager.GetCurrentDbConnection().Table<SidekickPartImage>()
                .FirstOrDefault(pi => pi.PtrPart == part.ID);
            Decorate(dbManager, partImage);
            return partImage;
        }

        /// <summary>
        ///     Gets a list of all the Part Images in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all part images in the database.</returns>
        public static List<SidekickPartImage> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPartImage> partImages = dbManager.GetCurrentDbConnection().Table<SidekickPartImage>().ToList();

            foreach (SidekickPartImage partImage in partImages)
            {
                Decorate(dbManager, partImage);
            }

            return partImages;
        }

        /// <summary>
        ///     Ensures that the given preset has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partImage">The part image to decorate</param>
        private static void Decorate(DatabaseManager dbManager, SidekickPartImage partImage)
        {
            if (partImage != null)
            {
                if (partImage.Part == null && partImage.PtrPart >= 0)
                {
                    partImage.Part ??= SidekickPart.GetByID(dbManager, partImage.PtrPart);
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
            dbManager.GetCurrentDbConnection().Delete<SidekickPartImage>(ID);
        }
    }
}
