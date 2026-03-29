using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ExpressionSwitcher : MonoBehaviour
{
    private Animator _animator;

    // Define a list of expression names (will be dynamically populated)
    private List<string> _expressionNames = new List<string>();

    private int _currentIndex = 0;

    // Reference to the UI text to display the current emotion
    [SerializeField] private Text _emotionText;

    // Reference to the button to cycle expressions
    [SerializeField] private Button _cycleButton;

    // Reference to the UI slider to adjust transition time
    [SerializeField] private Slider _transitionTimeSlider;

    private float _transitionTime = 0.2f; // Default transition time

    void Start()
    {
        _animator = GetComponent<Animator>();

        // Ensure the Animator is assigned
        if (_animator == null)
        {
            Debug.LogWarning("[ExpressionSwitcher] No Animator component found on the GameObject.");
            return;
        }

        // Populate the expression names from the Animator states in the specified layer
        PopulateExpressionNames();

        // Debug: Check what was found
        Debug.Log($"[ExpressionSwitcher] Found {_expressionNames.Count} expressions:");
        foreach (var expr in _expressionNames)
            Debug.Log($"  - {expr}");

        // Debug: Check the layer
        int layerIndex = _animator.GetLayerIndex("Emotion_Additive");
        Debug.Log($"[ExpressionSwitcher] Layer 'Emotion_Additive' index: {layerIndex}");

        // Wire up the button
        if (_cycleButton != null)
        {
            _cycleButton.onClick.AddListener(CycleExpressions);
            Debug.Log("[ExpressionSwitcher] Button wired up successfully");
        }
        else
        {
            Debug.LogWarning("[ExpressionSwitcher] No button assigned in Inspector!");
        }

        // Ensure the slider is assigned
        if (_transitionTimeSlider != null)
        {
            _transitionTimeSlider.minValue = 0.0f;
            _transitionTimeSlider.maxValue = 1.0f;
            _transitionTimeSlider.value = _transitionTime;

            // Read the initial value from the slider to set the transition time
            UpdateTransitionTime(_transitionTimeSlider.value);

            // Add a listener to handle value changes
            _transitionTimeSlider.onValueChanged.AddListener(UpdateTransitionTime);
        }
    }

    private void PopulateExpressionNames()
    {
        RuntimeAnimatorController ac = _animator.runtimeAnimatorController;

        if (ac != null)
        {
            Debug.Log($"[ExpressionSwitcher] Scanning {ac.animationClips.Length} animation clips...");

            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name.Contains("A_FacePose") && !_expressionNames.Contains(clip.name) && !clip.name.Contains("Neutral"))
                {
                    _expressionNames.Add(clip.name);
                }
            }
        }
        else
        {
            Debug.LogError("[ExpressionSwitcher] AnimatorController is not found.");
        }
    }

    public void CycleExpressions()
    {
        Debug.Log("[ExpressionSwitcher] CycleExpressions() called");

        if (_animator == null)
        {
            Debug.LogError("[ExpressionSwitcher] Animator is null!");
            return;
        }

        if (_expressionNames.Count == 0)
        {
            Debug.LogError("[ExpressionSwitcher] No expressions found in list!");
            return;
        }

        string layerName = "Emotion_Additive";
        int layerIndex = _animator.GetLayerIndex(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError($"[ExpressionSwitcher] Layer '{layerName}' not found!");
            return;
        }

        string expressionName = _expressionNames[_currentIndex];
        int stateHash = Animator.StringToHash(expressionName);
        bool hasState = _animator.HasState(layerIndex, stateHash);

        Debug.Log($"[ExpressionSwitcher] Trying expression: '{expressionName}' (index {_currentIndex})");
        Debug.Log($"[ExpressionSwitcher] State hash: {stateHash}, HasState: {hasState}");

        if (hasState)
        {
            _animator.CrossFadeInFixedTime(expressionName, _transitionTime, layerIndex);
            Debug.Log($"[ExpressionSwitcher] Playing '{expressionName}'");
        }
        else
        {
            Debug.LogWarning($"[ExpressionSwitcher] State '{expressionName}' not found, trying 'Neutral'");
            _animator.CrossFadeInFixedTime("Neutral", _transitionTime, layerIndex);
        }

        // Update the emotion text after playing the animation
        UpdateEmotionText();

        // Move to the next expression in the list
        _currentIndex = (_currentIndex + 1) % _expressionNames.Count;
    }

    private void UpdateEmotionText()
    {
        if (_emotionText != null && _expressionNames.Count > 0)
        {
            string expressionName = _expressionNames[_currentIndex];
            int underscoreIndex = expressionName.LastIndexOf('_');
            if (underscoreIndex != -1 && underscoreIndex < expressionName.Length - 1)
            {
                _emotionText.text = expressionName.Substring(underscoreIndex + 1);
            }
            else
            {
                _emotionText.text = expressionName; // Fallback if no underscore found
            }
        }
    }

    private void UpdateTransitionTime(float value)
    {
        _transitionTime = value;
    }
}
