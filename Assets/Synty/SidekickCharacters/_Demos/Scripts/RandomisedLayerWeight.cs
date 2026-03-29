using UnityEngine;
using UnityEngine.UI;

public class RandomisedLayerWeight : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Random Variance Settings")]
    [SerializeField, Range(0f, 1f)] private float maxRandomVariance = 0.5f;
    [SerializeField] private Slider randomVarianceSlider;
    [SerializeField] private Text maxRandomVarianceText; // UI Text to display maxRandomVariance

    [Header("Layer Weight Settings")] // Changed header name to "Layer Weight Settings"
    [SerializeField, Range(0f, 1f)] private float layerWeight = 1f; // Changed variable name to layerWeight
    [SerializeField] private Slider layerWeightSlider; // Changed slider name to layerWeightSlider
    [SerializeField] private Text layerWeightText; // Changed UI Text name to layerWeightText

    [Header("Transition Settings")]
    [SerializeField] private string layerName = "Emotion_Additive"; // Adjust this layer name in the Inspector
    [SerializeField] private float averageTransitionTime = 0.4f;
    [SerializeField] private float transitionVariationAmount = 0.3f;

    [Header("Hold Settings")]
    [SerializeField] private float averageHoldTime = 1.0f;
    [SerializeField] private float holdVariationAmount = 1.0f;

    private int layerIndex;
    private float currentWeight = 0f;
    private float targetWeight = 0f;
    private float transitionTimer = 0f;
    private float holdTimer = 0f;

    void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator component is not assigned.");
            enabled = false;
            return;
        }

        layerIndex = animator.GetLayerIndex(layerName);
        if (layerIndex == -1)
        {
            Debug.LogError($"Layer '{layerName}' not found in the Animator.");
            enabled = false;
            return;
        }

        // Initialize sliders and UI Texts
        if (randomVarianceSlider != null)
        {
            randomVarianceSlider.value = maxRandomVariance;
            randomVarianceSlider.onValueChanged.AddListener(SetMaxRandomVariance);
        }

        if (layerWeightSlider != null) // Changed to layerWeightSlider
        {
            layerWeightSlider.value = layerWeight; // Changed to layerWeight
            layerWeightSlider.onValueChanged.AddListener(SetLayerWeight); // Changed to SetLayerWeight
        }

        // Initialize UI Texts
        if (maxRandomVarianceText != null)
        {
            maxRandomVarianceText.text = $"Random Variance: {maxRandomVariance:P0}";
        }

        if (layerWeightText != null) // Changed to layerWeightText
        {
            layerWeightText.text = $"Layer Weight: {layerWeight:P0}"; // Changed to Layer Weight
        }

        // Initialize the weight to a random value between 0 and 1
        currentWeight = Random.value;
        animator.SetLayerWeight(layerIndex, currentWeight * layerWeight); // Changed to layerWeight

        // Start the initial transition
        StartTransition();
    }

    void Update()
    {
        // Update timers
        transitionTimer -= Time.deltaTime;
        holdTimer -= Time.deltaTime;

        // Check if it's time to transition to a new weight
        if (transitionTimer <= 0f)
        {
            StartTransition();
        }
        // Otherwise, check if it's time to hold the current weight
        else if (holdTimer <= 0f)
        {
            holdTimer = GenerateHoldTime();
        }

        // Smoothly adjust the weight towards the target weight
        currentWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime / averageTransitionTime);
        animator.SetLayerWeight(layerIndex, currentWeight * layerWeight); // Changed to layerWeight
    }

    private void StartTransition()
    {
        // Set a new target weight
        targetWeight = Random.Range(Mathf.Max(0f, 1f - maxRandomVariance), 1f) * layerWeight; // Changed to layerWeight
        transitionTimer = GenerateTransitionTime();
    }

    private float GenerateTransitionTime()
    {
        float variation = Random.Range(-transitionVariationAmount, transitionVariationAmount);
        return Mathf.Max(0f, averageTransitionTime + variation);
    }

    private float GenerateHoldTime()
    {
        float variation = Random.Range(-holdVariationAmount, holdVariationAmount);
        return Mathf.Max(0f, averageHoldTime + variation);
    }

    private void SetMaxRandomVariance(float value)
    {
        maxRandomVariance = value;
        if (maxRandomVarianceText != null)
        {
            maxRandomVarianceText.text = $"Random Variance: {maxRandomVariance:P0}";
        }
    }

    private void SetLayerWeight(float value) // Changed method name to SetLayerWeight
    {
        layerWeight = value; // Changed to layerWeight
        if (layerWeightText != null) // Changed to layerWeightText
        {
            layerWeightText.text = $"Layer Weight: {layerWeight:P0}"; // Changed to Layer Weight
        }
    }
}
