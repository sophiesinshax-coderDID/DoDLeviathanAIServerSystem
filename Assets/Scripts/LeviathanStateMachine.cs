using System;
using UnityEngine;
using UnityEngine.Events;

namespace DivisionOfDivinity.AI
{
    public enum LeviathanState
    {
        Idle,
        Processing,
        SearchingWeb,
        Speaking,
        Error,
        // --- Future Extensions (Combat, Minions, Infections) ---
        ExecutingCommand,
        SpreadingInfection,
        ManagingMinions
    }

    [Serializable] 
    public class StateChangedEvent : UnityEvent<LeviathanState, string> { }

    public class LeviathanStateMachine : MonoBehaviour
    {
        [Header("State Tracking")]
        [SerializeField] private LeviathanState currentState = LeviathanState.Idle;
        [SerializeField] private string currentActivityDetails = "System Standby";

        [Header("State Events")]
        public StateChangedEvent OnStateChanged;

        public LeviathanState CurrentState => currentState;
        public string CurrentActivityDetails => currentActivityDetails;
        public float TimeInCurrentState { get; private set; }

        private void Update()
        {
            TimeInCurrentState += Time.deltaTime;
        }

        public void ChangeState(LeviathanState newState, string details = "")
        {
            if (currentState == newState && currentActivityDetails == details) return;

            currentState = newState;
            currentActivityDetails = string.IsNullOrEmpty(details) 
                ? GetDefaultDetailsForState(newState) 
                : details;

            TimeInCurrentState = 0f;

            Debug.Log($"[Leviathan State Machine]: Transformed state to -> {currentState} | Details: {currentActivityDetails}");
            OnStateChanged?.Invoke(currentState, currentActivityDetails);
        }

        private string GetDefaultDetailsForState(LeviathanState state)
        {
            return state switch
            {
                LeviathanState.Idle => "Observing environment parameters...",
                LeviathanState.Processing => "Synthesizing response through core neural net...",
                LeviathanState.SearchingWeb => "Querying external data streams...",
                LeviathanState.Speaking => "Outputting voice payload...",
                LeviathanState.Error => "System fault encountered.",
                LeviathanState.ExecutingCommand => "Executing engine directive...",
                LeviathanState.SpreadingInfection => "Propagating corruption vectors...",
                LeviathanState.ManagingMinions => "Issuing unit commands...",
                _ => "Unknown state"
            };
        }
    }
}