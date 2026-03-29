// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Enums;
using System;

namespace Synty.SidekickCharacters.Utils
{
    /// <summary>
    ///     A collection of utility methods related to operations on CharacterPartType enum values.
    /// </summary>
    public static class CharacterPartTypeUtils
    {
        /// <summary>
        ///     Get the part group that the given part type belongs to
        /// </summary>
        /// <param name="basePartType">The part type to get the group of</param>
        /// <returns>The PartGroup the part type belongs to</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the part type is unrecognised</exception>
        public static PartGroup GetPartGroup(this CharacterPartType basePartType)
        {
            switch (basePartType)
            {
                case CharacterPartType.Head:
                case CharacterPartType.Hair:
                case CharacterPartType.EyebrowLeft:
                case CharacterPartType.EyebrowRight:
                case CharacterPartType.EyeLeft:
                case CharacterPartType.EyeRight:
                case CharacterPartType.EarLeft:
                case CharacterPartType.EarRight:
                case CharacterPartType.FacialHair:
                case CharacterPartType.AttachmentHead:
                case CharacterPartType.AttachmentFace:
                case CharacterPartType.Nose:
                case CharacterPartType.Teeth:
                case CharacterPartType.Tongue:
                    return PartGroup.Head;

                case CharacterPartType.Torso:
                case CharacterPartType.ArmUpperLeft:
                case CharacterPartType.ArmUpperRight:
                case CharacterPartType.ArmLowerLeft:
                case CharacterPartType.ArmLowerRight:
                case CharacterPartType.HandLeft:
                case CharacterPartType.HandRight:
                case CharacterPartType.AttachmentBack:
                case CharacterPartType.AttachmentShoulderLeft:
                case CharacterPartType.AttachmentShoulderRight:
                case CharacterPartType.AttachmentElbowLeft:
                case CharacterPartType.AttachmentElbowRight:
                case CharacterPartType.Wrap:
                    return PartGroup.UpperBody;

                case CharacterPartType.Hips:
                case CharacterPartType.LegLeft:
                case CharacterPartType.LegRight:
                case CharacterPartType.FootLeft:
                case CharacterPartType.FootRight:
                case CharacterPartType.AttachmentHipsFront:
                case CharacterPartType.AttachmentHipsBack:
                case CharacterPartType.AttachmentHipsLeft:
                case CharacterPartType.AttachmentHipsRight:
                case CharacterPartType.AttachmentKneeLeft:
                case CharacterPartType.AttachmentKneeRight:
                    return PartGroup.LowerBody;

                default:
                    throw new ArgumentOutOfRangeException(nameof(basePartType), basePartType, null);
            }
        }

