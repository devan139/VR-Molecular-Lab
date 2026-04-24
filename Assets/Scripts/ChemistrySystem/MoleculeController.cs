using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Attached to every molecule prefab that is the result of a bonding reaction.
    ///
    /// SINGLE SOURCE OF TRUTH DESIGN:
    /// Previously, composition was duplicated — once in MoleculeData.requiredAtoms
    /// and again in MoleculeController.initialComposition. This caused bugs when
    /// the two values drifted out of sync (e.g., H2O recipe said [H,H,O] but the
    /// manually placed H2 object had an empty or wrong initialComposition list).
    ///
    /// Now: MoleculeData is the ONLY place composition is defined.
    /// MoleculeController holds a reference to its MoleculeData and reads
    /// composition exclusively from moleculeData.requiredAtoms.
    ///
    /// USAGE:
    /// - Attach this script to every molecule prefab.
    /// - Assign the correct MoleculeData asset in the Inspector (e.g., H2 asset → H2 prefab).
    /// - BondManager calls Initialize(moleculeData) after spawning — this overrides
    ///   the Inspector assignment, so both manual and spawned molecules work correctly.
    /// - The prefab MUST also have an XRGrabInteractable so it remains pick-up-able.
    /// - The prefab MUST have at least one trigger Collider for proximity detection.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class MoleculeController : MonoBehaviour, IBondable
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        /// <summary>
        /// SINGLE SOURCE OF TRUTH: Assign the correct MoleculeData ScriptableObject
        /// here in the prefab Inspector (e.g., the H2 asset on the H2 prefab).
        ///
        /// WHY A REFERENCE INSTEAD OF A LIST:
        /// Storing a raw List<AtomType> here duplicates data already defined in
        /// MoleculeData.requiredAtoms and causes desync bugs. Holding a reference
        /// ensures composition is always read from one authoritative asset.
        ///
        /// BondManager will call Initialize() and override this at runtime when
        /// spawning, so manually placed scene objects and spawned objects both work.
        /// </summary>
        [Header("Molecule Identity")]
        [Tooltip("Assign the MoleculeData ScriptableObject that matches this prefab (e.g., H2 asset for the H2 prefab). " +
                 "BondManager overrides this automatically at runtime when spawning.")]
        [SerializeField] private MoleculeData moleculeData;

        [Header("Breaking")]
        [Tooltip("Shared AtomFactory asset that maps AtomType → prefab. " +
                 "Create one via: Right-click → Create → Chemistry System → Atom Factory.")]
        [SerializeField] private AtomFactory atomFactory;

        [Tooltip("Radius within which broken atoms are randomly scattered around the molecule position. " +
                 "Keep this larger than the atom's trigger collider radius to prevent immediate rebonding.")]
        [SerializeField] private float breakScatterRadius = 0.5f;

        // ─── Private State ────────────────────────────────────────────────────

        /// <summary>Guard flag — prevents BreakMolecule from firing more than once.</summary>
        private bool isBreaking = false;

        /// <summary>True while the player is holding this molecule.</summary>
        private bool isGrabbed = false;

        /// <summary>Cached reference to the XRGrabInteractable on this object.</summary>
        private XRGrabInteractable grabInteractable;

        // ─── IBondable Implementation ──────────────────────────────────────────

        /// <summary>
        /// Returns the composition by reading directly from the assigned MoleculeData.
        ///
        /// HOW INCREMENTAL BONDING WORKS:
        /// When H+H react → H2 spawns with moleculeData = H2 ScriptableObject.
        /// GetComposition() returns [Hydrogen, Hydrogen] from H2.requiredAtoms.
        /// When H2 + O react → BondManager merges [H,H] + [O] = [H,H,O] → matches H2O.
        /// The composition is NEVER stored redundantly.
        /// </summary>
        public List<AtomType> GetComposition()
        {
            if (moleculeData == null)
            {
                Debug.LogWarning($"[MoleculeController] {gameObject.name} has no MoleculeData assigned! " +
                                 "Assign the correct MoleculeData asset in the Inspector.");
                return new List<AtomType>();
            }

            if (moleculeData.requiredAtoms == null || moleculeData.requiredAtoms.Count == 0)
            {
                Debug.LogWarning($"[MoleculeController] {gameObject.name} → MoleculeData '{moleculeData.moleculeName}' " +
                                 "has an empty requiredAtoms list. Check the ScriptableObject.");
                return new List<AtomType>();
            }

            // Return a copy — prevents external code from mutating the ScriptableObject list
            return new List<AtomType>(moleculeData.requiredAtoms);
        }

        /// <summary>
        /// Gives BondManager a handle to destroy this object without casting.
        /// </summary>
        public GameObject BondableGameObject => gameObject;

        /// <summary>
        /// Tracks all IBondable objects currently within trigger range.
        /// Supports both AtomControllers and MoleculeControllers.
        /// </summary>
        public HashSet<IBondable> ProximalBondables { get; private set; } = new HashSet<IBondable>();

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by BondManager immediately after spawning this molecule prefab.
        ///
        /// Assigns the MoleculeData that matched the bonding recipe so that:
        /// 1. GetComposition() returns the correct atom list for future bonding steps.
        /// 2. There is no manual composition list to keep in sync.
        ///
        /// This replaces the old SetComposition(List<AtomType>) approach.
        /// The composition is now DERIVED from data, not stored separately.
        /// </summary>
        /// <param name="data">The MoleculeData recipe that was matched for this reaction.</param>
        public void Initialize(MoleculeData data)
        {
            if (data == null)
            {
                Debug.LogError($"[MoleculeController] Initialize() called on {gameObject.name} with null MoleculeData!");
                return;
            }

            moleculeData = data;
            Debug.Log($"[MoleculeController] {gameObject.name} initialized from MoleculeData '{data.moleculeName}'. " +
                      $"Composition: [{string.Join(", ", data.requiredAtoms)}]");
        }

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Cache and wire the XRGrabInteractable events
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(_ => isGrabbed = true);
                grabInteractable.selectExited.AddListener(_ => isGrabbed = false);

                // activated fires when the player presses the activation input
                // (trigger) while already holding the object — no raw InputAction needed.
                grabInteractable.activated.AddListener(_ => OnActivateWhileGrabbed());
            }
            else
            {
                Debug.LogWarning($"[MoleculeController] {gameObject.name} is missing XRGrabInteractable — " +
                                 "breaking via trigger will not work.");
            }

            // If moleculeData was set in the Inspector (manually placed prefab),
            // log it for verification so it's visible in the console at startup.
            if (moleculeData != null)
            {
                Debug.Log($"[MoleculeController] {gameObject.name} loaded from Inspector — " +
                          $"MoleculeData: '{moleculeData.moleculeName}', " +
                          $"Composition: [{string.Join(", ", moleculeData.requiredAtoms)}]");
            }
            else
            {
                Debug.LogWarning($"[MoleculeController] {gameObject.name} has no MoleculeData assigned in the Inspector. " +
                                 "This is fine if BondManager will spawn it — Initialize() will assign data at runtime.");
            }

            ValidateComponents();
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
                Debug.LogWarning($"[MoleculeController] {gameObject.name} is missing a trigger collider. " +
                                 "It will not participate in further bonding.", this);
            }
        }

        // ─── Breaking Logic ────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player presses the activation input while holding this molecule.
        /// Only proceeds if the molecule is currently grabbed and not already breaking.
        /// </summary>
        private void OnActivateWhileGrabbed()
        {
            if (!isGrabbed) return;
            BreakMolecule();
        }

        /// <summary>
        /// Dissolves the molecule back into its constituent atoms.
        ///
        /// HOW IT WORKS:
        /// Reads the atom list from moleculeData.requiredAtoms (single source of truth).
        /// Uses AtomFactory to instantiate each atom prefab at a randomised offset
        /// from the molecule's position so atoms don't all spawn at the same point.
        /// Destroys the molecule GameObject after spawning all atoms.
        /// </summary>
        public void BreakMolecule()
        {
            // Guard: prevent double-trigger (e.g., rapid button presses)
            if (isBreaking) return;
            isBreaking = true;

            if (moleculeData == null)
            {
                Debug.LogError($"[MoleculeController] Cannot break {gameObject.name} — no MoleculeData assigned.");
                isBreaking = false;
                return;
            }

            if (atomFactory == null)
            {
                Debug.LogError($"[MoleculeController] Cannot break {gameObject.name} — AtomFactory is not assigned in the Inspector.");
                isBreaking = false;
                return;
            }

            List<AtomType> composition = GetComposition();
            if (composition.Count == 0)
            {
                Debug.LogWarning($"[MoleculeController] {gameObject.name} has no atoms to break into.");
                isBreaking = false;
                return;
            }

            Debug.Log($"[MoleculeController] Breaking molecule: {moleculeData.formula} → spawning {composition.Count} atoms");

            Vector3 origin = transform.position;

            foreach (AtomType atomType in composition)
            {
                // Apply a small random 3D offset so atoms don't all overlap
                Vector3 randomOffset = Random.insideUnitSphere * breakScatterRadius;
                atomFactory.SpawnAtom(atomType, origin + randomOffset, Quaternion.identity);
            }

            // ── Play break sound via AudioManager ──────────────────────────────────
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBreakSound(transform.position);
            else
                Debug.LogWarning("[MoleculeController] AudioManager instance not found in scene.");

            // Destroy this molecule — atoms are now independent objects in the scene
            Destroy(gameObject);
        }

        // ─── Proximity Detection ────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            // Use GetComponentInParent so we find IBondable even when the trigger
            // collider lives on a child GameObject rather than the root prefab object.
            IBondable otherBondable = other.GetComponentInParent<IBondable>();

            if (otherBondable == null)
            {
                Debug.Log($"[MoleculeController] {gameObject.name} touched '{other.gameObject.name}' — not an IBondable, ignored.");
                return;
            }

            // Ignore self-contact (can happen with multi-collider setups)
            if (otherBondable == (IBondable)this) return;

            Debug.Log($"[MoleculeController] {gameObject.name} detected IBondable: {otherBondable.BondableGameObject.name} " +
                      $"(composition: [{string.Join(", ", otherBondable.GetComposition())}])");

            if (ProximalBondables.Add(otherBondable))
            {
                // Reciprocate so BFS graph is consistent in both directions
                otherBondable.ProximalBondables.Add(this);

                if (BondManager.Instance != null)
                {
                    BondManager.Instance.EvaluateCluster(this);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IBondable otherBondable = other.GetComponentInParent<IBondable>();
            if (otherBondable == null) return;
            if (otherBondable == (IBondable)this) return;

            ProximalBondables.Remove(otherBondable);
            otherBondable.ProximalBondables.Remove(this);
        }
    }
}
