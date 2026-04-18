using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "DSP/Dialogue Graph")]
public class DSP_ConversationGraphAsset : ScriptableObject, ISerializationCallbackReceiver
{
    Dictionary<DSP_NodeData, List<DSP_EdgeData>> graph = new();

    public List<DSP_NodeData> nodes = new();
    public List<DSP_EdgeData> edges = new();

    public void Clear()
    {
        graph.Clear();
        nodes.Clear();
        edges.Clear();
    }
    
    public int GetNodeCount()
    {
        return graph.Count;
    }

    public List<DSP_NodeData> GetNodes()
    {
        return new List<DSP_NodeData>(graph.Keys);
    }
    public List<DSP_EdgeData> GetAllEdges()
    {
        List<DSP_EdgeData> allEdges = new List<DSP_EdgeData>();
        foreach (var edgeList in graph.Values)
        {
            allEdges.AddRange(edgeList);
        }
        return allEdges;
    }

    public void AddNode(DSP_NodeData node)
    {
        if (!graph.ContainsKey(node))
        {
            graph.Add(node, new List<DSP_EdgeData>());
        }
    }
    public void AddEdge(DSP_EdgeData edge)
    {
        DSP_NodeData fromNode = graph.Keys.FirstOrDefault(n => n.id == edge.fromNode);
        if (fromNode != null)
        {
            graph[fromNode].Add(edge);
        }
    }

    public List<DSP_EdgeData> GetOutgoingEdges(DSP_NodeData node)
    {
        return new List<DSP_EdgeData>(graph[node]);
    }

    public void OnBeforeSerialize()
    {
        nodes.Clear();
        edges.Clear();

        foreach (var kvp in graph)
        {
            nodes.Add(kvp.Key);
            edges.AddRange(kvp.Value);
        }
    }
    public void OnAfterDeserialize()
    {
        graph.Clear();

        foreach (var node in nodes)
        {
            graph.Add(node, new List<DSP_EdgeData>());
        }
        foreach (var edge in edges)
        {
            DSP_NodeData fromNode = graph.Keys.FirstOrDefault(n => n.id == edge.fromNode);
            if (fromNode != null)
            {
                graph[fromNode].Add(edge);
            }
        }
    }
}
public enum DSP_NodeType
{
    Start,
    End,
    Dialogue,
    Choice,
    Event,
    Condition
}
public enum DSP_IteratorState
{
    Running,      // Iterator is auto-processing (events)
    WaitingForUI, // Paused, caller must call Advance()
    Finished      // Hit an End node or dead end
}

public class DSP_ConversationIterator
{
    private readonly DSP_ConversationGraphAsset _graph;
    private DSP_NodeData _currentNode;

    public DSP_NodeData CurrentNode => _currentNode;
    public DSP_IteratorState State { get; private set; } = DSP_IteratorState.Running;

    public DSP_ConversationIterator(DSP_ConversationGraphAsset graph)
    {
        _graph = graph;

        // Find and immediately step past the Start node
        _currentNode = graph.GetNodes().FirstOrDefault(n => n.nodeType == DSP_NodeType.Start);
        if (_currentNode == null)
            throw new InvalidOperationException("Graph has no Start node.");

        Advance(); // Skip Start
    }

    /// <summary>
    /// For linear nodes (Dialogue -> single edge). Throws if called on a Choice node.
    /// </summary>
    public void Advance()
    {
        if (State == DSP_IteratorState.Finished) return;

        if (_currentNode.nodeType == DSP_NodeType.Choice)
            throw new InvalidOperationException("Use Advance(portId) to resolve a Choice node.");

        var edges = _graph.GetOutgoingEdges(_currentNode);
        var next = edges.Count > 0
            ? _graph.GetNodes().FirstOrDefault(n => n.id == edges[0].toNode)
            : null;

        StepTo(next);
    }

    /// <summary>
    /// For Choice nodes, caller passes the chosen PortID.
    /// </summary>
    public void Advance(int chosenPortId)
    {
        if (State == DSP_IteratorState.Finished) return;

        if (_currentNode.nodeType != DSP_NodeType.Choice)
            throw new InvalidOperationException("Advance(portId) is only valid on Choice nodes.");

        var edges = _graph.GetOutgoingEdges(_currentNode);
        var chosenEdge = edges.FirstOrDefault(e => e.outPortID == chosenPortId);
        if (chosenEdge == null)
            throw new ArgumentException($"No edge found for portId {chosenPortId}.");

        var next = _graph.GetNodes().FirstOrDefault(n => n.id == chosenEdge.toNode);
        StepTo(next);
    }

