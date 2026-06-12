using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DSP_DialogueOptionsVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject optionsContainer;
    public GameObject optionButtonPrefab;
    
    private DSP_ConversationManager conversationManager;
    [SerializeField] private DSP_DialogueBoxVisualizer dialogueBoxVisualizer;

    private List<GameObject> options = new List<GameObject>();
    
    
    
    // Methods
    void Awake()
    {
        conversationManager = DSP_ConversationManager.GetInstance();
        ClearOptions();
    }
    
    private void OnConversationStarted()
    {
        ClearOptions();
    }
    
    private void OnConversationEnded()
    {
        ClearOptions();
    }
    
    private void OnChoiceNode(string[] choices)
    {
        if (choices == null || choices.Length == 0) return;
        
        CreateOptions(choices);

        dialogueBoxVisualizer.ForcePlayEffect(options, DSP_DialogueBoxVisualizer.EffectType.AppearOptions);
    }
    
    private void OnOptionSelected(int choiceIndex)
    {
        dialogueBoxVisualizer.ForcePlayEffect(options, DSP_DialogueBoxVisualizer.EffectType.DisappearOptions);
        conversationManager.AdvanceChoice(choiceIndex);
    }
    
    
    
    // Helpers
    private void CreateOptions(string[] choices)
    {
        ClearOptions();
        
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            
            GameObject optionButton = Instantiate(optionButtonPrefab, optionsContainer.transform);
            optionButton.GetComponent<Button>().onClick.AddListener(( )=> OnOptionSelected(index));
            optionButton.transform.GetChild(0).GetComponent<TMP_Text>().text = choices[index];
            options.Add(optionButton);
        }
    }
    
    private void ClearOptions()
    {
        foreach (Transform child in optionsContainer.transform)
            Destroy(child.gameObject);
        
        options.Clear();
    }
    
    
    
    // Event Bindings
    void OnEnable()
    {
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted += OnConversationStarted;
            conversationManager.OnConversationEnded += OnConversationEnded;
            conversationManager.OnChoiceNode += OnChoiceNode;
        }
    }

    private void OnDisable()
    {
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted -= OnConversationStarted;
            conversationManager.OnConversationEnded -= OnConversationEnded;
            conversationManager.OnChoiceNode -= OnChoiceNode;
        }
    }
}