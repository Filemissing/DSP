using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "DSP/Dialogue Graph")]
public class DSP_ConversationGraphAsset : ScriptableObject
{
    public List<DSP_NodeData> nodes = new();
    public List<DSP_EdgeData> edges = new();
}
public enum DSP_NodeType
{
    Start,
    End,
    Dialogue,
    Choice,
    Event
}

[System.Serializable] public class DSP_NodeData
{
    public int id;
    public DSP_NodeType nodeType;
    public Vector2 position;
    public SerializableValue[] values;

    public SerializableValue[] eventParameters; // only for Event nodes
    public SerializableEvent[] finalEvents; // only for Event nodes
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
    public SerializableEvent(Object target, string methodName, SerializableValue parameter)
    {
        this.target = target;
        this.methodName = methodName;
        this.parameter = parameter;
    }

    public Object target;
    public string methodName;
    public SerializableValue parameter;

    public void Invoke()
    {
        if (target == null || string.IsNullOrEmpty(methodName))
        {
            Debug.LogWarning("Event target or method name is null.");
            return;
        }

        var method = target.GetType().GetMethod(methodName);
        if (method == null)
        {
            Debug.LogWarning($"Method {methodName} not found on target {target.name}.");
            return;
        }

        var paramValue = parameter?.GetValue();
        if (paramValue != null)
        {
            method.Invoke(target, new object[] { paramValue });
        }
        else
        {
            method.Invoke(target, null);
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
            DSP_EditorWindow.Open(graphAsset); // your custom GraphView window
            return true; // tells Unity "I handled this"
        }

        return false;
    }
}
