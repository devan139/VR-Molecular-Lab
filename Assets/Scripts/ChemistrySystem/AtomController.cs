using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Represents a single interactable atom in the VR environment.
    ///
    /// Implements IBondable so it can participate in incremental molecule building
    /// alongside MoleculeController objects (e.g. H2 + O → H2O).
    ///
    /// WHY INCREMENTAL?
    /// In VR, a user can hold at most one object per hand. Requiring all atoms to
    /// be brought together simultaneously (e.g., 4 Hydrogens + Carbon for CH4)
    /// is physically impossible. Incremental building lets the user bond two
    /// objects at a time: H+H → H2, then H2+H → H3 (unstable, no recipe),
    /// or H2+O → H2O.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class AtomController : MonoBehaviour, IBondable
    {
        [Header("Atom Properties")]
        [Tooltip("The chemical element this object represents.")]
        [SerializeField] private AtomType atomType;

        [Tooltip("Seconds after spawning before this atom can participate in bonding. " +
                 "Prevents freshly broken-apart atoms from immediately rebonding.")]
        [SerializeField] private float bondingStartDelay = 0.4f;

        // ─── Private State ─────────────────────────────────────────────────────
        /// <summary>
        /// False during the bondingStartDelay immunity window after spawning.
        /// Prevents atoms spawned by BreakMolecule() from instantly re-forming.
        /// </summary>
        private bool isBondingEnabled = false;

        // ─── IBondable Implementation ──────────────────────────────────────────

        /// <summary>
        /// An individual atom's composition is just itself.
        /// Returns a new list each call to prevent external mutation.
        /// </summary>
        public List<AtomType> GetComposition() => new List<AtomType> { atomType };

        /// <summary>
        /// Satisfies IBondable — gives BondManager a reference to destroy this
        /// GameObject on a successful bond without needing a cast.
        /// </summary>
        public GameObject BondableGameObject => gameObject;

        /// <summary>
        /// Tracks all IBondable objects currently within trigger range.
        /// Supports both AtomControllers and MoleculeControllers.
        /// </summary>
        public HashSet<IBondable> ProximalBondables { get; private set; } = new HashSet<IBondable>();

        // ─── Read-only convenience accessor (used internally) ──────────────────
        public AtomType Type => atomType;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            ValidateComponents();
        }

        private void Start()
        {
            // Enable bonding after a short delay so atoms spawned by BreakMolecule()
            // don't immediately re-bond before they've scattered apart.
            StartCoroutine(EnableBondingAfterDelay());
        }

        private IEnumerator EnableBondingAfterDelay()
        {
            yield return new WaitForSeconds(bondingStartDelay);
            isBondingEnabled = true;
        }

        private void ValidateComponents()
        {
            bool hasTrigger = false;
            foreach (var col in GetComponents<Collider>())
            {
                if (col.isTrigger) { hasTrigger = true; break; }
            }

            if (!hasTrigger)
            {
                Debug.LogWarning($"[AtomController] {gameObject.name} is missing a trigger collider. Proximity detection will fail.", this);
            }
        }

        // ─── Proximity Detection ───────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!isBondingEnabled) return; // Still in immunity window after spawn

            // Use GetComponentInParent so we find IBondable even when the trigger
            // collider lives on a child GameObject rather than the root prefab object.
            IBondable otherBondable = other.GetComponentInParent<IBondable>();

            if (otherBondable == null)
            {
                Debug.Log($"[AtomController] {gameObject.name} touched '{other.gameObject.name}' — not an IBondable, ignored.");
                return;
            }

            // Ignore self-contact (can happen with multi-collider setups)
            if (otherBondable == (IBondable)this) return;

            Debug.Log($"[AtomController] {gameObject.name} detected IBondable: {otherBondable.BondableGameObject.name} " +
                      $"(composition: [{string.Join(", ", otherBondable.GetComposition())}])");

            if (ProximalBondables.Add(otherBondable))
            {
                // Reciprocate: add ourselves to the other object's proximal set
                // so BFS in BondManager finds a consistent bidirectional graph
                otherBondable.ProximalBondables.Add(this);

                if (BondManager.Instance != null)
                    BondManager.Instance.EvaluateCluster(this);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // FALLBACK: OnTriggerEnter can be missed on fast contacts or if the atom
            // was spawned while already overlapping. OnTriggerStay retries every physics
            // frame while objects remain in contact, fixing intermittent O2 failures.
            if (!isBondingEnabled) return;

            IBondable otherBondable = other.GetComponentInParent<IBondable>();
            if (otherBondable == null) return;
            if (otherBondable == (IBondable)this) return;

            // Only act if not yet registered — avoids spamming EvaluateCluster
            if (!ProximalBondables.Contains(otherBondable))
            {
                Debug.Log($"[AtomController] {gameObject.name} re-detected via TriggerStay: {otherBondable.BondableGameObject.name}");
                ProximalBondables.Add(otherBondable);
                otherBondable.ProximalBondables.Add(this);

                if (BondManager.Instance != null)
                    BondManager.Instance.EvaluateCluster(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IBondable otherBondable = other.GetComponentInParent<IBondable>();
            if (otherBondable == null) return;
            if (otherBondable == (IBondable)this) return;

            ProximalBondables.Remove(otherBondable);

            // Keep the graph consistent on both sides
            otherBondable.ProximalBondables.Remove(this);
        }
    }
}
