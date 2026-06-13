using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using Image = UnityEngine.UIElements.Image;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

public class DSP_EditorWindow : EditorWindow
{
    public static DSP_EditorWindow window;
    public static DSP_ConversationGraphAsset dialogueGraphAsset;

    public static void Open(DSP_ConversationGraphAsset graphAsset)
    {
        window = GetWindow<DSP_EditorWindow>();
        window.titleContent = new GUIContent("Dialogue Graph Editor");
        dialogueGraphAsset = graphAsset;

        graphView.LoadFromAsset(dialogueGraphAsset);
    }

    public static DSP_NodeGraphView graphView;
    static Toolbar toolbar;

    List<DSP_NodeData> copiedNodes = new();

    private void OnEnable()
    {
        // set up Node Graph element
        graphView = new DSP_NodeGraphView() { name = "Node Graph" };
        
        // set up ToolBar
        toolbar = new Toolbar();
        
        // save button
        ToolbarButton saveButton = new ToolbarButton(() => graphView.SaveToAsset(dialogueGraphAsset));
        saveButton.iconImage = Background.FromTexture2D(EditorGUIUtility.IconContent("SaveActive").image as Texture2D);
        saveButton.tooltip = "Save";

        // add elements in correct order
        toolbar.Add(saveButton);
        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(graphView);

        // setup callbacks
        graphView.graphViewChanged = OnGraphViewChanged;

        Undo.undoRedoPerformed += OnUndoRedo;

        rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
        {
            if ((evt.modifiers & EventModifiers.Control) != 0)
            {
                switch(evt.keyCode)
                {
                    case KeyCode.S:
                        graphView.SaveToAsset(dialogueGraphAsset);

                        evt.StopPropagation();
                        break;

                    case KeyCode.C:
                        Copy();
                        evt.StopPropagation();
                        break;

                    case KeyCode.V:
                        Paste();
                        evt.StopPropagation();
                        break;

                    case KeyCode.X:
                        Copy();
                        foreach (Node node in graphView.selection.OfType<Node>().ToList())
                            node.parent.Remove(node);
                        evt.StopPropagation();
                        break;

                    case KeyCode.D:
                        Copy();
                        Paste();
                        evt.StopPropagation();
                        break;
                }
            }
        });

        
    }
    GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (change.elementsToRemove != null || change.edgesToCreate != null || change.movedElements != null)
        {
            RecordChange("Graph Edit");
            EditorApplication.delayCall += () =>
            {
                graphView.SaveToAsset(dialogueGraphAsset); // delayed so removal can complete
            };
        }
        return change;
    }
    public void RecordChange(string label)
    {
        Undo.RegisterCompleteObjectUndo(dialogueGraphAsset, label);
    }
    void OnUndoRedo()
    {
        graphView.LoadFromAsset(dialogueGraphAsset);
    }

    void Copy()
    {
        copiedNodes.Clear();
        foreach (Node node in graphView.selection.OfType<Node>().ToList())
        {
            if ((node.capabilities & Capabilities.Deletable) != 0)
                copiedNodes.Add(graphView.SaveNode(node));
        }
    }
    void Paste()
    {
        graphView.SaveToAsset(dialogueGraphAsset);

        foreach (DSP_NodeData data in copiedNodes)
        {
            data.position += new Vector2(40, 40);

            Node newNode = graphView.CreateNodeInstance(data);
            graphView.AddElement(newNode);
        }

        RecordChange("Paste");
        graphView.SaveToAsset(dialogueGraphAsset);
    }
}

public class DSP_NodeGraphView : GraphView
{
    private bool isUpdating;

