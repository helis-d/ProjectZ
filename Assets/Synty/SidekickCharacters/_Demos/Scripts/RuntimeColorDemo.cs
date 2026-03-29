using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Synty.SidekickCharacters.Demo
{
    /// <summary>
    ///     An example script to show how to interact with the Sidekick API in regards to colors at runtime.
    /// </summary>
    public class RuntimeColorDemo : MonoBehaviour
    {
        private readonly string _OUTPUT_MODEL_NAME = "Sidekick Character";

        private Dictionary<string, SidekickPartPreset> _availableHeadPresetDictionary = new Dictionary<string, SidekickPartPreset>();
        private Dictionary<string, SidekickPartPreset> _availableUpperBodyPresetDictionary = new Dictionary<string, SidekickPartPreset>();
        private Dictionary<string, SidekickPartPreset> _availableLowerBodyPresetDictionary = new Dictionary<string, SidekickPartPreset>();
        private List<SidekickBodyShapePreset> _availableBodyShapes = new List<SidekickBodyShapePreset>();
        private List<SidekickColorPreset> _availableColorPresets = new List<SidekickColorPreset>();

        private int _currentHeadPresetIndex = 0;
        private int _currentUpperBodyPresetIndex = 0;
        private int _currentLowerBodyPresetIndex = 0;
        private int _currentBodyShapePresetIndex = 0;
        private int _currentColorPresetIndex = 0;

        private DatabaseManager _dbManager;
        private SidekickRuntime _sidekickRuntime;

        private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> _partLibrary;
        
        public TextMeshProUGUI _loadingText;

        /// <inheritdoc cref="Start"/>
        void Start()
        {
            _dbManager = new DatabaseManager();

            GameObject model = Resources.Load<GameObject>("Meshes/SK_BaseModel");
            Material material = Resources.Load<Material>("Materials/M_BaseMaterial");

            _sidekickRuntime = new SidekickRuntime(model, material, null, _dbManager);
            
            SidekickRuntime.PopulateToolData(_sidekickRuntime);
            _partLibrary = _sidekickRuntime.MappedPartDictionary;

            foreach (PartGroup partGroup in Enum.GetValues(typeof(PartGroup)))
            {
                // only filter head part presets by species
                List<SidekickPartPreset> presets = SidekickPartPreset.GetAllByGroup(_dbManager, partGroup);
                List<string> presetNames = new List<string>();
                if (presets.Count < 1)
                {
                    Debug.LogWarning("No parts found for " + partGroup + ". Please add at least 1 Sidekicks content pack.");
                    continue;
                }
                
                foreach (SidekickPartPreset preset in presets)
                {
                    switch (partGroup)
                    {
                        case PartGroup.Head:
                            _availableHeadPresetDictionary.Add(preset.Name, preset);
                            break;
                        case PartGroup.UpperBody:
                            _availableUpperBodyPresetDictionary.Add(preset.Name, preset);
                            break;
                        case PartGroup.LowerBody:
                            _availableLowerBodyPresetDictionary.Add(preset.Name, preset);
                            break;
                    }

                    presetNames.Add(preset.Name);
                }
            }

            _availableBodyShapes = SidekickBodyShapePreset.GetAll(_dbManager);

            // An example of how to retrieve color presets from the database. To retrieve presets for other areas of the material, use the ColorGroup
            // enum to retrieve other presets.
            _availableColorPresets = SidekickColorPreset.GetAllByColorGroup(_dbManager, ColorGroup.Outfits);
            
            _currentHeadPresetIndex = Random.Range(0, _availableHeadPresetDictionary.Count - 1);
            _currentUpperBodyPresetIndex = Random.Range(0, _availableUpperBodyPresetDictionary.Count - 1);
            _currentLowerBodyPresetIndex = Random.Range(0, _availableLowerBodyPresetDictionary.Count - 1);
            _currentBodyShapePresetIndex = Random.Range(0, _availableBodyShapes.Count - 1);
            _currentColorPresetIndex = Random.Range(0, _availableColorPresets.Count - 1);

            _loadingText.enabled = false;
            
            UpdateModel();
        }

        /// <summary>
        ///     Processes the change of the skin color on the character.
        /// </summary>
        /// <param name="image">The image tile that contains the color to change to.</param>
        public void ProcessSkinColorChange(Image image)
        {
            ColorType colorType = ColorType.MainColor;
            List<SidekickColorProperty> allProperties = SidekickColorProperty.GetAll(_dbManager);
            List<SidekickColorProperty> selectedProperties = allProperties.FindAll(scp => scp.Name.ToLower().Contains("skin"));
            foreach (SidekickColorProperty property in selectedProperties)
            {
                SidekickColorRow row = new SidekickColorRow()
                {
                    ColorProperty = property,
                    MainColor = ColorUtility.ToHtmlStringRGB(image.color),
                };
                _sidekickRuntime.UpdateColor(colorType, row);
            }
        }

        /// <summary>
        ///     Processes the change of the outfit color on the character.
        /// </summary>
        /// <param name="image">The image tile that contains the color to change to.</param>
        public void ProcessOutfitColorChange(Image image)
        {
            ColorType colorType = ColorType.MainColor;
            List<SidekickColorProperty> allProperties = SidekickColorProperty.GetAll(_dbManager);
            List<SidekickColorProperty> selectedProperties = allProperties.FindAll(scp => scp.Name.ToLower().Contains("outfit"));
            foreach (SidekickColorProperty property in selectedProperties)
            {
                SidekickColorRow row = new SidekickColorRow()
                {
                    ColorProperty = property,
                    MainColor = ColorUtility.ToHtmlStringRGB(image.color),
                };
                _sidekickRuntime.UpdateColor(colorType, row);
            }
        }

        /// <summary>
        ///     Updates the created character model.
        /// </summary>
        private void UpdateModel()
        {
            // If there aren't enough presets, stop trying to update the model.
            if (_availableHeadPresetDictionary.Values.Count < 1 
                || _availableUpperBodyPresetDictionary.Values.Count < 1 
                || _availableLowerBodyPresetDictionary.Values.Count < 1)
            {
                return;
            }
            
            // Create and populate the list of parts to use from the parts list and the selected colors.
            List<SidekickPartPreset> presets = new List<SidekickPartPreset>()
            {
                _availableHeadPresetDictionary.Values.ToArray()[_currentHeadPresetIndex],
                _availableUpperBodyPresetDictionary.Values.ToArray()[_currentUpperBodyPresetIndex],
                _availableLowerBodyPresetDictionary.Values.ToArray()[_currentLowerBodyPresetIndex]
            };

            List<SkinnedMeshRenderer> partsToUse = new List<SkinnedMeshRenderer>();

            foreach (SidekickPartPreset preset in presets)
            {
                List<SidekickPartPresetRow> rows = SidekickPartPresetRow.GetAllByPreset(_dbManager, preset);
                foreach (SidekickPartPresetRow row in rows)
                {
                    if (!string.IsNullOrEmpty(row.PartName))
                    {
                        CharacterPartType type = Enum.Parse<CharacterPartType>(CharacterPartTypeUtils.GetTypeNameFromShortcode(row.PartType));
                        Dictionary<string, SidekickPart> partLocationDictionary = _partLibrary[type];
                        GameObject selectedPart = partLocationDictionary[row.PartName].GetPartModel();
                        SkinnedMeshRenderer selectedMesh = selectedPart.GetComponentInChildren<SkinnedMeshRenderer>();
                        partsToUse.Add(selectedMesh);
                    }
                }
            }

            SidekickBodyShapePreset bodyPreset = _availableBodyShapes[_currentBodyShapePresetIndex];
            _sidekickRuntime.BodyTypeBlendValue = bodyPreset.BodyType;
            _sidekickRuntime.BodySizeHeavyBlendValue = bodyPreset.BodySize > 0 ? bodyPreset.BodySize : 0;
            _sidekickRuntime.BodySizeSkinnyBlendValue = bodyPreset.BodySize < 0 ? -bodyPreset.BodySize : 0;
            _sidekickRuntime.MusclesBlendValue = bodyPreset.Musculature;

            List<SidekickColorPresetRow> colorRows = SidekickColorPresetRow.GetAllByPreset(_dbManager, _availableColorPresets[_currentColorPresetIndex]);
            foreach (SidekickColorPresetRow row in colorRows)
            {
                SidekickColorRow colorRow = SidekickColorRow.CreateFromPresetColorRow(row);
                foreach (ColorType property in Enum.GetValues(typeof(ColorType)))
                {
                    _sidekickRuntime.UpdateColor(property, colorRow);
                }
            }

            // Check for an existing copy of the model, if it exists, delete it so that we don't end up with duplicates.
            GameObject character = GameObject.Find(_OUTPUT_MODEL_NAME);

            if (character != null)
            {
                Destroy(character);
            }

            // Create a new character using the selected parts using the Sidekicks API.
            character = _sidekickRuntime.CreateCharacter(_OUTPUT_MODEL_NAME, partsToUse, false, true);
        }

        /// <summary>
        ///     Gets a resource path for using with Resources.Load() from a full path.
        /// </summary>
        /// <param name="fullPath">The full path to get the resource path from.</param>
        /// <returns>The resource path.</returns>
        private string GetResourcePath(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath);
            int startIndex = directory.IndexOf("Resources") + 10;
            directory = directory.Substring(startIndex, directory.Length - startIndex);
            return Path.Combine(directory, Path.GetFileNameWithoutExtension(fullPath));
        }
    }
}
