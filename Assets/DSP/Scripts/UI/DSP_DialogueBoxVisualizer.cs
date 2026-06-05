using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DSP_DialogueBoxVisualizer : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI characterNameText;
    public Image characterImage;
    public Button continueButton;
    
    [Header("Effect Targets")]
    public GameObject dialogueBoxTarget;
    public GameObject continueButtonTarget;
    public GameObject characterImageTarget;
    public GameObject characterNameTarget;
    public GameObject dialogueTextTarget;
    
    private DSP_ConversationManager conversationManager;
    private DSP_SettingsObject settings;
    private string currentCharacterName;
    private Sprite currentCharacterSprite;
    
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
        
        continueButton.onClick.AddListener(OnContinueClicked);
        continueButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
        
        ApplySettings();
        characterNameText.gameObject.SetActive(false);
        characterImage.gameObject.SetActive(false);
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
        
        if (dialogueText != null && settings.mainFont != null)
        {
            dialogueText.font = settings.mainFont;
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
        gameObject.SetActive(true);
        StartCoroutine(PlayDialogueBoxAppearEffect());
    }
    
    private void OnConversationEnded()
    {
        StartCoroutine(PlayDialogueBoxDisappearEffect());
    }
    
    private void OnDialogueNode(string dialogue, string characterName, Sprite characterSprite)
    {
        dialogueText.text = dialogue;
        
        bool characterChanged = currentCharacterName != characterName || currentCharacterSprite != characterSprite;
        currentCharacterName = characterName;
        currentCharacterSprite = characterSprite;
        
        if (!string.IsNullOrEmpty(characterName))
        {
            characterNameText.text = characterName;
            characterNameText.gameObject.SetActive(true);
            if (characterChanged)
            {
                DSP_Effects.PlayEffect(settings.characterAppearEffect, characterNameTarget, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            }
        }
        else
        {
            characterNameText.gameObject.SetActive(false);
        }
        
        if (characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.gameObject.SetActive(true);
            if (characterChanged)
            {
                DSP_Effects.PlayEffect(settings.characterAppearEffect, characterImageTarget, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            }
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }
        
        DSP_Effects.PlayEffect(settings.textRevealEffect, dialogueTextTarget, 0.5f, AnimationCurve.Linear(0, 0, 1, 1));
        
        DSP_NodeData nextNode = PeekNextNode();
        if (nextNode == null || nextNode.nodeType != DSP_NodeType.Choice)
        {
            DSP_Effects.PlayEffect(settings.continueButtonAppearEffect, continueButtonTarget, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            continueButton.gameObject.SetActive(true);
        }
    }
    
    private void OnChoiceNode(string[] choices)
    {
        DSP_Effects.PlayEffect(settings.continueButtonDisappearEffect, continueButtonTarget, 0.2f, AnimationCurve.EaseInOut(0, 0, 1, 1));
        continueButton.gameObject.SetActive(false);
    }
    
    private IEnumerator PlayDialogueBoxAppearEffect()
    {
        DSP_Effects.PlayEffect(settings.dialogueBoxAppearEffect, dialogueBoxTarget, 0.4f, AnimationCurve.EaseInOut(0, 0, 1, 1));
        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator PlayDialogueBoxDisappearEffect()
    {
        DSP_Effects.PlayEffect(settings.dialogueBoxDisappearEffect, dialogueBoxTarget, 0.3f, AnimationCurve.EaseInOut(0, 0, 1, 1));
        yield return new WaitForSeconds(0.3f);
        gameObject.SetActive(false);
    }
    
    private DSP_NodeData PeekNextNode()
    {
        if (conversationManager == null || conversationManager.currentConversation == null)
            return null;
            
        var iterator = new DSP_ConversationIterator(conversationManager.currentConversation);
        var edges = conversationManager.currentConversation.GetOutgoingEdges(iterator.CurrentNode);
        if (edges.Count == 0)
            return null;
            
        return conversationManager.currentConversation.GetNodes().FirstOrDefault(n => n.id == edges[0].toNode);
    }
    
    private void OnContinueClicked()
    {
        if (conversationManager.IsAtChoiceNode)
        {
            if (conversationManager.debugMode) Debug.LogWarning("[DSP] Cannot continue - waiting for choice selection");
            return;
        }
        
        conversationManager.Advance();
    }
}