    public DSP_NodeGraphView()
    {
        style.flexGrow = 1;

        ContentZoomer contentZoomer = new ContentZoomer { minScale = 0.05f, maxScale = 5f };
        this.AddManipulator(contentZoomer);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        EditorApplication.update += OnEditorUpdate;
    }
    public void LoadFromAsset(DSP_ConversationGraphAsset graphAsset)
    {
        foreach (Node node in nodes) RemoveElement(node);
        foreach (Edge edge in edges) RemoveElement(edge);

        var grid = new GridBackground();
        Insert(0, grid);

        schedule.Execute(() =>
        {
            // if this is a new/empty asset (e.g. start and end nodes aren't accounted for)
            if (graphAsset.GetNodeCount() < 2)
            {
                AddElement(new DSP_StartNode(contentViewContainer.contentRect.center + new Vector2(-200, 300)));
                AddElement(new DSP_EndNode(contentViewContainer.contentRect.center + new Vector2(200, 300)));
            }

            // instantiate nodes
            foreach (DSP_NodeData nodeData in graphAsset.GetNodes())
                AddElement(CreateNodeInstance(nodeData));

            // restore edges
            foreach (DSP_EdgeData edgeData in graphAsset.GetAllEdges())
            {
                Node fromNode = nodes.ElementAt(edgeData.fromNode);
                Node toNode = nodes.ElementAt(edgeData.toNode);

                Port outPort = fromNode.GetPorts(Direction.Output).ElementAt(edgeData.outPortID);
                Port inPort = toNode.GetPorts(Direction.Input).ElementAt(edgeData.inPortID);

                Edge edge = outPort.ConnectTo(inPort);
                AddElement(edge);
            }
        });
    }
    public Node CreateNodeInstance(DSP_NodeData data)
    {
        // handle Event and Condition nodes separately since they require special handling
        if (data.nodeType == DSP_NodeType.StaticEvent)
        {
            DSP_StaticEventNode eventNode = new DSP_StaticEventNode(data.position, data.values, data.eventParameters);
            return eventNode;
        }
        if (data.nodeType == DSP_NodeType.Condition)
        {
            DSP_ConditionNode conditionNode = new DSP_ConditionNode(data.position, data.values, data.eventParameters);
            return conditionNode;
        }
        if (data.nodeType == DSP_NodeType.Choice)
        {
            DSP_ChoiceNode choiceNode = new DSP_ChoiceNode(data.position, data.values, data.eventParameters);
            return choiceNode;
        }

        Type nodeType = data.nodeType switch
        {
            DSP_NodeType.Start => typeof(DSP_StartNode),
            DSP_NodeType.End => typeof(DSP_EndNode),
            DSP_NodeType.Dialogue => typeof(DSP_DialogueNode),
            DSP_NodeType.SceneEvent => typeof(DSP_SceneEventNode),
            _ => throw new Exception("Unknown node type")
        };

        Node node = (Node)Activator.CreateInstance(nodeType, data.position, data.values);
        return node;
    }
    public void SaveToAsset(DSP_ConversationGraphAsset graphAsset)
    {
        graphAsset.Clear();

        // save node data
        foreach (Node node in nodes)
            graphAsset.AddNode(SaveNode(node));

        // save edge data
        foreach (Edge edge in edges)
        {
            Node fromNode = edge.output.node;
            Node toNode = edge.input.node;

            int fromNodeIndex = nodes.ToList().FindIndex(n => n == fromNode);
            int toNodeIndex = nodes.ToList().FindIndex(n => n == toNode);
            int outPortID = fromNode.GetPorts(Direction.Output).ToList().FindIndex(p => p == edge.output);
            int inPortID = toNode.GetPorts(Direction.Input).ToList().FindIndex(p => p == edge.input);

            graphAsset.AddEdge(new DSP_EdgeData
            {
                fromNode = fromNodeIndex,
                outPortID = outPortID,
                toNode = toNodeIndex,
                inPortID = inPortID
            });
        }

        EditorUtility.SetDirty(graphAsset);
        AssetDatabase.SaveAssets();
    }
    public DSP_NodeData SaveNode(Node node)
    {
        DSP_NodeData data = null;

        if (node is DSP_StartNode)
        {
            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.Start,
                position = node.GetPosition().position,
                values = new SerializableValue[] { }
            };
        }
        else if (node is DSP_EndNode endNode)
        {
            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.End,
                position = node.GetPosition().position,
                values = new SerializableValue[] { new(node.inputContainer.childCount) }
            };
        }
        else if (node is DSP_DialogueNode dialogueNode)
        {
            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.Dialogue,
                position = node.GetPosition().position,
                values = new SerializableValue[]
                {
                        new(dialogueNode.dialogueText),
                        new(dialogueNode.characterAsset),
                        new(dialogueNode.dialogueAudioClip)
                }
            };
        }
        else if (node is DSP_ChoiceNode choiceNode)
        {
            string[] methodkeys = new string[choiceNode.choices.Count];
            SerializableValue[] parameters = new SerializableValue[methodkeys.Length];
            for (int i = 0; i < choiceNode.finalConditions.Count; i++)
            {
                SerializableCondition condition = choiceNode.finalConditions[i];
                if (condition != null)
                {
                    methodkeys[i] = condition.isStatic
                        ? $"Static/{BuildSignature(condition)}"
                        : $"{condition?.GetType().Name}/{BuildSignature(condition)}";

                    parameters[i] = condition.parameter;
                }
            }

            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.Choice,
                position = node.GetPosition().position,
                values = new SerializableValue[]
                {
                        new(choiceNode.choices.Select(c => c.Item1.value).ToArray()),
                        new(choiceNode.conditionalStates.ToArray()),
                        new(choiceNode.assignedObjects.ToArray()),
                        new(methodkeys)
                },
                eventParameters = parameters,
                finalConditions = choiceNode.finalConditions.ToArray()
            };
        }
        else if (node is DSP_StaticEventNode staticEventNode)
        {
            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.StaticEvent,
                position = node.GetPosition().position,
                values = new SerializableValue[]
                {
                        new(staticEventNode.rowCount),
                        // read directly from tracked references, not finalActions
                        new(staticEventNode.assignedObjects.Select(o => o).ToArray()),
                        new(staticEventNode.finalActions.Select(a =>
                        {
                            if (a == null) return null;
                            if (a.isStatic)
                            {
                                Type t = Type.GetType(a.staticTypeName);
                                MethodInfo m = t?.GetMethod(a.methodName, BindingFlags.Static | BindingFlags.Public);
                                string sig = m != null ? BuildSignature(m) : $"{a.methodName}()";
                                return $"Static/{sig}";
                            }
                            else
                            {
                                MethodInfo m = a.target?.GetType().GetMethod(a.methodName,
                                    BindingFlags.Instance | BindingFlags.Public);
                                string sig = m != null ? BuildSignature(m) : $"{a.methodName}()";
                                return $"{a.target?.GetType().Name}/{sig}";
                            }
                        }).ToArray())
                },
                eventParameters = staticEventNode.parameters.Select(p => new SerializableValue(p)).ToArray(),
                finalEvents = staticEventNode.finalActions.ToArray()
            };
        }
        else if (node is DSP_SceneEventNode sceneEventNode)
        {
            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.SceneEvent,
                position = node.GetPosition().position,
                values = new SerializableValue[]
                {
                        new(sceneEventNode.eventAsset)
                }
            };
        }
        else if (node is DSP_ConditionNode conditionNode)
        {
            string methodKey = null;
            if (conditionNode.finalCondition != null)
            {
                methodKey = conditionNode.finalCondition.isStatic
                    ? $"Static/{BuildSignature(conditionNode.finalCondition)}"
                    : $"{conditionNode.assignedObject?.GetType().Name}/{BuildSignature(conditionNode.finalCondition)}";
            }

            data = new DSP_NodeData
            {
                id = nodes.ToList().FindIndex(n => n == node),
                nodeType = DSP_NodeType.Condition,
                position = node.GetPosition().position,
                values = new SerializableValue[]
                {
                        new(conditionNode.assignedObject),
                        new(methodKey)
                },
                eventParameters = conditionNode.finalCondition?.parameter != null
                    ? new SerializableValue[] { conditionNode.finalCondition.parameter }
                    : null,
                finalConditions = new SerializableCondition[] { conditionNode.finalCondition }
            };
        }

        return data;
    }
    string BuildSignature(MethodInfo method)
    {
        string paramList = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({paramList})";
    }
    string BuildSignature(SerializableCondition condition)
    {
        if (condition.isStatic)
        {
            Type t = Type.GetType(condition.staticTypeName);
            MethodInfo m = t?.GetMethod(condition.methodName, BindingFlags.Static | BindingFlags.Public);
            return m != null ? BuildSignature(m) : $"{condition.methodName}()";
        }
        else
        {
            MethodInfo m = condition.target?.GetType().GetMethod(condition.methodName, BindingFlags.Instance | BindingFlags.Public);
            return m != null ? BuildSignature(m) : $"{condition.methodName}()";
        }
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        Vector2 mousePos = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);

        evt.menu.AppendAction("Dialogue Node", (a) => AddNodeWithUndo(new DSP_DialogueNode(mousePos)));
        evt.menu.AppendAction("Choice Node", (a) => AddNodeWithUndo(new DSP_ChoiceNode(mousePos)));
        evt.menu.AppendAction("Static Event Node", (a) => AddNodeWithUndo(new DSP_StaticEventNode(mousePos)));
        evt.menu.AppendAction("Scene Event Node", (a) => AddNodeWithUndo(new DSP_SceneEventNode(mousePos)));
        evt.menu.AppendAction("Condition Node", (a) => AddNodeWithUndo(new DSP_ConditionNode(mousePos)));
    }
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach((port) =>
        {
            bool isNotSamePort = port != startPort;
            bool isNotSameNode = port.node != startPort.node;
            bool isOppositeDirection = port.direction != startPort.direction;

            if (isNotSamePort && isNotSameNode && isOppositeDirection)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }

    void AddNodeWithUndo(Node node)
    {
        SaveToAsset(DSP_EditorWindow.dialogueGraphAsset);
        DSP_EditorWindow.window.RecordChange("Add Node");
        AddElement(node);

        EditorApplication.delayCall += () =>
        {
            SaveToAsset(DSP_EditorWindow.dialogueGraphAsset); // delayed so constructor can complete
        };
    }

    private void OnEditorUpdate()
    {
        foreach (var element in nodes)
        {
            if (element is DSP_DialogueNode node)
            {
                node.UpdateCharacterPreview();
            }
        }
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }
}

