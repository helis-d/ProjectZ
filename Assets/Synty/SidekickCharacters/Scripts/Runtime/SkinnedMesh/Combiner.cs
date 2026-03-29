// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Synty.SidekickCharacters.SkinnedMesh
{
    /// <summary>
    ///     Combines a set of given SkinnedMeshRenderers into a single SkinnedMEshRenderer.
    /// </summary>
    public static class Combiner
    {
        /// <summary>
        ///     Merges meshes together, including maintaining blend shape data.
        /// </summary>
        /// <param name="skinnedMeshesToMerge">Meshes to merge.</param>
        /// <param name="finalMesh">The mesh to merge everything into.</param>
        /// <param name="finalSkinnedMeshRenderer">The SkinnedMeshRenderer to attach the combined mesh to.</param>
        public static void MergeAndGetAllBlendShapeDataOfSkinnedMeshRenderers(
            SkinnedMeshRenderer[] skinnedMeshesToMerge,
            Mesh finalMesh,
            SkinnedMeshRenderer finalSkinnedMeshRenderer
        )
        {
            List<BlendShapeData> allBlendShapeData = new List<BlendShapeData>();

            //Verify each skinned mesh renderer and get info about all blendshapes of all meshes
            int totalVerticesVerifiedAtHereForBlendShapes = 0;

            foreach (SkinnedMeshRenderer combine in skinnedMeshesToMerge)
            {
                // Skip any parts that have not been assigned
                if (combine == null)
                {
                    continue;
                }

                List<BlendShapeData> newData = BlendShapeUtils.GetBlendShapeData(
                    finalMesh,
                    combine,
                    Array.Empty<string>(),
                    totalVerticesVerifiedAtHereForBlendShapes,
                    allBlendShapeData
                );

                //Set vertices verified at here, after processing all blendshapes for this mesh
                totalVerticesVerifiedAtHereForBlendShapes += combine.sharedMesh.vertexCount;
            }

            BlendShapeUtils.RestoreBlendShapeData(allBlendShapeData, finalMesh, finalSkinnedMeshRenderer);
        }

        /// <summary>
        ///     Processes the given GameObject and combines the objects contained into combined meshes grouped by their material.
        ///     Then returns a new GameObject with the combined data.
        /// </summary>
        /// <param name="skinnedMeshesToCombine">All of the meshes to combine into a single model</param>
        /// <param name="baseModel">The base model that has the base rig and where the skinned meshes will be combined to.</param>
        /// <param name="baseMaterial">The base material to use for the combined model.</param>
        /// <returns>A new GameObject containing all the combined objects, grouped and combined by Material.</returns>
        public static GameObject CreateCombinedSkinnedMesh(
            List<SkinnedMeshRenderer> skinnedMeshesToCombine,
            GameObject baseModel,
            Material baseMaterial
        )
        {
            // Create the new base GameObject. This will store all the combined meshes.
            GameObject combinedModel = new GameObject("Prefab Character");
            GameObject combinedSkinnedMesh = new GameObject("mesh");
            combinedSkinnedMesh.transform.parent = combinedModel.transform;

            Transform modelRootBone = baseModel.GetComponentInChildren<SkinnedMeshRenderer>().rootBone;

            // Initialise bone data stores.
            Transform[] bones = Array.Empty<Transform>();
            int boneCount = 0;

            skinnedMeshesToCombine.Sort((a, b) => string.Compare(a.name, b.name));
            Material material = null;
            Mesh mesh = new Mesh();
            int boneOffset = 0;
            GameObject rootBone = GameObject.Instantiate(modelRootBone.gameObject, combinedModel.transform, true);
            rootBone.name = modelRootBone.name;
            Hashtable boneNameMap = CreateBoneNameMap(rootBone);
            Transform[] additionalBones = FindAdditionalBones(boneNameMap, new List<SkinnedMeshRenderer>(skinnedMeshesToCombine));
            if (additionalBones.Length > 0)
            {
                JoinAdditionalBonesToBoneArray(bones, additionalBones, boneNameMap);
                // Need to redo the name map now that we have updated the bone array.
                boneNameMap = CreateBoneNameMap(rootBone);
            }

            List<CombineInstance> combineInstances = new List<CombineInstance>();
            List<Matrix4x4> bindPosesToMerge = new List<Matrix4x4>();

            // Iterate through the skinned meshes and process them into Material groupings, and also process the bones as required.
            foreach (SkinnedMeshRenderer child in skinnedMeshesToCombine)
            {
                material = child.sharedMaterial;

                mesh = MeshUtils.CopyMesh(child.sharedMesh);

                boneCount += child.bones.Length;

                Transform[] existingBones = bones;
                bones = new Transform[boneCount];
                Array.Copy(existingBones, bones, existingBones.Length);
                Transform[] newBones = new Transform[child.bones.Length];

                for (int i = 0; i < newBones.Length; i++)
                {
                    Transform currentBone = (Transform) boneNameMap[child.bones[i].name];

                    newBones[i] = currentBone;
                    bindPosesToMerge.Add(currentBone.worldToLocalMatrix * child.transform.worldToLocalMatrix);
                }
                Array.Copy(newBones, 0, bones, boneOffset, child.bones.Length);

                boneOffset = bones.Length;

                Matrix4x4 transformMatrix = child.localToWorldMatrix;

                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = mesh;
                combineInstance.transform = transformMatrix;
                combineInstances.Add(combineInstance);
            }

            SkinnedMeshRenderer renderer = combinedSkinnedMesh.AddComponent<SkinnedMeshRenderer>();
            renderer.bones = bones;
            renderer.updateWhenOffscreen = true;
            Mesh newMesh = new Mesh();
            newMesh.CombineMeshes(combineInstances.ToArray(), true, true);
            newMesh.RecalculateBounds();
            newMesh.name = combinedModel.name;
            renderer.rootBone = combinedModel.transform.Find("root");
            renderer.sharedMesh = newMesh;
            renderer.enabled = true;
            renderer.sharedMesh.bindposes = bindPosesToMerge.ToArray();
            renderer.sharedMaterial = baseMaterial == null ? material : baseMaterial;
            MergeAndGetAllBlendShapeDataOfSkinnedMeshRenderers(skinnedMeshesToCombine.ToArray(), renderer.sharedMesh, renderer);

            return combinedModel;
        }

        /// <summary>
        ///     Processes the movement of bones if required for the given movement dictionary.
        /// </summary>
        /// <param name="boneNameMap">The bone name map that has all the bones of the rig.</param>
        /// <param name="movementDictionary">The dictionary of bones to process the movement from.</param>
        /// <param name="rotationDictionary">The dictionary of bone rotations to process.</param>
        public static void ProcessBoneMovement(Hashtable boneNameMap, Dictionary<string, Vector3> movementDictionary, Dictionary<string, Quaternion> rotationDictionary)
        {
            Dictionary<string, Vector3> bonePositionDictionary = new Dictionary<string, Vector3>();
            Dictionary<string, Quaternion> boneRotationDictionary = new Dictionary<string, Quaternion>();
            Dictionary<string, Vector3> boneMovementDictionary = new Dictionary<string, Vector3>();
            foreach (Transform currentBone in boneNameMap.Values)
            {
                // Store bone positions from rig before processing joints.
                bonePositionDictionary.TryAdd(currentBone.name, currentBone.transform.localPosition);
                boneRotationDictionary.TryAdd(currentBone.name, currentBone.transform.localRotation);

                if (movementDictionary.ContainsKey(currentBone.name))
                {
                    float jointDistance = Vector3.Distance(bonePositionDictionary[currentBone.name], movementDictionary[currentBone.name]);
                    float rotationDistance = Quaternion.Angle(boneRotationDictionary[currentBone.name], rotationDictionary[currentBone.name]);

                    // If the bone in the new part is at a different location, move the actual bone to the same position.
                    if (jointDistance > 0.0001)
                    {
                        Vector3 rigMovement = movementDictionary[currentBone.name];
                        // If an existing joint movement exists, and is further from the standard joint position, use that instead.
                        if (boneMovementDictionary.TryGetValue(currentBone.name, out Vector3 existingMovement)
                            && Math.Abs(Vector3.Distance(bonePositionDictionary[currentBone.name], existingMovement)) > Math.Abs(jointDistance))
                        {
                            rigMovement = existingMovement;
                        }

                        currentBone.transform.localPosition = rigMovement;
                        boneMovementDictionary[currentBone.name] = rigMovement;
                    }

                    if (rotationDistance > 0.01)
                    {
                        Quaternion rigRotation = rotationDictionary[currentBone.name];
                        if (boneRotationDictionary.TryGetValue(currentBone.name, out Quaternion existingRotation)
                            && Math.Abs(Quaternion.Angle(boneRotationDictionary[currentBone.name], existingRotation)) > Math.Abs(rotationDistance))
                        {
                            rigRotation = existingRotation;
                        }

                        currentBone.transform.localRotation = rigRotation;
                        boneRotationDictionary[currentBone.name] = rigRotation;
                    }
                }
            }
        }

        /// <summary>
        ///     Creates a map between bones and their names.
        /// </summary>
        /// <param name="currentBone">The Current bone being mapped.</param>
        /// <returns>A hashmap between bone names and bones.</returns>
        public static Hashtable CreateBoneNameMap(GameObject currentBone)
        {
            Hashtable boneNameMap = new Hashtable();
            boneNameMap.Add(currentBone.name, currentBone.transform);

            for (int i = 0; i < currentBone.transform.childCount; i++)
            {
                Hashtable childBoneMap = CreateBoneNameMap(currentBone.transform.GetChild(i).gameObject);
                foreach (DictionaryEntry entry in childBoneMap)
                {
                    if (!boneNameMap.ContainsKey(entry.Key))
                    {
                        boneNameMap.Add(entry.Key, (Transform) entry.Value);
                    }
                }
            }
            return boneNameMap;
        }

        /// <summary>
        ///     Finds any bones in a given list of SkinnedMeshRenderers that aren't in the given bone map.
        /// </summary>
        /// <param name="boneMap">The bone map to check for the existence of bones.</param>
        /// <param name="meshes">The list of SkinnedMeshRenderers to check for additional bones.</param>
        /// <returns>An array of all additional bones.</returns>
        public static Transform[] FindAdditionalBones(Hashtable boneMap, List<SkinnedMeshRenderer> meshes)
        {
            List<Transform> newBones = new List<Transform>();
            foreach (SkinnedMeshRenderer mesh in meshes)
            {
                foreach (Transform bone in mesh.bones)
                {
                    if (!boneMap.ContainsKey(bone.name))
                    {
                        newBones.Add(bone);
                    }
                }
            }
            return newBones.ToArray();
        }

        /// <summary>
        ///     Adds additional bones to the current bone array.
        /// </summary>
        /// <param name="bones">The current bone array.</param>
        /// <param name="additionBones">The new bones to add.</param>
        /// <param name="boneMap">The current bone name map.</param>
        /// <returns>The new bone array.</returns>
        public static Transform[] JoinAdditionalBonesToBoneArray(Transform[] bones, Transform[] additionBones, Hashtable boneMap)
        {
            List<Transform> fullBones = new List<Transform>();
            fullBones.AddRange(bones);
            foreach (Transform bone in additionBones)
            {
                Transform newParent = (Transform) boneMap[bone.parent.name];

                if (newParent != null && !newParent.Find(bone.name))
                {
                    GameObject newBone = GameObject.Instantiate(bone.gameObject, newParent);
                    newBone.name = newBone.name.Replace("(Clone)", "");
                    fullBones.Add(newBone.transform);
                    if (!boneMap.ContainsKey(bone.name))
                    {
                        boneMap.Add(bone.name, newBone.transform);
                    }
                }
            }
            return fullBones.ToArray();
        }
    }
}
