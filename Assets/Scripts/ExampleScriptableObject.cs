using UnityEngine;

[CreateAssetMenu(fileName = "ExampleScriptableObject", menuName = "Scriptable Objects/ExampleScriptableObject")]
public class ExampleScriptableObject : ScriptableObject
{
    public void Print(int number)
    {
        Debug.Log($"The number is: {number}");
    }

    public void Shout()
    {
        Debug.Log("Hello from the ScriptableObject!");
    }

    public bool True()
    {
        return true;
    }
}