public static class DSP_NodeExtensions
{
    public static Port GeneratePort(this Node node, Direction direction, Port.Capacity capacity = Port.Capacity.Single, string portName = "", System.Type type = null)
    {
        type ??= typeof(float); // fallback to float if no type provided

        var port = node.InstantiatePort(Orientation.Horizontal, direction, capacity, type);
        port.portName = portName;
        return port;
    }
    public static void AddInputPort(this Node node, string name = "In", Port.Capacity capacity = Port.Capacity.Multi, Type type = null)
    {
        var port = node.GeneratePort(Direction.Input, capacity, name, type);
        node.inputContainer.Add(port);
    }
    public static void AddOutputPort(this Node node, string name = "Out", Port.Capacity capacity = Port.Capacity.Single, Type type = null)
    {
        var port = node.GeneratePort(Direction.Output, capacity, name, type);
        node.outputContainer.Add(port);
    }
    public static Port[] GetPorts(this Node node, Direction direction)
    {
        switch (direction)
        {
            case Direction.Input:
                return node.inputContainer.Children().Cast<Port>().ToArray();
            case Direction.Output:
                if (node is DSP_ChoiceNode choiceNode)
                    return choiceNode.choices.Select(c => c.Item2).ToArray();
                else
                    return node.outputContainer.Children().Cast<Port>().ToArray();
            default:
                return null;
        }
    }

    public static void FixTransparency(this Node node)
    {
        node.mainContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    }

    public static void StartUndoRecord(this Node node)
    {
        DSP_EditorWindow.graphView.SaveToAsset(DSP_EditorWindow.dialogueGraphAsset);
    }
    public static void EndUndoRecord(this Node node, string label = "node changed")
    {
        DSP_EditorWindow.window.RecordChange(label);
        DSP_EditorWindow.graphView.SaveToAsset(DSP_EditorWindow.dialogueGraphAsset);
    }
}

public class DSP_StartNode : Node
{
    public DSP_StartNode(Vector2 position, SerializableValue[] values = null)
    {
        title = "Start";
        this.FixTransparency();

        this.AddOutputPort();

        capabilities &= ~Capabilities.Deletable;

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }
}
public class DSP_EndNode : Node
{
    public DSP_EndNode(Vector2 position, SerializableValue[] values = null)
    {
        title = "End";
        this.FixTransparency();

        if (values != null && values.Length > 0)
        {
            for (int i = 0; i < (int)values[0].GetValue(); i++)
            {
                this.AddInputPort();
            } 
        }
        else
            this.AddInputPort();

        // buttons
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;

        Button removeButton = new Button(() => RemoveInput())
        {
            text = "-"
        };
        Button addButton = new Button(() => AddInput())
        {
            text = "+"
        };

        buttonContainer.Add(removeButton);
        buttonContainer.Add(addButton);

        mainContainer.Add(buttonContainer);

        capabilities &= ~Capabilities.Deletable;

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }
    void AddInput()
    {
        this.AddInputPort();
    }
    void RemoveInput()
    {
        if (inputContainer.childCount <= 1)
            return;

        Port port = inputContainer.Children().ElementAt(inputContainer.childCount - 1) as Port;
        port.DisconnectAll();
        inputContainer.Remove(port);
    }
}
public class DSP_DialogueNode : Node
{
    public DSP_CharacterAsset characterAsset;
    public string dialogueText;
    public AudioClip dialogueAudioClip;

    private Image characterPreviewImage;
    private Label characterPreviewLabel;
    private VisualElement previewContainer;
    private static Texture2D placeholderPortrait;

    private bool hasTextChanged = false;

