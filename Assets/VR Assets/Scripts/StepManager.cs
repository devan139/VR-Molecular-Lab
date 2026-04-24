using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls step-by-step visibility of UI panel GameObjects.
    /// Each step is a GameObject that is shown while active and hidden otherwise.
    /// Calling Next() advances to the next step in the list (wraps around).
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        class Step
        {
            [Tooltip("The GameObject to show when this step is active.")]
            [SerializeField]
            public GameObject stepObject;
        }

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

        int m_CurrentStepIndex = 0;

        private void Start()
        {
            // Ensure only the first step is visible at startup
            for (int i = 0; i < m_StepList.Count; i++)
            {
                if (m_StepList[i].stepObject != null)
                    m_StepList[i].stepObject.SetActive(i == 0);
            }
        }

        public void Next()
        {
            if (m_StepList.Count == 0) return;

            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);
            m_CurrentStepIndex = (m_CurrentStepIndex + 1) % m_StepList.Count;
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
        }
    }
}
