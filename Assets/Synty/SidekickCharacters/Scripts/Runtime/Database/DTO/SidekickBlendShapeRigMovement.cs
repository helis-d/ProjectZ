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
using System.Linq;
using UnityEngine;

namespace Synty.SidekickCharacters.Database.DTO
{
    [Table("sk_blend_shape_rig_movement")]
    public class SidekickBlendShapeRigMovement
    {
        [PrimaryKey, AutoIncrement, Column("id")]
        public int ID { get; set; }
        [Column("part_type")]
        public CharacterPartType PartType { get; set; }
        [Column("blend_type")]
        public BlendShapeType BlendType { get; set; }
        [Column("max_offset_x")]
        public float MaxOffsetX { get; set; }
        [Column("max_offset_y")]
        public float MaxOffsetY { get; set; }
        [Column("max_offset_z")]
        public float MaxOffsetZ { get; set; }
        [Column("max_rotation_x")]
        public float MaxRotationX { get; set; }
        [Column("max_rotation_y")]
        public float MaxRotationY { get; set; }
        [Column("max_rotation_z")]
        public float MaxRotationZ { get; set; }
        [Column("max_scale_x")]
        public float MaxScaleX { get; set; }
        [Column("max_scale_y")]
        public float MaxScaleY { get; set; }
        [Column("max_scale_z")]
        public float MaxScaleZ { get; set; }

        [Ignore]
        public Vector3 MaxOffset
        {
            get => new Vector3(MaxOffsetX, MaxOffsetY, MaxOffsetZ);
            set
            {
                MaxOffsetX = value.x;
                MaxOffsetY = value.y;
                MaxOffsetZ = value.z;
            }
        }

        [Ignore]
        public Quaternion MaxRotation
        {
            get => Quaternion.Euler(new Vector3(MaxRotationX, MaxRotationY, MaxRotationZ));
            set
            {
                Vector3 rot = value.eulerAngles;
                MaxRotationX = rot.x;
                MaxRotationY = rot.y;
                MaxRotationZ = rot.z;
            }
        }

        [Ignore]
        public Vector3 MaxScale
        {
            get => new Vector3(MaxScaleX, MaxScaleY, MaxScaleZ);
            set
            {
                MaxScaleX = value.x;
                MaxScaleY = value.y;
                MaxScaleZ = value.z;
            }
        }

        public static readonly Dictionary<CharacterPartType, string> PART_TYPE_JOINT_MAP = new Dictionary<CharacterPartType, string>
        {
            [CharacterPartType.AttachmentBack] = "backAttach",
            [CharacterPartType.AttachmentHipsFront] = "hipAttachFront",
            [CharacterPartType.AttachmentHipsBack] = "hipAttachBack",
            [CharacterPartType.AttachmentHipsLeft] = "hipAttach_l",
            [CharacterPartType.AttachmentHipsRight] = "hipAttach_r",
            [CharacterPartType.AttachmentShoulderLeft] = "shoulderAttach_l",
            [CharacterPartType.AttachmentShoulderRight] = "shoulderAttach_r",
            [CharacterPartType.AttachmentElbowLeft] = "elbowAttach_l",
            [CharacterPartType.AttachmentElbowRight] = "elbowAttach_r",
            [CharacterPartType.AttachmentKneeLeft] = "kneeAttach_l",
            [CharacterPartType.AttachmentKneeRight] = "kneeAttach_r"
        };

        public SidekickBlendShapeRigMovement()
        {
            // Empty constructor required for SQLite
        }

        private SidekickBlendShapeRigMovement(CharacterPartType partType, BlendShapeType blendType)
        {
            ID = -1;
            PartType = partType;
            BlendType = blendType;
        }

        /// <summary>
        ///     Gets a list of all the parts in the database.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <returns>A list of all blend shape rig movements in the database.</returns>
        public static List<SidekickBlendShapeRigMovement> GetAll(DatabaseManager dbManager)
        {
            return dbManager.GetCurrentDbConnection().Table<SidekickBlendShapeRigMovement>().ToList();
        }

        /// <summary>
        ///     Gets all the blend shape rig movements from the database, sorted into dictionaries for easy processing.
        /// </summary>
        /// <param name="dbManager">The database manager to use.</param>
        /// <returns>All the blend shape rig movements from the database, sorted into dictionaries for easy processing.</returns>
        public static Dictionary<CharacterPartType, Dictionary<BlendShapeType, SidekickBlendShapeRigMovement>> GetAllForProcessing(
            DatabaseManager dbManager
        )
        {
            Dictionary<CharacterPartType, Dictionary<BlendShapeType, SidekickBlendShapeRigMovement>> offsetLibrary =
                new Dictionary<CharacterPartType, Dictionary<BlendShapeType, SidekickBlendShapeRigMovement>>();
            List<SidekickBlendShapeRigMovement> allRigMovements = GetAll(dbManager);

            foreach (CharacterPartType type in PART_TYPE_JOINT_MAP.Keys)
            {
                List<SidekickBlendShapeRigMovement> typeMovements = allRigMovements.Where(rm => rm.PartType == type).ToList();
                Dictionary<BlendShapeType, SidekickBlendShapeRigMovement> typeMovementDictionary =
                    new Dictionary<BlendShapeType, SidekickBlendShapeRigMovement>();
                foreach (BlendShapeType blendType in Enum.GetValues(typeof(BlendShapeType)))
                {
                    SidekickBlendShapeRigMovement movement = typeMovements.FirstOrDefault(tm => tm.BlendType == blendType);
                    typeMovementDictionary[blendType] = movement;
                }

                offsetLibrary[type] = typeMovementDictionary;
            }

            return offsetLibrary;
        }

        /// <summary>
        ///     Gets a specific blend shape rig movement by its part type and blend shape type.
        /// </summary>
        /// <param name="dbManager">The Database Manager to use.</param>
        /// <param name="partType">The part type to filter by.</param>
        /// <param name="blendType">The blend shape to filter by.</param>
        /// <returns>The blend shape rig movement if it exists; otherwise null.</returns>
        public static SidekickBlendShapeRigMovement GetByPartTypeAndBlendType(DatabaseManager dbManager, CharacterPartType partType, BlendShapeType blendType)
        {
            SidekickBlendShapeRigMovement rigMovement = dbManager.GetCurrentDbConnection().Table<SidekickBlendShapeRigMovement>().FirstOrDefault(rm
                => rm.BlendType == blendType && rm.PartType == partType);
            return rigMovement ?? new SidekickBlendShapeRigMovement(partType, blendType);
        }

        public Vector3 GetBlendedOffsetValue(float blendValue)
        {
            return Vector3.Lerp(Vector3.zero, MaxOffset, blendValue);
        }

        public Quaternion GetBlendedRotationValue(float blendValue)
        {
            return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), MaxRotation, blendValue);
        }

        public Vector3 GetBlendedScaleValue(float blendValue)
        {
            return Vector3.Lerp(Vector3.zero, MaxScale, blendValue);
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
            dbManager.GetCurrentDbConnection().Delete<SidekickBlendShapeRigMovement>(ID);
        }
    }
}
