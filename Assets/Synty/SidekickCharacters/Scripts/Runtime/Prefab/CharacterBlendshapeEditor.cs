// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.
#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class CharacterBlendshapeEditor : MonoBehaviour
{
    [Range(-1, 1)] public float _bodyType;
    [Range(-1, 1)] public float _bodySize;
    [Range(-1, 1)] public float _musculature;

    // Attachment Back values.
    public Vector3 _abacFeminineMaxPosOffset = new Vector3(0f, -0.01f, 0f);
    public Vector3 _abacFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _abacHeavyMaxPosOffset = new Vector3(0f, 0.08f, 0f);
    public Vector3 _abacHeavyMaxRotOffset = new Vector3(8f, 0f, 0f);
    public Vector3 _abacSkinnyMaxPosOffset = new Vector3(0f, -0.01f, 0f);
    public Vector3 _abacSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _abacBulkMaxPosOffset = new Vector3(-0.04f, 0.04f, 0f);
    public Vector3 _abacBulkMaxRotOffset = new Vector3(356f, 0f, 0f);

    // Attachment Hips Front values.
    public Vector3 _ahpfFeminineMaxPosOffset = new Vector3(0f, -0.005f, 0f);
    public Vector3 _ahpfFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahpfHeavyMaxPosOffset = new Vector3(0.06f, 0.15f, 0f);
    public Vector3 _ahpfHeavyMaxRotOffset = new Vector3(15f, 0f, 0f);
    public Vector3 _ahpfSkinnyMaxPosOffset = new Vector3(0f, -0.007f, 0f);
    public Vector3 _ahpfSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahpfBulkMaxPosOffset = new Vector3(0f, -0.013f, 0f);
    public Vector3 _ahpfBulkMaxRotOffset = new Vector3(2f, 0f, 0f);

    // Attachment Hips Back values.
    public Vector3 _ahpbFeminineMaxPosOffset = new Vector3(0f, -0.015f, 0f);
    public Vector3 _ahpbFeminineMaxRotOffset = new Vector3(3f, 0f, 0f);
    public Vector3 _ahpbHeavyMaxPosOffset = new Vector3(0.005f, -0.14f, 0f);
    public Vector3 _ahpbHeavyMaxRotOffset = new Vector3(352f, 0f, 0f);
    public Vector3 _ahpbSkinnyMaxPosOffset = new Vector3(0f, 0.005f, 0f);
    public Vector3 _ahpbSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahpbBulkMaxPosOffset = new Vector3(-0.01f, -0.02f, 0f);
    public Vector3 _ahpbBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Hips Left values.
    public Vector3 _ahplFeminineMaxPosOffset = new Vector3(0f, 0f, 0.005f);
    public Vector3 _ahplFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahplHeavyMaxPosOffset = new Vector3(0f, 0f, 0.13f);
    public Vector3 _ahplHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahplSkinnyMaxPosOffset = new Vector3(0f, 0f, -0.01f);
    public Vector3 _ahplSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahplBulkMaxPosOffset = new Vector3(0f, 0f, 0.015f);
    public Vector3 _ahplBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Hips Right values.
    public Vector3 _ahprFeminineMaxPosOffset = new Vector3(0f, 0f, -0.005f);
    public Vector3 _ahprFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahprHeavyMaxPosOffset = new Vector3(0f, 0f, -0.13f);
    public Vector3 _ahprHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahprSkinnyMaxPosOffset = new Vector3(0f, 0f, 0.008f);
    public Vector3 _ahprSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ahprBulkMaxPosOffset = new Vector3(0f, 0f, -0.01f);
    public Vector3 _ahprBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Shoulder Left values.
    public Vector3 _ashlFeminineMaxPosOffset = new Vector3(0.01f, 0f, -0.015f);
    public Vector3 _ashlFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashlHeavyMaxPosOffset = new Vector3(-0.04f, 0.01f, 0.028f);
    public Vector3 _ashlHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashlSkinnyMaxPosOffset = new Vector3(0.015f, 0f, -0.01f);
    public Vector3 _ashlSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashlBulkMaxPosOffset = new Vector3(-0.03f, 0f, 0.05f);
    public Vector3 _ashlBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Shoulder Right values.
    public Vector3 _ashrFeminineMaxPosOffset = new Vector3(0.01f, 0f, -0.015f);
    public Vector3 _ashrFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashrHeavyMaxPosOffset = new Vector3(-0.04f, -0.01f, 0.028f);
    public Vector3 _ashrHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashrSkinnyMaxPosOffset = new Vector3(0.015f, 0f, -0.01f);
    public Vector3 _ashrSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _ashrBulkMaxPosOffset = new Vector3(-0.03f, 0f, 0.05f);
    public Vector3 _ashrBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Elbow Left values.
    public Vector3 _aeblFeminineMaxPosOffset = new Vector3(0f, -0.01f, 0f);
    public Vector3 _aeblFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aeblHeavyMaxPosOffset = new Vector3(0f, 0.025f, 0f);
    public Vector3 _aeblHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aeblSkinnyMaxPosOffset = new Vector3(0f, -0.015f, 0.01f);
    public Vector3 _aeblSkinnyMaxRotOffset = new Vector3(0f, 2f, 0f);
    public Vector3 _aeblBulkMaxPosOffset = new Vector3(0f, 0.06f, 0.02f);
    public Vector3 _aeblBulkMaxRotOffset = new Vector3(13f, 0f, 0f);

    // Attachment Elbow Right values.
    public Vector3 _aebrFeminineMaxPosOffset = new Vector3(0f, -0.01f, 0f);
    public Vector3 _aebrFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aebrHeavyMaxPosOffset = new Vector3(0f, 0.035f, 0f);
    public Vector3 _aebrHeavyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aebrSkinnyMaxPosOffset = new Vector3(0f, -0.015f, 0.01f);
    public Vector3 _aebrSkinnyMaxRotOffset = new Vector3(0f, 358f, 0f);
    public Vector3 _aebrBulkMaxPosOffset = new Vector3(0f, 0.06f, 0.02f);
    public Vector3 _aebrBulkMaxRotOffset = new Vector3(13f, 0f, 0f);

    // Attachment Knee Left values.
    public Vector3 _aknlFeminineMaxPosOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknlFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknlHeavyMaxPosOffset = new Vector3(0f, -0.015f, 0f);
    public Vector3 _aknlHeavyMaxRotOffset = new Vector3(0f, 20f, 0f);
    public Vector3 _aknlSkinnyMaxPosOffset = new Vector3(0f, 0.01f, 0f);
    public Vector3 _aknlSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknlBulkMaxPosOffset = new Vector3(0f, -0.002f, 0f);
    public Vector3 _aknlBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    // Attachment Knee Right values.
    public Vector3 _aknrFeminineMaxPosOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknrFeminineMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknrHeavyMaxPosOffset = new Vector3(0f, 0.015f, 0f);
    public Vector3 _aknrHeavyMaxRotOffset = new Vector3(0f, 0f, 15f);
    public Vector3 _aknrSkinnyMaxPosOffset = new Vector3(0f, -0.01f, 0f);
    public Vector3 _aknrSkinnyMaxRotOffset = new Vector3(0f, 0f, 0f);
    public Vector3 _aknrBulkMaxPosOffset = new Vector3(0f, 0.002f, 0f);
    public Vector3 _aknrBulkMaxRotOffset = new Vector3(0f, 0f, 0f);

    private List<SkinnedMeshRenderer> _meshRenderers = new List<SkinnedMeshRenderer>();
    private Dictionary<SkinnedMeshRenderer, Dictionary<string, int>> _blendshapeIndices;

    private readonly Vector3 _minValues = Vector3.zero;
    private readonly Vector3 _maxValues = new Vector3(359.9999f, 359.9999f, 359.9999f);

    private Dictionary<string, Vector3> _blendShapeRigMovement;
    private Dictionary<string, Quaternion> _blendShapeRigRotation;
    private Hashtable _boneNameMap;

    private static readonly Dictionary<AttachmentType, string> _PART_TYPE_JOINT_MAP = new Dictionary<AttachmentType, string>
    {
        [AttachmentType.AttachmentBack] = "backAttach",
        [AttachmentType.AttachmentHipsFront] = "hipAttachFront",
        [AttachmentType.AttachmentHipsBack] = "hipAttachBack",
        [AttachmentType.AttachmentHipsLeft] = "hipAttach_l",
        [AttachmentType.AttachmentHipsRight] = "hipAttach_r",
        [AttachmentType.AttachmentShoulderLeft] = "shoulderAttach_l",
        [AttachmentType.AttachmentShoulderRight] = "shoulderAttach_r",
        [AttachmentType.AttachmentElbowLeft] = "elbowAttach_l",
        [AttachmentType.AttachmentElbowRight] = "elbowAttach_r",
        [AttachmentType.AttachmentKneeLeft] = "kneeAttach_l",
        [AttachmentType.AttachmentKneeRight] = "kneeAttach_r"
    };

    // Initial positions of attachment joints, used for applying offsets.
    private static readonly Dictionary<string, Vector3> _JOINT_BASE_POSITION_MAP = new Dictionary<string, Vector3>
    {
        ["hipAttach_l"] = new Vector3(-0.06f, -0.02f, 0.16f),
        ["hipAttach_r"] = new Vector3(-0.06f, -0.02f, -0.16f),
        ["hipAttachBack"] = new Vector3(-0.08f, -0.10f, 0.00f),
        ["hipAttachFront"] = new Vector3(-0.02f, 0.14f, 0.00f),
        ["backAttach"] = new Vector3(-0.19f, 0.14f, 0.00f),
        ["shoulderAttach_l"] = new Vector3(-0.17f, 0.01f, 0.08f),
        ["elbowAttach_l"] = new Vector3(0.01f, 0.03f, -0.01f),
        ["shoulderAttach_r"] = new Vector3(-0.17f, -0.01f, 0.08f),
        ["elbowAttach_r"] = new Vector3(-0.01f, 0.03f, -0.01f),
        ["kneeAttach_l"] = new Vector3(-0.01f, -0.05f, -0.01f),
        ["kneeAttach_r"] = new Vector3(0.01f, 0.05f, 0.01f)
    };

    // Initial rotations of attachment joints, used for applying offsets.
    private static readonly Dictionary<string, Quaternion> _JOINT_BASE_ROTATION_MAP = new Dictionary<string, Quaternion>
    {
        ["hipAttach_l"] = new Quaternion(-0.50000f, 0.50000f, 0.50000f, 0.50000f),
        ["hipAttach_r"] = new Quaternion(-0.50000f, 0.50000f, 0.50000f, 0.50000f),
        ["hipAttachBack"] = new Quaternion(-0.50000f, 0.50000f, 0.50000f, 0.50000f),
        ["hipAttachFront"] = new Quaternion(-0.50000f, 0.50000f, 0.50000f, 0.50000f),
        ["backAttach"] = new Quaternion(0.49823f, -0.50177f, 0.49822f, 0.50176f),
        ["shoulderAttach_l"] = new Quaternion(0.69873f, -0.00835f, 0.01867f, 0.71509f),
        ["elbowAttach_l"] = new Quaternion(0.70711f, 0.00230f, -0.02266f, 0.70674f),
        ["shoulderAttach_r"] = new Quaternion(0.71507f, -0.01867f, 0.00835f, 0.69875f),
        ["elbowAttach_r"] = new Quaternion(0.70711f, -0.00230f, 0.02266f, 0.70674f),
        ["kneeAttach_l"] = new Quaternion(0.70616f, -0.03582f, -0.02210f, 0.70680f),
        ["kneeAttach_r"] = new Quaternion(-0.00045f, -0.00970f, -0.04096f, 0.99911f)
    };

    /// <inheritdoc cref="OnEnable"/>
    private void OnEnable()
    {
        UpdateMeshReference();
        CacheBlendshapeIndices();
    }

    /// <inheritdoc cref="OnValidate"/>
    private void OnValidate()
    {
        _abacFeminineMaxRotOffset = MinMaxVector3(_abacFeminineMaxRotOffset);
        _abacHeavyMaxRotOffset = MinMaxVector3(_abacHeavyMaxRotOffset);
        _abacSkinnyMaxRotOffset = MinMaxVector3(_abacSkinnyMaxRotOffset);
        _abacBulkMaxRotOffset = MinMaxVector3(_abacBulkMaxRotOffset);
        _ahpfFeminineMaxRotOffset = MinMaxVector3(_ahpfFeminineMaxRotOffset);
        _ahpfHeavyMaxRotOffset = MinMaxVector3(_ahpfHeavyMaxRotOffset);
        _ahpfSkinnyMaxRotOffset = MinMaxVector3(_ahpfSkinnyMaxRotOffset);
        _ahpfBulkMaxRotOffset = MinMaxVector3(_ahpfBulkMaxRotOffset);
        _ahpbFeminineMaxRotOffset = MinMaxVector3(_ahpbFeminineMaxRotOffset);
        _ahpbHeavyMaxRotOffset = MinMaxVector3(_ahpbHeavyMaxRotOffset);
        _ahpbSkinnyMaxRotOffset = MinMaxVector3(_ahpbSkinnyMaxRotOffset);
        _ahpbBulkMaxRotOffset = MinMaxVector3(_ahpbBulkMaxRotOffset);
        _ahplFeminineMaxRotOffset = MinMaxVector3(_ahplFeminineMaxRotOffset);
        _ahplHeavyMaxRotOffset = MinMaxVector3(_ahplHeavyMaxRotOffset);
        _ahplSkinnyMaxRotOffset = MinMaxVector3(_ahplSkinnyMaxRotOffset);
        _ahplBulkMaxRotOffset = MinMaxVector3(_ahplBulkMaxRotOffset);
        _ahprFeminineMaxRotOffset = MinMaxVector3(_ahprFeminineMaxRotOffset);
        _ahprHeavyMaxRotOffset = MinMaxVector3(_ahprHeavyMaxRotOffset);
        _ahprSkinnyMaxRotOffset = MinMaxVector3(_ahprSkinnyMaxRotOffset);
        _ahprBulkMaxRotOffset = MinMaxVector3(_ahprBulkMaxRotOffset);
        _ashlFeminineMaxRotOffset = MinMaxVector3(_ashlFeminineMaxRotOffset);
        _ashlHeavyMaxRotOffset = MinMaxVector3(_ashlHeavyMaxRotOffset);
        _ashlSkinnyMaxRotOffset = MinMaxVector3(_ashlSkinnyMaxRotOffset);
        _ashlBulkMaxRotOffset = MinMaxVector3(_ashlBulkMaxRotOffset);
        _ashrFeminineMaxRotOffset = MinMaxVector3(_ashrFeminineMaxRotOffset);
        _ashrHeavyMaxRotOffset = MinMaxVector3(_ashrHeavyMaxRotOffset);
        _ashrSkinnyMaxRotOffset = MinMaxVector3(_ashrSkinnyMaxRotOffset);
        _ashrBulkMaxRotOffset = MinMaxVector3(_ashrBulkMaxRotOffset);
        _aeblFeminineMaxRotOffset = MinMaxVector3(_aeblFeminineMaxRotOffset);
        _aeblHeavyMaxRotOffset = MinMaxVector3(_aeblHeavyMaxRotOffset);
        _aeblSkinnyMaxRotOffset = MinMaxVector3(_aeblSkinnyMaxRotOffset);
        _aeblBulkMaxRotOffset = MinMaxVector3(_aeblBulkMaxRotOffset);
        _aebrFeminineMaxRotOffset = MinMaxVector3(_aebrFeminineMaxRotOffset);
        _aebrHeavyMaxRotOffset = MinMaxVector3(_aebrHeavyMaxRotOffset);
        _aebrSkinnyMaxRotOffset = MinMaxVector3(_aebrSkinnyMaxRotOffset);
        _aebrBulkMaxRotOffset = MinMaxVector3(_aebrBulkMaxRotOffset);
        _aknlFeminineMaxRotOffset = MinMaxVector3(_aknlFeminineMaxRotOffset);
        _aknlHeavyMaxRotOffset = MinMaxVector3(_aknlHeavyMaxRotOffset);
        _aknlSkinnyMaxRotOffset = MinMaxVector3(_aknlSkinnyMaxRotOffset);
        _aknlBulkMaxRotOffset = MinMaxVector3(_aknlBulkMaxRotOffset);
        _aknrFeminineMaxRotOffset = MinMaxVector3(_aknrFeminineMaxRotOffset);
        _aknrHeavyMaxRotOffset = MinMaxVector3(_aknrHeavyMaxRotOffset);
        _aknrSkinnyMaxRotOffset = MinMaxVector3(_aknrSkinnyMaxRotOffset);
        _aknrBulkMaxRotOffset = MinMaxVector3(_aknrBulkMaxRotOffset);

        // Ensure the mesh references are populated before trying to update blend shapes
        if (_meshRenderers == null)
        {
            UpdateMeshReference();
        }

        // Ensure the blend shape indices are populated before trying to update blend shapes
        if (_blendshapeIndices == null)
        {
            CacheBlendshapeIndices();
        }

        // Ensure the bone map is populated before trying to move joints.
        if (_boneNameMap == null)
        {
            _boneNameMap = CreateBoneNameMap(gameObject.GetComponentsInChildren<Transform>().First(t => t.name == "root").gameObject);
        }



        ApplyBlendshapes();
        ProcessRigMovementOnBlendShapeChange();
        ProcessBoneMovement(_boneNameMap, _blendShapeRigMovement, _blendShapeRigRotation);
    }

    /// <summary>
    ///     Updates the reference list of the all skinned meshes on the character.
    /// </summary>
    private void UpdateMeshReference()
    {
        if (_meshRenderers.Count < 1)
        {
            _meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        }

        if (_meshRenderers.Count > 0)
        {
            SkinnedMeshRenderer meshRenderer = _meshRenderers[0];
            _boneNameMap = CreateBoneNameMap(meshRenderer.rootBone.gameObject);
        }
    }

    /// <summary>
    ///     Updates the blend shape indices cache.
    /// </summary>
    private void CacheBlendshapeIndices()
    {
        _blendshapeIndices = new Dictionary<SkinnedMeshRenderer, Dictionary<string, int>>();
        if (_meshRenderers.Count < 1)
        {
            return;
        }

        foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
        {
            if (meshRenderer == null || meshRenderer.sharedMesh == null)
            {
                continue;
            }

            Dictionary<string, int> currentIndices = new Dictionary<string, int>();

            Mesh mesh = meshRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string blendName = mesh.GetBlendShapeName(i);
                currentIndices[blendName] = i;
            }

            _blendshapeIndices[meshRenderer] = currentIndices;
        }
    }

    /// <summary>
    ///     Sets the blend shape value for the given blend shape name.
    /// </summary>
    /// <param name="blendName">The name for the blend shape to be updated.</param>
    /// <param name="value">The new value to send for the given blend shape.</param>
    private void SetBlendValue(string blendName, float value)
    {
        foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
        {
            if (_blendshapeIndices.TryGetValue(meshRenderer, out Dictionary<string, int> currentIndices))
            {
                if (currentIndices.TryGetValue(blendName, out int index))
                {
                    meshRenderer.SetBlendShapeWeight(index, Mathf.Clamp01(value) * 100f);
                }
            }
        }
    }

    /// <summary>
    ///     Applies changes to the blend shapes from the changes on the UI sliders.
    /// </summary>
    private void ApplyBlendshapes()
    {
        foreach (SkinnedMeshRenderer meshRenderer in _meshRenderers)
        {
            if (meshRenderer == null)
            {
                continue;
            }

            if (_blendshapeIndices.TryGetValue(meshRenderer, out Dictionary<string, int> currentIndices))
            {
                foreach (KeyValuePair<string, int> pair in currentIndices)
                {
                    string blendName = pair.Key;
                    float weight = 0f;

                    if (blendName.EndsWith("masculineFeminine"))
                    {
                        weight = _bodyType;
                    }
                    else if (blendName.EndsWith("defaultSkinny"))
                    {
                        weight = _bodySize < 0 ? -_bodySize : 0;
                    }
                    else if (blendName.EndsWith("defaultHeavy"))
                    {
                        weight = _bodySize > 0 ? _bodySize : 0;
                    }
                    else if (blendName.EndsWith("Buff"))
                    {
                        weight = (_musculature + 1) / 2;
                    }

                    SetBlendValue(blendName, weight);
                }
            }
        }
    }

    /// <summary>
    ///     Ensures that the values in the given Vector3 are within the desired min-max range.
    /// </summary>
    /// <param name="value">The Vector3 to clamp to the values.</param>
    /// <returns>The new Vector3 with clamped values.</returns>
    private Vector3 MinMaxVector3(Vector3 value)
    {
        Vector3 temp = Vector3.Max(value, _minValues);
        Vector3 newValue = Vector3.Min(temp, _maxValues);
        return newValue;
    }

    /// <summary>
    ///     Creates a map between bones and their names.
    /// </summary>
    /// <param name="currentBone">The Current bone being mapped.</param>
    /// <returns>A hashmap between bone names and bones.</returns>
    private Hashtable CreateBoneNameMap(GameObject currentBone)
    {
        Hashtable boneNameMap = new Hashtable
        {
            {
                currentBone.name, currentBone.transform
            }
        };

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
    ///     Processes the movement of rig joints based on blend shape changes.
    /// </summary>
    private void ProcessRigMovementOnBlendShapeChange()
    {
        _blendShapeRigMovement = new Dictionary<string, Vector3>();
        _blendShapeRigRotation = new Dictionary<string, Quaternion>();

        foreach (KeyValuePair<AttachmentType, string> entry in _PART_TYPE_JOINT_MAP)
        {
            if (!_boneNameMap.ContainsKey(entry.Value))
            {
                continue;
            }

            Transform bone = (Transform) _boneNameMap[entry.Value];

            float feminineBlendValue = (_bodyType + 1) / 2;
            float heavyBlendValue = _bodySize > 0 ? _bodySize : 0;
            float skinnyBlendValue = _bodySize < 0 ? -_bodySize : 0;
            float bulkBlendValue = (_musculature + 1) / 2;

            Vector3 allMovement = _JOINT_BASE_POSITION_MAP[bone.name]; //bone.localPosition;
            Quaternion allRotation = _JOINT_BASE_ROTATION_MAP[bone.name]; //bone.localRotation;

            foreach (BlendShapeType blendType in Enum.GetValues(typeof(BlendShapeType)))
            {
                switch (blendType)
                {
                    case BlendShapeType.Feminine:
                        allMovement += GetBlendedFeminineOffsetValue(entry.Key, feminineBlendValue);
                        allRotation *= GetBlendedFeminineRotationValue(entry.Key, feminineBlendValue);
                        break;
                    case BlendShapeType.Heavy:
                        allMovement += GetBlendedHeavyOffsetValue(entry.Key, heavyBlendValue);
                        allRotation *= GetBlendedHeavyRotationValue(entry.Key, heavyBlendValue);
                        break;
                    case BlendShapeType.Skinny:
                        allMovement += GetBlendedSkinnyOffsetValue(entry.Key, skinnyBlendValue);
                        allRotation *= GetBlendedSkinnyRotationValue(entry.Key, skinnyBlendValue);
                        break;
                    case BlendShapeType.Bulk:
                        allMovement += GetBlendedBulkOffsetValue(entry.Key, bulkBlendValue);
                        allRotation *= GetBlendedBulkRotationValue(entry.Key, bulkBlendValue);
                        break;
                }
            }

            _blendShapeRigMovement[entry.Value] = allMovement;
            _blendShapeRigRotation[entry.Value] = allRotation;
        }
    }

    /// <summary>
    ///     Processes the movement of bones if required for the given movement dictionary.
    /// </summary>
    /// <param name="boneNameMap">The bone name map that has all the bones of the rig.</param>
    /// <param name="movementDictionary">The dictionary of bones to process the movement from.</param>
    /// <param name="rotationDictionary">The dictionary of bone rotations to process.</param>
    private static void ProcessBoneMovement(Hashtable boneNameMap, Dictionary<string, Vector3> movementDictionary, Dictionary<string, Quaternion>
        rotationDictionary)
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
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Vector3 GetBlendedFeminineOffsetValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Vector3.Lerp(Vector3.zero, _abacFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Vector3.Lerp(Vector3.zero, _ahpfFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Vector3.Lerp(Vector3.zero, _ahpbFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Vector3.Lerp(Vector3.zero, _ahplFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Vector3.Lerp(Vector3.zero, _ahprFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Vector3.Lerp(Vector3.zero, _ashlFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Vector3.Lerp(Vector3.zero, _ashrFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Vector3.Lerp(Vector3.zero, _aeblFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Vector3.Lerp(Vector3.zero, _aebrFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Vector3.Lerp(Vector3.zero, _aknlFeminineMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Vector3.Lerp(Vector3.zero, _aknrFeminineMaxPosOffset, blendValue);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Vector3 GetBlendedHeavyOffsetValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Vector3.Lerp(Vector3.zero, _abacHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Vector3.Lerp(Vector3.zero, _ahpfHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Vector3.Lerp(Vector3.zero, _ahpbHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Vector3.Lerp(Vector3.zero, _ahplHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Vector3.Lerp(Vector3.zero, _ahprHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Vector3.Lerp(Vector3.zero, _ashlHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Vector3.Lerp(Vector3.zero, _ashrHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Vector3.Lerp(Vector3.zero, _aeblHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Vector3.Lerp(Vector3.zero, _aebrHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Vector3.Lerp(Vector3.zero, _aknlHeavyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Vector3.Lerp(Vector3.zero, _aknrHeavyMaxPosOffset, blendValue);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Vector3 GetBlendedSkinnyOffsetValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Vector3.Lerp(Vector3.zero, _abacSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Vector3.Lerp(Vector3.zero, _ahpfSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Vector3.Lerp(Vector3.zero, _ahpbSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Vector3.Lerp(Vector3.zero, _ahplSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Vector3.Lerp(Vector3.zero, _ahprSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Vector3.Lerp(Vector3.zero, _ashlSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Vector3.Lerp(Vector3.zero, _ashrSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Vector3.Lerp(Vector3.zero, _aeblSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Vector3.Lerp(Vector3.zero, _aebrSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Vector3.Lerp(Vector3.zero, _aknlSkinnyMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Vector3.Lerp(Vector3.zero, _aknrSkinnyMaxPosOffset, blendValue);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Vector3 GetBlendedBulkOffsetValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Vector3.Lerp(Vector3.zero, _abacBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Vector3.Lerp(Vector3.zero, _ahpfBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Vector3.Lerp(Vector3.zero, _ahpbBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Vector3.Lerp(Vector3.zero, _ahplBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Vector3.Lerp(Vector3.zero, _ahprBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Vector3.Lerp(Vector3.zero, _ashlBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Vector3.Lerp(Vector3.zero, _ashrBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Vector3.Lerp(Vector3.zero, _aeblBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Vector3.Lerp(Vector3.zero, _aebrBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Vector3.Lerp(Vector3.zero, _aknlBulkMaxPosOffset, blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Vector3.Lerp(Vector3.zero, _aknrBulkMaxPosOffset, blendValue);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Quaternion GetBlendedFeminineRotationValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_abacFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpfFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpbFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahplFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahprFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashlFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashrFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aeblFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aebrFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknlFeminineMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknrFeminineMaxPosOffset), blendValue);
            default:
                return Quaternion.Euler(Vector3.zero);
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Quaternion GetBlendedHeavyRotationValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_abacHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpfHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpbHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahplHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahprHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashlHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashrHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aeblHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aebrHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknlHeavyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknrHeavyMaxPosOffset), blendValue);
            default:
                return Quaternion.Euler(Vector3.zero);
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Quaternion GetBlendedSkinnyRotationValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_abacSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpfSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpbSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahplSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahprSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashlSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashrSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aeblSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aebrSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknlSkinnyMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknrSkinnyMaxPosOffset), blendValue);
            default:
                return Quaternion.Euler(Vector3.zero);
        }
    }

    /// <summary>
    ///     Gets the blended offset value for the given attachment type and blend value.
    /// </summary>
    /// <param name="attachmentType">The attachment type to get the blended offset value for.</param>
    /// <param name="blendValue">The blend value to use.</param>
    /// <returns>The blended offset value.</returns>
    private Quaternion GetBlendedBulkRotationValue(AttachmentType attachmentType, float blendValue)
    {
        switch (attachmentType)
        {
            case AttachmentType.AttachmentBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_abacBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsFront:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpfBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsBack:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahpbBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahplBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentHipsRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ahprBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashlBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentShoulderRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_ashrBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aeblBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentElbowRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aebrBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeLeft:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknlBulkMaxPosOffset), blendValue);
            case AttachmentType.AttachmentKneeRight:
                return Quaternion.Lerp(Quaternion.Euler(Vector3.zero), Quaternion.Euler(_aknrBulkMaxPosOffset), blendValue);
            default:
                return Quaternion.Euler(Vector3.zero);
        }
    }

    /// <summary>
    ///     The attachment types to be offset.
    /// </summary>
    private enum AttachmentType
    {
        AttachmentBack,
        AttachmentHipsFront,
        AttachmentHipsBack,
        AttachmentHipsLeft,
        AttachmentHipsRight,
        AttachmentShoulderLeft,
        AttachmentShoulderRight,
        AttachmentElbowLeft,
        AttachmentElbowRight,
        AttachmentKneeLeft,
        AttachmentKneeRight
    }

    /// <summary>
    ///     The blend shape types to adjust.
    /// </summary>
    private enum BlendShapeType
    {
        Feminine,
        Heavy,
        Skinny,
        Bulk
    }
}

/// <summary>
///     Custom UI inspector for the script.
/// </summary>
[CustomEditor(typeof(CharacterBlendshapeEditor))]
public class MyCustomInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        Slider bodyTypeSlider = new Slider
        {
            bindingPath = "_bodyType",
            label = "Body Type",
            lowValue = -1,
            highValue = 1,
            showInputField = true
        };

        VisualElement bodyTypeLabelContainer = new VisualElement
        {
            style =
            {
                flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                width = Length.Percent(100),
                height = 20
            }
        };

        Label masculineLabel = new Label
        {
            text = "Masculine",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                left = 155
            }
        };

        Label feminineLabel = new Label
        {
            text = "Feminine",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                right = 50
            }
        };

        bodyTypeLabelContainer.Add(masculineLabel);
        bodyTypeLabelContainer.Add(feminineLabel);

        Slider bodySizeSlider = new Slider
        {
            bindingPath = "_bodySize",
            label = "Body Size",
            lowValue = -1,
            highValue = 1,
            showInputField = true
        };

        VisualElement bodySizeLabelContainer = new VisualElement
        {
            style =
            {
                flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                width = Length.Percent(100),
                height = 20
            }
        };

        Label skinnyLabel = new Label
        {
            text = "Skinny",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                left = 155
            }
        };

        Label heavyLabel = new Label
        {
            text = "Heavy",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                right = 50
            }
        };

        bodySizeLabelContainer.Add(skinnyLabel);
        bodySizeLabelContainer.Add(heavyLabel);

        Slider musculatureSlider = new Slider
        {
            bindingPath = "_musculature",
            label = "Musculature",
            lowValue = -1,
            highValue = 1,
            showInputField = true
        };

        VisualElement musculatureLabelContainer = new VisualElement
        {
            style =
            {
                flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                width = Length.Percent(100),
                height = 20
            }
        };

        Label leanLabel = new Label
        {
            text = "Lean",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                left = 155
            }
        };

        Label muscularLabel = new Label
        {
            text = "Muscular",
            style =
            {
                position = new StyleEnum<Position>(Position.Absolute),
                right = 50
            }
        };

        musculatureLabelContainer.Add(leanLabel);
        musculatureLabelContainer.Add(muscularLabel);

        Foldout offsetContainer = new Foldout
        {
            text = "Attachment Offsets",
            value = false,
            style =
            {
                marginTop = 10
            }
        };

        #region abacControls
        Foldout abacContainer = new Foldout
        {
            text = "Attachment Back",
            value = false
        };

        Vector3Field abacFemOffsetPos = new Vector3Field
        {
            bindingPath = "_abacFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field abacFemOffsetRot = new Vector3Field
        {
            bindingPath = "_abacFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field abacHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_abacHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field abacHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_abacHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field abacSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_abacSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field abacSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_abacSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field abacBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_abacBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field abacBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_abacBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        abacContainer.Add(abacFemOffsetPos);
        abacContainer.Add(abacFemOffsetRot);
        abacContainer.Add(abacHeavyOffsetPos);
        abacContainer.Add(abacHeavyOffsetRot);
        abacContainer.Add(abacSkinnyOffsetPos);
        abacContainer.Add(abacSkinnyOffsetRot);
        abacContainer.Add(abacBulkOffsetPos);
        abacContainer.Add(abacBulkOffsetRot);
        offsetContainer.Add(abacContainer);
        #endregion

        #region ahpfControls
        Foldout ahpfContainer = new Foldout
        {
            text = "Attachment Hips Front",
            value = false
        };

        Vector3Field ahpfFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpfFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ahpfFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpfFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ahpfHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpfHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ahpfHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpfHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ahpfSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpfSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ahpfSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpfSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ahpfBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpfBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ahpfBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpfBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ahpfContainer.Add(ahpfFemOffsetPos);
        ahpfContainer.Add(ahpfFemOffsetRot);
        ahpfContainer.Add(ahpfHeavyOffsetPos);
        ahpfContainer.Add(ahpfHeavyOffsetRot);
        ahpfContainer.Add(ahpfSkinnyOffsetPos);
        ahpfContainer.Add(ahpfSkinnyOffsetRot);
        ahpfContainer.Add(ahpfBulkOffsetPos);
        ahpfContainer.Add(ahpfBulkOffsetRot);
        offsetContainer.Add(ahpfContainer);
        #endregion

        #region ahpbControls
        Foldout ahpbContainer = new Foldout
        {
            text = "Attachment Hips Back",
            value = false
        };

        Vector3Field ahpbFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpbFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ahpbFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpbFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ahpbHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpbHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ahpbHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpbHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ahpbSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpbSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ahpbSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpbSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ahpbBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ahpbBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ahpbBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ahpbBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ahpbContainer.Add(ahpbFemOffsetPos);
        ahpbContainer.Add(ahpbFemOffsetRot);
        ahpbContainer.Add(ahpbHeavyOffsetPos);
        ahpbContainer.Add(ahpbHeavyOffsetRot);
        ahpbContainer.Add(ahpbSkinnyOffsetPos);
        ahpbContainer.Add(ahpbSkinnyOffsetRot);
        ahpbContainer.Add(ahpbBulkOffsetPos);
        ahpbContainer.Add(ahpbBulkOffsetRot);
        offsetContainer.Add(ahpbContainer);
        #endregion

        #region ahplControls
        Foldout ahplContainer = new Foldout
        {
            text = "Attachment Hips Left",
            value = false
        };

        Vector3Field ahplFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ahplFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ahplFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ahplFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ahplHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahplHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ahplHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahplHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ahplSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahplSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ahplSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahplSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ahplBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ahplBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ahplBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ahplBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ahplContainer.Add(ahplFemOffsetPos);
        ahplContainer.Add(ahplFemOffsetRot);
        ahplContainer.Add(ahplHeavyOffsetPos);
        ahplContainer.Add(ahplHeavyOffsetRot);
        ahplContainer.Add(ahplSkinnyOffsetPos);
        ahplContainer.Add(ahplSkinnyOffsetRot);
        ahplContainer.Add(ahplBulkOffsetPos);
        ahplContainer.Add(ahplBulkOffsetRot);
        offsetContainer.Add(ahplContainer);
        #endregion

        #region ahprControls
        Foldout ahprContainer = new Foldout
        {
            text = "Attachment Hips Right",
            value = false
        };

        Vector3Field ahprFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ahprFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ahprFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ahprFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ahprHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahprHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ahprHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahprHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ahprSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ahprSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ahprSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ahprSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ahprBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ahprBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ahprBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ahprBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ahprContainer.Add(ahprFemOffsetPos);
        ahprContainer.Add(ahprFemOffsetRot);
        ahprContainer.Add(ahprHeavyOffsetPos);
        ahprContainer.Add(ahprHeavyOffsetRot);
        ahprContainer.Add(ahprSkinnyOffsetPos);
        ahprContainer.Add(ahprSkinnyOffsetRot);
        ahprContainer.Add(ahprBulkOffsetPos);
        ahprContainer.Add(ahprBulkOffsetRot);
        offsetContainer.Add(ahprContainer);
        #endregion

        #region ashlControls
        Foldout ashlContainer = new Foldout
        {
            text = "Attachment Shoulder Left",
            value = false
        };

        Vector3Field ashlFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ashlFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ashlFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ashlFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ashlHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ashlHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ashlHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ashlHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ashlSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ashlSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ashlSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ashlSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ashlBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ashlBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ashlBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ashlBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ashlContainer.Add(ashlFemOffsetPos);
        ashlContainer.Add(ashlFemOffsetRot);
        ashlContainer.Add(ashlHeavyOffsetPos);
        ashlContainer.Add(ashlHeavyOffsetRot);
        ashlContainer.Add(ashlSkinnyOffsetPos);
        ashlContainer.Add(ashlSkinnyOffsetRot);
        ashlContainer.Add(ashlBulkOffsetPos);
        ashlContainer.Add(ashlBulkOffsetRot);
        offsetContainer.Add(ashlContainer);
        #endregion

        #region ashrControls
        Foldout ashrContainer = new Foldout
        {
            text = "Attachment Shoulder Right",
            value = false
        };

        Vector3Field ashrFemOffsetPos = new Vector3Field
        {
            bindingPath = "_ashrFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field ashrFemOffsetRot = new Vector3Field
        {
            bindingPath = "_ashrFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field ashrHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_ashrHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field ashrHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_ashrHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field ashrSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_ashrSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field ashrSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_ashrSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field ashrBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_ashrBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field ashrBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_ashrBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        ashrContainer.Add(ashrFemOffsetPos);
        ashrContainer.Add(ashrFemOffsetRot);
        ashrContainer.Add(ashrHeavyOffsetPos);
        ashrContainer.Add(ashrHeavyOffsetRot);
        ashrContainer.Add(ashrSkinnyOffsetPos);
        ashrContainer.Add(ashrSkinnyOffsetRot);
        ashrContainer.Add(ashrBulkOffsetPos);
        ashrContainer.Add(ashrBulkOffsetRot);
        offsetContainer.Add(ashrContainer);
        #endregion

        #region aeblControls
        Foldout aeblContainer = new Foldout
        {
            text = "Attachment Elbow Left",
            value = false
        };

        Vector3Field aeblFemOffsetPos = new Vector3Field
        {
            bindingPath = "_aeblFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field aeblFemOffsetRot = new Vector3Field
        {
            bindingPath = "_aeblFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field aeblHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_aeblHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field aeblHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_aeblHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field aeblSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_aeblSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field aeblSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_aeblSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field aeblBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_aeblBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field aeblBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_aeblBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        aeblContainer.Add(aeblFemOffsetPos);
        aeblContainer.Add(aeblFemOffsetRot);
        aeblContainer.Add(aeblHeavyOffsetPos);
        aeblContainer.Add(aeblHeavyOffsetRot);
        aeblContainer.Add(aeblSkinnyOffsetPos);
        aeblContainer.Add(aeblSkinnyOffsetRot);
        aeblContainer.Add(aeblBulkOffsetPos);
        aeblContainer.Add(aeblBulkOffsetRot);
        offsetContainer.Add(aeblContainer);
        #endregion

        #region aebrControls
        Foldout aebrContainer = new Foldout
        {
            text = "Attachment Elbow Right",
            value = false
        };

        Vector3Field aebrFemOffsetPos = new Vector3Field
        {
            bindingPath = "_aebrFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field aebrFemOffsetRot = new Vector3Field
        {
            bindingPath = "_aebrFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field aebrHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_aebrHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field aebrHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_aebrHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field aebrSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_aebrSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field aebrSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_aebrSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field aebrBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_aebrBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field aebrBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_aebrBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        aebrContainer.Add(aebrFemOffsetPos);
        aebrContainer.Add(aebrFemOffsetRot);
        aebrContainer.Add(aebrHeavyOffsetPos);
        aebrContainer.Add(aebrHeavyOffsetRot);
        aebrContainer.Add(aebrSkinnyOffsetPos);
        aebrContainer.Add(aebrSkinnyOffsetRot);
        aebrContainer.Add(aebrBulkOffsetPos);
        aebrContainer.Add(aebrBulkOffsetRot);
        offsetContainer.Add(aebrContainer);
        #endregion

        #region aknlControls
        Foldout aknlContainer = new Foldout
        {
            text = "Attachment Knee Left",
            value = false
        };

        Vector3Field aknlFemOffsetPos = new Vector3Field
        {
            bindingPath = "_aknlFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field aknlFemOffsetRot = new Vector3Field
        {
            bindingPath = "_aknlFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field aknlHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_aknlHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field aknlHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_aknlHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field aknlSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_aknlSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field aknlSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_aknlSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field aknlBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_aknlBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field aknlBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_aknlBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        aknlContainer.Add(aknlFemOffsetPos);
        aknlContainer.Add(aknlFemOffsetRot);
        aknlContainer.Add(aknlHeavyOffsetPos);
        aknlContainer.Add(aknlHeavyOffsetRot);
        aknlContainer.Add(aknlSkinnyOffsetPos);
        aknlContainer.Add(aknlSkinnyOffsetRot);
        aknlContainer.Add(aknlBulkOffsetPos);
        aknlContainer.Add(aknlBulkOffsetRot);
        offsetContainer.Add(aknlContainer);
        #endregion

        #region aknrControls
        Foldout aknrContainer = new Foldout
        {
            text = "Attachment Knee Right",
            value = false
        };

        Vector3Field aknrFemOffsetPos = new Vector3Field
        {
            bindingPath = "_aknrFeminineMaxPosOffset",
            label = "Feminine Max Position"
        };

        Vector3Field aknrFemOffsetRot = new Vector3Field
        {
            bindingPath = "_aknrFeminineMaxRotOffset",
            label = "Feminine Max Rotation"
        };

        Vector3Field aknrHeavyOffsetPos = new Vector3Field
        {
            bindingPath = "_aknrHeavyMaxPosOffset",
            label = "Heavy Max Position"
        };

        Vector3Field aknrHeavyOffsetRot = new Vector3Field
        {
            bindingPath = "_aknrHeavyMaxRotOffset",
            label = "Heavy Max Rotation"
        };

        Vector3Field aknrSkinnyOffsetPos = new Vector3Field
        {
            bindingPath = "_aknrSkinnyMaxPosOffset",
            label = "Skinny Max Position"
        };

        Vector3Field aknrSkinnyOffsetRot = new Vector3Field
        {
            bindingPath = "_aknrSkinnyMaxRotOffset",
            label = "Skinny Max Rotation"
        };

        Vector3Field aknrBulkOffsetPos = new Vector3Field
        {
            bindingPath = "_aknrBulkMaxPosOffset",
            label = "Bulk Max Position"
        };

        Vector3Field aknrBulkOffsetRot = new Vector3Field
        {
            bindingPath = "_aknrBulkMaxRotOffset",
            label = "Bulk Max Rotation"
        };

        aknrContainer.Add(aknrFemOffsetPos);
        aknrContainer.Add(aknrFemOffsetRot);
        aknrContainer.Add(aknrHeavyOffsetPos);
        aknrContainer.Add(aknrHeavyOffsetRot);
        aknrContainer.Add(aknrSkinnyOffsetPos);
        aknrContainer.Add(aknrSkinnyOffsetRot);
        aknrContainer.Add(aknrBulkOffsetPos);
        aknrContainer.Add(aknrBulkOffsetRot);
        offsetContainer.Add(aknrContainer);
        #endregion

        root.Add(bodyTypeSlider);
        root.Add(bodyTypeLabelContainer);
        root.Add(bodySizeSlider);
        root.Add(bodySizeLabelContainer);
        root.Add(musculatureSlider);
        root.Add(musculatureLabelContainer);
        root.Add(offsetContainer);

        return root;
    }
}
#endif
