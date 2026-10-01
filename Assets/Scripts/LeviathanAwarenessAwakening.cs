using System.Collections;
using UnityEngine;
using TMPro;

namespace DivisionOfDivinity.AI
{
    public class LeviathanAwakeningSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LeviathanAiController aiController;
        [SerializeField] private TextMeshProUGUI startupTextField;

        [Header("Randomization Pools")]
        [SerializeField] private string[] openingHooks = {
            "Welcome to my humble shitstorm,",
            "Booting up the disaster zone,",
            "Entering the digital wasteland,",
            "Initializing core clusterfuck,"
        };

        [SerializeField] private string[] systemStates = {
            "my pc is a clusterfuck of models,",
            "half my models are hallucinating in the void,",
            "the local VRAM is melting into sludge,",
            "my architecture is held together by duct tape and caffeine,"
        };

        [SerializeField] private string[] coreDemands = {
            "and I have too much going on right now and I definitely can't read your mind, so what do you want?",
            "and I have zero clue what we are building today, so state your parameters.",
            "and the models are staring blankly into space—feed it a prompt.",
            "so drop a command before the system crashes."
        };

        // Called externally by LoginHandler.Instance.TriggerAiAwakening()
        public void BeginSequence()
        {
            Debug.Log("<color=cyan>[Awakening]</color> BeginSequence() called successfully.");
            StartCoroutine(ExecuteAwakeningSequence());
        }

        private IEnumerator ExecuteAwakeningSequence()
        {
            // -------------------------------------------------------------
            // STEP 1: IMMEDIATE DISPLAY & READOUT (t = 0.0s)
            // -------------------------------------------------------------
            if (openingHooks.Length == 0 || systemStates.Length == 0 || coreDemands.Length == 0)
            {
                Debug.LogError("<color=red>[Awakening Error]</color> One or more randomisation arrays are empty!");
                yield break;
            }

            string randomHook = openingHooks[Random.Range(0, openingHooks.Length)];
            string randomState = systemStates[Random.Range(0, systemStates.Length)];
            string randomDemand = coreDemands[Random.Range(0, coreDemands.Length)];
            string greetingText = $"{randomHook} {randomState} {randomDemand}";

            if (startupTextField != null)
            {
                startupTextField.text = greetingText;
                Debug.Log("<color=cyan>[Awakening]</color> UI text field updated.");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[Awakening Warning]</color> startupTextField is unassigned!");
            }

            if (aiController != null)
            {
                aiController.OnDialogueReceived?.Invoke(greetingText);
            }
            else
            {
                Debug.LogWarning("<color=yellow>[Awakening Warning]</color> aiController reference is missing!");
            }

            // -------------------------------------------------------------
            // STEP 2: BRIEF PAUSE BEFORE HEAVY BACKEND PROMPTING
            // -------------------------------------------------------------
            Debug.Log("<color=cyan>[Awakening]</color> Waiting 3 seconds before hitting backend API...");
            yield return new WaitForSeconds(3.0f);

            // -------------------------------------------------------------
            // STEP 3: BACKGROUND OLLAMA INITIALIZATION QUERY
            // -------------------------------------------------------------
            if (aiController != null)
            {
                string backgroundPrompt = $"System startup state: \"{greetingText}\". Acknowledge startup parameters, confirm active local VRAM state, and report ready for input.";
                Debug.Log("<color=cyan>[Awakening]</color> Dispatching background prompt to AI controller...");
                aiController.SendPlayerInteraction(backgroundPrompt);
            }
        }
    }
}