    public DSP_DialogueNode(Vector2 position, SerializableValue[] values = null)
    {
        // load values if they exist
        if (values != null && values.Length > 0)
        {
            dialogueText = values[0].GetValue() as string;
            
            if (values.Length > 1)
                characterAsset = values[1].GetValue() as DSP_CharacterAsset;
            
            if (values.Length > 2)
                dialogueAudioClip = values[2].GetValue() as AudioClip;
        }

        title = "Dialogue";
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        VisualElement characterContainer = new VisualElement();
        characterContainer.style.flexDirection = FlexDirection.Row;
        characterContainer.style.flexGrow = 1;
        characterContainer.style.flexShrink = 1;
        characterContainer.style.alignItems = Align.Center;

        // Character label
        var characterLabel = new Label("Character");
        characterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        characterLabel.style.paddingLeft = 2;
        
        characterContainer.Add(characterLabel);
        
        // CharacterField
        ObjectField characterField = new ObjectField()
        {
            objectType = typeof(DSP_CharacterAsset),
            allowSceneObjects = false,
            value = characterAsset
        };

        characterField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();

            characterAsset = evt.newValue as DSP_CharacterAsset;
            UpdateCharacterPreview();

            this.EndUndoRecord();
        });
        characterContainer.Add(characterField);

        mainContainer.Add(characterContainer);

        // Preview container (hidden if characterField is null)
        previewContainer = new VisualElement();
        previewContainer.style.flexDirection = FlexDirection.Row;
        previewContainer.style.alignItems = Align.Center;
        
        // Portrait image
        characterPreviewImage = new Image();
        characterPreviewImage.style.width = 12;
        characterPreviewImage.style.height = 12;
        characterPreviewImage.style.marginLeft = 4;
        characterPreviewImage.style.marginRight = 2;

        characterPreviewImage.image = GetPlaceholder();
        
        // Name label
        characterPreviewLabel = new Label();
        characterPreviewLabel.style.fontSize = 11;
        
        previewContainer.Add(characterPreviewImage);
        previewContainer.Add(characterPreviewLabel);
        
        mainContainer.Add(previewContainer);

        Label separator = new Label();
        separator.style.height = 10;
        mainContainer.Add(separator);

        VisualElement audioContainer = new VisualElement();
        audioContainer.style.flexDirection = FlexDirection.Row;
        audioContainer.style.flexGrow = 1;
        audioContainer.style.flexShrink = 1;
        audioContainer.style.alignItems = Align.Center;

        // Audio clip field
        var audioLabel = new Label("Audio clip");
        audioLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        audioLabel.style.paddingLeft = 2;
        audioContainer.Add(audioLabel);

        ObjectField audioField = new ObjectField()
        {
            objectType = typeof(AudioClip),
            allowSceneObjects = false,
            value = dialogueAudioClip
        };
        audioField.style.alignSelf = Align.FlexEnd;
        audioField.RegisterValueChangedCallback(evt => 
        {
            this.StartUndoRecord();

            dialogueAudioClip = evt.newValue as AudioClip;

            this.EndUndoRecord();
        });
        audioContainer.Add(audioField);

        mainContainer.Add(audioContainer);

        Label separator2 = new Label();
        separator2.style.height = 5;
        mainContainer.Add(separator2);

        // Dialogue label
        var textLabel = new Label("Text");
        textLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        textLabel.style.paddingTop = 2;
        textLabel.style.paddingBottom = 2;
        textLabel.style.paddingLeft = 2;
        
        mainContainer.Add(textLabel);

        // TextField without built-in label
        var textField = new TextField
        {
            value = dialogueText,
            multiline = true
        };
        textField.style.flexWrap = Wrap.Wrap;

        textField.RegisterValueChangedCallback(evt => { dialogueText = evt.newValue; hasTextChanged = true; } );
        textField.RegisterCallback<FocusInEvent>(evt => { this.StartUndoRecord(); hasTextChanged = false; } );
        textField.RegisterCallback<FocusOutEvent>(evt => { if (hasTextChanged) { this.EndUndoRecord(); } } );

        mainContainer.Add(textField);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
        UpdateCharacterPreview();
    }

    public void UpdateCharacterPreview()
    {
        if (characterAsset != null)
        {
            previewContainer.style.display = DisplayStyle.Flex;

            var tex = characterAsset.characterImage != null ? characterAsset.characterImage.texture : GetPlaceholder();
            
            characterPreviewImage.image = tex;
            characterPreviewLabel.text  = characterAsset.characterName;
        }
        else
        {
            previewContainer.style.display = DisplayStyle.None;

            characterPreviewImage.image = GetPlaceholder();
            characterPreviewLabel.text = "";
        }
        
        characterPreviewImage.MarkDirtyRepaint();
    }

    private static Texture2D GetPlaceholder()
    {
        if (placeholderPortrait == null)
        {
            placeholderPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("placeholderPortrait t:Texture2D").First()));
        }

        return placeholderPortrait;
    }
}
public class DSP_ChoiceNode : Node
{
    public List<(TextField, Port)> choices = new();

    public List<bool> conditionalStates = new();

    private List<Dictionary<string, (MethodInfo method, Object target)>> methodMaps = new();
    public List<SerializableCondition> finalConditions = new();
    public List<Object> assignedObjects = new();

    public bool hasTextChanged = false;

