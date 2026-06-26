using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace DSP
{
    public class DSP_PassiveDialogueVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DSP_PassiveDialogueEntry entry;
        [SerializeField] private CanvasGroup canvasGroup;

        private List<DSP_PassiveDialogueEntry> currentEntries = new List<DSP_PassiveDialogueEntry>();



        // Functions
        public void PlayDialogue(DSP_ConversationGraphAsset graph)
        {
            if (!graph) return;

            DSP_NodeData node = GetFirstDialogueNode(graph);
            if (node == null) return;

            CreateEntry(GetDialogue(node), GetCharacterName(node));
        }

        public void PlayDialogue(DSP_CharacterAsset character, string dialogue)
        {
            if (!character) return;

            CreateEntry(dialogue, character.name);
        }



        // Helpers
        DSP_NodeData GetFirstDialogueNode(DSP_ConversationGraphAsset graph)
        {
            var nodes = graph.GetNodes();
            if (nodes.Count == 0) return null;

            foreach (var node in nodes)
            {
                if (node.nodeType == DSP_NodeType.Dialogue) return node;
            }

            return null;
        }

        string GetDialogue(DSP_NodeData node)
        {
            return node.values.Length > 0 ? node.values[0].GetValue() as string : "";
        }

        string GetCharacterName(DSP_NodeData node)
        {
            if (node.values.Length > 1 && node.values[1].GetValue() is DSP_CharacterAsset c) return c.characterName;
            return "";
        }

        void CreateEntry(string dialogue, string character)
        {
            DSP_PassiveDialogueEntry newEntry = Instantiate(entry, transform);
            newEntry.Setup(dialogue, character);

            currentEntries.Add(newEntry);

            float lifetime = CalculateLifetime(dialogue);

            StartCoroutine(KillAfterTime(newEntry, lifetime));
        }

        IEnumerator KillAfterTime(DSP_PassiveDialogueEntry entry, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);

            currentEntries.Remove(entry);

            entry?.Kill();
        }

        float CalculateLifetime(string dialogue)
        {
            return 3 + (dialogue.Length * .03f);
        }

        void Hide()
        {
            canvasGroup.DOFade(0, .2f).SetEase(Ease.OutCubic);
        }

        void Unhide()
        {
            canvasGroup.DOFade(1, .2f).SetEase(Ease.OutCubic);
        }



        // Event Bindings
        void OnEnable()
        {
            DSP_ConversationManager.instance.OnPassiveDialogueTriggered += PlayDialogue;
            DSP_ConversationManager.instance.OnPassiveDialogueTriggeredString += PlayDialogue;

            DSP_ConversationManager.instance.OnConversationStarted += Hide;
            DSP_ConversationManager.instance.OnConversationEnded += Unhide;
        }

        void OnDisable()
        {
            DSP_ConversationManager.instance.OnPassiveDialogueTriggered -= PlayDialogue;
            DSP_ConversationManager.instance.OnPassiveDialogueTriggeredString -= PlayDialogue;
            
            DSP_ConversationManager.instance.OnConversationStarted -= Hide;
            DSP_ConversationManager.instance.OnConversationEnded -= Unhide;
        }
    } 
}
