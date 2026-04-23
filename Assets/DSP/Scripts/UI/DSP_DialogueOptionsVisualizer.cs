using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DSP_DialogueOptionsVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject optionsContainer;
    public GameObject optionButtonPrefab;
    
    private DSP_ConversationManager conversationManager;
    
    void Start()
    {
        conversationManager = DSP_ConversationManager.GetInstance();
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted += OnConversationStarted;
            conversationManager.OnConversationEnded += OnConversationEnded;
            conversationManager.OnChoiceNode += OnChoiceNode;
        }
        
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
        }
        else
        {
            if (conversationManager != null && conversationManager.debugMode) Debug.LogError("[DSP] Options container not assigned!");
        }
    }
    
    void OnDestroy()
    {
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted -= OnConversationStarted;
            conversationManager.OnConversationEnded -= OnConversationEnded;
            conversationManager.OnChoiceNode -= OnChoiceNode;
        }
    }
    
    private void OnConversationStarted()
    {
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
        }
        ClearOptions();
    }
    
    private void OnConversationEnded()
    {
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
        }
        ClearOptions();
    }
    
    private void OnChoiceNode(string[] choices)
    {
        if (choices == null || choices.Length == 0)
        {
            LogWarning("No choices provided for choice node");
            return;
        }
        
        if (optionButtonPrefab == null)
        {
            LogError("Option button prefab is not assigned! Please assign it in the inspector.");
            return;
        }
        
        if (optionsContainer == null)
        {
            LogError("Options container is not assigned!");
            return;
        }
        
        ClearOptions();
        
        LogDebug($"Creating {choices.Length} choice buttons");
        
        // Create option buttons for each choice
        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i;
            GameObject optionButton = Instantiate(optionButtonPrefab, optionsContainer.transform);
            
            // Set button text
            TextMeshProUGUI buttonText = optionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choices[i];
                LogDebug($"Created choice button: {choices[i]}");
            }
            else
            {
                LogWarning("Button text component not found on option button prefab");
            }
            
            // Set button click event
            Button button = optionButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnOptionSelected(choiceIndex));
            }
            else
            {
                LogWarning("Button component not found on option button prefab");
            }
        }
        
        optionsContainer.SetActive(true);
        LogDebug("Options container activated");
    }
    
    private void OnOptionSelected(int choiceIndex)
    {
        LogDebug($"Option selected: {choiceIndex}");
        conversationManager.AdvanceChoice(choiceIndex);
        
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
        }
        ClearOptions();
    }
    
    private void ClearOptions()
    {
        if (optionsContainer == null) return;
        
        // Destroy all existing option buttons
        foreach (Transform child in optionsContainer.transform)
        {
            Destroy(child.gameObject);
        }
        
        LogDebug("Options cleared");
    }
    
    private void LogDebug(string message)
    {
        if (conversationManager != null && conversationManager.debugMode)
        {
            Debug.Log($"[DSP] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        if (conversationManager != null && conversationManager.debugMode)
        {
            Debug.LogWarning($"[DSP] {message}");
        }
    }
    
    private void LogError(string message)
    {
        if (conversationManager != null && conversationManager.debugMode)
        {
            Debug.LogError($"[DSP] {message}");
        }
    }
}
