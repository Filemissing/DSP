using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using Edge = UnityEditor.Experimental.GraphView.Edge;
using Object = UnityEngine.Object;

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
    public DSP_NodeGraphView()
    {
        style.flexGrow = 1;

        ContentZoomer contentZoomer = new ContentZoomer { minScale = 0.5f, maxScale = 5f };
        this.AddManipulator(contentZoomer);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        
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
            if (graphAsset.nodes.Count < 2)
            {
                AddElement(new DSP_StartNode(contentViewContainer.contentRect.center + new Vector2(-200, 300)));
                AddElement(new DSP_EndNode(contentViewContainer.contentRect.center + new Vector2(200, 300)));
            }

            // instantiate nodes
            foreach (DSP_NodeData nodeData in graphAsset.nodes)
            {
                // handle Event nodes separately since they require special handling
                if (nodeData.nodeType == DSP_NodeType.Event)
                {
                    DSP_EventNode eventNode = new DSP_EventNode(nodeData.position, nodeData.values, nodeData.eventParameters);
                    AddElement(eventNode);
                    break;
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
            foreach (DSP_EdgeData edgeData in graphAsset.edges)
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
        graphAsset.nodes.Clear();
        graphAsset.edges.Clear();

        // save node data
        foreach (Node node in nodes)
        {
            if (node is DSP_StartNode)
            {
                graphAsset.nodes.Add(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Start,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { }
                });
            }
            else if (node is DSP_EndNode endNode)
            {
                graphAsset.nodes.Add(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.End,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { new(node.inputContainer.childCount) }
                });
            }
            else if (node is DSP_DialogueNode dialogueNode)
            {
                graphAsset.nodes.Add(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Dialogue,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { new(dialogueNode.dialogueText) }
                });
            }
            else if (node is DSP_ChoiceNode choiceNode)
            {
                graphAsset.nodes.Add(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Choice,
                    position = node.GetPosition().position,
                    values = new SerializableValue[] { new(choiceNode.choices.Select(c => c.Item1.value).ToArray()) }
                });
            }
            else if (node is DSP_EventNode eventNode)
            {
                graphAsset.nodes.Add(new DSP_NodeData
                {
                    id = nodes.ToList().FindIndex(n => n == node),
                    nodeType = DSP_NodeType.Event,
                    position = node.GetPosition().position,
                    values = new SerializableValue[]
                    {
                        new(eventNode.rowCount),
                        new(eventNode.validMethodsForObject.Select(o => o.Item1).ToArray()), // store only object references
                        new(eventNode.chosenMethods.Select(m => $"{m.Item1.DeclaringType}/{m.Item1.Name}({m.Item1.GetParameters()[0].ParameterType.Name})").ToArray()) // store signature of chosen method to restore using reflection
                    },
                    eventParameters = eventNode.parameters.Select(p => new SerializableValue(p)).ToArray(),
                    finalEvents = eventNode.finalActions.ToArray()
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

            graphAsset.edges.Add(new DSP_EdgeData
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

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        Vector2 mousePos = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);

        evt.menu.AppendAction("Dialogue Node", (a) => AddElement(new DSP_DialogueNode(mousePos)));
        evt.menu.AppendAction("Choice Node", (a) => AddElement(new DSP_ChoiceNode(mousePos)));
        evt.menu.AppendAction("Event Node", (a) => AddElement(new DSP_EventNode(mousePos)));
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
    public string dialogueText;

    public DSP_DialogueNode(Vector2 position, SerializableValue[] values = null)
    {
        // load values if they exist
        if (values != null && values.Length > 0)
            dialogueText = values[0].GetValue() as string;

        title = "Dialogue";
        style.width = 200;
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        // Manual label
        var label = new Label("Dialogue");
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.paddingTop = 2;
        label.style.paddingBottom = 2;
        label.style.paddingLeft = 2;

        mainContainer.Add(label);

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
    // all valid methods for currently assigned object
    // --------------------------------------------------------
    // Structure: (component instance, list of valid methods)
    // --------------------------------------------------------
    public List<(Object, List<MethodInfo>)> validMethodsForObject = new();

    // chosen methods
    // -------------------------------------------
    // Structure: (method, target instance) 
    // -------------------------------------------
    public List<(MethodInfo, object)> chosenMethods = new();

    public List<object> parameters = new();

    public List<SerializableEvent> finalActions = new();

    // 0-based
    public int rowCount = 0;

    public DSP_EventNode(Vector2 position, SerializableValue[] values = null, SerializableValue[] parameters = null)
    {
        title = "Event";
        this.FixTransparency();

        this.AddInputPort();
        this.AddOutputPort();

        // event container
        VisualElement eventContainer = new VisualElement();
        eventContainer.style.flexDirection = FlexDirection.Column;

        // load values if they exist
        if (values != null && values.Length >= 2)
        {
            int rowCount = (int)values[0].GetValue();
            Object[] objects = values[1].objectArray;
            string[] chosenMethods = (string[])values[2].GetValue();

            for (int i = 0; i < rowCount; i++)
            {
                // any of these may be null
                Object assignedObject = i < objects.Length ? objects[i] : null;
                string chosenMethod = i < chosenMethods.Length ? chosenMethods[i] : null;
                object parameter = i < parameters.Length ? parameters[i].GetValue() : null;

                AddEvent(eventContainer, assignedObject, chosenMethod, parameter);
            }
        }
        else
            AddEvent(eventContainer);

        mainContainer.Add(eventContainer);

        // buttons
        VisualElement buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;

        Button removeButton = new Button(() => RemoveEvent(eventContainer))
        {
            text = "-"
        };
        Button addButton = new Button(() => AddEvent(eventContainer))
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

    void AddEvent(VisualElement container, Object assignedObject = null, string chosenMethod = null, object parameter = null)
    {
        int index = rowCount;

        VisualElement lines = new VisualElement();
        lines.style.flexDirection = FlexDirection.Column;
        lines.style.alignContent = Align.Center;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignContent = Align.Center;
        row.style.width = new StyleLength(StyleKeyword.Auto);
        row.style.flexGrow = 1;
        
        // dropdown menu
        DropdownField dropdownField = new DropdownField(new List<string> { "No Function" }, 0);
        dropdownField.style.flexGrow = 1;
        dropdownField.RegisterValueChangedCallback(evt => OnMenuValueChanged(evt.newValue, lines, index));

        // objectField
        ObjectField objectField = new ObjectField();
        objectField.allowSceneObjects = false;
        objectField.style.width = 100;
        objectField.RegisterValueChangedCallback(evt => OnObjectAssigned(evt.newValue, dropdownField, index));

        row.Add(objectField);
        row.Add(dropdownField);

        lines.Add(row);

        container.Add(lines);

        // assign saved data if it exists
        if (assignedObject != null)
        {
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
    void RemoveEvent(VisualElement container)
    {
        if (rowCount <= 1) return;

        container.RemoveAt(container.childCount - 1);

        if (validMethodsForObject.Count > rowCount)
            validMethodsForObject.RemoveAt(rowCount);
        if (chosenMethods.Count > rowCount)
            chosenMethods.RemoveAt(rowCount);

        rowCount--;
    }

    void OnObjectAssigned(Object obj, DropdownField menu, int eventIndex)
    {
        menu.choices.Clear();
        menu.choices.Add("No Function");

        if (obj == null) return;

        if (obj.GetType() == typeof(GameObject))
        {
            GameObject gameObject = obj as GameObject;
            Component[] components = gameObject.GetComponents<Component>();

            MethodInfo[] gameObjectMethods = gameObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in gameObjectMethods)
            {
                if(!IsValidEventMethod(method)) continue;

                string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                string signature = $"{method.Name}({parameters})";

                menu.choices.Add("GameObject/" + signature);

                if (validMethodsForObject.Count <= eventIndex)
                    validMethodsForObject.Add((obj, new List<MethodInfo>()));

                validMethodsForObject[eventIndex].Item2.Add(method);
            }

            foreach (Component component in components)
            {
                Type type = component.GetType();
                if (type == typeof(UnityEngine.Object) || type == typeof(Component) || type == typeof(Behaviour))
                    continue;

                MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                
                foreach (MethodInfo method in methods)
                {
                    if (!IsValidEventMethod(method)) continue;

                    string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                    string signature = $"{method.Name}({parameters})";

                    menu.choices.Add($"{type.Name}/{signature}");

                    if (validMethodsForObject.Count <= eventIndex)
                        validMethodsForObject.Add((obj, new List<MethodInfo>()));
                    
                    validMethodsForObject[eventIndex].Item2.Add(method);
                }
            }
        }
    }
    void OnMenuValueChanged(string optionName, VisualElement container, int eventIndex, object parameter = null)
    {
        if(optionName == "No Function")
        {
            if (container.childCount > 1)
                container.RemoveAt(1);
            return;
        }

        string className = optionName.Split('/')[0];
        string methodName = optionName.Split('/')[1].Split('(')[0];

        Type type = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().Where(t => t.Name == className).FirstOrDefault();
        MethodInfo method = validMethodsForObject[eventIndex].Item2.Where(m => m.Name == methodName).FirstOrDefault();
        Type paramType = method.GetParameters()[0].ParameterType;

        if (method == null) Debug.LogWarning($"MethodInfo for method {optionName} not found on Object {validMethodsForObject[eventIndex]}");

        // make sure the list extends to the current eventIndex
        if (chosenMethods.Count <= eventIndex)
            chosenMethods.Add((null, null));

        chosenMethods[eventIndex] = (method, validMethodsForObject[eventIndex].Item1.GetComponent(type));

        // create Parameter field
        VisualElement lines = container.parent;

        // Linq shenanigens to remove old row object if it exists
        VisualElement oldRow = lines.Children().Where(c => c.name == "ParamRow " + eventIndex).FirstOrDefault();
        if (oldRow != null)
            lines.Remove(oldRow);

        VisualElement row = new VisualElement();
        row.name = "ParamRow " + eventIndex;
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignContent = Align.Center;
        row.style.width = new StyleLength(StyleKeyword.Auto);
        row.style.flexGrow = 1;

        if(typeof(Object).IsAssignableFrom(paramType))
        {
            ObjectField objectField = new();
            objectField.allowSceneObjects = false;
            objectField.objectType = paramType;
            objectField.style.flexGrow = 1;

            objectField.RegisterValueChangedCallback(evt => OnParamFieldChanged(evt.newValue, eventIndex));
            if (parameter != null)
            {
                objectField.value = parameter as Object;
                OnParamFieldChanged((Object)parameter, eventIndex);
            }

            row.Add(objectField);
        }
        else if(paramType == typeof(string))
        {
            TextField textField = new();
            textField.style.flexGrow = 1;

            textField.RegisterValueChangedCallback(evt => OnParamFieldChanged(evt.newValue, eventIndex));
            if (parameter != null)
            {
                textField.value = parameter as string;
                OnParamFieldChanged(parameter as string, eventIndex);
            }
            row.Add(textField);
        }
        else if(paramType == typeof(int))
        {
            IntegerField intField = new();
            intField.style.flexGrow = 1;

            intField.RegisterValueChangedCallback(evt => OnParamFieldChanged(evt.newValue, eventIndex));
            if (parameter != null)
            {
                intField.value = (int)parameter;
                OnParamFieldChanged((int)parameter, eventIndex);
            }
            row.Add(intField);
        }
        else if(paramType == typeof(float))
        {
            FloatField floatField = new();
            floatField.style.flexGrow = 1;

            floatField.RegisterValueChangedCallback(evt => OnParamFieldChanged(evt.newValue, eventIndex));
            if (parameter != null)
            {
                floatField.value = (float)parameter;
                OnParamFieldChanged((float)parameter, eventIndex);
            }
            row.Add(floatField);
        }
        else if(paramType == typeof(bool))
        {
            Toggle boolfield = new();
            boolfield.style.flexGrow = 1;

            boolfield.RegisterValueChangedCallback(evt => OnParamFieldChanged(evt.newValue, eventIndex));
            if (parameter != null)
            {
                boolfield.value = (bool)parameter;
                OnParamFieldChanged((bool)parameter, eventIndex);
            }
            row.Add(boolfield);
        }

        lines.Add(row);
    }
    void OnParamFieldChanged(object newValue, int eventIndex)
    {
        MethodInfo method = chosenMethods[eventIndex].Item1;
        object target = chosenMethods[eventIndex].Item2;

        // make sure the list extends to eventIndex
        if (finalActions.Count <= eventIndex)
            finalActions.Add(null);

        if (parameters.Count <= eventIndex)
            parameters.Add(null);
        parameters[eventIndex] = newValue;

        finalActions[eventIndex] = new SerializableEvent(target as Object, method.Name, new SerializableValue(newValue));
        finalActions[eventIndex].Invoke();
    }

    // helper methods
    bool IsValidEventMethod(MethodInfo method)
    {
        if (method.IsSpecialName) return false; // skip property accessors
        if (method.ReturnType != typeof(void)) return false;

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length > 1) return false; // only 0 or 1 parameter allowed

        if (parameters.Length == 1)
        {
            Type paramType = parameters[0].ParameterType;

            if (!IsSerializableType(paramType)) return false;
        }


        return true;
    }
    bool IsSerializableType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(type);
    }
}
