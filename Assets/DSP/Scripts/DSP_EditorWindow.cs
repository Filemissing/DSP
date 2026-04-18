using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
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

    static DSP_NodeGraphView graphView;
    static Toolbar toolbar;
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
    }
}

public class DSP_NodeGraphView : GraphView
{
    private bool isUpdating;
    
    public DSP_NodeGraphView()
    {
        style.flexGrow = 1;

        ContentZoomer contentZoomer = new ContentZoomer { minScale = 0.5f, maxScale = 5f };
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
            {
                // handle Event and Condition nodes separately since they require special handling
                if (nodeData.nodeType == DSP_NodeType.Event)
                {
                    DSP_EventNode eventNode = new DSP_EventNode(nodeData.position, nodeData.values, nodeData.eventParameters);
                    AddElement(eventNode);
                    continue;
                }
                if (nodeData.nodeType == DSP_NodeType.Condition)
                {
                    DSP_ConditionNode eventNode = new DSP_ConditionNode(nodeData.position, nodeData.values, nodeData.eventParameters);
                    AddElement(eventNode);
                    continue;
                }

                Type nodeType = nodeData.nodeType switch
                {
                    DSP_NodeType.Start => typeof(DSP_StartNode),
                    DSP_NodeType.End => typeof(DSP_EndNode),
                    DSP_NodeType.Dialogue => typeof(DSP_DialogueNode),
                    DSP_NodeType.Choice => typeof(DSP_ChoiceNode),
                    _ => throw new Exception("Unknown node type")
                };

                Node node = (Node)Activator.CreateInstance(nodeType, nodeData.position, nodeData.values);
                AddElement(node);
            }

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
    public void SaveToAsset(DSP_ConversationGraphAsset graphAsset)
    {
        graphAsset.Clear();

        // save node data
        foreach (Node node in nodes)
        {
            if (node is DSP_StartNode)
            {
                graphAsset.AddNode(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Start,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { }
                });
            }
            else if (node is DSP_EndNode endNode)
            {
                graphAsset.AddNode(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.End,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { new(node.inputContainer.childCount) }
                });
            }
            else if (node is DSP_DialogueNode dialogueNode)
            {
                graphAsset.AddNode(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Dialogue,
                    position = node.GetPosition().position,
                    values = new SerializableValue[]
                    {
                        new(dialogueNode.dialogueText),
                        new(dialogueNode.characterAsset)
                    }
                });
            }
            else if (node is DSP_ChoiceNode choiceNode)
            {
                graphAsset.AddNode(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Choice,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { new(choiceNode.choices.Select(c => c.Item1.value).ToArray()) }
                });
            }
            else if (node is DSP_EventNode eventNode)
            {
                graphAsset.AddNode(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Event,
                    position = node.GetPosition().position,
                    values = new SerializableValue[]
                    {
                        new(eventNode.rowCount),
                        // read directly from tracked references, not finalActions
                        new(eventNode.assignedObjects.Select(o => o).ToArray()),
                        new(eventNode.finalActions.Select(a =>
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
                    eventParameters = eventNode.parameters.Select(p => new SerializableValue(p)).ToArray(),
                    finalEvents = eventNode.finalActions.ToArray()
                });
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

                graphAsset.AddNode(new DSP_NodeData
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
                    finalCondition = conditionNode.finalCondition
                });
            }
        }

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

        evt.menu.AppendAction("Dialogue Node", (a) => AddElement(new DSP_DialogueNode(mousePos)));
        evt.menu.AppendAction("Choice Node", (a) => AddElement(new DSP_ChoiceNode(mousePos)));
        evt.menu.AppendAction("Event Node", (a) => AddElement(new DSP_EventNode(mousePos)));
        evt.menu.AppendAction("Condition Node", (a) => AddElement(new DSP_ConditionNode(mousePos)));
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

    private Image characterPreviewImage;
    private Label characterPreviewLabel;
    private VisualElement previewContainer;
    private static Texture2D placeholderPortrait;

    public DSP_DialogueNode(Vector2 position, SerializableValue[] values = null)
    {
        // load values if they exist
        if (values != null && values.Length > 0)
        {
            dialogueText = values[0].GetValue() as string;
            
            if (values.Length > 1)
                characterAsset = values[1].GetValue() as DSP_CharacterAsset;
        }

        title = "Dialogue";
        style.width = 200;
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        
        // Character label
        var characterLabel = new Label("Character");
        characterLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        characterLabel.style.paddingTop = 2;
        characterLabel.style.paddingBottom = 2;
        characterLabel.style.paddingLeft = 2;
        
        mainContainer.Add(characterLabel);
        
        
        // CharacterField
        ObjectField characterField = new ObjectField()
        {
            objectType = typeof(DSP_CharacterAsset),
            allowSceneObjects = false,
            value = characterAsset
        };

        characterField.RegisterValueChangedCallback(evt =>
        {
            characterAsset = evt.newValue as DSP_CharacterAsset;
            UpdateCharacterPreview();
        });
        mainContainer.Add(characterField);
        
        
        // Preview container (hidden if characterField is nil)
        previewContainer = new VisualElement();
        previewContainer.style.flexDirection = FlexDirection.Row;
        previewContainer.style.marginTop = 4;
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

        textField.RegisterValueChangedCallback(evt => dialogueText = evt.newValue);
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

    public DSP_ChoiceNode(Vector2 position, SerializableValue[] values = null)
    {
        title = "Choice";
        this.FixTransparency();

        this.AddInputPort();

        // choice container
        VisualElement choiceContainer = new VisualElement();
        choiceContainer.style.flexDirection = FlexDirection.Column;


        if (values != null && values.Length > 0)
        {
            foreach (string choiceText in values[0].GetValue() as string[])
            {
                AddChoice(choiceContainer, choiceText);
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

        Button removeButton = new Button(() => RemoveChoice(choiceContainer))
        {
            text = "-"
        };
        Button addButton = new Button(() => AddChoice(choiceContainer))
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

    void AddChoice(VisualElement container, string text = null)
    {
        int index = choices.Count;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignContent = Align.Center;
        row.style.width = new StyleLength(StyleKeyword.Auto);
        row.style.flexGrow = 1;

        // text field
        TextField textField = new TextField()
        {
            value = text == null ? $"Option {index}" : text,
            multiline = true
        };
        textField.style.marginRight = 5;
        textField.style.flexGrow = 1;

        row.Add(textField);

        // port
        Port port = this.GeneratePort(Direction.Output, Port.Capacity.Single);

        row.Add(port);

        // add to list
        choices.Add((textField, port));

        container.Add(row);
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
    }
}
public class DSP_EventNode : Node
{
    // Per-row data needed to resolve the dropdown selection back to a MethodInfo
    // Key: "GroupName/MethodSignature" string → (MethodInfo, instance target or null if static)
    private List<Dictionary<string, (MethodInfo method, Object target)>> _rowMethodMaps = new();

    public List<SerializableEvent> finalActions = new();
    public List<Object> assignedObjects = new();
    public List<object> parameters = new();

    public int rowCount = 0;

    public DSP_EventNode(Vector2 position, SerializableValue[] values = null, SerializableValue[] parameters = null)
    {
        title = "Event";
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
        buttonContainer.Add(new Button(() => RemoveEventRow(eventContainer)) { text = "-" });
        buttonContainer.Add(new Button(() => AddEventRow(eventContainer)) { text = "+" });
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
            assignedObjects[index] = evt.newValue;
            OnObjectAssigned(evt.newValue, dropdownField, index);
        });
        dropdownField.RegisterValueChangedCallback(evt =>
            OnMenuValueChanged(evt.newValue, lines, index));

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

            if (chosenMethod != null)
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

        if (optionName == "No Function" || !_rowMethodMaps[index].ContainsKey(optionName)) return;

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
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            if (savedParam != null) field.value = savedParam as Object;
            Commit(field.value); // always commit, whether saved or default

            paramRow.Add(field);
        }
        else if (paramType == typeof(string))
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            if (savedParam != null) field.value = (string)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(int))
        {
            var field = new IntegerField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            if (savedParam != null) field.value = (int)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(float))
        {
            var field = new FloatField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            if (savedParam != null) field.value = (float)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }
        else if (paramType == typeof(bool))
        {
            var field = new Toggle();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            if (savedParam != null) field.value = (bool)savedParam;
            Commit(field.value);

            paramRow.Add(field);
        }

        lines.Add(paramRow);
    }

    // -------------------------------------------------------------------------
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
public class DSP_ConditionNode : Node
{
    private Dictionary<string, (MethodInfo method, Object target)> _methodMap = new();

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
            assignedObject = evt.newValue;
            OnObjectAssigned(evt.newValue, dropdownField);
        });
        dropdownField.RegisterValueChangedCallback(evt =>
            OnMenuValueChanged(evt.newValue, lines));

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
        _methodMap.Clear();

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
                _methodMap[key] = (method, instanceTarget);
            }
        }

        // Static methods
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            if (!IsValidConditionMethod(method)) continue;

            string key = $"Static/{BuildSignature(method)}";
            menu.choices.Add(key);
            _methodMap[key] = (method, null);
        }
    }
    void OnMenuValueChanged(string optionName, VisualElement lines, object savedParam = null)
    {
        var oldRow = lines.Children().FirstOrDefault(c => c.name == "ParamRow_0");
        if (oldRow != null) lines.Remove(oldRow);

        if (optionName == "No Function" || !_methodMap.ContainsKey(optionName))
        {
            finalCondition = null;
            return;
        }

        var (method, instanceTarget) = _methodMap[optionName];
        bool isStatic = instanceTarget == null;

        ParameterInfo[] methodParams = method.GetParameters();

        if (methodParams.Length == 0)
        {
            finalCondition = isStatic
                ? new SerializableCondition(method.DeclaringType, method.Name)
                : new SerializableCondition(instanceTarget, method.Name);
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
        }

        if (typeof(Object).IsAssignableFrom(paramType))
        {
            var field = new ObjectField { allowSceneObjects = false, objectType = paramType };
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));
            if (savedParam != null) field.value = savedParam as Object;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(string))
        {
            var field = new TextField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));
            if (savedParam != null) field.value = (string)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(int))
        {
            var field = new IntegerField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));
            if (savedParam != null) field.value = (int)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(float))
        {
            var field = new FloatField();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));
            if (savedParam != null) field.value = (float)savedParam;
            Commit(field.value);
            paramRow.Add(field);
        }
        else if (paramType == typeof(bool))
        {
            var field = new Toggle();
            field.style.flexGrow = 1;
            field.RegisterValueChangedCallback(evt => Commit(evt.newValue));
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