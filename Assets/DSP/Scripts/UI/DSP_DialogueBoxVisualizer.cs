using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DSP_DialogueBoxVisualizer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI characterNameText;
    public Image characterImage;
    public Button continueButton;
    
    private DSP_ConversationManager conversationManager;
    
    void Start()
    {
        conversationManager = DSP_ConversationManager.GetInstance();
        if (conversationManager != null)
        {
            conversationManager.OnConversationStarted += OnConversationStarted;
            conversationManager.OnConversationEnded += OnConversationEnded;
            conversationManager.OnDialogueNode += OnDialogueNode;
            conversationManager.OnChoiceNode += OnChoiceNode;
        }
        
        continueButton.onClick.AddListener(OnContinueClicked);
        continueButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
        
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
    
    private void OnConversationStarted()
    {
        gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
    }
    
    private void OnConversationEnded()
    {
        gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
    }
    
    private void OnDialogueNode(string dialogue, string characterName, Sprite characterSprite)
    {
        dialogueText.text = dialogue;
        
        if (!string.IsNullOrEmpty(characterName))
        {
            characterNameText.text = characterName;
            characterNameText.gameObject.SetActive(true);
        }
        else
        {
            characterNameText.gameObject.SetActive(false);
        }
        
        if (characterSprite != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.gameObject.SetActive(true);
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }
        
        // Only show continue button if the next node is NOT a choice node
        DSP_NodeData nextNode = PeekNextNode();
        if (nextNode == null || nextNode.nodeType != DSP_NodeType.Choice)
        {
            continueButton.gameObject.SetActive(true);
        }
        else
        {
            continueButton.gameObject.SetActive(false);
        }
    }
    
    private void OnChoiceNode(string[] choices)
    {
        continueButton.gameObject.SetActive(false);
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
}
