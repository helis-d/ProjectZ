// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.SkinnedMesh;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Synty.SidekickCharacters.Utils
{
    public static class BlendShapeUtils
    {
        /// <summary>
        ///     Collects all of the blend shape data from a given skinnedMesh and Mesh, and stores them as blend shape data.
        /// </summary>
        /// <param name="mesh">The mesh to get the blend shape data from.</param>
        /// <param name="skinnedMesh">The skinned mesh to get the blend shape data from.</param>
        /// <param name="excludedBlendNames">The blend shape names, or partial names to exclude from the data.</param>
        /// <param name="verticesCountStartIndex">The start position for the delta vertices index.</param>
        /// <param name="allBlendShapeData">Passed in blendshape data used </param>
        /// <returns>A list of BlendShapeData from all blend shape data on the mesh and skinned mesh.</returns>
        public static List<BlendShapeData> GetBlendShapeData(
            Mesh mesh,
            SkinnedMeshRenderer skinnedMesh,
            string[] excludedBlendNames,
            int verticesCountStartIndex,
            List<BlendShapeData> allBlendShapeData
        )
        {

            // Debug.Log("Blend count in getter: " + allBlendShapeData.Count);

            int totalVerticesVerifiedAtHereForBlendShapes = verticesCountStartIndex;

            string[] blendShapes = new string[skinnedMesh.sharedMesh.blendShapeCount];
            for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = skinnedMesh.sharedMesh.GetBlendShapeName(i);
                if (excludedBlendNames.All(ebn => !blendShapeName.Contains(ebn)))
                {
                    blendShapes[i] = blendShapeName;
                }
            }

            for (int i = 0; i < blendShapes.Length; i++)
            {
                if (blendShapes[i] == null)
                {
                    continue;
                }

                string bsn = blendShapes[i];
                int index = bsn.IndexOf('.') + 1;
                bsn = "MESHBlends." + bsn.Substring(index, bsn.Length - index);

                int blendIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapes[i]);

                BlendShapeData blendShapeData = new BlendShapeData
                {
                    blendShapeFrameName = bsn,
                    blendShapeFrameIndex = blendIndex
                };

                if (allBlendShapeData.Count > 0 && allBlendShapeData.Contains(blendShapeData))
                {
                    BlendShapeData existingData = allBlendShapeData.Find(data => data.Equals(blendShapeData));
                    if (existingData != null)
                    {
                        blendShapeData = existingData;
                        allBlendShapeData.Remove(blendShapeData);
                    }
                }

                blendShapeData.blendShapeCurrentValue = skinnedMesh.GetBlendShapeWeight(blendIndex);

                Mesh sharedMesh = skinnedMesh.sharedMesh;
                int framesCount = sharedMesh.GetBlendShapeFrameCount(blendIndex);

                Vector3[] originalDeltaVertices = new Vector3[sharedMesh.vertexCount];
                Vector3[] originalDeltaNormals = new Vector3[sharedMesh.vertexCount];
                Vector3[] originalDeltaTangents = new Vector3[sharedMesh.vertexCount];

                Vector3[] finalDeltaVertices = new Vector3[mesh.vertexCount];
                Vector3[] finalDeltaNormals = new Vector3[mesh.vertexCount];
                Vector3[] finalDeltaTangents = new Vector3[mesh.vertexCount];

                if (blendShapeData.startDeltaVertices.Count < 1)
                {
                    blendShapeData.startDeltaVertices.AddRange(finalDeltaVertices);
                    blendShapeData.startDeltaNormals.AddRange(finalDeltaNormals);
                    blendShapeData.startDeltaTangents.AddRange(finalDeltaTangents);

                    blendShapeData.finalDeltaVertices.AddRange(finalDeltaVertices);
                    blendShapeData.finalDeltaNormals.AddRange(finalDeltaNormals);
                    blendShapeData.finalDeltaTangents.AddRange(finalDeltaTangents);
                }

                if (skinnedMesh.sharedMesh.GetBlendShapeIndex(blendShapes[i]) != -1)
                {
                    skinnedMesh.sharedMesh.GetBlendShapeFrameVertices(
                        blendIndex,
                        framesCount - 1,
                        originalDeltaVertices,
                        originalDeltaNormals,
                        originalDeltaTangents
                    );
                }

                for (int x = 0; x < originalDeltaVertices.Length; x++)
                {
                    blendShapeData.finalDeltaVertices[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaVertices[x];
                }

                for (int x = 0; x < originalDeltaNormals.Length; x++)
                {
                    blendShapeData.finalDeltaNormals[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaNormals[x];
                }

                for (int x = 0; x < originalDeltaTangents.Length; x++)
                {
                    blendShapeData.finalDeltaTangents[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaTangents[x];
                }

                allBlendShapeData.Add(blendShapeData);
            }

            return allBlendShapeData;
        }

        /// <summary>
        ///     Restores the given blend shape data to the given mesh and skinned mesh.
        /// </summary>
        /// <param name="blendData">The blend shape data to restore.</param>
        /// <param name="meshToRestoreTo">The mesh to restore the blend shape data to.</param>
        /// <param name="meshRenderer">The mesh renderer that the mesh belongs to.</param>
        public static void RestoreBlendShapeData(List<BlendShapeData> blendData, Mesh meshToRestoreTo, SkinnedMeshRenderer meshRenderer)
        {
            Dictionary<string, int> alreadyAddedBlendShapesNames = new Dictionary<string, int>();

            foreach (BlendShapeData blendShape in blendData)
            {
                string blendShapeName = blendShape.blendShapeFrameName;
                if (alreadyAddedBlendShapesNames.TryGetValue(blendShape.blendShapeFrameName, out int name))
                {
                    blendShapeName += " (" + name + ")";
                }

                meshToRestoreTo.AddBlendShapeFrame(
                    blendShapeName,
                    0.0f,
                    blendShape.startDeltaVertices.ToArray(),
                    blendShape.startDeltaNormals.ToArray(),
                    blendShape.startDeltaTangents.ToArray()
                );
                meshToRestoreTo.AddBlendShapeFrame(
                    blendShapeName,
                    100.0f,
                    blendShape.finalDeltaVertices.ToArray(),
                    blendShape.finalDeltaNormals.ToArray(),
                    blendShape.finalDeltaTangents.ToArray()
                );

                blendShape.blendShapeNameOnCombinedMesh = blendShapeName;

                if (alreadyAddedBlendShapesNames.ContainsKey(blendShape.blendShapeFrameName))
                {
                    alreadyAddedBlendShapesNames[blendShape.blendShapeFrameName] += 1;
                }
                else
                {
                    alreadyAddedBlendShapesNames.Add(blendShape.blendShapeFrameName, 0);
                }

            }

            foreach (BlendShapeData blendShape in blendData)
            {
                meshRenderer.SetBlendShapeWeight(
                    meshToRestoreTo.GetBlendShapeIndex(blendShape.blendShapeFrameName),
                    blendShape.blendShapeCurrentValue
                );
            }
        }
    }
}