    public DSP_ChoiceNode(Vector2 position, SerializableValue[] values = null, SerializableValue[] parameters = null)
    {
        title = "Choice";
        this.FixTransparency();

        this.AddInputPort();

        // choice container
        VisualElement choiceContainer = new VisualElement();
        choiceContainer.style.flexDirection = FlexDirection.Column;


        if (values != null && values.Length > 0)
        {
            string[] choiceTexts = values[0].GetValue() as string[];
            bool[] conditionalStates = values.Length > 1 ? values[1].GetValue() as bool[] : null;
            Object[] savedObjects = values.Length > 2 ? values[2].GetValue() as Object[] : null;
            string[] savedMethods = values.Length > 3 ? values[3].GetValue() as string[] : null;
            SerializableValue[] savedParams = parameters.Length > 0 ? parameters : null;

            for (int i = 0; i < choiceTexts.Length; i++)
            {
                int index = i; // capture i

                string choiceText = choiceTexts[i];
                bool conditionalState = conditionalStates != null ? conditionalStates[i] : false;
                Object savedObject = savedObjects != null ? savedObjects[i] : null;
                string savedMethod = savedMethods != null ? savedMethods[i] : null;
                object savedParam = savedParams != null ? savedParams[i]?.GetValue() : null;

                AddChoice(choiceContainer, choiceText, conditionalState, savedObject, savedMethod, savedParam);
            }
        }
        else
        {
            AddChoice(choiceContainer);
            AddChoice(choiceContainer);
        }

        mainContainer.Add(choiceContainer);

        // buttons
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;

        Button removeButton = new Button(() =>
        {
            this.StartUndoRecord();
            RemoveChoice(choiceContainer);
            this.EndUndoRecord();
        })
        {
            text = "-"
        };
        Button addButton = new Button(() =>
        {
            this.StartUndoRecord();
            AddChoice(choiceContainer);
            this.EndUndoRecord();
        })
        {
            text = "+"
        };

        buttonContainer.Add(removeButton);
        buttonContainer.Add(addButton);

        mainContainer.Add(buttonContainer);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }

    void AddChoice(VisualElement container, string text = null, bool conditional = false, Object savedObject = null, string savedMethod = null, object savedParam = null)
    {
        int index = choices.Count;

        VisualElement chunk = new VisualElement();
        chunk.style.flexDirection = FlexDirection.Column;
        chunk.style.alignContent = Align.Center;
        chunk.style.width = new StyleLength(StyleKeyword.Auto);
        chunk.style.flexGrow = 1;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignContent = Align.Center;
        row.style.width = new StyleLength(StyleKeyword.Auto);
        row.style.flexGrow = 1;

        // conditional toggle
        Toggle conditionalToggle = new Toggle();
        conditionalToggle.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            ToggleConditional(index, chunk, evt.newValue);
            this.EndUndoRecord();
        });

        row.Add(conditionalToggle);

        // text field
        TextField textField = new TextField()
        {
            value = text == null ? $"Option {index}" : text,
            multiline = true
        };
        textField.style.marginRight = 5;
        textField.style.flexGrow = 1;

        textField.RegisterValueChangedCallback(evt => { hasTextChanged = true; });
        textField.RegisterCallback<FocusInEvent>(evt => { this.StartUndoRecord(); hasTextChanged = false; });
        textField.RegisterCallback<FocusOutEvent>(evt => { if (hasTextChanged) { this.EndUndoRecord(); } });

        row.Add(textField);

        // port
        Port port = this.GeneratePort(Direction.Output, Port.Capacity.Single);

        row.Add(port);

        chunk.Add(row);

        // add to list
        choices.Add((textField, port));

        // grow lists
        conditionalStates.Add(false);
        methodMaps.Add(new());
        finalConditions.Add(null);
        assignedObjects.Add(null);

        // restore saved data
        conditionalToggle.value = conditional;
        ToggleConditional(index, chunk, conditional, savedObject, savedMethod, savedParam);

        container.Add(chunk);
    }
    void RemoveChoice(VisualElement container)
    {
        if (choices.Count <= 2) return;

        (TextField, Port) choice = choices.Last();

        // clear connections
        Port port = choice.Item2;
        port.DisconnectAll();

        // remove visual element
        container.RemoveAt(container.childCount - 1);

        // remove from list
        choices.RemoveAt(choices.Count - 1);

        // shrink other lists
        methodMaps.RemoveAt(methodMaps.Count - 1);
        finalConditions.RemoveAt(finalConditions.Count - 1);
        assignedObjects.RemoveAt(assignedObjects.Count - 1);
    }

    void ToggleConditional(int index, VisualElement choiceChunk, bool isConditional, Object savedObject = null, string savedMethod = null, object savedParam = null)
    {
        conditionalStates[index] = isConditional;

        if (isConditional || savedObject != null) // add conditional field below choice text
        {
            if (choiceChunk.childCount > 1)
            {
                choiceChunk.Children().ElementAt(choiceChunk.childCount - 1).SetEnabled(true);
                return;
            }

            VisualElement conditionChunk = new VisualElement();
            conditionChunk.style.flexDirection = FlexDirection.Column;
            conditionChunk.style.alignContent = Align.Center;
            conditionChunk.style.width = new StyleLength(StyleKeyword.Auto);
            conditionChunk.style.flexGrow = 1;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignContent = Align.Center;
            row.style.width = new StyleLength(StyleKeyword.Auto);
            row.style.flexGrow = 1;

            ObjectField objectField = new ObjectField();
            objectField.allowSceneObjects = false;
            objectField.style.width = 100;
            objectField.objectType = typeof(Object);

            DropdownField dropdownField = new DropdownField(new List<string> { "No Function" }, 0);
            dropdownField.style.flexGrow = 1;

            objectField.RegisterValueChangedCallback(evt =>
            {
                this.StartUndoRecord();
                OnObjectAssigned(index, evt.newValue, dropdownField);
                this.EndUndoRecord();
            });
            dropdownField.RegisterValueChangedCallback(evt =>
            {
                this.StartUndoRecord();
                OnMenuValueChanged(index, evt.newValue, conditionChunk);
                this.EndUndoRecord();
            });

            row.Add(objectField);
            row.Add(dropdownField);

            conditionChunk.Add(row);

            // restore saved data
            if (savedObject != null)
            {
                objectField.value = savedObject;
                OnObjectAssigned(index, savedObject, dropdownField);

                if (!string.IsNullOrEmpty(savedMethod))
                {
                    dropdownField.value = savedMethod;
                    OnMenuValueChanged(index, savedMethod, conditionChunk, savedParam);
                }
            }

            choiceChunk.Add(conditionChunk);
        }

        if (!isConditional) // remove or disable condition field
        {
            if (choiceChunk.childCount > 1)
            {
                if (assignedObjects[index] == null)
                    choiceChunk.RemoveAt(choiceChunk.childCount - 1);
                else
                    choiceChunk.Children().ElementAt(choiceChunk.childCount - 1).SetEnabled(false);
            }
        }
    }

    void OnObjectAssigned(int index, Object obj, DropdownField menu)
    {
        assignedObjects[index] = obj;

        menu.choices.Clear();
        menu.choices.Add("No Function");
        menu.value = "No Function";
        methodMaps[index].Clear();

        if (obj == null) return;

        Type type = null;
        Object instanceTarget = null;

        if (obj is MonoScript monoScript)
        {
            type = monoScript.GetClass();
        }
        else if (obj is ScriptableObject so)
        {
            type = so.GetType();
            instanceTarget = so;
        }
        else
        {
            type = obj.GetType();
        }

        if (type == null) return;

        // Instance methods (ScriptableObject only)
        if (instanceTarget != null)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!IsValidConditionMethod(method)) continue;

                string key = $"{type.Name}/{BuildSignature(method)}";
                menu.choices.Add(key);
                methodMaps[index][key] = (method, instanceTarget);
            }
        }

        // Static methods
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (!IsValidConditionMethod(method)) continue;

            string key = $"Static/{BuildSignature(method)}";
            menu.choices.Add(key);
            methodMaps[index][key] = (method, null);
        }
    }
    void OnMenuValueChanged(int index, string optionName, VisualElement chunk, object savedParam = null)
    {
        var oldRow = chunk.Children().FirstOrDefault(c => c.name == "ParamRow_0");
        if (oldRow != null) chunk.Remove(oldRow);

        if (optionName == "No Function" || !methodMaps[index].ContainsKey(optionName))
        {
            finalConditions[index] = null;
            return;
        }

        var (method, instanceTarget) = methodMaps[index][optionName];
        bool isStatic = instanceTarget == null;

        ParameterInfo[] methodParams = method.GetParameters();

        if (methodParams.Length == 0)
        {
            finalConditions[index] = isStatic
                ? new SerializableCondition(method.DeclaringType, method.Name)
                : new SerializableCondition(instanceTarget, method.Name);
            finalConditions[index].hasValue = true;
            return;
        }

        // Build parameter field
        Type paramType = methodParams[0].ParameterType;

        VisualElement paramRow = new VisualElement();
        paramRow.name = "ParamRow_0";
        paramRow.style.flexDirection = FlexDirection.Row;
        paramRow.style.flexGrow = 1;

        void Commit(object value)
        {
            var sv = new SerializableValue(value);
            finalConditions[index] = isStatic
                ? new SerializableCondition(method.DeclaringType, method.Name, sv)
                : new SerializableCondition(instanceTarget, method.Name, sv);

            finalConditions[index].hasValue = true;
        }

        if (typeof(Object).IsAssignableFrom(paramType))
        {
            var field = new ObjectField { allowSceneObjects = false, objectType = paramType };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = savedParam as Object;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(string))
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (string)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(int))
        {
            var field = new IntegerField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (int)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(float))
        {
            var field = new FloatField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (float)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(bool))
        {
            var field = new Toggle();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (bool)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }

        chunk.Add(paramRow);
    }

    bool IsValidConditionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.ReturnType != typeof(bool)) return false;
        var methodParams = method.GetParameters();
        if (methodParams.Length > 1) return false;
        if (methodParams.Length == 1 && !IsSerializableType(methodParams[0].ParameterType)) return false;
        return true;
    }

    bool IsSerializableType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) ||
               typeof(Object).IsAssignableFrom(type);
    }

    string BuildSignature(MethodInfo method)
    {
        string paramList = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({paramList})";
    }
}
public class DSP_StaticEventNode : Node
{
    // Per-row data needed to resolve the dropdown selection back to a MethodInfo
    // Key: "GroupName/MethodSignature" string → (MethodInfo, instance target or null if static)
    private List<Dictionary<string, (MethodInfo method, Object target)>> _rowMethodMaps = new();

