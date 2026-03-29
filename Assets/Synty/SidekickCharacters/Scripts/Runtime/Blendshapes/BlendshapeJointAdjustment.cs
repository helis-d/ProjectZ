// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.Blendshapes
{
    public static class BlendshapeJointAdjustment
    {
        // Feminine_Offset Values
        private static readonly Vector3 FEMININE_OFFSET_HIP_ATTACH_BACK = new Vector3(0.0f, 0.00095f, 0.00072f);
        private static readonly Vector3 FEMININE_OFFSET_HIP_ATTACH_L = new Vector3(-0.0031f, -0.00241f, 0.00727f);
        private static readonly Vector3 FEMININE_OFFSET_HIP_ATTACH_FRONT = new Vector3(0.0f, 0.00138f, -0.01824f);
        private static readonly Vector3 FEMININE_OFFSET_KNEE_ATTACH_L = new Vector3(-0.00064f, -0.00106f, -0.001f);
        private static readonly Vector3 FEMININE_OFFSET_KNEE_ATTACH_R = new Vector3(-0.00064f, -0.00106f, -0.001f);
        private static readonly Vector3 FEMININE_OFFSET_HIP_ATTACH_R = new Vector3(0.0031f, -0.00241f, 0.00727f);
        private static readonly Vector3 FEMININE_OFFSET_ELBOW_ATTACH_R = new Vector3(0.00125f, -0.00021f, 0.0093f);
        private static readonly Vector3 FEMININE_OFFSET_SHOULDER_ATTACH_R = new Vector3(-0.01559f, -0.00467f, 0.00124f);
        private static readonly Vector3 FEMININE_OFFSET_BACK_ATTACH = new Vector3(0.0f, 0.00137f, 0.01903f);
        private static readonly Vector3 FEMININE_OFFSET_SHOULDER_ATTACH_L = new Vector3(-0.01559f, -0.00467f, -0.00124f);
        private static readonly Vector3 FEMININE_OFFSET_ELBOW_ATTACH_L = new Vector3(-0.00125f, -0.00021f, 0.0093f);

        // Heavy_Offset Values
        private static readonly Vector3 HEAVY_OFFSET_HIP_ATTACH_BACK = new Vector3(0.0f, -0.02628f, -0.15593f);
        private static readonly Vector3 HEAVY_OFFSET_HIP_ATTACH_L = new Vector3(-0.14163f, 0.00034f, 0.00718f);
        private static readonly Vector3 HEAVY_OFFSET_HIP_ATTACH_FRONT = new Vector3(0.0f, -0.06413f, 0.14101f);
        private static readonly Vector3 HEAVY_OFFSET_KNEE_ATTACH_L = new Vector3(0.00331f, -0.00084f, 0.02737f);
        private static readonly Vector3 HEAVY_OFFSET_KNEE_ATTACH_R = new Vector3(0.00331f, -0.00084f, 0.02737f);
        private static readonly Vector3 HEAVY_OFFSET_HIP_ATTACH_R = new Vector3(0.14163f, 0.00034f, 0.00718f);
        private static readonly Vector3 HEAVY_OFFSET_ELBOW_ATTACH_R = new Vector3(0.0f, 0.00138f, -0.02725f);
        private static readonly Vector3 HEAVY_OFFSET_SHOULDER_ATTACH_R = new Vector3(-0.00707f, 0.03154f, 0.00351f);
        private static readonly Vector3 HEAVY_OFFSET_BACK_ATTACH = new Vector3(0.0f, 0.02174f, -0.07185f);
        private static readonly Vector3 HEAVY_OFFSET_SHOULDER_ATTACH_L = new Vector3(-0.00707f, 0.03154f, -0.00351f);
        private static readonly Vector3 HEAVY_OFFSET_ELBOW_ATTACH_L = new Vector3(0.0f, 0.00138f, -0.02725f);

        // Skinny_Offset Values
        private static readonly Vector3 SKINNY_OFFSET_HIP_ATTACH_BACK = new Vector3(0.0f, -0.0042f, 0.00849f);
        private static readonly Vector3 SKINNY_OFFSET_HIP_ATTACH_L = new Vector3(0.01571f, -0.00161f, -0.00123f);
        private static readonly Vector3 SKINNY_OFFSET_HIP_ATTACH_FRONT = new Vector3(0.0f, 0.00159f, -0.01787f);
        private static readonly Vector3 SKINNY_OFFSET_KNEE_ATTACH_L = new Vector3(0.00167f, 0.00145f, -0.00388f);
        private static readonly Vector3 SKINNY_OFFSET_KNEE_ATTACH_R = new Vector3(0.00167f, 0.00145f, -0.00388f);
        private static readonly Vector3 SKINNY_OFFSET_HIP_ATTACH_R = new Vector3(-0.01571f, -0.00161f, -0.00123f);
        private static readonly Vector3 SKINNY_OFFSET_ELBOW_ATTACH_R = new Vector3(0.00128f, -0.00342f, 0.01043f);
        private static readonly Vector3 SKINNY_OFFSET_SHOULDER_ATTACH_R = new Vector3(0.0003f, -0.00818f, 0.00089f);
        private static readonly Vector3 SKINNY_OFFSET_BACK_ATTACH = new Vector3(-0.00001f, -0.00079f, 0.00938f);
        private static readonly Vector3 SKINNY_OFFSET_SHOULDER_ATTACH_L = new Vector3(0.0003f, -0.00818f, -0.00089f);
        private static readonly Vector3 SKINNY_OFFSET_ELBOW_ATTACH_L = new Vector3(-0.00128f, -0.00342f, 0.01043f);

        // Bulk_Offset Values
        private static readonly Vector3 BULK_OFFSET_HIP_ATTACH_BACK = new Vector3(0.0f, 0.00093f, -0.01182f);
        private static readonly Vector3 BULK_OFFSET_HIP_ATTACH_L = new Vector3(0.00115f, 0.00139f, 0.00299f);
        private static readonly Vector3 BULK_OFFSET_HIP_ATTACH_FRONT = new Vector3(0.0f, 0.00132f, 0.00489f);
        private static readonly Vector3 BULK_OFFSET_KNEE_ATTACH_L = new Vector3(0.00041f, 0.00005f, 0.00033f);
        private static readonly Vector3 BULK_OFFSET_KNEE_ATTACH_R = new Vector3(0.00041f, 0.00005f, 0.00033f);
        private static readonly Vector3 BULK_OFFSET_HIP_ATTACH_R = new Vector3(-0.00115f, 0.00139f, 0.00299f);
        private static readonly Vector3 BULK_OFFSET_ELBOW_ATTACH_R = new Vector3(0.00609f, 0.01381f, -0.06119f);
        private static readonly Vector3 BULK_OFFSET_SHOULDER_ATTACH_R = new Vector3(0.02127f, 0.04615f, -0.00861f);
        private static readonly Vector3 BULK_OFFSET_BACK_ATTACH = new Vector3(0.0f, 0.00465f, -0.03104f);
        private static readonly Vector3 BULK_OFFSET_SHOULDER_ATTACH_L = new Vector3(0.02127f, 0.04615f, 0.00861f);
        private static readonly Vector3 BULK_OFFSET_ELBOW_ATTACH_L = new Vector3(-0.00609f, 0.01381f, -0.06119f);

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

        /// <summary>
        ///     Gets the combined offset value for the current blend shape settings.
        /// </summary>
        /// <param name="blendValueFeminine">The feminine blend shape value.</param>
        /// <param name="blendValueSize">The body size blend shape value.</param>
        /// <param name="blendValueMuscle">The muscle blend shape value.</param>
        /// <param name="currentPosition">The current position of the rig.</param>
        /// <param name="partType">The current part type.</param>
        /// <returns>The combined blend shape offset.</returns>
        public static Vector3 GetCombinedOffsetValue(
            float blendValueFeminine,
            float blendValueSize,
            float blendValueMuscle,
            Vector3 currentPosition,
            CharacterPartType partType
        )
        {
            switch (partType)
            {
                case CharacterPartType.AttachmentBack:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_BACK_ATTACH, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_BACK_ATTACH, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_BACK_ATTACH, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_BACK_ATTACH, -blendValueSize));
                case CharacterPartType.AttachmentHipsFront:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_HIP_ATTACH_FRONT, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_HIP_ATTACH_FRONT, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_HIP_ATTACH_FRONT, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_HIP_ATTACH_FRONT, -blendValueSize));
                case CharacterPartType.AttachmentHipsBack:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_HIP_ATTACH_BACK, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_HIP_ATTACH_BACK, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_HIP_ATTACH_BACK, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_HIP_ATTACH_BACK, -blendValueSize));
                case CharacterPartType.AttachmentHipsLeft:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_HIP_ATTACH_L, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_HIP_ATTACH_L, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_HIP_ATTACH_L, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_HIP_ATTACH_L, -blendValueSize));
                case CharacterPartType.AttachmentHipsRight:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_HIP_ATTACH_R, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_HIP_ATTACH_R, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_HIP_ATTACH_R, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_HIP_ATTACH_R, -blendValueSize));
                case CharacterPartType.AttachmentShoulderLeft:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_SHOULDER_ATTACH_L, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_SHOULDER_ATTACH_L, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_SHOULDER_ATTACH_L, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_SHOULDER_ATTACH_L, -blendValueSize));
                case CharacterPartType.AttachmentShoulderRight:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_SHOULDER_ATTACH_R, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_SHOULDER_ATTACH_R, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_SHOULDER_ATTACH_R, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_SHOULDER_ATTACH_R, -blendValueSize));
                case CharacterPartType.AttachmentElbowLeft:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_ELBOW_ATTACH_L, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_ELBOW_ATTACH_L, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_ELBOW_ATTACH_L, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_ELBOW_ATTACH_L, -blendValueSize));
                case CharacterPartType.AttachmentElbowRight:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_ELBOW_ATTACH_R, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_ELBOW_ATTACH_R, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_ELBOW_ATTACH_R, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_ELBOW_ATTACH_R, -blendValueSize));
                case CharacterPartType.AttachmentKneeLeft:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_KNEE_ATTACH_L, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_KNEE_ATTACH_L, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_KNEE_ATTACH_L, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_KNEE_ATTACH_L, -blendValueSize));
                case CharacterPartType.AttachmentKneeRight:
                    return currentPosition
                        + Vector3.Lerp(Vector3.zero, FEMININE_OFFSET_KNEE_ATTACH_R, blendValueFeminine)
                        + Vector3.Lerp(Vector3.zero, BULK_OFFSET_KNEE_ATTACH_R, blendValueMuscle)
                        + (blendValueSize > 0
                            ? Vector3.Lerp(Vector3.zero, HEAVY_OFFSET_KNEE_ATTACH_R, blendValueSize)
                            : Vector3.Lerp(Vector3.zero, SKINNY_OFFSET_KNEE_ATTACH_R, -blendValueSize));
            }

            return Vector3.zero;
        }
    }
}
