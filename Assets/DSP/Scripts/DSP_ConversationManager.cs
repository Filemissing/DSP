using UnityEngine;
using System.Collections;
using System.Linq;

public class DSP_ConversationManager : MonoBehaviour
{
    public static DSP_ConversationManager instance;
    public void Awake()
    {
        if (instance != null && instance != this)
            Destroy(this);
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public DSP_ConversationGraphAsset currentConversation;
    
    [Header("Settings")]
    public DSP_SettingsObject settings;
    
    public DSP_ConversationIterator iterator;
    private Coroutine currentConversationRoutine;
    [SerializeField] private bool waitingForUserInput = false;
    
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
            return;
        }
        
        if (IsConversationActive)
        {
            EndConversation();
        }
        
        currentConversation = conversation;
        iterator = new DSP_ConversationIterator(currentConversation);
        IsConversationActive = true;
        IsAtChoiceNode = false;
        waitingForUserInput = false;
        
        OnConversationStarted?.Invoke();
        
        if (currentConversationRoutine != null)
            StopCoroutine(currentConversationRoutine);
        
        currentConversationRoutine = StartCoroutine(ProcessConversation());
    }
    
    private IEnumerator ProcessConversation()
    {
        while (iterator != null && iterator.State != DSP_IteratorState.Finished)
        {
            var currentNode = iterator.CurrentNode;
            
            switch (currentNode.nodeType)
            {
                case DSP_NodeType.Dialogue:
                    IsAtChoiceNode = false;
                    waitingForUserInput = true;
                    HandleDialogueNode(currentNode);
                    
                    DSP_NodeData nextNode = PeekNextNode();
                    if (nextNode != null && nextNode.nodeType == DSP_NodeType.Choice)
                    {
                        waitingForUserInput = false;
                        iterator.Advance();
                    }
                    else
                    {
                        yield return new WaitUntil(() => !waitingForUserInput);
                    }
                    break;
                    
                case DSP_NodeType.Choice:
                    IsAtChoiceNode = true;
                    waitingForUserInput = true;
                    HandleChoiceNode(currentNode);
                    yield return new WaitUntil(() => !waitingForUserInput);
                    IsAtChoiceNode = false;
                    break;
                    
                default:
                    IsAtChoiceNode = false;
                    waitingForUserInput = false;
                    iterator.Advance();
                    break;
            }
            
            if (iterator.State == DSP_IteratorState.Finished)
            {
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
        
        OnDialogueNode?.Invoke(dialogueText, characterName, characterImage);
    }
    
    private void HandleChoiceNode(DSP_NodeData node)
    {
        string[] choices = null;
        
        if (node.values.Length > 0 && node.values[0].GetValue() is string[] choicesArray)
        {
            choices = choicesArray;
        }
        
        OnChoiceNode?.Invoke(choices);
    }
    
    public void Advance()
    {
        if (waitingForUserInput && !IsAtChoiceNode)
        {
            waitingForUserInput = false;
            iterator.Advance();
        }
    }
    
    public void AdvanceChoice(int choiceIndex)
    {
        if (waitingForUserInput && IsAtChoiceNode)
        {
            waitingForUserInput = false;
            iterator.Advance(choiceIndex);
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
        
        OnConversationEnded?.Invoke();
    }
    
    public static DSP_ConversationManager GetInstance()
    {
        return FindObjectOfType<DSP_ConversationManager>();
    }
}