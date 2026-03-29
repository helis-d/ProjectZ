// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Enums;
using System;
using System.Collections.Generic;

namespace Synty.SidekickCharacters.Utils
{
    /// <summary>
    ///     A collection of utility methods related to operations on PartGroup enum values.
    /// </summary>
    public static class PartGroupUtils
    {
        /// <summary>
        ///     Get the part types that the given part group contains
        /// </summary>
        /// <param name="basePartGroup">The part group to get the types of</param>
        /// <returns>A List of part types that belongs to this PartGroup</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the part group is unrecognised</exception>
        public static List<CharacterPartType> GetPartTypes(this PartGroup basePartGroup)
        {
            switch (basePartGroup)
            {
                case PartGroup.Head:
                    return new List<CharacterPartType>
                    {
                        CharacterPartType.Head,
                        CharacterPartType.Hair,
                        CharacterPartType.EyebrowLeft,
                        CharacterPartType.EyebrowRight,
                        CharacterPartType.EyeLeft,
                        CharacterPartType.EyeRight,
                        CharacterPartType.EarLeft,
                        CharacterPartType.EarRight,
                        CharacterPartType.FacialHair,
                        CharacterPartType.AttachmentHead,
                        CharacterPartType.AttachmentFace,
                        CharacterPartType.Nose,
                        CharacterPartType.Teeth,
                        CharacterPartType.Tongue,
                    };

                case PartGroup.UpperBody:
                    return new List<CharacterPartType>
                    {
                        CharacterPartType.Torso,
                        CharacterPartType.ArmUpperLeft,
                        CharacterPartType.ArmUpperRight,
                        CharacterPartType.ArmLowerLeft,
                        CharacterPartType.ArmLowerRight,
                        CharacterPartType.HandLeft,
                        CharacterPartType.HandRight,
                        CharacterPartType.AttachmentBack,
                        CharacterPartType.AttachmentShoulderLeft,
                        CharacterPartType.AttachmentShoulderRight,
                        CharacterPartType.AttachmentElbowLeft,
                        CharacterPartType.AttachmentElbowRight,
                        CharacterPartType.Wrap,
                        // CharacterPartType.AttachmentHandLeft,
                        // CharacterPartType.AttachmentHandRight,
                    };

                case PartGroup.LowerBody:
                    return new List<CharacterPartType>
                    {
                        CharacterPartType.Hips,
                        CharacterPartType.LegLeft,
                        CharacterPartType.LegRight,
                        CharacterPartType.FootLeft,
                        CharacterPartType.FootRight,
                        CharacterPartType.AttachmentHipsFront,
                        CharacterPartType.AttachmentHipsBack,
                        CharacterPartType.AttachmentHipsLeft,
                        CharacterPartType.AttachmentHipsRight,
                        CharacterPartType.AttachmentKneeLeft,
                        CharacterPartType.AttachmentKneeRight,
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(basePartGroup), basePartGroup, null);
            }
        }
    }
}
