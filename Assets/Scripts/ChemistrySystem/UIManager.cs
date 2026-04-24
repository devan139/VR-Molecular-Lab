using System.Collections;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Handles UI panel scale animation when a molecule is formed.
    /// Subscribes to BondManager.OnMoleculeFormed automatically.
    /// Call ShowMoleculePanel() manually if needed from other scripts.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ─── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel Reference")]
        [Tooltip("The RectTransform of the UI panel to animate.")]
        [SerializeField] private RectTransform panelTransform;

        [Header("Animation Settings")]
        [Tooltip("Scale the panel starts at before animating in.")]
        [SerializeField] private float startScale = 0.8f;

        [Tooltip("Target scale the panel reaches at the end of the animation.")]
        [SerializeField] private float endScale = 1.0f;

        [Tooltip("Duration of the scale animation in seconds.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float animDuration = 0.25f;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            BondManager.OnMoleculeFormed += HandleMoleculeFormed;
        }

        private void OnDisable()
        {
            BondManager.OnMoleculeFormed -= HandleMoleculeFormed;
        }

        // ─── Event Handler ─────────────────────────────────────────────────────

        private void HandleMoleculeFormed(MoleculeData data)
        {
            ShowMoleculePanel();
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the panel and animates it from startScale → endScale
        /// with an ease-out curve. Safe to call while already animating.
        /// </summary>
        public void ShowMoleculePanel()
        {
            if (panelTransform == null)
            {
                Debug.LogWarning("[UIManager] panelTransform is not assigned.");
                return;
            }

            StopAllCoroutines();
            panelTransform.gameObject.SetActive(true);
            panelTransform.localScale = Vector3.one * startScale;
            StartCoroutine(ScalePanel());
        }

        /// <summary>
        /// Hides the panel instantly without animation.
        /// </summary>
        public void HidePanel()
        {
            StopAllCoroutines();
            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * startScale;
                panelTransform.gameObject.SetActive(false);
            }
        }

        // ─── Animation Coroutine ───────────────────────────────────────────────

        private IEnumerator ScalePanel()
        {
            float elapsed = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);

                // Ease-out quad: fast start, soft landing
                float eased = 1f - (1f - t) * (1f - t);
                panelTransform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, endScale, eased);

                yield return null;
            }

            // Snap to exact final value
            panelTransform.localScale = Vector3.one * endScale;
        }
    }
}
