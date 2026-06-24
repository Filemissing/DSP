using DSP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[CustomEditor(typeof(DSP_EventHandler))]
public class DSP_EventHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DSP_EventHandler myTarget = target as DSP_EventHandler;

        for (int i = 0; i < myTarget.eventObjects.Count; i++)
        {
            DSP_SceneEvent eventObject = myTarget.eventObjects[i];

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(eventObject != null ? eventObject.name : "Assign new event", EditorStyles.boldLabel);
            myTarget.eventObjects[i] = EditorGUILayout.ObjectField(eventObject, typeof(DSP_SceneEvent), false) as DSP_SceneEvent;

            EditorGUILayout.EndHorizontal();

            if (eventObject != null)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unityEvents").GetArrayElementAtIndex(i), new GUIContent("Unity Event"));

            EditorGUILayout.Space(20);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Event"))
        {
            myTarget.eventObjects.Add(null);
            myTarget.unityEvents.Add(new UnityEvent());
        }

        if (GUILayout.Button("Remove Event"))
        {
            myTarget.eventObjects.RemoveAt(myTarget.eventObjects.Count - 1);
            myTarget.unityEvents.RemoveAt(myTarget.unityEvents.Count - 1);
        }

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
