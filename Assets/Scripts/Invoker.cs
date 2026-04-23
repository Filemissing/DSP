using UnityEngine;

public class Invoker : MonoBehaviour
{
    [SerializeField] DSP_ConversationGraphAsset conversation;

    private void Start()
    {
        foreach (DSP_NodeData node in conversation.GetNodes())
        {
            if (node.nodeType == DSP_NodeType.StaticEvent)
            {
                foreach (SerializableEvent e in node.finalEvents)
                {
                    e.Invoke();
                }
            }
        }
    }
}
