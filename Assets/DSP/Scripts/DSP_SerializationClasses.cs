using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public enum DSP_NodeType
{
    Start,
    End,
    Dialogue,
    Choice,
    StaticEvent,
    SceneEvent,
    Condition
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
