using System;
using UnityEngine;

namespace DivisionOfDivinity.AI
{
    [Serializable]
    public class AspectData
    {
        public string characterName = "Leviathan";
        [Range(-100f, 100f)] public float karma = 0f;          // -100 (Ruthless) to +100 (Heroic)
        [Range(0f, 1f)] public float corruption = 0f;          // 0.0 (Pure) to 1.0 (Fully Corrupted)
        [Range(0f, 100f)] public float heart = 50f;            // 0 (Cold/Logical) to 100 (Empathetic/Emotional)
		public float soul = 100f;
        public string dominantTrait = "Neutral Observer";      // Current moral alignment focus
    }

    public class CharacterAspects : MonoBehaviour
    {
        public AspectData aspects = new AspectData();

		public void ModifySoul(float amount)
		{
			aspects.soul = Mathf.Clamp(aspects.soul + amount, 0f, 100f);
			
			// Optional: Trigger event or check threshold effects (e.g., Soul Fracture)
			if (aspects.soul <= 20f)
			{
				Debug.LogWarning("[Soul System]: Player soul is fracturing! Vulnerability increased.");
			}
		}
	
        public string GetSerializedAspects()
        {
            return JsonUtility.ToJson(aspects);
        }

        public void ModifyKarma(float amount)
        {
            aspects.karma = Mathf.Clamp(aspects.karma + amount, -100f, 100f);
        }

        public void ModifyCorruption(float amount)
        {
            aspects.corruption = Mathf.Clamp01(aspects.corruption + amount);
        }

        public void ModifyHeart(float amount)
        {
            aspects.heart = Mathf.Clamp(aspects.heart + amount, 0f, 100f);
        }

        // Aliases for backwards compatibility
        public void AdjustKarma(float amount) => ModifyKarma(amount);
        public void AdjustCorruption(float amount) => ModifyCorruption(amount);
        public void AdjustHeart(float amount) => ModifyHeart(amount);
    }
}