    public List<SerializableEvent> finalActions = new();
    public List<Object> assignedObjects = new();
    public List<object> parameters = new();

    public int rowCount = 0;

    public DSP_StaticEventNode(Vector2 position, SerializableValue[] values = null, SerializableValue[] parameters = null)
    {
        title = "Static Event";
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        VisualElement eventContainer = new VisualElement();
        eventContainer.style.flexDirection = FlexDirection.Column;

        if (values != null && values.Length >= 3)
        {
            int savedRowCount = (int)values[0].GetValue();
            Object[] objects = values[1].objectArray;
            string[] methods = (string[])values[2].GetValue();

            for (int i = 0; i < savedRowCount; i++)
            {
                Object assignedObject = i < objects.Length ? objects[i] : null;
                string chosenMethod = i < methods.Length ? methods[i] : null;
                object parameter = (parameters != null && i < parameters.Length) ? parameters[i].GetValue() : null;

                AddEventRow(eventContainer, assignedObject, chosenMethod, parameter);
            }
        }
        else
        {
            AddEventRow(eventContainer);
        }

        mainContainer.Add(eventContainer);

        // +/- buttons
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.Add(new Button(() => { this.StartUndoRecord(); RemoveEventRow(eventContainer); this.EndUndoRecord(); }) { text = "-" });
        buttonContainer.Add(new Button(() => { this.StartUndoRecord(); AddEventRow(eventContainer); this.EndUndoRecord(); }) { text = "+" });
        mainContainer.Add(buttonContainer);

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }

