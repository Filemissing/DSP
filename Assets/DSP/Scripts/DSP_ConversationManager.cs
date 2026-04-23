using UnityEngine;
using System.Collections;
using System.Linq;

public class DSP_ConversationManager : MonoBehaviour
{
    public DSP_ConversationGraphAsset currentConversation;
    
    [Header("Debug Settings")]
    public bool debugMode = false;
    
    private DSP_ConversationIterator iterator;
    private Coroutine currentConversationRoutine;
    private bool waitingForUserInput = false;
    
    // Delegate definitions
    public delegate void ConversationEventHandler();
    public delegate void DialogueEventHandler(string dialogue, string characterName, Sprite characterSprite);
    public delegate void ChoiceEventHandler(string[] choices);
    
    // Events
    public event ConversationEventHandler OnConversationStarted;
    public event ConversationEventHandler OnConversationEnded;
    public event DialogueEventHandler OnDialogueNode;
    public event ChoiceEventHandler OnChoiceNode;
    public event ConversationEventHandler OnEventNode;
    
    public bool IsConversationActive { get; private set; }
    public bool IsAtChoiceNode { get; private set; }
    public bool WaitingForUserInput => waitingForUserInput;
    
    void Start()
    {
        IsConversationActive = false;
        IsAtChoiceNode = false;
        waitingForUserInput = false;
    }
    
    public void StartConversation(DSP_ConversationGraphAsset conversation)
    {
        if (conversation == null)
        {
            if (debugMode) Debug.LogError("[DSP] No conversation provided!");
            return;
        }
        
        if (IsConversationActive)
        {
            if (debugMode) Debug.LogWarning("[DSP] Conversation already in progress. Ending current conversation.");
            EndConversation();
        }
        
        currentConversation = conversation;
        iterator = new DSP_ConversationIterator(currentConversation);
        IsConversationActive = true;
        IsAtChoiceNode = false;
        waitingForUserInput = false;
        
        if (debugMode) Debug.Log($"[DSP] Starting conversation: {conversation.name}");
        FireEvent(OnConversationStarted, "OnConversationStarted");
        
        if (currentConversationRoutine != null)
            StopCoroutine(currentConversationRoutine);
        
        currentConversationRoutine = StartCoroutine(ProcessConversation());
    }
    
    private IEnumerator ProcessConversation()
    {
        while (iterator != null && iterator.State != DSP_IteratorState.Finished)
        {
            var currentNode = iterator.CurrentNode;
            
            if (debugMode) Debug.Log($"[DSP] Processing node: {currentNode.nodeType} (ID: {currentNode.id})");
            
            switch (currentNode.nodeType)
            {
                case DSP_NodeType.Dialogue:
                    IsAtChoiceNode = false;
                    waitingForUserInput = true;
                    HandleDialogueNode(currentNode);
                    
                    DSP_NodeData nextNode = PeekNextNode();
                    if (nextNode != null && nextNode.nodeType == DSP_NodeType.Choice)
                    {
                        if (debugMode) Debug.Log("[DSP] Dialogue followed by choice - auto-advancing");
                        waitingForUserInput = false;
                        iterator.Advance();
                    }
                    else
                    {
                        if (debugMode) Debug.Log("[极DSP] Waiting for user input to continue...");
                        yield return new WaitUntil(() => !waitingForUserInput);
                    }
                    break;
                    
                case DSP_NodeType.Choice:
                    IsAtChoiceNode = true;
                    waitingForUserInput = true;
                    HandleChoiceNode(currentNode);
                    if (debugMode) Debug.Log("[DSP] Waiting for choice selection...");
                    yield return new WaitUntil(() => !waitingForUserInput);
                    IsAtChoiceNode = false;
                    break;
                    
                case DSP_NodeType.Event:
                    IsAtChoiceNode = false;
                    waitingForUserInput = false;
                    HandleEventNode(currentNode);
                    if (debugMode) Debug.Log("[DSP] Event node - auto-advancing");
                    iterator.Advance();
                    break;
                    
                default:
                    IsAtChoiceNode = false;
                    waitingForUserInput = false;
                    if (debugMode) Debug.Log($"[DSP] Default node ({currentNode.nodeType}) - auto-advancing");
                    iterator.Advance();
                    break;
            }
            
            if (iterator.State == DSP_IteratorState.Finished)
            {
                if (debugMode) Debug.Log("[DSP] Conversation finished");
                break;
            }
        }
        
        EndConversation();
    }
    
