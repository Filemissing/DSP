using UnityEngine;

public class ExampleScript : MonoBehaviour
{
    public void DoSomething(int number)
    {
        Debug.Log($"The number is: {number}");
    }

    public static void Main()
    {
        ExampleScript example = new ExampleScript();
        example.DoSomething(42);
    }

    public static bool False()
    {
        return false;
    }

    public static bool Return(bool value)
    {
        return value;
    }
}