    void AddEventRow(VisualElement container, Object assignedObject = null, string chosenMethod = null, object parameter = null)
    {
        int index = rowCount;
        _rowMethodMaps.Add(new Dictionary<string, (MethodInfo, Object)>());

        VisualElement lines = new VisualElement();
        lines.style.flexDirection = FlexDirection.Column;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;

        ObjectField objectField = new ObjectField();
        objectField.allowSceneObjects = false;
        objectField.style.width = 100;
        // Accept both MonoScript and ScriptableObject
        objectField.objectType = typeof(Object);

        DropdownField dropdownField = new DropdownField(new List<string> { "No Function" }, 0);
        dropdownField.style.flexGrow = 1;

        // Extend assignedObjects list alongside _rowMethodMaps
        while (assignedObjects.Count <= index) assignedObjects.Add(null);
        objectField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            assignedObjects[index] = evt.newValue;
            OnObjectAssigned(evt.newValue, dropdownField, index);
            this.EndUndoRecord();
        });
        dropdownField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            OnMenuValueChanged(evt.newValue, lines, index);
            this.EndUndoRecord();
        });

        row.Add(objectField);
        row.Add(dropdownField);
        lines.Add(row);
        container.Add(lines);

        // restore saved state
        if (assignedObject != null)
        {
            assignedObjects[index] = assignedObject;
            objectField.value = assignedObject;
            OnObjectAssigned(assignedObject, dropdownField, index);

            if (!string.IsNullOrEmpty(chosenMethod))
            {
                dropdownField.value = chosenMethod;
                OnMenuValueChanged(chosenMethod, lines, index, parameter);
            }
        }

        rowCount++;
    }
    void RemoveEventRow(VisualElement container)
    {
        if (rowCount <= 1) return;

        container.RemoveAt(container.childCount - 1);
        _rowMethodMaps.RemoveAt(rowCount - 1);
        if (assignedObjects.Count >= rowCount) assignedObjects.RemoveAt(rowCount - 1);
        if (finalActions.Count >= rowCount) finalActions.RemoveAt(rowCount - 1);
        if (parameters.Count >= rowCount) parameters.RemoveAt(rowCount - 1);

        rowCount--;
    }

    void OnObjectAssigned(Object obj, DropdownField menu, int index)
    {
        menu.choices.Clear();
        menu.choices.Add("No Function");
        menu.value = "No Function";  // reset selection
        _rowMethodMaps[index].Clear();

        if (obj == null) return;

        Type type = null;
        Object instanceTarget = null;

        if (obj is MonoScript monoScript)
        {
            // Static methods only — no instance available
            type = monoScript.GetClass();
        }
        else if (obj is ScriptableObject so)
        {
            // Instance + static methods
            type = so.GetType();
            instanceTarget = so;
        }
        else
        {
            // Anything else: try static only
            type = obj.GetType();
        }

        if (type == null) return;

        // Instance methods (ScriptableObject only)
        if (instanceTarget != null)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!IsValidEventMethod(method)) continue;

                string key = $"{type.Name}/{BuildSignature(method)}";
                menu.choices.Add(key);
                _rowMethodMaps[index][key] = (method, instanceTarget);
            }
        }

        var staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

        // Static methods
        foreach (var method in staticMethods)
        {
            if (!IsValidEventMethod(method)) continue;

            string key = $"Static/{BuildSignature(method)}";
            menu.choices.Add(key);
            _rowMethodMaps[index][key] = (method, null); // null target = static
        }
    }

    void OnMenuValueChanged(string optionName, VisualElement lines, int index, object savedParam = null)
    {
        // Remove old param row if present
        var oldRow = lines.Children().FirstOrDefault(c => c.name == $"ParamRow_{index}");
        if (oldRow != null) lines.Remove(oldRow);

        if (optionName == "No Function" || !_rowMethodMaps[index].ContainsKey(optionName)) 
        {
            while (finalActions.Count <= index) finalActions.Add(null);
            finalActions[index] = null;
            return;
        }

        var (method, instanceTarget) = _rowMethodMaps[index][optionName];
        bool isStatic = instanceTarget == null;

        // Extend lists
        while (finalActions.Count <= index) finalActions.Add(null);
        while (parameters.Count <= index) parameters.Add(null);

        ParameterInfo[] methodParams = method.GetParameters();

        if (methodParams.Length == 0)
        {
            // No parameter needed — build the action immediately
            finalActions[index] = isStatic
                ? new SerializableEvent(method.DeclaringType, method.Name, null)
                : new SerializableEvent(instanceTarget, method.Name, null);
            return;
        }

        // Build parameter field
        Type paramType = methodParams[0].ParameterType;

        VisualElement paramRow = new VisualElement();
        paramRow.name = $"ParamRow_{index}";
        paramRow.style.flexDirection = FlexDirection.Row;
        paramRow.style.flexGrow = 1;

        void Commit(object value)
        {
            parameters[index] = value;
            var sv = new SerializableValue(value);
            finalActions[index] = isStatic
                ? new SerializableEvent(method.DeclaringType, method.Name, sv)
                : new SerializableEvent(instanceTarget, method.Name, sv);
        }

        if (typeof(Object).IsAssignableFrom(paramType))
        {
            var field = new ObjectField { allowSceneObjects = false, objectType = paramType };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });

            if (savedParam != null) field.value = savedParam as Object;
            Commit(field.value); // always commit, whether saved or default

            paramRow.Add(field);
        }
        else if (paramType == typeof(string))
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });

            if (savedParam != null) field.value = (string)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(int))
        {
            var field = new IntegerField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });

            if (savedParam != null) field.value = (int)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(float))
        {
            var field = new FloatField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });

            if (savedParam != null) field.value = (float)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(bool))
        {
            var field = new Toggle();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });

            if (savedParam != null) field.value = (bool)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }

        lines.Add(paramRow);
    }

    string BuildSignature(MethodInfo method)
    {
        string paramList = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({paramList})";
    }

    bool IsValidEventMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.ReturnType != typeof(void)) return false;

        var methodParams = method.GetParameters();
        if (methodParams.Length > 1) return false;
        if (methodParams.Length == 1 && !IsSerializableType(methodParams[0].ParameterType)) return false;

        return true;
    }

    bool IsSerializableType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) ||
               typeof(Object).IsAssignableFrom(type);
    }
}
public class DSP_SceneEventNode : Node
{
    public DSP_SceneEvent eventAsset;