    private DSP_NodeData PeekNextNode()
    {
        if (iterator == null || iterator.State == DSP_IteratorState.Finished)
            return null;
            
        var edges = currentConversation.GetOutgoingEdges(iterator.CurrentNode);
        if (edges.Count == 0)
            return null;
            
        return currentConversation.GetNodes().FirstOrDefault(n => n.id == edges[0].toNode);
    }
    
    private void HandleDialogueNode(DSP_NodeData node)
    {
        string dialogueText = "";
        string characterName = "";
        Sprite characterImage = null;
        
        if (node.values.Length > 0)
            dialogueText = node.values[0].GetValue() as string;
        
        if (node.values.Length > 1 && node.values[1].GetValue() is DSP_CharacterAsset characterAsset)
        {
            characterName = characterAsset.characterName;
            characterImage = characterAsset.characterImage;
        }
        
        if (debugMode) Debug.Log($"[DSP] Dialogue: '{dialogueText}' by {characterName}");
        FireEvent(OnDialogueNode, "OnDialogueNode", dialogueText, characterName, characterImage);
    }
    
    private void HandleChoiceNode(DSP_NodeData node)
    {
        string[] choices = null;
        
        if (node.values.Length > 0 && node.values[0].GetValue() is string[] choicesArray)
        {
            choices = choicesArray;
        }
        
        if (choices != null)
        {
            if (debugMode) Debug.Log($"[DSP] Choice node with {choices.Length} options: {string.Join(", ", choices)}");
        }
        else
        {
            if (debugMode) Debug.LogWarning("[DSP] Choice node has no valid choices array");
        }
        
        FireEvent(OnChoiceNode, "OnChoiceNode", choices);
    }
    
    private void HandleEventNode(DSP_NodeData node)
    {
        if (debugMode) Debug.Log("[DSP] Event node triggered");
        FireEvent(OnEventNode, "OnEventNode");
    }
    
    public void Advance()
    {
        if (waitingForUserInput && !IsAtChoiceNode)
        {
            if (debugMode) Debug.Log("[DSP] Advancing to next node");
            waitingForUserInput = false;
            iterator.Advance();
        }
        else if (IsAtChoiceNode)
        {
            if (debugMode) Debug.LogWarning("[DSP] Cannot use Advance() on a Choice node. Use AdvanceChoice(choiceIndex) instead.");
        }
        else
        {
            if (debugMode) Debug.LogWarning("[DSP] Cannot advance - not waiting for user input");
        }
    }
    
    public void AdvanceChoice(int choiceIndex)
    {
        if (waitingForUserInput && IsAtChoiceNode)
        {
            if (debugMode) Debug.Log($"[DSP] Advancing with choice index: {choiceIndex}");
            waitingForUserInput = false;
            iterator.Advance(choiceIndex);
        }
        else if (!IsAtChoiceNode)
        {
            if (debugMode) Debug.LogWarning("[DSP] Cannot use AdvanceChoice() on a non-Choice node. Use Advance() instead.");
        }
        else
        {
            if (debugMode) Debug.LogWarning("[DSP] Cannot advance choice - not waiting for user input");
        }
    }
    
    public void EndConversation()
    {
        if (currentConversationRoutine != null)
        {
            StopCoroutine(currentConversationRoutine);
            currentConversationRoutine = null;
        }
        
        iterator = null;
        IsConversationActive = false;
        IsAtChoiceNode = false;
        waitingForUserInput = false;
        
        if (debugMode) Debug.Log("[DSP] Conversation ended");
        FireEvent(OnConversationEnded, "OnConversationEnded");
    }
    
    // Event firing methods with debug logging
    private void FireEvent(ConversationEventHandler eventHandler, string eventName)
    {
        if (debugMode) Debug.Log($"[DSP] Firing event: {eventName} (Subscribers: {eventHandler?.GetInvocationList()?.Length ?? 0})");
        eventHandler?.Invoke();
    }
    
    private void FireEvent(DialogueEventHandler eventHandler, string eventName, string dialogue, string characterName, Sprite characterSprite)
    {
        if (debugMode) Debug.Log($"[DSP] Firing event: {eventName} (Subscribers: {eventHandler?.GetInvocationList()?.Length ?? 0})");
        eventHandler?.Invoke(dialogue, characterName, characterSprite);
    }
    
    private void FireEvent(ChoiceEventHandler eventHandler, string eventName, string[] choices)
    {
        if (debugMode) Debug.Log($"[DSP] Firing event: {eventName} (Subscribers: {eventHandler?.GetInvocationList()?.Length ?? 0})");
        eventHandler?.Invoke(choices);
    }
    
    public static DSP_ConversationManager GetInstance()
    {
        return FindObjectOfType<DSP_ConversationManager>();
    }
}