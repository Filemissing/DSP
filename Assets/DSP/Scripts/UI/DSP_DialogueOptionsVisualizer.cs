using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DSP_DialogueOptionsVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject optionsContainer;
    public GameObject optionButtonPrefab;
    
    private DSP_ConversationManager conversationManager;
    private DSP_SettingsObject settings;
    
    void Start()
    {
        conversationManager = DSP_ConversationManager.GetInstance();
        if (conversationManager != null)
        {
            settings = conversationManager.settings;
            conversationManager.OnConversationStarted += OnConversationStarted;
            conversationManager.OnConversationEnded += OnConversationEnded;
            conversationManager.OnChoiceNode += OnChoiceNode;
        }
        
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
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
        if (choices == null || choices.Length == 0) return;
        
        ClearOptions();
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        StartCoroutine(PlayOptionsAppearEffect(choices));
    }
    
    private IEnumerator PlayOptionsAppearEffect(string[] choices)
    {
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(true);
        }
    
        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i;
            GameObject optionButton = Instantiate(optionButtonPrefab, optionsContainer.transform);
            ApplyButtonSettings(optionButton, choices[i], choiceIndex);
            //DSP_Effects.PlayEffect(settings.optionsAppearEffect, optionButton, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
        }
        
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator PlayOptionsDisappearEffect(int choiceIndex)
    {
        foreach (Transform child in optionsContainer.transform)
        {
            //DSP_Effects.PlayEffect(settings.optionsDisappearEffect, child.gameObject, 0.2f, AnimationCurve.EaseInOut(0, 0, 1, 1));
        }
        
        yield return new WaitForSeconds(0.2f);
        
        conversationManager.AdvanceChoice(choiceIndex);
        ClearOptions();
        if (optionsContainer != null)
        {
            optionsContainer.SetActive(false);
        }
    }
    
    private void ApplyButtonSettings(GameObject optionButton, string choiceText, int choiceIndex)
    {
        TextMeshProUGUI buttonText = optionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = choiceText;
            if (settings != null && settings.mainFont != null)
            {
                buttonText.font = settings.mainFont;
            }
        }
        
        Button button = optionButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnOptionSelected(choiceIndex));
            
            if (settings != null && settings.continueSprite != null)
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = settings.continueSprite;
                }
            }
        }
    }
    
    private void OnOptionSelected(int choiceIndex)
    {
        StartCoroutine(PlayOptionsDisappearEffect(choiceIndex));
    }
    
    private void ClearOptions()
    {
        if (optionsContainer == null) return;
        
        foreach (Transform child in optionsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}