    // -------------------------------------------------------------------------
    // Core stepping logic — runs forward automatically through
    // Start/End/Event nodes, pauses on Dialogue/Choice.
    // -------------------------------------------------------------------------
    private void StepTo(DSP_NodeData node)
    {
        while (node != null)
        {
            _currentNode = node;

            switch (node.nodeType)
            {
                case DSP_NodeType.Start:
                    // Transparent — just follow the single outgoing edge
                    node = FollowSingleEdge(node);
                    break;

                case DSP_NodeType.End:
                    State = DSP_IteratorState.Finished;
                    return;

                case DSP_NodeType.Event:
                    InvokeEventNode(node);
                    node = FollowSingleEdge(node); // auto-advance
                    break;

                case DSP_NodeType.Condition:
                    bool result = node.finalCondition.Invoke();
                    var conditionEdges = _graph.GetOutgoingEdges(node);
                    var branch = conditionEdges.FirstOrDefault(e => e.outPortID == (result ? 0 : 1));
                    node = branch != null ? _graph.GetNodes().FirstOrDefault(n => n.id == branch.toNode) : null;
                    break;

                case DSP_NodeType.Dialogue:
                case DSP_NodeType.Choice:
                    State = DSP_IteratorState.WaitingForUI;
                    return; // Pause here
            }
        }

        // Fell off the graph with no End node
        State = DSP_IteratorState.Finished;
    }

    private DSP_NodeData FollowSingleEdge(DSP_NodeData node)
    {
        var edges = _graph.GetOutgoingEdges(node);
        if (edges.Count == 0) return null;
        return _graph.GetNodes().FirstOrDefault(n => n.id == edges[0].toNode);
    }

    private void InvokeEventNode(DSP_NodeData node)
    {
        foreach (var e in node.finalEvents) e.Invoke();
    }
}

