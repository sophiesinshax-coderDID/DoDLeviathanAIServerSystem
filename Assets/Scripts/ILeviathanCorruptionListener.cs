using UnityEngine;

namespace DivisionOfDivinity.AI
{
    // Interface for objects that react when Leviathan adjusts world corruption
    public interface ILeviathanCorruptionListener
    {
        void OnCorruptionChanged(float deltaAmount);
    }

    // Interface for objects that display AI dialogue
    public interface ILeviathanDialogueDisplay
    {
        void DisplayDialogue(string speakerName, string message);
    }
}