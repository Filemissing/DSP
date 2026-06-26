using System;
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

        private List<Func<bool>> _subscriptions = new();

        private void OnEnable()
        {
            _subscriptions.Clear();

            for (int i = 0; i < eventObjects.Count; i++)
            {
                var eventObject = eventObjects.ElementAt(i);
                if (eventObject == null || unityEvents[i] == null)
                    return;

                int index = i;
                Func<bool> handler = () =>
                {
                    unityEvents[index].Invoke();
                    return true;
                };

                _subscriptions.Add(handler);
                eventObject.Subscribe(handler);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                var eventObject = eventObjects.ElementAt(i);
                if (eventObject == null) continue;

                eventObject.Unsubscribe(_subscriptions[i]);
            }

            _subscriptions.Clear();
        }
    }
}
