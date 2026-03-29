// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using SQLite;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_color_preset_row")]
    public class SidekickColorPresetRow
    {
        private SidekickColorPreset _colorPreset;
        private SidekickColorProperty _colorProperty;
        private Color? _niceColor;
        private Color? _niceMetallic;
        private Color? _niceSmoothness;
        private Color? _niceReflection;
        private Color? _niceEmission;
        private Color? _niceOpacity;

        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int ID { get; set; }
        [Column("ptr_color_preset")]
        public int PtrColorPreset { get; set; }
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
        public SidekickColorPreset ColorPreset
        {
            get => _colorPreset;
            set
            {
                _colorPreset = value;
                PtrColorPreset = value.ID;
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

        /// <summary>
        ///     Gets a list of all the Color Preset Rows in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all Color Preset Rows in the database.</returns>
        public static List<SidekickColorPresetRow> GetAll(DatabaseManager dbManager)
        {
            List<SidekickColorPresetRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetRow>().ToList();

            foreach (SidekickColorPresetRow row in rows)
            {
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Gets a specific Color Preset Row by its database ID.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="id">The id of the required Color Preset Row.</param>
        /// <returns>The specific Color Preset Row if it exists; otherwise null.</returns>
        public static SidekickColorPresetRow GetByID(DatabaseManager dbManager, int id)
        {
            SidekickColorPresetRow row = dbManager.GetCurrentDbConnection().Find<SidekickColorPresetRow>(id);
            Decorate(dbManager, row);
            return row;
        }

        /// <summary>
        ///     Gets a list of all the Color Preset Rows in the database that have the matching Property.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="property">The property to get all the color preset rows for.</param>
        /// <returns>A list of all color preset rows in the database for the given property.</returns>
        public static List<SidekickColorPresetRow> GetAllByProperty(DatabaseManager dbManager, SidekickColorProperty property)
        {
            List<SidekickColorPresetRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetRow>()
                .Where(row => row.PtrColorProperty == property.ID)
                .ToList();

            foreach (SidekickColorPresetRow row in rows)
            {
                row.ColorProperty = property;
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Gets a list of all the Color Preset Rows in the database that have the matching Set and Property.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="preset">The color preset to get the color preset rows for.</param>
        /// <param name="property">The property to get all the color preset rows for.</param>
        /// <returns>A list of all color preset rows in the database for the given set and property.</returns>
        public static List<SidekickColorPresetRow> GetAllByPresetAndProperty(
            DatabaseManager dbManager,
            SidekickColorPreset preset,
            SidekickColorProperty property
        )
        {
            List<SidekickColorPresetRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetRow>()
                .Where(row => row.PtrColorPreset == preset.ID && row.PtrColorProperty == property.ID)
                .ToList();

            foreach (SidekickColorPresetRow row in rows)
            {
                row.ColorProperty = property;
                row.ColorPreset = preset;
            }

            return rows;
        }

        /// <summary>
        ///     Gets a list of all the Color Preset Rows in the database that have the matching Set.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="preset">The color preset to get the color preset rows for.</param>
        /// <returns>A list of all color preset rows in the database for the given set.</returns>
        public static List<SidekickColorPresetRow> GetAllByPreset(DatabaseManager dbManager, SidekickColorPreset preset)
        {
            List<SidekickColorPresetRow> rows = dbManager.GetCurrentDbConnection().Table<SidekickColorPresetRow>()
                .Where(row => row.PtrColorPreset == preset.ID)
                .ToList();

            foreach (SidekickColorPresetRow row in rows)
            {
                row.ColorPreset = preset;
                Decorate(dbManager, row);
            }

            return rows;
        }

        /// <summary>
        ///     Ensures that the given row has its nice DTO class properties set
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="row">The color preset row to decorate</param>
        /// <returns>A color preset row with all DTO class properties set</returns>
        private static void Decorate(DatabaseManager dbManager, SidekickColorPresetRow row)
        {
            // don't need PtrColorProperty check as should always be >= 0; if it's not, we have bad data and want the error
            row.ColorProperty ??= SidekickColorProperty.GetByID(dbManager, row.PtrColorProperty);
            row.ColorPreset ??= SidekickColorPreset.GetByID(dbManager, row.PtrColorPreset);
        }

        /// <summary>
        ///     Updates or Inserts this item in the Database.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        public int Save(DatabaseManager dbManager)
        {
            if (ID < 0)
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
            dbManager.GetCurrentDbConnection().Delete<SidekickColorPresetRow>(ID);
        }
    }
}
