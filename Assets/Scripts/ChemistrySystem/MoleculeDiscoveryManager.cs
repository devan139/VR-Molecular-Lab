using System.Collections.Generic;
using TMPro;
using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.Events;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Tracks molecule formations and fires checkpoint events ONLY for molecules
    /// that are explicitly designated as checkpoints in the Inspector.
    ///
    /// CHECKPOINT DESIGN:
    /// Not every molecule the player forms should count as a level checkpoint.
    /// For example, H2 might be an intermediate step — only H2O is the real goal.
    /// By assigning a curated list of MoleculeData assets to `checkpointMolecules`,
    /// the designer controls exactly which formations trigger progression events,
    /// without touching any code.
    ///
    /// NON-CHECKPOINT MOLECULES:
    /// Molecules formed that are NOT in the checkpoint list are silently tracked
    /// in `discoveredMolecules` (for query purposes) but do NOT fire any event.
    ///
    /// DECOUPLED:
    /// This script fires UnityEvents only. UI, audio, and other systems subscribe
    /// in their own OnEnable without needing a reference to this manager.
    /// </summary>
    public class MoleculeDiscoveryManager : MonoBehaviour
    {
        // ─── Singleton ─────────────────────────────────────────────────────────

        public static MoleculeDiscoveryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[MoleculeDiscoveryManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCheckpointLookup();
        }

        // ─── Inspector: Checkpoint List ────────────────────────────────────────

        [Header("Checkpoints")]
        [Tooltip("Only molecules in this list will trigger OnCheckpointDiscovered. " +
                 "Drag MoleculeData ScriptableObject assets here. " +
                 "Order does not matter — each fires once on first formation.")]
        [SerializeField] private List<MoleculeData> checkpointMolecules = new List<MoleculeData>();

        // ─── Inspector: Molecule Info Panel ─────────────────────────────────────────

        [Header("Molecule Info Panel")]
        [Tooltip("Displays the common name of the discovered checkpoint molecule.")]
        [SerializeField] private TextMeshProUGUI moleculeNameText;

        [Tooltip("Displays the chemical formula (e.g., H2O).")]
        [SerializeField] private TextMeshProUGUI formulaText;

        [Tooltip("Displays the bond type (e.g., Covalent — Single Bond).")]
        [SerializeField] private TextMeshProUGUI bondTypeText;

        [Tooltip("Displays a bulleted list of all unique molecules discovered so far.")]
        [SerializeField] private TextMeshProUGUI discoveredListText;

        // ─── Inspector: Step Manager ─────────────────────────────────────────

        [Header("Step Manager")]
        [Tooltip("StepManager that advances to the next step each time a checkpoint molecule is discovered.")]
        [SerializeField] private StepManager stepManager;

        // ─── Events ────────────────────────────────────────────────────────────

        [Header("Events")]
        [Tooltip("Fired the first time a CHECKPOINT molecule is formed. " +
                 "Payload: molecule name (e.g., 'Water'). " +
                 "Wire to AudioSource.Play(), UIManager.ShowBanner(), Animator, etc.")]
        public UnityEvent<string> OnCheckpointDiscovered;

        [Tooltip("Fired when ALL checkpoint molecules have been discovered at least once. " +
                 "Use this to trigger a level-complete screen or unlock the next stage.")]
        public UnityEvent OnAllCheckpointsComplete;

        // ─── State ─────────────────────────────────────────────────────────────

        /// <summary>Fast lookup set built from checkpointMolecules list in Awake.</summary>
        private HashSet<string> checkpointNames = new HashSet<string>();

        /// <summary>Checkpoint molecule names that have been discovered so far.</summary>
        private HashSet<string> discoveredCheckpoints = new HashSet<string>();

        /// <summary>All molecule names formed this session (checkpoints and non-checkpoints).</summary>
        private HashSet<string> discoveredMolecules = new HashSet<string>();

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>Total unique molecules formed this session (including non-checkpoints).</summary>
        public int TotalDiscoveredCount => discoveredMolecules.Count;

        /// <summary>Number of checkpoint molecules discovered so far.</summary>
        public int CheckpointsDiscovered => discoveredCheckpoints.Count;

        /// <summary>Total number of designated checkpoints.</summary>
        public int TotalCheckpoints => checkpointNames.Count;

        /// <summary>True if every checkpoint molecule has been discovered at least once.</summary>
        public bool AllCheckpointsComplete => discoveredCheckpoints.Count == checkpointNames.Count
                                             && checkpointNames.Count > 0;

        /// <summary>Returns true if the given molecule name has been discovered (any molecule).</summary>
        public bool IsDiscovered(string moleculeName) => discoveredMolecules.Contains(moleculeName);

        /// <summary>Returns true if the given molecule name is a discovered checkpoint.</summary>
        public bool IsCheckpointDiscovered(string moleculeName) => discoveredCheckpoints.Contains(moleculeName);

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            // Initialize the list UI so it's not empty/showing placeholder text on start
            UpdateDiscoveredListUI();
        }

        private void OnEnable()
        {
            BondManager.OnMoleculeFormed += HandleMoleculeFormed;
        }

        private void OnDisable()
        {
            BondManager.OnMoleculeFormed -= HandleMoleculeFormed;
        }

        // ─── Internals ─────────────────────────────────────────────────────────

        /// <summary>
        /// Converts the Inspector list into a HashSet for O(1) lookup at runtime.
        /// Called once in Awake before any events can fire.
        /// </summary>
        private void BuildCheckpointLookup()
        {
            checkpointNames.Clear();
            foreach (var data in checkpointMolecules)
            {
                if (data == null) continue;
                checkpointNames.Add(data.moleculeName);
            }

            Debug.Log($"[MoleculeDiscoveryManager] {checkpointNames.Count} checkpoint(s) registered: " +
                      $"[{string.Join(", ", checkpointNames)}]");
        }

        /// <summary>
        /// Called every time any molecule is formed via BondManager.OnMoleculeFormed.
        /// Only checkpoint molecules trigger the discovery event.
        /// </summary>
        private void HandleMoleculeFormed(MoleculeData data)
        {
            if (data == null) return;

            string name = data.moleculeName;

            // Track all formations regardless of checkpoint status
            bool isNewDiscovery = discoveredMolecules.Add(name);

            if (isNewDiscovery)
            {
                UpdateDiscoveredListUI();
            }

            // ── Not a checkpoint — log silently and exit ───────────────────────
            if (!checkpointNames.Contains(name))
            {
                Debug.Log($"[MoleculeDiscoveryManager] '{name}' formed (not a checkpoint, skipped).");
                return;
            }

            // ── Checkpoint molecule — check if already discovered ──────────────
            if (!discoveredCheckpoints.Add(name))
            {
                Debug.Log($"[MoleculeDiscoveryManager] Checkpoint '{name}' formed again (already discovered).");
                return;
            }

            // ── First-time checkpoint discovery ────────────────────────────────
            Debug.Log($"[MoleculeDiscoveryManager] ★ Checkpoint discovered: {name} ({data.formula})! " +
                      $"Progress: {discoveredCheckpoints.Count}/{checkpointNames.Count}");

            // ── Update the info panel texts from MoleculeData ─────────────────────
            UpdateInfoPanel(data);

            // ── Play checkpoint sound via AudioManager ────────────────────────
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCheckpointSound();

            // ── Advance the step manager ───────────────────────────────────
            if (stepManager != null)
                stepManager.Next();
            else
                Debug.LogWarning("[MoleculeDiscoveryManager] StepManager is not assigned.");

            OnCheckpointDiscovered?.Invoke(name);

            // ── Check if all checkpoints are now complete ──────────────────────
            if (AllCheckpointsComplete)
            {
                Debug.Log($"[MoleculeDiscoveryManager] ★★ ALL CHECKPOINTS COMPLETE! " +
                          $"All {checkpointNames.Count} molecules discovered.");

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayAllCompleteSound();

                OnAllCheckpointsComplete?.Invoke();
            }
        }

        // ─── Info Panel Helper ─────────────────────────────────────────────────

        /// <summary>
        /// Writes molecule details from the MoleculeData asset into the three TMP text fields.
        /// Called only when a checkpoint molecule is discovered for the first time.
        /// </summary>
        private void UpdateInfoPanel(MoleculeData data)
        {
            if (moleculeNameText != null)
                moleculeNameText.text = "Molecule name: " + data.moleculeName;
            else
                Debug.LogWarning("[MoleculeDiscoveryManager] moleculeNameText is not assigned.");

            if (formulaText != null)
                formulaText.text = "Formula: " + data.formula;
            else
                Debug.LogWarning("[MoleculeDiscoveryManager] formulaText is not assigned.");

            if (bondTypeText != null)
                bondTypeText.text = "Bond type: " + FormatBondType(data.bondType);
            else
                Debug.LogWarning("[MoleculeDiscoveryManager] bondTypeText is not assigned.");

            //Debug.Log($"[MoleculeDiscoveryManager] Info panel updated: {data.moleculeName} / {data.formula} / {data.bondType}");
            // ↑ RE-ENABLE THIS to diagnose: check console for what values are actually read from MoleculeData
            Debug.Log($"[MoleculeDiscoveryManager] UpdateInfoPanel called." +
                      $"\n  → moleculeName = '{data.moleculeName}' | nameText assigned: {moleculeNameText != null}" +
                      $"\n  → formula      = '{data.formula}'       | formulaText assigned: {formulaText != null}" +
                      $"\n  → bondType     = '{data.bondType}'      | bondTypeText assigned: {bondTypeText != null}");
        }

        /// <summary>
        /// Updates the bulleted list text field with all molecules found so far.
        /// </summary>
        private void UpdateDiscoveredListUI()
        {
            if (discoveredListText == null) return;

            if (discoveredMolecules.Count == 0)
            {
                discoveredListText.text = "None yet";
                return;
            }

            // Join all discovered molecules with wide white space
            discoveredListText.text = string.Join("     ", discoveredMolecules);
        }

        private string FormatBondType(BondType bondType)
        {
            return bondType switch
            {
                BondType.Single => "Covalent — Single Bond",
                BondType.Double => "Covalent — Double Bond",
                BondType.Triple => "Covalent — Triple Bond",
                _               => bondType.ToString()
            };
        }
    }
}
