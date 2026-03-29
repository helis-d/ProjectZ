// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_color_row")]
    public class SidekickColorRow
    {
        private SidekickColorSet _colorSet;
        private SidekickColorProperty _colorProperty;
        private Color? _niceColor;
        private Color? _niceMetallic;
        private Color? _niceSmoothness;
        private Color? _niceReflection;
        private Color? _niceEmission;
        private Color? _niceOpacity;

        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("ptr_color_set")]
        public int PtrColorSet { get; set; }
        [Column("ptr_color_property")]
        public int PtrColorProperty { get; set; }
        [Column("color")]
        public string MainColor { get; set; }
        [Column("metallic")]
        public string Metallic { get; set; }
        [Column("smoothness")]
        public string Smoothness { get; set; }
        [Column("reflection")]
        public string Reflection { get; set; }
        [Column("emission")]
        public string Emission { get; set; }
        [Column("opacity")]
        public string Opacity { get; set; }

        [Ignore]
        public SidekickColorSet ColorSet
        {
            get => _colorSet;
            set
            {
                _colorSet = value;
                PtrColorSet = value.ID;
            }
        }
        [Ignore]
        public SidekickColorProperty ColorProperty
        {
            get => _colorProperty;
            set
            {
                _colorProperty = value;
                PtrColorProperty = value.ID;
            }
        }
        [Ignore]
        public Color NiceColor
        {
            get
            {
                _niceColor ??= ColorUtility.TryParseHtmlString("#" + MainColor, out Color color) ? color : Color.white;
                return (Color) _niceColor;
            }
            set
            {
                _niceColor = value;
                MainColor = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public Color NiceMetallic
        {
            get
            {
                _niceMetallic ??= ColorUtility.TryParseHtmlString("#" + Metallic, out Color color) ? color : Color.white;
                return (Color) _niceMetallic;
            }
            set
            {
                _niceMetallic = value;
                Metallic = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public Color NiceSmoothness
        {
            get
            {
                _niceSmoothness ??= ColorUtility.TryParseHtmlString("#" + Smoothness, out Color color) ? color : Color.white;
                return (Color) _niceSmoothness;
            }
            set
            {
                _niceSmoothness = value;
                Smoothness = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public Color NiceReflection
        {
            get
            {
                _niceReflection ??= ColorUtility.TryParseHtmlString("#" + Reflection, out Color color) ? color : Color.white;
                return (Color) _niceReflection;
            }
            set
            {
                _niceReflection = value;
                Reflection = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public Color NiceEmission
        {
            get
            {
                _niceEmission ??= ColorUtility.TryParseHtmlString("#" + Emission, out Color color) ? color : Color.white;
                return (Color) _niceEmission;
            }
            set
            {
                _niceEmission = value;
                Emission = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public Color NiceOpacity
        {
            get
            {
                _niceOpacity ??= ColorUtility.TryParseHtmlString("#" + Opacity, out Color color) ? color : Color.white;
                return (Color) _niceOpacity;
            }
            set
            {
                _niceOpacity = value;
                Opacity = ColorUtility.ToHtmlStringRGB(value);
            }
        }
        [Ignore]
        public bool IsLocked { get; set; }
        [Ignore]
        public Image ButtonImage { get; set; }

        /// <summary>
        ///     Gets a list of all the Color Rows in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Color Rows in the database.</returns>
        public static List<SidekickColorRow> GetAll(DatabaseManager dbManager)
        {
            List<SidekickColorRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorRow>().ToList();

            foreach (SidekickColorRow row in rows)
            {
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Gets a specific Color Row by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Row.</param>
        /// <returns>The specific Color Row if it exists; otherwise null.</returns>
        public static SidekickColorRow GetByID(DatabaseManager dbManager, int id)
        {
            SidekickColorRow row = dbManager.GetCurrentDbConnection().Find<SidekickColorRow>(id);
            Decorate(dbManager, row);
            return row;
        }

        /// <summary>
        ///     Gets a list of all the Color Rows in the database that have the matching Property.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="property">The property to get all the color rows for.</param>
        /// <returns>A list of all color rows in the database for the given property.</returns>
        public static List<SidekickColorRow> GetAllByProperty(DatabaseManager dbManager, SidekickColorProperty property)
        {
            List<SidekickColorRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorRow>()
                .Where(row => row.PtrColorProperty == property.ID)
                .ToList();

            foreach (SidekickColorRow row in rows)
            {
                row.ColorProperty = property;
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Gets a list of all the Color Rows in the database that have the matching Set and Property.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="set">The color set to get the color rows for.</param>
        /// <param name="property">The property to get all the color rows for.</param>
        /// <returns>A list of all color rows in the database for the given set and property.</returns>
        public static List<SidekickColorRow> GetAllBySetAndProperty(DatabaseManager dbManager, SidekickColorSet set, SidekickColorProperty property)
        {
            List<SidekickColorRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorRow>()
                .Where(row => row.PtrColorSet == set.ID && row.PtrColorProperty == property.ID)
                .ToList();

            foreach (SidekickColorRow row in rows)
            {
                row.ColorSet = set;
                row.ColorProperty = property;
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Gets a list of all the Color Rows in the database that have the matching Set.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="set">The color set to get the color rows for.</param>
        /// <returns>A list of all color rows in the database for the given set.</returns>
        public static List<SidekickColorRow> GetAllBySet(DatabaseManager dbManager, SidekickColorSet set)
        {
            List<SidekickColorRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorRow>()
                .Where(row => row.PtrColorSet == set.ID)
                .ToList();

            foreach (SidekickColorRow row in rows)
            {
                row.ColorSet = set;
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Creates a SidekickColorRow from a SidekickColorPresetRow.
        /// </summary>
        /// <param name="row">The SidekickColorPresetRow to convert.</param>
        /// <returns>A SidekickColorRow created from a SidekickColorPresetRow.</returns>
        public static SidekickColorRow CreateFromPresetColorRow(SidekickColorPresetRow row)
        {
            SidekickColorRow newRow = new SidekickColorRow()
            {
                MainColor = row.MainColor,
                Emission = row.Emission,
                Metallic = row.Metallic,
                Opacity = row.Opacity,
                Reflection = row.Reflection,
                Smoothness = row.Smoothness,
                ColorProperty = row.ColorProperty
            };

            return newRow;
        }

        /// <summary>
        ///     Ensures that the given row has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="row">The color row to decorate</param>
        /// <returns>A color row with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickColorRow row)
        {
            // don't need PtrColorProperty check as should always be >= 0; if it's not, we have bad data and want the error
            row.ColorProperty ??= SidekickColorProperty.GetByID(dbManager, row.PtrColorProperty);
            if (row.ColorSet == null && row.PtrColorSet >= 0)
            {
                row.ColorSet = SidekickColorSet.GetByID(dbManager, row.PtrColorSet);
            }
        }

        /// <summary>
        ///     Delete this color row from the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        public void Delete(DatabaseManager dbManager)
        {
            int deletedCount = dbManager.GetCurrentDbConnection().Delete<SidekickColorRow>(ID);
            if (deletedCount == 0)
            {
                throw new Exception($"Could not delete color set with ID '{ID}'");
            }
        }

        /// <summary>
        ///     Inserts, or updates the values in the database, depending on this object has been saved before or not.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        public void Save(DatabaseManager dbManager)
        {
            if (ID < 0)
            {
                SaveToDB(dbManager);
            }
            else
            {
                UpdateDB(dbManager);
            }
        }

        /// <summary>
        ///     Saves this Color Set to the database with the current values.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        private void SaveToDB(DatabaseManager dbManager)
        {
            SQLiteConnection connection = dbManager.GetCurrentDbConnection();
            int insertCount = connection.Insert(this);
            if (insertCount == 0)
            {
                throw new Exception("Unable to save current color row");
            }

            // in theory this could return a different ID, but in practice it's highly unlikely
            ID = (int) SQLite3.LastInsertRowid(connection.Handle);
        }

        /// <summary>
        ///     Updates this Color Set in the database with the current values.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        private void UpdateDB(DatabaseManager dbManager)
        {
            int updatedCount = dbManager.GetCurrentDbConnection().Update(this);
            if (updatedCount == 0)
            {
                throw new Exception($"Could not update color row with ID '{ID}'");
            }
        }
    }
}
