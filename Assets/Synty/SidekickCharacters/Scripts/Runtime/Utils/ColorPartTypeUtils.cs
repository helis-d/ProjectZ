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
    ///     A collection of utility methods related to operations on ColorPartType enum values.
    /// </summary>
    public static class ColorPartTypeUtils
    {
        /// <summary>
        ///     Get the part types that the given color part type represents
        /// </summary>
        /// <param name="basePartType">The part type to get the associated types of</param>
        /// <returns>A List of part types that belongs to this ColorPartType</returns>
        public static List<ColorPartType> GetPartTypes(this ColorPartType basePartType)
        {
            switch (basePartType)
            {
                case ColorPartType.AllParts:
                    return new List<ColorPartType>
                    {
                        ColorPartType.Head,
                        ColorPartType.Hair,
                        ColorPartType.EyebrowLeft,
                        ColorPartType.EyebrowRight,
                        ColorPartType.EyeLeft,
                        ColorPartType.EyeRight,
                        ColorPartType.EarLeft,
                        ColorPartType.EarRight,
                        ColorPartType.FacialHair,
                        ColorPartType.Torso,
                        ColorPartType.ArmUpperLeft,
                        ColorPartType.ArmUpperRight,
                        ColorPartType.ArmLowerLeft,
                        ColorPartType.ArmLowerRight,
                        ColorPartType.HandLeft,
                        ColorPartType.HandRight,
                        ColorPartType.Hips,
                        ColorPartType.LegLeft,
                        ColorPartType.LegRight,
                        ColorPartType.FootLeft,
                        ColorPartType.FootRight,
                        ColorPartType.AttachmentHead,
                        ColorPartType.AttachmentFace,
                        ColorPartType.AttachmentBack,
                        ColorPartType.AttachmentHipsFront,
                        ColorPartType.AttachmentHipsBack,
                        ColorPartType.AttachmentHipsLeft,
                        ColorPartType.AttachmentHipsRight,
                        ColorPartType.AttachmentShoulderLeft,
                        ColorPartType.AttachmentShoulderRight,
                        ColorPartType.AttachmentElbowLeft,
                        ColorPartType.AttachmentElbowRight,
                        ColorPartType.AttachmentKneeLeft,
                        ColorPartType.AttachmentKneeRight,
                        ColorPartType.Nose,
                        ColorPartType.Teeth,
                        ColorPartType.Tongue,
                        // ColorPartType.Wrap,
                        // ColorPartType.AttachmentHandLeft,
                        // ColorPartType.AttachmentHandRight,
                    };

                case ColorPartType.CharacterHead:
                    return new List<ColorPartType>
                    {
                        ColorPartType.Head,
                        ColorPartType.Hair,
                        ColorPartType.EyebrowLeft,
                        ColorPartType.EyebrowRight,
                        ColorPartType.EyeLeft,
                        ColorPartType.EyeRight,
                        ColorPartType.EarLeft,
                        ColorPartType.EarRight,
                        ColorPartType.FacialHair,
                        ColorPartType.AttachmentHead,
                        ColorPartType.AttachmentFace,
                        ColorPartType.Nose,
                        ColorPartType.Teeth,
                        ColorPartType.Tongue,
                    };

                case ColorPartType.CharacterUpperBody:
                    return new List<ColorPartType>
                    {
                        ColorPartType.Torso,
                        // ColorPartType.Wrap,
                        ColorPartType.ArmUpperLeft,
                        ColorPartType.ArmUpperRight,
                        ColorPartType.ArmLowerLeft,
                        ColorPartType.ArmLowerRight,
                        ColorPartType.HandLeft,
                        ColorPartType.HandRight,
                        ColorPartType.AttachmentBack,
                        ColorPartType.AttachmentShoulderLeft,
                        ColorPartType.AttachmentShoulderRight,
                        ColorPartType.AttachmentElbowLeft,
                        ColorPartType.AttachmentElbowRight,
                        // ColorPartType.AttachmentHandLeft,
                        // ColorPartType.AttachmentHandRight,
                    };

                case ColorPartType.CharacterLowerBody:
                    return new List<ColorPartType>
                    {
                        ColorPartType.Hips,
                        ColorPartType.LegLeft,
                        ColorPartType.LegRight,
                        ColorPartType.FootLeft,
                        ColorPartType.FootRight,
                        ColorPartType.AttachmentHipsFront,
                        ColorPartType.AttachmentHipsBack,
                        ColorPartType.AttachmentHipsLeft,
                        ColorPartType.AttachmentHipsRight,
                        ColorPartType.AttachmentKneeLeft,
                        ColorPartType.AttachmentKneeRight,
                    };

                default:
                    // if it's not a group, then it only represents itself, so only return itself
                    return new List<ColorPartType>
                    {
                        basePartType,
                    };
            }
        }
    }
}
