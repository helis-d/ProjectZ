// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.SkinnedMesh
{
    /// <summary>
    ///     Wrapper class to hold information about a given blendshape.
    /// </summary>
    public class BlendShapeData
    {
        public string blendShapeFrameName = "";
        public int blendShapeFrameIndex = -1;
        public float blendShapeCurrentValue = 0.0f;
        public List<Vector3> startDeltaVertices = new List<Vector3>();
        public List<Vector3> startDeltaNormals = new List<Vector3>();
        public List<Vector3> startDeltaTangents = new List<Vector3>();
        public List<Vector3> finalDeltaVertices = new List<Vector3>();
        public List<Vector3> finalDeltaNormals = new List<Vector3>();
        public List<Vector3> finalDeltaTangents = new List<Vector3>();
        public string blendShapeNameOnCombinedMesh = "";

        protected bool Equals(BlendShapeData other)
        {
            return blendShapeFrameName.Equals(other.blendShapeFrameName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((BlendShapeData) obj);
        }

        public override int GetHashCode()
        {
            return (blendShapeFrameName != null ? blendShapeFrameName.GetHashCode() : 0);
        }
    }
}
