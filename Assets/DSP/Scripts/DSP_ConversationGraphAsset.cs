using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
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
