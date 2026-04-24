using System.Collections;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Handles UI panel animations for the Chemistry System.
    ///
    /// USAGE:
    /// - Attach to any scene GameObject.
    /// - Assign the panel's RectTransform and (optionally) its CanvasGroup.
    /// - The panel animates automatically when a molecule is formed via
    ///   BondManager.OnMoleculeFormed, or call ShowMoleculePanel() manually.
    ///
    /// ANIMATION:
    /// Scale: 0.8 → 1.0 with smooth ease-out (no external tweening library needed).
    /// Fade:  0.0 → 1.0 on the CanvasGroup alpha (optional).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ─── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel Reference")]
        [Tooltip("The RectTransform of the UI panel to animate.")]
        [SerializeField] private RectTransform panelTransform;

        [Tooltip("Optional CanvasGroup for fade-in effect. Leave empty to skip fading.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Animation Settings")]
        [Tooltip("Scale the panel starts at before animating in.")]
        [SerializeField] private float startScale = 0.8f;

        [Tooltip("Target scale the panel reaches at the end of the animation.")]
        [SerializeField] private float endScale = 1.0f;

        [Tooltip("Duration of the scale and fade animation in seconds.")]
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

        private void Start()
        {
            // Start hidden so the first ShowMoleculePanel() is always an animation
            SetPanelImmediate(startScale, alpha: 0f, active: false);
        }

        // ─── Event Handler ─────────────────────────────────────────────────────

        private void HandleMoleculeFormed(MoleculeData data)
        {
            ShowMoleculePanel();
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Animates the panel from 0.8 → 1.0 scale with an optional alpha fade-in.
        /// Safe to call multiple times — cancels any running animation first.
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
            StartCoroutine(AnimatePanel());
        }

        /// <summary>
        /// Hides the panel instantly without animation.
        /// </summary>
        public void HidePanel()
        {
            StopAllCoroutines();
            SetPanelImmediate(startScale, alpha: 0f, active: false);
        }

        // ─── Animation Coroutine ───────────────────────────────────────────────

        private IEnumerator AnimatePanel()
        {
            float elapsed = 0f;

            // Initialise at the "from" state
            panelTransform.localScale = Vector3.one * startScale;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;

                // Normalised time 0 → 1
                float t = Mathf.Clamp01(elapsed / animDuration);

                // Ease-out quad: fast start, decelerates to final value
                // f(t) = 1 - (1-t)²
                float eased = 1f - (1f - t) * (1f - t);

                // Scale
                float scale = Mathf.LerpUnclamped(startScale, endScale, eased);
                panelTransform.localScale = Vector3.one * scale;

                // Fade (only if CanvasGroup is assigned)
                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = eased;

                yield return null;
            }

            // Snap to final values to avoid floating-point drift
            panelTransform.localScale = Vector3.one * endScale;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        }

        // ─── Helper ────────────────────────────────────────────────────────────

        private void SetPanelImmediate(float scale, float alpha, bool active)
        {
            if (panelTransform == null) return;
            panelTransform.localScale = Vector3.one * scale;
            panelTransform.gameObject.SetActive(active);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = alpha;
        }
    }
}
