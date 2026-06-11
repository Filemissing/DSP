using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DSP_DialogueBoxVisualizer : MonoBehaviour
{
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
        
        DSP_EffectsHandler.DisappearContinueButton(continueButton.gameObject);
        DSP_EffectsHandler.DisappearDialogueBox(gameObject);
        
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
        DSP_EffectsHandler.AppearDialogueBox(gameObject);
    }
    
    private void OnConversationEnded()
    {
        if (gameObject.activeSelf)
            DSP_EffectsHandler.DisappearDialogueBox(gameObject);
    }
    
    private void OnDialogueNode(string dialogue, string characterName, Sprite characterSprite)
    {
        bool characterChanged = currentCharacterName != characterName || currentCharacterSprite != characterSprite;
        
        DSP_EffectsHandler.RevealText(dialogueTextTarget, dialogue);
        currentCharacterName = characterName;
        currentCharacterSprite = characterSprite;
        
        if (!string.IsNullOrEmpty(characterName))
        {
            characterNameText.text = characterName;
            if (characterChanged)
            {
                DSP_EffectsHandler.AppearNameBox(characterNameBoxTarget);
            }
        }
        else
        {
            DSP_EffectsHandler.DisappearNameBox(characterNameBoxTarget);
        }
        
        if (characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            DSP_EffectsHandler.AppearCharacterImage(characterImageTarget);
        }
        else
        {
            DSP_EffectsHandler.DisappearCharacterImage(characterImageTarget);
        }
        
        
        
        DSP_NodeData nextNode = PeekNextNode();
        if (nextNode.nodeType != DSP_NodeType.Choice)
            DSP_EffectsHandler.AppearContinueButton(continueButton.gameObject);
    }
    
    private void OnChoiceNode((string, bool)[] choices)
    {
        DSP_EffectsHandler.DisappearContinueButton(continueButton.gameObject);
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
}