    public DSP_SceneEventNode(Vector2 position, SerializableValue[] values = null)
    {
        title = "Scene Event";
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        ObjectField eventField = new ObjectField()
        {
            objectType = typeof(DSP_SceneEvent),
            allowSceneObjects = false
        };

        eventField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            eventAsset = evt.newValue as DSP_SceneEvent;
            this.EndUndoRecord();
        });

        mainContainer.Add(eventField);

        // load saved value if it exists
        if (values != null && values.Length > 0)
        {
            eventAsset = values[0].GetValue() as DSP_SceneEvent;
            eventField.value = eventAsset;
        }

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }
}
public class DSP_ConditionNode : Node
{
    private Dictionary<string, (MethodInfo method, Object target)> methodMap = new();

    public SerializableCondition finalCondition;
    public Object assignedObject;

    public DSP_ConditionNode(Vector2 position, SerializableValue[] values = null, SerializableValue[] parameters = null)
    {
        title = "Condition";
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort("True");  // outPortID 0
        this.AddOutputPort("False"); // outPortID 1

        VisualElement lines = new VisualElement();
        lines.style.flexDirection = FlexDirection.Column;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;

        ObjectField objectField = new ObjectField();
        objectField.allowSceneObjects = false;
        objectField.style.width = 100;
        objectField.objectType = typeof(Object);

        DropdownField dropdownField = new DropdownField(new List<string> { "No Function" }, 0);
        dropdownField.style.flexGrow = 1;

        objectField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            assignedObject = evt.newValue;
            OnObjectAssigned(evt.newValue, dropdownField);
            this.EndUndoRecord();
        });
        dropdownField.RegisterValueChangedCallback(evt =>
        {
            this.StartUndoRecord();
            OnMenuValueChanged(evt.newValue, lines);
            this.EndUndoRecord();
        });

        row.Add(objectField);
        row.Add(dropdownField);
        lines.Add(row);
        mainContainer.Add(lines);

        // restore saved state
        if (values != null && values.Length >= 2)
        {
            Object savedObject = values[0].objectValue;
            string savedMethod = (string)values[1].GetValue();
            object savedParam = parameters?.Length > 0 ? parameters[0].GetValue() : null;

            if (savedObject != null)
            {
                assignedObject = savedObject;
                objectField.value = savedObject;
                OnObjectAssigned(savedObject, dropdownField);

                if (savedMethod != null)
                {
                    dropdownField.value = savedMethod;
                    OnMenuValueChanged(savedMethod, lines, savedParam);
                }
            }
        }

        RefreshExpandedState();
        RefreshPorts();
        SetPosition(new Rect(position, Vector2.zero));
    }

    void OnObjectAssigned(Object obj, DropdownField menu)
    {
        menu.choices.Clear();
        menu.choices.Add("No Function");
        menu.value = "No Function";
        methodMap.Clear();

        if (obj == null) return;

        Type type = null;
        Object instanceTarget = null;

        if (obj is MonoScript monoScript)
        {
            type = monoScript.GetClass();
        }
        else if (obj is ScriptableObject so)
        {
            type = so.GetType();
            instanceTarget = so;
        }
        else
        {
            type = obj.GetType();
        }

        if (type == null) return;

        // Instance methods (ScriptableObject only)
        if (instanceTarget != null)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!IsValidConditionMethod(method)) continue;

                string key = $"{type.Name}/{BuildSignature(method)}";
                menu.choices.Add(key);
                methodMap[key] = (method, instanceTarget);
            }
        }

        // Static methods
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (!IsValidConditionMethod(method)) continue;

            string key = $"Static/{BuildSignature(method)}";
            menu.choices.Add(key);
            methodMap[key] = (method, null);
        }
    }
    void OnMenuValueChanged(string optionName, VisualElement lines, object savedParam = null)
    {
        var oldRow = lines.Children().FirstOrDefault(c => c.name == "ParamRow_0");
        if (oldRow != null) lines.Remove(oldRow);

        if (optionName == "No Function" || !methodMap.ContainsKey(optionName))
        {
            finalCondition = null;
            return;
        }

        var (method, instanceTarget) = methodMap[optionName];
        bool isStatic = instanceTarget == null;

        ParameterInfo[] methodParams = method.GetParameters();

        if (methodParams.Length == 0)
        {
            finalCondition = isStatic
                ? new SerializableCondition(method.DeclaringType, method.Name)
                : new SerializableCondition(instanceTarget, method.Name);
            finalCondition.hasValue = true;
            return;
        }

        // Build parameter field
        Type paramType = methodParams[0].ParameterType;

        VisualElement paramRow = new VisualElement();
        paramRow.name = "ParamRow_0";
        paramRow.style.flexDirection = FlexDirection.Row;
        paramRow.style.flexGrow = 1;

        void Commit(object value)
        {
            var sv = new SerializableValue(value);
            finalCondition = isStatic
                ? new SerializableCondition(method.DeclaringType, method.Name, sv)
                : new SerializableCondition(instanceTarget, method.Name, sv);

            finalCondition.hasValue = true;
        }

        if (typeof(Object).IsAssignableFrom(paramType))
        {
            var field = new ObjectField { allowSceneObjects = false, objectType = paramType };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = savedParam as Object;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(string))
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (string)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(int))
        {
            var field = new IntegerField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (int)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(float))
        {
            var field = new FloatField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (float)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(bool))
        {
            var field = new Toggle();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => { this.StartUndoRecord(); Commit(evt.newValue); this.EndUndoRecord(); });
            if (savedParam != null) field.value = (bool)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }

        lines.Add(paramRow);
    }

    bool IsValidConditionMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.ReturnType != typeof(bool)) return false;
        var methodParams = method.GetParameters();
        if (methodParams.Length > 1) return false;
        if (methodParams.Length == 1 && !IsSerializableType(methodParams[0].ParameterType)) return false;
        return true;
    }

    bool IsSerializableType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) ||
               typeof(Object).IsAssignableFrom(type);
    }

    string BuildSignature(MethodInfo method)
    {
        string paramList = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"{method.Name}({paramList})";
    }
}