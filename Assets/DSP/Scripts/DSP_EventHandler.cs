using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace DSP
{
    public class DSP_EventHandler : MonoBehaviour
    {
        [HideInInspector] public List<DSP_SceneEvent> eventObjects = new();
        [HideInInspector] public List<UnityEvent> unityEvents = new();

        private void Awake()
        {
            for (int i = 0; i < eventObjects.Count; i++)
            {
                var eventObject = eventObjects.ElementAt(i);

                if (eventObject == null || unityEvents[i] == null)
                    return; // early return if not all variables are set properly

                int index = i; // Capture the current index for the lambda
                eventObject.Subscribe(() =>
                {
                    unityEvents[index].Invoke();
                    return true;
                });
            }
        }
    }
}