        /// <summary>
        ///     Checks if the given part type is a species specific part or not.
        /// </summary>
        /// <param name="partType">The part type to check.</param>
        /// <returns>True if a species specific part type; otherwise false.</returns>
        public static bool IsSpeciesSpecificPartType(this CharacterPartType partType)
        {
            switch (partType)
            {
                case CharacterPartType.Head:
                case CharacterPartType.Hair:
                case CharacterPartType.EyebrowLeft:
                case CharacterPartType.EyebrowRight:
                case CharacterPartType.EyeLeft:
                case CharacterPartType.EyeRight:
                case CharacterPartType.EarLeft:
                case CharacterPartType.EarRight:
                case CharacterPartType.FacialHair:
                case CharacterPartType.Nose:
                case CharacterPartType.Teeth:
                case CharacterPartType.Tongue:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        ///     Gets the string value of the part type from a part short code.
        /// </summary>
        /// <param name="shortCode">The short code to get the part type name for.</param>
        /// <returns>The part type name for the given short code.</returns>
        public static string GetTypeNameFromShortcode(string shortCode)
        {
            switch (shortCode)
            {
                case "01HEAD":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Head);
                case "02HAIR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Hair);
                case "03EBRL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EyebrowLeft);
                case "04EBRR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EyebrowRight);
                case "05EYEL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EyeLeft);
                case "06EYER":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EyeRight);
                case "07EARL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EarLeft);
                case "08EARR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.EarRight);
                case "09FCHR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.FacialHair);
                case "10TORS":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Torso);
                case "11AUPL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.ArmUpperLeft);
                case "12AUPR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.ArmUpperRight);
                case "13ALWL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.ArmLowerLeft);
                case "14ALWR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.ArmLowerRight);
                case "15HNDL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.HandLeft);
                case "16HNDR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.HandRight);
                case "17HIPS":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Hips);
                case "18LEGL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.LegLeft);
                case "19LEGR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.LegRight);
                case "20FOTL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.FootLeft);
                case "21FOTR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.FootRight);
                case "22AHED":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentHead);
                case "23AFAC":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentFace);
                case "24ABAC":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentBack);
                case "25AHPF":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentHipsFront);
                case "26AHPB":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentHipsBack);
                case "27AHPL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentHipsLeft);
                case "28AHPR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentHipsRight);
                case "29ASHL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentShoulderLeft);
                case "30ASHR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentShoulderRight);
                case "31AEBL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentElbowLeft);
                case "32AEBR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentElbowRight);
                case "33AKNL":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentKneeLeft);
                case "34AKNR":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.AttachmentKneeRight);
                case "35NOSE":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Nose);
                case "36TETH":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Teeth);
                case "37TONG":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Tongue);
                case "38WRAP":
                    return Enum.GetName(typeof(CharacterPartType), CharacterPartType.Wrap);
                default:
                    return shortCode;
            }
        }

        /// <summary>
        ///     Returns the part type string based onm the CharacterPartType
        /// </summary>
        /// <param name="type">The CharacterPartType to get the part type string from</param>
        /// <returns>The part type string</returns>
        public static string GetPartTypeString(CharacterPartType type)
        {
            switch (type)
            {
                case CharacterPartType.Head:
                    return "01HEAD";
                case CharacterPartType.Hair:
                    return "02HAIR";
                case CharacterPartType.EyebrowLeft:
                    return "03EBRL";
                case CharacterPartType.EyebrowRight:
                    return "04EBRR";
                case CharacterPartType.EyeLeft:
                    return "05EYEL";
                case CharacterPartType.EyeRight:
                    return "06EYER";
                case CharacterPartType.EarLeft:
                    return "07EARL";
                case CharacterPartType.EarRight:
                    return "08EARR";
                case CharacterPartType.FacialHair:
                    return "09FCHR";
                case CharacterPartType.Torso:
                    return "10TORS";
                case CharacterPartType.ArmUpperLeft:
                    return "11AUPL";
                case CharacterPartType.ArmUpperRight:
                    return "12AUPR";
                case CharacterPartType.ArmLowerLeft:
                    return "13ALWL";
                case CharacterPartType.ArmLowerRight:
                    return "14ALWR";
                case CharacterPartType.HandLeft:
                    return "15HNDL";
                case CharacterPartType.HandRight:
                    return "16HNDR";
                case CharacterPartType.Hips:
                    return "17HIPS";
                case CharacterPartType.LegLeft:
                    return "18LEGL";
                case CharacterPartType.LegRight:
                    return "19LEGR";
                case CharacterPartType.FootLeft:
                    return "20FOTL";
                case CharacterPartType.FootRight:
                    return "21FOTR";
                case CharacterPartType.AttachmentHead:
                    return "22AHED";
                case CharacterPartType.AttachmentFace:
                    return "23AFAC";
                case CharacterPartType.AttachmentBack:
                    return "24ABAC";
                case CharacterPartType.AttachmentHipsFront:
                    return "25AHPF";
                case CharacterPartType.AttachmentHipsBack:
                    return "26AHPB";
                case CharacterPartType.AttachmentHipsLeft:
                    return "27AHPL";
                case CharacterPartType.AttachmentHipsRight:
                    return "28AHPR";
                case CharacterPartType.AttachmentShoulderLeft:
                    return "29ASHL";
                case CharacterPartType.AttachmentShoulderRight:
                    return "30ASHR";
                case CharacterPartType.AttachmentElbowLeft:
                    return "31AEBL";
                case CharacterPartType.AttachmentElbowRight:
                    return "32AEBR";
                case CharacterPartType.AttachmentKneeLeft:
                    return "33AKNL";
                case CharacterPartType.AttachmentKneeRight:
                    return "34AKNR";
                case CharacterPartType.Nose:
                    return "35NOSE";
                case CharacterPartType.Teeth:
                    return "36TETH";
                case CharacterPartType.Tongue:
                    return "37TONG";
                case CharacterPartType.Wrap:
                    return "38WRAP";
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Returns the tool tip text for the given character part type.
        /// </summary>
        /// <param name="partType">The character part type to get the tooltip for.</param>
        /// <returns>The tooltip text for the given character part type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid character part type is provided.</exception>
        public static string GetTooltipForPartType(this CharacterPartType partType)
        {
            switch (partType)
            {
                case CharacterPartType.Head:
                    return "A character part that makes up the base of the head area";
                case CharacterPartType.Hair:
                    return "A character part makes up hair on the top of a character's head";
                case CharacterPartType.EyebrowLeft:
                    return "A character part that is above the eyes as an eyebrows";
                case CharacterPartType.EyebrowRight:
                    return "A character part that is above the eyes as an eyebrows";
                case CharacterPartType.EyeLeft:
                    return "A character part that acts as an eye for the character";
                case CharacterPartType.EyeRight:
                    return "A character part that acts as an eye for the character";
                case CharacterPartType.EarLeft:
                    return "A character part that is attached to the side of the head as an ear";
                case CharacterPartType.EarRight:
                    return "A character part that is attached to the side of the head as an ear";
                case CharacterPartType.FacialHair:
                    return "A character part that is on the face as facial hair";
                case CharacterPartType.Torso:
                    return "A character part that sits at the center of the character as the torso";
                case CharacterPartType.ArmUpperLeft:
                    return "A character part that is attached to the torso as the upper arm";
                case CharacterPartType.ArmUpperRight:
                    return "A character part that is attached to the torso as the upper arm";
                case CharacterPartType.ArmLowerLeft:
                    return "A character part that is attached to the upper arm as the lower arm";
                case CharacterPartType.ArmLowerRight:
                    return "A character part that is attached to the upper arm as the lower arm";
                case CharacterPartType.HandLeft:
                    return "A character part that is attached to the lower arm as a hand";
                case CharacterPartType.HandRight:
                    return "A character part that is attached to the lower arm as a hand";
                case CharacterPartType.Hips:
                    return "A character part that sits below the torso as the hips of the character";
                case CharacterPartType.LegLeft:
                    return "A character part that is attached to the hips as a left leg of the character";
                case CharacterPartType.LegRight:
                    return "A character part that is attached to the hips as a right leg of the character";
                case CharacterPartType.FootLeft:
                    return "A character part that is attached to the lower leg as a left foot of the character";
                case CharacterPartType.FootRight:
                    return "A character part that is attached to the lower leg as a right foot of the character";
                case CharacterPartType.AttachmentHead:
                    return "The head attachment is a part that covers the head (for example - hat, helmet, mask etc)";
                case CharacterPartType.AttachmentFace:
                    return "The face attachment is a part that covers the face but doesn’t cover the full head like a head attachment (for example - glasses, VRheadset, eye patch etc)";
                case CharacterPartType.AttachmentBack:
                    return "A character part that is attached to the back of the torso";
                case CharacterPartType.AttachmentHipsFront:
                    return "A character part that sit at the front of the hips as a front hip attachment";
                case CharacterPartType.AttachmentHipsBack:
                    return "A character part that sit at the back of the hips as a back hip attachment";
                case CharacterPartType.AttachmentHipsLeft:
                    return "A character part that sit on the left side of the hips as a left hip attachment";
                case CharacterPartType.AttachmentHipsRight:
                    return "A character part that sit on the right side of the hips as a right hip attachment";
                case CharacterPartType.AttachmentShoulderLeft:
                    return "A character part that is attached to the left shoulder of the character";
                case CharacterPartType.AttachmentShoulderRight:
                    return "A character part that is attached to the right shoulder of the character";
                case CharacterPartType.AttachmentElbowLeft:
                    return "A character part that is attached to the left elbow of the character";
                case CharacterPartType.AttachmentElbowRight:
                    return "A character part that is attached to the right elbow of the character";
                case CharacterPartType.AttachmentKneeLeft:
                    return "A character part that is attached to the left knee of the character";
                case CharacterPartType.AttachmentKneeRight:
                    return "A character part that is attached to the right knee of the character";
                case CharacterPartType.Nose:
                    return "A character part that sits in the middle of the face as a nose";
                case CharacterPartType.Teeth:
                    return "A character part that sits in the mouth area of a character as teeth";
                case CharacterPartType.Tongue:
                    return "A character part that sits in the mouth area as a tongue";
                case CharacterPartType.Wrap:
                    return "A character part that that covers the chest of feminine characters. Only shown when required.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(partType), partType, "Invalid part type provided");
            }
        }
    }
}
