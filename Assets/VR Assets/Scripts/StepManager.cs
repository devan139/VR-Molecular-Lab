using System.Collections.Generic;
using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls step-by-step visibility of UI panel GameObjects.
    /// Calling Next() hides the current step and shows the next one (wraps around).
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        [Tooltip("Add one GameObject per step. Only the active step is visible at a time.")]
        [SerializeField] private List<GameObject> steps = new List<GameObject>();

        private int currentIndex = 0;

        private void Start()
        {
            // Ensure only the first step is visible at startup
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    steps[i].SetActive(i == 0);
            }
        }

        public void Next()
        {
            if (steps.Count == 0) return;

            if (steps[currentIndex] != null)
                steps[currentIndex].SetActive(false);

            currentIndex = (currentIndex + 1) % steps.Count;

            if (steps[currentIndex] != null)
                steps[currentIndex].SetActive(true);
        }
    }
}