[System.Serializable] public class DSP_NodeData
{
    public int id;
    public DSP_NodeType nodeType;
    public Vector2 position;
    public SerializableValue[] values;

    public SerializableValue[] eventParameters; // only for Event and Condition nodes
    public SerializableEvent[] finalEvents; // only for Event nodes
    public SerializableCondition finalCondition; // only for Condition nodes
}
[System.Serializable] public class DSP_EdgeData
{
    public int fromNode;
    public int outPortID;
    public int toNode;
    public int inPortID;
}
[System.Serializable] public class SerializableValue
{
    public SerializableValue(object value)
    {
        SetValue(value);
    }
    public enum ValueType
    {
        None,
        Int,
        Float,
        String,
        Bool,
        Object,
        IntArray,
        FloatArray,
        StringArray,
        BoolArray,
        ObjectArray
    }

    public ValueType type;

    public int intValue;
    public float floatValue;
    public string stringValue;
    public bool boolValue;
    public Object objectValue;

    public int[] intArray;
    public float[] floatArray;
    public string[] stringArray;
    public bool[] boolArray;
    public Object[] objectArray;

    public object GetValue()
    {
        return type switch
        {
            ValueType.Int => intValue,
            ValueType.Float => floatValue,
            ValueType.String => stringValue,
            ValueType.Bool => boolValue,
            ValueType.Object => objectValue,
            ValueType.IntArray => intArray,
            ValueType.FloatArray => floatArray,
            ValueType.StringArray => stringArray,
            ValueType.BoolArray => boolArray,
            ValueType.ObjectArray => objectArray,
            _ => null
        };
    }
    public void SetValue(object value)
    {
        switch (value)
        {
            case int i:
                type = ValueType.Int;
                intValue = i;
                break;
            case float f:
                type = ValueType.Float;
                floatValue = f;
                break;
            case string s:
                type = ValueType.String;
                stringValue = s;
                break;
            case bool b:
                type = ValueType.Bool;
                boolValue = b;
                break;
            case Object o:
                type = ValueType.Object;
                objectValue = o;
                break;
            case int[] iA:
                type = ValueType.IntArray;
                intArray = iA;
                break;
            case float[] fA:
                type = ValueType.FloatArray;
                floatArray = fA;
                break;
            case string[] sA:
                type = ValueType.StringArray;
                stringArray = sA;
                break;
            case bool[] bA:
                type = ValueType.BoolArray;
                boolArray = bA;
                break;
            case Object[] oA:
                type = ValueType.ObjectArray;
                objectArray = oA;
                break;
            default:
                type = ValueType.None;
                break;
        }
    }
}
[System.Serializable] public class SerializableEvent
{
    public Object target;        // null when static
    public string methodName;
    public string staticTypeName; // AssemblyQualifiedName, only used when isStatic
    public bool isStatic;
    public SerializableValue parameter;

    // Instance constructor
    public SerializableEvent(Object target, string methodName, SerializableValue parameter)
    {
        this.target = target;
        this.methodName = methodName;
        this.parameter = parameter;
        this.isStatic = false;
    }

    // Static constructor
    public SerializableEvent(Type staticType, string methodName, SerializableValue parameter)
    {
        this.target = null;
        this.methodName = methodName;
        this.staticTypeName = staticType.AssemblyQualifiedName;
        this.parameter = parameter;
        this.isStatic = true;
    }

    public void Invoke()
    {
        MethodInfo method;
        object[] args = null;

        var paramValue = parameter?.GetValue();
        if (paramValue != null)
            args = new object[] { paramValue };

        if (isStatic)
        {
            if (string.IsNullOrEmpty(staticTypeName))
            {
                Debug.LogWarning("SerializableEvent: staticTypeName is empty.");
                return;
            }

            Type type = Type.GetType(staticTypeName);
            if (type == null)
            {
                Debug.LogWarning($"SerializableEvent: could not resolve type '{staticTypeName}'.");
                return;
            }

            method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogWarning($"SerializableEvent: static method '{methodName}' not found on '{type.Name}'.");
                return;
            }

            method.Invoke(null, args);
        }
        else
        {
            if (target == null || string.IsNullOrEmpty(methodName))
            {
                Debug.LogWarning("SerializableEvent: target or methodName is null.");
                return;
            }

            method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public);

            if (method == null)
            {
                Debug.LogWarning($"SerializableEvent: instance method '{methodName}' not found on '{target.name}'.");
                return;
            }

            method.Invoke(target, args);
        }
    }
}
[System.Serializable] public class SerializableCondition
{
    public Object target;
    public string methodName;
    public string staticTypeName;
    public bool isStatic;
    public SerializableValue parameter;

    public SerializableCondition(Object target, string methodName, SerializableValue parameter = null)
    {
        this.target = target;
        this.methodName = methodName;
        this.isStatic = false;
        this.parameter = parameter;
    }

    public SerializableCondition(Type staticType, string methodName, SerializableValue parameter = null)
    {
        this.staticTypeName = staticType.AssemblyQualifiedName;
        this.methodName = methodName;
        this.isStatic = true;
        this.parameter = parameter;
    }

    public bool Invoke()
    {
        MethodInfo method;
        object[] args = null;

        var paramValue = parameter?.GetValue();
        if (paramValue != null)
            args = new object[] { paramValue };

        if (isStatic)
        {
            if (string.IsNullOrEmpty(staticTypeName))
            {
                Debug.LogWarning("SerializableCondition: staticTypeName is empty.");
                return false;
            }
            Type type = Type.GetType(staticTypeName);
            if (type == null)
            {
                Debug.LogWarning($"SerializableCondition: could not resolve type '{staticTypeName}'.");
                return false;
            }
            method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogWarning($"SerializableCondition: static method '{methodName}' not found on '{type.Name}'.");
                return false;
            }
            return (bool)method.Invoke(null, args);
        }
        else
        {
            if (target == null || string.IsNullOrEmpty(methodName))
            {
                Debug.LogWarning("SerializableCondition: target or methodName is null.");
                return false;
            }
            method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogWarning($"SerializableCondition: method '{methodName}' not found on '{target.name}'.");
                return false;
            }
            return (bool)method.Invoke(target, args);
        }
    }
}

public static class DSP_GraphAssetHandler
{
    [OnOpenAsset]
    public static bool OnOpen(int instanceID, int line)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);

        if (obj is DSP_ConversationGraphAsset graphAsset)
        {
            DSP_EditorWindow.Open(graphAsset);
            return true;
        }

        return false;
    }
}
