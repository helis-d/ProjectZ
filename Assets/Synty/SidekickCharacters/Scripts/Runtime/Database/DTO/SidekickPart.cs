// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using Synty.SidekickCharacters.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_part")]
    public class SidekickPart
    {
        private SidekickSpecies _species;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_species")]
        public int PtrSpecies { get; set; }
        [Column("type")]
        public CharacterPartType Type { get; set; }
        [Column("part_group")]
        public PartGroup PartGroup { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("part_file_name")]
        public string FileName { get; set; }
        [Column("part_location")]
        public string Location { get; set; }
        [Column("uses_wrap")]
        public bool UsesWrap { get; set; }
        [Column("file_exists")]
        public bool FileExists { get; set; }

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
        ///     Gets a specific Preset Part by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Preset Part.</param>
        /// <returns>The specific Preset Part if it exists; otherwise null.</returns>
        public static SidekickPart GetByID(DatabaseManager dbManager, int id)
        {
            SidekickPart part = dbManager.GetCurrentDbConnection().Find<SidekickPart>(id);
            Decorate(dbManager, part);
            return part;
        }

        /// <summary>
        ///     Gets a list of all the parts in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all parts in the database.</returns>
        public static List<SidekickPart> GetAll(DatabaseManager dbManager)
        {
            List<SidekickPart> parts = dbManager.GetCurrentDbConnection().Table<SidekickPart>().ToList();

            foreach (SidekickPart part in parts)
            {
                Decorate(dbManager, part);
            }

            return parts;
        }

        /// <summary>
        ///     Gets a list of all the parts in the database for a given Character Part Type.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partType">The Character Part Type to get all the parts for.</param>
        /// <returns>A list of all parts for the given Character Part Type in the database.</returns>
        public static List<SidekickPart> GetAllForPartType(DatabaseManager dbManager, CharacterPartType partType)
        {
            List<SidekickPart> parts = dbManager.GetCurrentDbConnection().Table<SidekickPart>().Where(part => part.Type == partType).ToList();

            foreach (SidekickPart part in parts)
            {
                Decorate(dbManager, part);
            }

            return parts;
        }

        /// <summary>
        ///     Gets a list of all the parts in the database for a given Character Part Type.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="species">The Character Part Type to get all the parts for.</param>
        /// <param name="onlyPartsWithFile">Whether to include parts that have a file in the project or not.</param>
        /// <returns>A list of all parts for the given Character Part Type in the database.</returns>
        public static List<SidekickPart> GetAllForSpecies(DatabaseManager dbManager, SidekickSpecies species, bool onlyPartsWithFile = true)
        {
            List<SidekickPart> parts = new List<SidekickPart>();
            if (onlyPartsWithFile)
            {
                parts = dbManager.GetCurrentDbConnection().Table<SidekickPart>().Where(part => part.PtrSpecies == species.ID && part.FileExists == onlyPartsWithFile).ToList();
            }
            else
            {
                parts = dbManager.GetCurrentDbConnection().Table<SidekickPart>().Where(part => part.PtrSpecies == species.ID).ToList();
            }

            foreach (SidekickPart part in parts)
            {
                Decorate(dbManager, part);
            }

            return parts;
        }

        /// <summary>
        ///     Get the part in the database for the given part file name.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="fileName">The file name to get the part for.</param>
        /// <returns>A part in the database for the given part file name if it exists; otherwise null</returns>
        public static SidekickPart GetByPartFileName(DatabaseManager dbManager, string fileName)
        {
            SidekickPart part = dbManager.GetCurrentDbConnection().Table<SidekickPart>().FirstOrDefault(part => part.FileName == fileName);
            Decorate(dbManager, part);
            return part;
        }

        /// <summary>
        ///     Gets all the base parts from the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all the base parts from the database.</returns>
        public static List<SidekickPart> GetBaseParts(DatabaseManager dbManager)
        {
            List<SidekickPart> parts = dbManager.GetCurrentDbConnection().Table<SidekickPart>().Where(part => part.FileName.Contains("_BASE_")).ToList();

            foreach (SidekickPart part in parts)
            {
                Decorate(dbManager, part);
            }

            return parts;
        }

        /// <summary>
        ///     Search for a part in the database where the part name or filename match the given term.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partName">The term to search for the part with.</param>
        /// <returns>A part in the database for that matches the search term if it exists; otherwise null</returns>
        public static SidekickPart SearchForByName(DatabaseManager dbManager, string partName)
        {
            SidekickPart part = dbManager.GetCurrentDbConnection().Table<SidekickPart>().FirstOrDefault(part => part.Name == partName || part
                .FileName.Contains(partName));
            Decorate(dbManager, part);
            return part;
        }

        /// <summary>
        ///     Checks to see if the provided part name is unique.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partName">The part name to check and see if it is unique.</param>
        /// <returns>True if no part exists in the database with given name; otherwise false.</returns>
        public static bool IsPartNameUnique(DatabaseManager dbManager, string partName)
        {
            SidekickPart part = dbManager.GetCurrentDbConnection().Table<SidekickPart>().FirstOrDefault(part => part.Name == partName);
            return part == null;
        }

        /// <summary>
        ///     Gets the species for a specific part.
        ///     TODO: get from the DB when data is populated.
        /// </summary>
        /// <param name="allSpecies">The list of all species to return a populated species object from.</param>
        /// <param name="partName">The name of the part to get the species for.</param>
        /// <returns>The species the part belongs to.</returns>
        public static SidekickSpecies GetSpeciesForPart(List<SidekickSpecies> allSpecies, string partName)
        {
            string shortcode = partName.Split('_').Last().Substring(0, 2);
            SidekickSpecies selectedSpecies = allSpecies[0];
            foreach (SidekickSpecies species in allSpecies)
            {
                if (string.Equals(shortcode, species.Code, StringComparison.CurrentCultureIgnoreCase))
                {
                    selectedSpecies = species;
                }
            }

            return selectedSpecies;
        }

        /// <summary>
        ///     Updates all of the given parts in the DB. This is an Update only, and will not insert new objects.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <param name="parts">The parts to update</param>
        public static void UpdateAll(DatabaseManager dbManager, List<SidekickPart> parts)
        {
            dbManager.GetCurrentDbConnection().UpdateAll(parts, false);
        }

        /// <summary>
        ///     Ensures that the given part has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="part">The part to decorate</param>
        private static void Decorate(DatabaseManager dbManager, SidekickPart part)
        {
            if (part != null)
            {
                if (part.Species == null && part.PtrSpecies >= 0)
                {
                    part.Species = SidekickSpecies.GetByID(dbManager, part.PtrSpecies);
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
        ///     Gets the image associated with this part.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <returns>The image associated with this part.</returns>
        public SidekickPartImage GetImageForPart(DatabaseManager dbManager)
        {
            return SidekickPartImage.GetByPart(dbManager, this);
        }

        /// <summary>
        ///     Deletes this item from the database
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            foreach (SidekickPartFilterRow row in SidekickPartFilterRow.GetAllForPart(dbManager, this))
            {
                row.Delete(dbManager);
            }

            foreach (SidekickPartPresetRow row in SidekickPartPresetRow.GetAllByPart(dbManager, this))
            {
                row.Delete(dbManager);
            }

            foreach (SidekickPartSpeciesLink link in SidekickPartSpeciesLink.GetAllForPart(dbManager, this))
            {
                link.Delete(dbManager);
            }

            SidekickPartImage image = SidekickPartImage.GetByPart(dbManager, this);
            image?.Delete(dbManager);

            dbManager.GetCurrentDbConnection().Delete<SidekickPart>(ID);
        }

        /// <summary>
        ///     Gets the GameObject model of this part.
        /// </summary>
        /// <returns>A GameObject with the part model</returns>
        public GameObject GetPartModel()
        {
            string resource = GetResourcePath(Location);
            return Resources.Load<GameObject>(resource);
        }

        /// <summary>
        ///     Checks if the file for this part exists.
        /// </summary>
        /// <returns>True if the file is available; otherwise false</returns>
        public bool IsFileAvailable()
        {
            FileExists = File.Exists(Location);
            return FileExists;
        }

        /// <summary>
        ///     Gets a resource path for using with Resources.Load() from a full path.
        /// </summary>
        /// <param name="fullPath">The full path to get the resource path from.</param>
        /// <returns>The resource path.</returns>
        private string GetResourcePath(string fullPath)
        {
            int startIndex = fullPath.IndexOf("Resources", StringComparison.Ordinal) + 10;
            string resourcePath = fullPath.Substring(startIndex, fullPath.Length - startIndex);
            return Path.Combine(Path.GetDirectoryName(resourcePath)?? "", Path.GetFileNameWithoutExtension(resourcePath));
        }

        /// <inheritdoc cref="Equals"/>
        public override bool Equals(object obj)
        {
            SidekickPart part = (SidekickPart) obj;
            if (ID > 0 && part?.ID > 0)
            {
                return ID == part?.ID;
            }

            return Name.Equals(part?.Name);
        }

        /// <inheritdoc cref="GetHashCode"/>
        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(_species);
            hashCode.Add(ID);
            hashCode.Add(PtrSpecies);
            hashCode.Add((int) Type);
            hashCode.Add((int) PartGroup);
            hashCode.Add(Name);
            hashCode.Add(FileName);
            hashCode.Add(Location);
            hashCode.Add(UsesWrap);
            hashCode.Add(FileExists);
            return hashCode.ToHashCode();
        }
    }
}
