using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DSP_DialogueBoxVisualizer : MonoBehaviour
{
    public enum EffectType
    {
        AppearDialogueBox,
        DisappearDialogueBox,
        AppearContinueButton,
        DisappearContinueButton,
        AppearOptions,
        DisappearOptions,
        AppearCharacterImage,
        DisappearCharacterImage,
        AppearNameBox,
        DisappearNameBox,
        RevealText,
    }
    
    [Header("UI References")]
    public TextMeshProUGUI characterNameText;
    public Image characterImage;
    public Button continueButton;
    
    [Header("Effect Targets")]
    public GameObject dialogueBoxTarget;
    public GameObject continueButtonTarget;
    public GameObject characterImageTarget;
    public GameObject characterNameBoxTarget;
    public GameObject dialogueTextTarget;
    
    private DSP_ConversationManager conversationManager;
    private DSP_SettingsObject settings;
    private string currentCharacterName;
    private Sprite currentCharacterSprite;
    
    // Events
    public event Action<IEnumerable<GameObject>, EffectType> PlayEffect;
    
    
    
    void Start()
    {
        conversationManager = DSP_ConversationManager.GetInstance();
        if (conversationManager != null)
        {
            settings = conversationManager.settings;
            conversationManager.OnConversationStarted += OnConversationStarted;
            conversationManager.OnConversationEnded += OnConversationEnded;
            conversationManager.OnDialogueNode += OnDialogueNode;
            conversationManager.OnChoiceNode += OnChoiceNode;
        }

        PlayEffect?.Invoke(new[] {continueButton.gameObject}, EffectType.DisappearContinueButton);
        PlayEffect?.Invoke(new[] {dialogueBoxTarget}, EffectType.DisappearDialogueBox);
        
        ApplySettings();
    }
    
    void OnDestroy()
    {
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted -= OnConversationStarted;
            conversationManager.OnConversationEnded -= OnConversationEnded;
            conversationManager.OnDialogueNode -= OnDialogueNode;
            conversationManager.OnChoiceNode -= OnChoiceNode;
        }
    }
    
    private void ApplySettings()
    {
        if (settings == null) return;
        
        Image dialogueBoxImage = GetComponent<Image>();
        if (dialogueBoxImage != null && settings.textboxSprite != null)
        {
            dialogueBoxImage.sprite = settings.textboxSprite;
        }
        
        if (characterNameText != null && settings.characterNameFont != null)
        {
            characterNameText.font = settings.characterNameFont;
        }
        
        if (continueButton != null && settings.continueSprite != null)
        {
            Image buttonImage = continueButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = settings.continueSprite;
            }
        }
    }
    
    private void OnConversationStarted()
    {
        PlayEffect?.Invoke(new[] {dialogueBoxTarget}, EffectType.AppearDialogueBox);
    }
    
    private void OnConversationEnded()
    {
        PlayEffect?.Invoke(new[] {dialogueBoxTarget}, EffectType.DisappearDialogueBox);
    }
    
    private void OnDialogueNode(string dialogue, string characterName, Sprite characterSprite)
    {
        bool characterChanged = currentCharacterName != characterName || currentCharacterSprite != characterSprite;
        
        dialogueTextTarget.GetComponent<TMP_Text>().text = dialogue;
        PlayEffect?.Invoke(new[] {dialogueTextTarget}, EffectType.RevealText);
        
        currentCharacterName = characterName;
        currentCharacterSprite = characterSprite;
        
        if (!string.IsNullOrEmpty(characterName))
        {
            characterNameText.text = characterName;
            if (characterChanged)
                PlayEffect?.Invoke(new[] {characterNameBoxTarget}, EffectType.AppearNameBox);
        }
        else
            PlayEffect?.Invoke(new[] {characterNameBoxTarget}, EffectType.DisappearNameBox);
        
        
        
        if (characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            PlayEffect?.Invoke(new[] {characterImageTarget}, EffectType.AppearCharacterImage);
        }
        else
            PlayEffect?.Invoke(new[] {characterImageTarget}, EffectType.DisappearCharacterImage);
        
        
        
        DSP_NodeData nextNode = PeekNextNode();
        if (nextNode.nodeType != DSP_NodeType.Choice)
            PlayEffect?.Invoke(new[] {continueButton.gameObject}, EffectType.AppearContinueButton);
    }
    
    private void OnChoiceNode((string, bool)[] choices)
    {
        PlayEffect?.Invoke(new[] {continueButton.gameObject}, EffectType.DisappearContinueButton);
    }
    
    private DSP_NodeData PeekNextNode()
    {
        if (conversationManager == null || conversationManager.currentConversation == null)
            return null;
            
        var iterator = conversationManager.iterator;
        
        var edges = conversationManager.currentConversation.GetOutgoingEdges(iterator.CurrentNode);
        var next = edges.Count > 0
            ? conversationManager.currentConversation.GetNodes().FirstOrDefault(n => n.id == edges[0].toNode)
            : null;


        return next;
    }
    
    public void OnContinueClicked()
    {
        if (conversationManager.IsAtChoiceNode)
            return;
        
        conversationManager.Advance();
    }

    public void ForcePlayEffect(IEnumerable<GameObject> objects, EffectType effectType)
    {
        PlayEffect?.Invoke(objects, effectType);
    }
}