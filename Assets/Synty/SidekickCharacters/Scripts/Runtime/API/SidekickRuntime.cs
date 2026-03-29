// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using Synty.SidekickCharacters.Blendshapes;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.SkinnedMesh;
using Synty.SidekickCharacters.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Synty.SidekickCharacters.API
{
    public class SidekickRuntime
    {
        private const string _BLEND_GENDER_NAME = "masculineFeminine";
        private const string _BLEND_MUSCLE_NAME = "defaultBuff";
        private const string _BLEND_SHAPE_HEAVY_NAME = "defaultHeavy";
        private const string _BLEND_SHAPE_SKINNY_NAME = "defaultSkinny";

        private const string _TEXTURE_COLOR_NAME = "ColorMap.png";
        private const string _TEXTURE_METALLIC_NAME = "MetallicMap.png";
        private const string _TEXTURE_SMOOTHNESS_NAME = "SmoothnessMap.png";
        private const string _TEXTURE_REFLECTION_NAME = "ReflectionMap.png";
        private const string _TEXTURE_EMISSION_NAME = "EmissionMap.png";
        private const string _TEXTURE_OPACITY_NAME = "OpacityMap.png";
        private const string _TEXTURE_PREFIX = "T_";

        private static readonly int _COLOR_MAP = Shader.PropertyToID("_ColorMap");
        private static readonly int _METALLIC_MAP = Shader.PropertyToID("_MetallicMap");
        private static readonly int _SMOOTHNESS_MAP = Shader.PropertyToID("_SmoothnessMap");
        private static readonly int _REFLECTION_MAP = Shader.PropertyToID("_ReflectionMap");
        private static readonly int _EMISSION_MAP = Shader.PropertyToID("_EmissionMap");
        private static readonly int _OPACITY_MAP = Shader.PropertyToID("_OpacityMap");

        private DatabaseManager _dbManager;
        private GameObject _baseModel;
        private Material _currentMaterial;
        private RuntimeAnimatorController _currentAnimationController;
        private List<Vector2> _currentUVList;
        private Dictionary<ColorPartType, List<Vector2>> _currentUVDictionary;
        private Dictionary<string, Vector3> _blendShapeRigMovement;
        private Dictionary<string, Quaternion> _blendShapeRigRotation;
        private Dictionary<CharacterPartType, Dictionary<string, string>> _partLibrary;
        private Dictionary<CharacterPartType, List<SidekickPart>> _allPartsLibrary;
        private Dictionary<string, List<string>> _partOutfitMap;
        private Dictionary<string, bool> _partOutfitToggleMap;
        private Dictionary<string, Dictionary<SidekickSpecies, Dictionary<CharacterPartType, List<string>>>> _filterPartDictionary;
        private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> _mappedPartDictionary;
        private Dictionary<CharacterPartType, List<string>> _mappedPartList;
        private Dictionary<SidekickSpecies, Dictionary<CharacterPartType, List<string>>> _mappedBasePartDictionary;
        private Dictionary<string, SidekickSpecies> _speciesDictionary;
        private Dictionary<string, List<SidekickPartPreset>> _mappedPresetFilterDictionary;
        private Dictionary<SidekickSpecies, List<SidekickPartPreset>> _mappedBasePresetDictionary;
        private int _partCount;
        private SidekickSpecies _currentSpecies;

        private float _bodyTypeBlendValue;
        private float _bodySizeSkinnyBlendValue;
        private float _bodySizeHeavyBlendValue;
        private float _musclesBlendValue;

        public DatabaseManager DBManager
        {
            get => _dbManager;
            set => _dbManager = value;
        }

        public GameObject BaseModel
        {
            get => _baseModel;
            set => _baseModel = value;
        }

        public Material CurrentMaterial
        {
            get => _currentMaterial;
            set => _currentMaterial = value;
        }

        public RuntimeAnimatorController CurrentAnimationController
        {
            get => _currentAnimationController;
            set => _currentAnimationController = value;
        }

        public List<Vector2> CurrentUVList
        {
            get => _currentUVList;
            set => _currentUVList = value;
        }

        public Dictionary<ColorPartType, List<Vector2>> CurrentUVDictionary
        {
            get => _currentUVDictionary;
            set => _currentUVDictionary = value;
        }

        public Dictionary<CharacterPartType, Dictionary<string, string>> PartLibrary
        {
            get => _partLibrary;
            set => _partLibrary = value;
        }

        public int PartCount
        {
            get => _partCount;
            private set => _partCount = value;
        }

        public Dictionary<string, List<string>> PartOutfitMap
        {
            get => _partOutfitMap;
            set => _partOutfitMap = value;
        }

        public Dictionary<string, bool> PartOutfitToggleMap
        {
            get => _partOutfitToggleMap;
            set => _partOutfitToggleMap = value;
        }

        public float BodyTypeBlendValue
        {
            get => _bodyTypeBlendValue;
            set => _bodyTypeBlendValue = value;
        }

        public float BodySizeSkinnyBlendValue
        {
            get => _bodySizeSkinnyBlendValue;
            set => _bodySizeSkinnyBlendValue = value;
        }

        public float BodySizeHeavyBlendValue
        {
            get => _bodySizeHeavyBlendValue;
            set => _bodySizeHeavyBlendValue = value;
        }

        public float MusclesBlendValue
        {
            get => _musclesBlendValue;
            set => _musclesBlendValue = value;
        }

        public SidekickSpecies CurrentSpecies
        {
            get => _currentSpecies;
            set => _currentSpecies = value;
        }

        public Dictionary<string, Dictionary<SidekickSpecies, Dictionary<CharacterPartType, List<string>>>> FilterPartDictionary
        {
            get => _filterPartDictionary;
            private set => _filterPartDictionary = value;
        }

        public Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> MappedPartDictionary
        {
            get => _mappedPartDictionary;
            private set => _mappedPartDictionary = value;
        }

        public Dictionary<SidekickSpecies, Dictionary<CharacterPartType, List<string>>> MappedBasePartDictionary
        {
            get => _mappedBasePartDictionary;
            private set => _mappedBasePartDictionary = value;
        }

        public Dictionary<CharacterPartType, List<string>> MappedPartList
        {
            get => _mappedPartList;
            private set => _mappedPartList = value;
        }

        public Dictionary<CharacterPartType, List<SidekickPart>> AllPartsLibrary
        {
            get => _allPartsLibrary;
            private set => _allPartsLibrary = value;
        }

        public Dictionary<string, List<SidekickPartPreset>> MappedPresetFilterDictionary
        {
            get => _mappedPresetFilterDictionary;
            private set => _mappedPresetFilterDictionary = value;
        }

        public Dictionary<SidekickSpecies, List<SidekickPartPreset>> MappedBasePresetDictionary
        {
            get => _mappedBasePresetDictionary;
            private set => _mappedBasePresetDictionary = value;
        }

        /// <summary>
        ///     Creates and instance of the SidekickRuntime with the given parameters.
        /// </summary>
        /// <param name="model">The base donor model to use. This is used to provide a base rig that parts can be added and removed from.</param>
        /// <param name="material">The base material that will be applied to all parts that are added or removed from the character.</param>
        /// <param name="animationController">The animation controller to apply to the created model.</param>
        /// <param name="dbManager">The Database Manager to use, if not provided a new connection will be created.</param>
        public SidekickRuntime(GameObject model, Material material, RuntimeAnimatorController animationController = null, DatabaseManager dbManager = null)
        {
            _dbManager = dbManager ?? new DatabaseManager();

            if (_dbManager.GetCurrentDbConnection() == null)
            {
                _dbManager.GetDbConnection(true);
            }

            _baseModel = model;
            _currentMaterial = material;
            _currentAnimationController = animationController;
        }

        public static async Task PopulateToolData(SidekickRuntime runtime)
        {
            await runtime.LoadPartLibrary();
            await runtime.PopulatePresetLibrary();
        }

        /// <summary>
        ///     Takes all the parts selected in the window, and combines them into a single model in the scene.
        /// </summary>
        /// <param name="modelName">What to call the parent GameObject of the created character.</param>;
        /// <param name="toCombine">The list of SkinnedMeshes to combine to create the character.</param>
        /// <param name="combineMesh">When true the character mesh will be combined into a single mesh.</param>
        /// <param name="processBoneMovement">When true the bones will be moved to match the blend shape settings.</param>
        /// <returns>A new character object.</returns>
        public GameObject CreateCharacter(
            string modelName,
            List<SkinnedMeshRenderer> toCombine,
            bool combineMesh,
            bool processBoneMovement,
            GameObject existingModel = null
        )
        {
            PopulateUVDictionary(toCombine);

            GameObject newSpawn;

            if (combineMesh)
            {
                newSpawn = Combiner.CreateCombinedSkinnedMesh(toCombine, _baseModel, _currentMaterial);
            }
            else
            {
                newSpawn = CreateModelFromParts(toCombine, modelName, existingModel);
            }

            newSpawn.name = modelName;

            Renderer renderer = newSpawn.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _currentMaterial;
            }

            if (newSpawn.GetComponent<Animator>() == null)
            {
                if (existingModel == null)
                {
                    Animator newModelAnimator = newSpawn.AddComponent<Animator>();
                    Animator baseModelAnimator = _baseModel.GetComponentInChildren<Animator>();
                    newModelAnimator.avatar = baseModelAnimator.avatar;
                    newModelAnimator.Rebind();

                    if (_currentAnimationController != null)
                    {
                        newModelAnimator.runtimeAnimatorController = _currentAnimationController;
                    }
                }
                else
                {
                    Animator newModelAnimator = newSpawn.AddComponent<Animator>();
                    Animator baseModelAnimator = existingModel.GetComponentInChildren<Animator>();
                    newModelAnimator.avatar = baseModelAnimator.avatar;
                    newModelAnimator.Rebind();
                }
            }

            UpdateBlendShapes(newSpawn);

            if (processBoneMovement)
            {
                ProcessRigMovementOnBlendShapeChange(SidekickBlendShapeRigMovement.GetAllForProcessing(_dbManager));
                ProcessBoneMovement(newSpawn);
            }

            return newSpawn;
        }

        /// <summary>
        ///     Creates the model but with all parts as separate meshes.
        /// </summary>
        /// <param name="parts">The parts to build into the character.</param>
        /// <param name="outputModelName">What to call the parent GameObject of the created character.</param>
        /// <returns>A new game object with all the part meshes and a single rig.</returns>
        public GameObject CreateModelFromParts(
            List<SkinnedMeshRenderer> parts,
            string outputModelName,
            GameObject existingModel = null
        )
        {

            List<CharacterPartType> allTypes = Enum.GetValues(typeof(CharacterPartType)).Cast<CharacterPartType>().ToList();

            GameObject partsModel = existingModel == null ? new GameObject(outputModelName) : existingModel;

            Transform modelRootBone = _baseModel.GetComponentInChildren<SkinnedMeshRenderer>().rootBone;
            GameObject newRootBone;
            if (existingModel != null)
            {
                GameObject oldRootBone = existingModel.transform.Find("root").gameObject;
#if UNITY_EDITOR
                GameObject.DestroyImmediate(oldRootBone);
#else
                GameObject.Destroy(oldRootBone);
#endif
            }

            newRootBone = Object.Instantiate(modelRootBone.gameObject, partsModel.transform, true);
            newRootBone.name = modelRootBone.name;

            Hashtable boneNameMap = Combiner.CreateBoneNameMap(newRootBone);
            Transform[] bones = new Transform[boneNameMap.Count];
            if (existingModel != null)
            {
                boneNameMap.Values.CopyTo(bones, 0);
            }

            Transform[] additionalBones = Combiner.FindAdditionalBones(boneNameMap, new List<SkinnedMeshRenderer>(parts));
            if (additionalBones.Length > 0)
            {
                Combiner.JoinAdditionalBonesToBoneArray(bones, additionalBones, boneNameMap);
                // Need to redo the name map now that we have updated the bone array.
                boneNameMap = Combiner.CreateBoneNameMap(newRootBone);
            }

            for (int i = 0; i < parts.Count; i++)
            {
                SkinnedMeshRenderer part = parts[i];

                allTypes.Remove(ExtractPartType(part.name));

                if (existingModel != null && partsModel != null)
                {
                    SkinnedMeshRenderer existingPart = partsModel.GetComponentsInChildren<SkinnedMeshRenderer>()
                        .FirstOrDefault(go => go.name.Contains(ExtractPartTypeString(part.name)));

                    if (existingPart != null)
                    {
#if UNITY_EDITOR
                        GameObject.DestroyImmediate(existingPart.gameObject);
#else
                        GameObject.Destroy(existingModel.gameObject);
#endif
                    }
                }

                GameObject newPart = new GameObject(part.name);
                newPart.transform.parent = partsModel.transform;
                SkinnedMeshRenderer renderer = newPart.AddComponent<SkinnedMeshRenderer>();
                renderer.updateWhenOffscreen = true;
                Transform[] oldBones = part.bones;
                Transform[] newBones = new Transform[part.bones.Length];
                for (int j = 0; j < oldBones.Length; j++)
                {
                    newBones[j] = (Transform) boneNameMap[oldBones[j].name];
                }

                renderer.sharedMesh = MeshUtils.CopyMesh(part.sharedMesh);
                renderer.rootBone = (Transform) boneNameMap[part.rootBone.name];

                Combiner.MergeAndGetAllBlendShapeDataOfSkinnedMeshRenderers(
                    new[]
                    {
                        part
                    },
                    renderer.sharedMesh,
                    renderer
                );

                renderer.bones = newBones;
                renderer.sharedMaterial = _currentMaterial;
            }

            foreach (CharacterPartType type in allTypes)
            {
                SkinnedMeshRenderer existingPart = partsModel.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .FirstOrDefault(go => go.name.Contains(CharacterPartTypeUtils.GetPartTypeString(type)));

                if (existingPart != null)
                {
#if UNITY_EDITOR
                    GameObject.DestroyImmediate(existingPart.gameObject);
#else
                    GameObject.Destroy(existingModel.gameObject);
#endif
                }
            }

            return partsModel;
        }

        /// <summary>
        ///     Populates the list of current UVs and UV part dictionary.
        /// </summary>
        public void PopulateUVDictionary(List<SkinnedMeshRenderer> usedParts)
        {
            _currentUVList = new List<Vector2>();
            _currentUVDictionary = new Dictionary<ColorPartType, List<Vector2>>();

            foreach (ColorPartType type in Enum.GetValues(typeof(ColorPartType)))
            {
                _currentUVDictionary.Add(type, new List<Vector2>());
            }

            foreach (SkinnedMeshRenderer skinnedMesh in usedParts)
            {
                ColorPartType type = Enum.Parse<ColorPartType>(ExtractPartType(skinnedMesh.name).ToString());
                List<Vector2> partUVs = _currentUVDictionary[type];
                foreach (Vector2 uv in skinnedMesh.sharedMesh.uv)
                {
                    int scaledU = (int) Math.Floor(uv.x * 16);
                    int scaledV = (int) Math.Floor(uv.y * 16);

                    if (scaledU == 16)
                    {
                        scaledU = 15;
                    }

                    if (scaledV == 16)
                    {
                        scaledV = 15;
                    }

                    Vector2 scaledUV = new Vector2(scaledU, scaledV);
                    // For the global UV list, we don't want any duplicates on a global level
                    if (!_currentUVList.Contains(scaledUV))
                    {
                        _currentUVList.Add(scaledUV);
                    }

                    // For the part specific UV list we may have UVs that are in the global list already, we don't want to exclude these, so check
                    // them separately to the global list
                    if (!partUVs.Contains(scaledUV))
                    {
                        partUVs.Add(scaledUV);
                    }
                }

                _currentUVDictionary[type] = partUVs;
            }
        }

        /// <summary>
        ///     Updates the blend shape values of the combined model.
        /// </summary>
        public void UpdateBlendShapes(GameObject model)
        {
            if (model == null)
            {
                return;
            }

            List<SkinnedMeshRenderer> allMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
            foreach (SkinnedMeshRenderer skinnedMesh in allMeshes)
            {
                Mesh sharedMesh = skinnedMesh.sharedMesh;
                for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                {
                    string blendName = sharedMesh.GetBlendShapeName(i);
                    if (blendName.Contains(_BLEND_GENDER_NAME))
                    {
                        skinnedMesh.SetBlendShapeWeight(i, (_bodyTypeBlendValue + 100) / 2);
                    }
                    else if (blendName.Contains(_BLEND_SHAPE_SKINNY_NAME))
                    {
                        skinnedMesh.SetBlendShapeWeight(i, _bodySizeSkinnyBlendValue);
                    }
                    else if (blendName.Contains(_BLEND_SHAPE_HEAVY_NAME))
                    {
                        skinnedMesh.SetBlendShapeWeight(i, _bodySizeHeavyBlendValue);
                    }
                    else if (blendName.Contains(_BLEND_MUSCLE_NAME))
                    {
                        skinnedMesh.SetBlendShapeWeight(i, (_musclesBlendValue + 100) / 2);
                    }
                }
            }
        }

        /// <summary>
        ///     Populates the internal library of parts based on the files in the project.
        /// </summary>
        public Dictionary<CharacterPartType, Dictionary<string, string>> PopulatePartLibrary()
        {
            _partLibrary = new Dictionary<CharacterPartType, Dictionary<string, string>>();
            _partOutfitMap = new Dictionary<string, List<string>>();
            _partOutfitToggleMap = new Dictionary<string, bool>();
            _partCount = 0;

            List<string> files = Directory.GetFiles("Assets", "SK_*_*_*_*_*.fbx", SearchOption.AllDirectories).ToList();

            foreach (CharacterPartType partType in Enum.GetValues(typeof(CharacterPartType)))
            {
                Dictionary<string, string> partLocationDictionary = new Dictionary<string, string>();

                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string partName = fileInfo.Name;
                    partName = partName.Substring(0, partName.IndexOf(".fbx", StringComparison.Ordinal));
                    CharacterPartType characterPartType = ExtractPartType(partName);
                    if (characterPartType > 0 && characterPartType == partType && !partLocationDictionary.ContainsKey(partName))
                    {
                        partLocationDictionary.Add(partName, file);
                        _partCount++;

                        // TODO: populate with actual outfit data when we have proper information about part outfits
                        string tempPartOutfit = GetOutfitNameFromPartName(partName);
                        List<string> partNameList;
                        if (_partOutfitMap.TryGetValue(tempPartOutfit, out List<string> value))
                        {
                            partNameList = value;
                            partNameList.Add(partName);
                            _partOutfitMap[tempPartOutfit] = partNameList;
                        }
                        else
                        {
                            partNameList = new List<string>();
                            partNameList.Add(partName);
                            _partOutfitMap.Add(tempPartOutfit, partNameList);
                            _partOutfitToggleMap.Add(tempPartOutfit, true);
                        }
                    }
                }

                _partLibrary.Add(partType, partLocationDictionary);
            }

            return _partLibrary;
        }

        /// <summary>
        ///     Populates the internal library of Presets into libraries based on filters and base parts.
        /// </summary>
        public Task PopulatePresetLibrary()
        {
            HashSet<SidekickPartPreset> uniqueList = new HashSet<SidekickPartPreset>();

            _mappedPresetFilterDictionary = new Dictionary<string, List<SidekickPartPreset>>();
            _mappedBasePresetDictionary = new Dictionary<SidekickSpecies, List<SidekickPartPreset>>();

            foreach (SidekickPresetFilter filter in SidekickPresetFilter.GetAll(_dbManager))
            {
                List<SidekickPartPreset> presets = filter.GetAllPresetsForFilter(_dbManager, true, true);
                _mappedPresetFilterDictionary[filter.Term] = presets;
                uniqueList.UnionWith(presets);
            }

            // Check for and add BASE only presets
            List<SidekickPartPreset> allPresets = SidekickPartPreset.GetAll(_dbManager);
            List<SidekickPartPreset> presetsNotInFilters = allPresets.Where(preset => !uniqueList.Contains(preset)).ToList();
            foreach (SidekickPartPreset preset in presetsNotInFilters)
            {
                if (preset.HasOnlyBasePartsAndAllAvailable(_dbManager))
                {
                    if (_mappedBasePresetDictionary.TryGetValue(preset.Species, out List<SidekickPartPreset> mappedPresets))
                    {
                        mappedPresets.Add(preset);
                        _mappedBasePresetDictionary[preset.Species] = mappedPresets;
                    }
                    else
                    {
                        List<SidekickPartPreset> presets = new List<SidekickPartPreset>
                        {
                            preset
                        };

                        _mappedBasePresetDictionary.Add(preset.Species, presets);
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Populates the internal library of parts based on the files in the project.
        /// </summary>
        public Task LoadPartLibrary()
        {
            _allPartsLibrary = new Dictionary<CharacterPartType, List<SidekickPart>>();
            _mappedPartList = new Dictionary<CharacterPartType, List<string>>();
            _mappedPartDictionary = new Dictionary<CharacterPartType, Dictionary<string, SidekickPart>>();
            _mappedBasePartDictionary = new Dictionary<SidekickSpecies, Dictionary<CharacterPartType, List<string>>>();
            _speciesDictionary = new Dictionary<string, SidekickSpecies>();
            _partCount = 0;

            List<string> files = Directory.GetFiles("Assets", "SK_*_*_*_*_*.fbx", SearchOption.AllDirectories).ToList();
            Dictionary<string, string> filesOnDisk = new Dictionary<string, string>();
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                string partName = fileInfo.Name;
                partName = partName.Substring(0, partName.IndexOf(".fbx", StringComparison.Ordinal));
                filesOnDisk.TryAdd(partName, file);
            }

            foreach (CharacterPartType type in Enum.GetValues(typeof(CharacterPartType)))
            {
                _allPartsLibrary[type] = new List<SidekickPart>();
                _mappedPartDictionary[type] = new Dictionary<string, SidekickPart>();
                _mappedPartList[type] = new List<string>();
            }

            SidekickSpecies unrestrictedSpecies = null;

            foreach (SidekickSpecies species in SidekickSpecies.GetAll(_dbManager, false))
            {
                _speciesDictionary[species.Name] = species;
                _mappedBasePartDictionary[species] = new Dictionary<CharacterPartType, List<string>>();

                if (species.Name == "Unrestricted")
                {
                    unrestrictedSpecies = species;
                }
            }

            List<SidekickPart> allParts = SidekickPart.GetAll(_dbManager);

            foreach (SidekickPart part in allParts)
            {
                if (filesOnDisk.ContainsKey(part.Name))
                {
                    _partCount++;

                    part.FileExists = true;

                    List<SidekickPart> parts = _allPartsLibrary.TryGetValue(part.Type, out List<SidekickPart> value)
                        ? value
                        : new List<SidekickPart>();
                    parts.Add(part);
                    _allPartsLibrary[part.Type] = parts;

                    Dictionary<string, SidekickPart> partMap = _mappedPartDictionary[part.Type];
                    partMap[part.Name] = part;
                    _mappedPartDictionary[part.Type] = partMap;

                    List<string> currentList = _mappedPartList[part.Type];
                    currentList.Add(part.Name);
                    _mappedPartList[part.Type] = currentList;

                    if (part.Name.Contains("_BASE_"))
                    {
                        if (!_mappedBasePartDictionary.ContainsKey(part.Species))
                        {
                            continue;
                        }

                        Dictionary<CharacterPartType, List<string>> basePartMap = _mappedBasePartDictionary[part.Species];
                        List<string> partList = basePartMap.TryGetValue(part.Type, out List<string> existingList) ? existingList : new List<string>();
                        partList.Add(part.Name);
                        basePartMap[part.Type] = partList;
                        _mappedBasePartDictionary[part.Species] = basePartMap;

                        if (unrestrictedSpecies != null)
                        {
                            Dictionary<CharacterPartType, List<string>> unrestrictedBasePartMap = _mappedBasePartDictionary[unrestrictedSpecies];
                            List<string> unrestrictedPartList = unrestrictedBasePartMap.TryGetValue(part.Type, out List<string> unrestrictedList) ? unrestrictedList : new List<string>();
                            unrestrictedPartList.Add(part.Name);
                            unrestrictedBasePartMap[part.Type] = unrestrictedPartList;
                            _mappedBasePartDictionary[unrestrictedSpecies] = unrestrictedBasePartMap;
                        }
                    }

                }
                else
                {
                    part.FileExists = false;
                }

            }

            SidekickPart.UpdateAll(_dbManager, allParts);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Gets the "outfit" name from the part name.
        ///     TODO: This will be replaced once parts and outfits have a proper relationship.
        /// </summary>
        /// <param name="partName">The part name to parse the "outfit" name from.</param>
        /// <returns>The "outfit" name.</returns>
        public string GetOutfitNameFromPartName(string partName)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return "None";
            }

            return string.Join('_', partName.Substring(3).Split('_').Take(2));
        }

        /// <summary>
        ///     Determines the part type from the part name. This will work as long as the naming format is correct.
        /// </summary>
        /// <param name="partName">The name of the part.</param>
        /// <returns>The part type.</returns>
        public CharacterPartType ExtractPartType(string partName)
        {
            string partTypeString = ExtractPartTypeString(partName);
            string partIndexString = "0";
            if (partTypeString.Length > 2)
            {
                partIndexString = partTypeString.Substring(0, 2);
            }

            bool valueParsed = int.TryParse(partIndexString, out int index);
            return valueParsed ? (CharacterPartType) index : 0;
        }

        /// <summary>
        ///     Extracts the part type string from the file name.
        /// </summary>
        /// <param name="partName">The name of the part.</param>
        /// <returns>The part type string</returns>
        public string ExtractPartTypeString(string partName)
        {
            return partName.Split('_').Reverse().ElementAt(1);
        }

        /// <summary>
        ///     Processes the movement of rig joints based on blend shape changes.
        /// </summary>
        public void ProcessRigMovementOnBlendShapeChange(Dictionary<CharacterPartType, Dictionary<BlendShapeType, SidekickBlendShapeRigMovement>> offsetLibrary)
        {
            Transform modelRootBone = _baseModel.transform.Find("root");
            Hashtable boneNameMap = Combiner.CreateBoneNameMap(modelRootBone.gameObject);

            _blendShapeRigMovement = new Dictionary<string, Vector3>();
            _blendShapeRigRotation = new Dictionary<string, Quaternion>();

            foreach (KeyValuePair<CharacterPartType, string> entry in BlendshapeJointAdjustment.PART_TYPE_JOINT_MAP)
            {
                Transform bone = (Transform) boneNameMap[entry.Value];

                float feminineBlendValue = (_bodyTypeBlendValue + 100) / 2 / 100;

                Vector3 allMovement = bone.localPosition;
                Quaternion allRotation = bone.localRotation;

                if (offsetLibrary.TryGetValue(entry.Key, out Dictionary<BlendShapeType, SidekickBlendShapeRigMovement> blendOffsetLibrary))
                {
                    foreach (BlendShapeType blendType in Enum.GetValues(typeof(BlendShapeType)))
                    {
                        if (blendOffsetLibrary.TryGetValue(blendType, out SidekickBlendShapeRigMovement rigMovement))
                        {
                            if (rigMovement == null)
                            {
                                continue;
                            }

                            switch (blendType)
                            {
                                case BlendShapeType.Feminine:
                                    allMovement += rigMovement.GetBlendedOffsetValue(feminineBlendValue);
                                    allRotation *= rigMovement.GetBlendedRotationValue(feminineBlendValue);
                                    break;
                                case BlendShapeType.Heavy:
                                    allMovement += rigMovement.GetBlendedOffsetValue(_bodySizeHeavyBlendValue / 100);
                                    allRotation *= rigMovement.GetBlendedRotationValue(_bodySizeHeavyBlendValue / 100);
                                    break;
                                case BlendShapeType.Skinny:
                                    allMovement += rigMovement.GetBlendedOffsetValue(_bodySizeSkinnyBlendValue / 100);
                                    allRotation *= rigMovement.GetBlendedRotationValue(_bodySizeSkinnyBlendValue / 100);
                                    break;
                                case BlendShapeType.Bulk:
                                    allMovement += rigMovement.GetBlendedOffsetValue((_musclesBlendValue + 100) / 2 / 100);
                                    allRotation *= rigMovement.GetBlendedRotationValue((_musclesBlendValue + 100) / 2 / 100);
                                    break;
                            }
                        }
                    }
                }

                _blendShapeRigMovement[entry.Value] = allMovement;
                _blendShapeRigRotation[entry.Value] = allRotation;
            }
        }

        /// <summary>
        ///     Processes the movement of the rig with regards to the current blend shape settings.
        /// </summary>
        /// <param name="model">The model to process the movement on.</param>
        public void ProcessBoneMovement(GameObject model)
        {
            if (model == null)
            {
                return;
            }

            Transform modelRootBone = model.transform.Find("root");
            Hashtable boneNameMap = Combiner.CreateBoneNameMap(modelRootBone.gameObject);
            Combiner.ProcessBoneMovement(boneNameMap, _blendShapeRigMovement, _blendShapeRigRotation);
        }

        /// <summary>
        ///     Updates the texture on the given color row for the specified color type.
        /// </summary>
        /// <param name="colorType">The color type to update.</param>
        /// <param name="colorRow">The color row to get the updated color from.</param>
        public void UpdateColor(ColorType colorType, SidekickColorRow colorRow)
        {
            if (colorRow == null)
            {
                return;
            }

            if (_currentMaterial == null)
            {
                return;
            }

            switch (colorType)
            {
                case ColorType.Metallic:
                    Texture2D metallic = (Texture2D) _currentMaterial.GetTexture(_METALLIC_MAP);
                    UpdateTexture(metallic, colorRow.NiceMetallic, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_METALLIC_MAP, metallic);
                    break;
                case ColorType.Smoothness:
                    Texture2D smoothness = (Texture2D) _currentMaterial.GetTexture(_SMOOTHNESS_MAP);
                    UpdateTexture(smoothness, colorRow.NiceSmoothness, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_SMOOTHNESS_MAP, smoothness);
                    break;
                case ColorType.Reflection:
                    Texture2D reflection = (Texture2D) _currentMaterial.GetTexture(_REFLECTION_MAP);
                    UpdateTexture(reflection, colorRow.NiceReflection, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_REFLECTION_MAP, reflection);
                    break;
                case ColorType.Emission:
                    Texture2D emission = (Texture2D) _currentMaterial.GetTexture(_EMISSION_MAP);
                    UpdateTexture(emission, colorRow.NiceEmission, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_EMISSION_MAP, emission);
                    break;
                case ColorType.Opacity:
                    Texture2D opacity = (Texture2D) _currentMaterial.GetTexture(_OPACITY_MAP);
                    UpdateTexture(opacity, colorRow.NiceOpacity, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_OPACITY_MAP, opacity);
                    break;
                case ColorType.MainColor:
                default:
                    Texture2D color = (Texture2D) _currentMaterial.GetTexture(_COLOR_MAP);
                    UpdateTexture(color, colorRow.NiceColor, colorRow.ColorProperty.U, colorRow.ColorProperty.V);
                    _currentMaterial.SetTexture(_COLOR_MAP, color);
                    break;
            }
        }

        /// <summary>
        ///     Updates the color on the texture with the given new color.
        /// </summary>
        /// <param name="texture">The texture to update.</param>
        /// <param name="newColor">The color to assign to the texture.</param>
        /// <param name="u">The u positioning on the texture to update.</param>
        /// <param name="v">The v positioning on the texture to update.</param>
        public void UpdateTexture(Texture2D texture, Color newColor, int u, int v)
        {
            int scaledU = u * 2;
            int scaledV = v * 2;
            texture.SetPixel(scaledU, scaledV, newColor);
            texture.SetPixel(scaledU + 1, scaledV, newColor);
            texture.SetPixel(scaledU, scaledV + 1, newColor);
            texture.SetPixel(scaledU + 1, scaledV + 1, newColor);
            texture.Apply();
        }

    }
}
