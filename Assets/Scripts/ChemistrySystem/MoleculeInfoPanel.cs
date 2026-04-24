using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Listens for molecule formation events and updates a UI text panel
    /// with details read directly from the matched MoleculeData ScriptableObject.
    ///
    /// SETUP:
    /// 1. Attach this script to your UI panel GameObject.
    /// 2. Assign the TMP text fields in the Inspector.
    /// 3. No other wiring needed — BondManager raises OnMoleculeFormed automatically.
    ///
    /// DATA DISPLAYED (all sourced from MoleculeData):
    /// - Molecule name      (e.g., "Water")
    /// - Chemical formula   (e.g., "H₂O")
    /// - Bond type          (e.g., "Covalent — Single Bond")
    /// - Atom composition   (e.g., "H × 2,  O × 1")
    /// </summary>
    public class MoleculeInfoPanel : MonoBehaviour
    {
        // ─── Inspector: Text Fields ────────────────────────────────────────────

        [Header("Text Fields")]
        [Tooltip("Displays the common name of the molecule (e.g., Water).")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Tooltip("Displays the chemical formula (e.g., H2O).")]
        [SerializeField] private TextMeshProUGUI formulaText;

        [Tooltip("Displays the bond type (e.g., Single Bond).")]
        [SerializeField] private TextMeshProUGUI bondTypeText;

        [Tooltip("Displays the atom breakdown (e.g., H × 2,  O × 1).")]
        [SerializeField] private TextMeshProUGUI compositionText;

        // ─── Inspector: Panel Visibility ──────────────────────────────────────

        [Header("Panel Visibility")]
        [Tooltip("If assigned, this GameObject is shown when a molecule forms and " +
                 "hidden when cleared. Useful if the panel starts hidden.")]
        [SerializeField] private GameObject panelRoot;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Subscribe to the static event raised by BondManager on every successful bond
            BondManager.OnMoleculeFormed += HandleMoleculeFormed;
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks and ghost callbacks
            BondManager.OnMoleculeFormed -= HandleMoleculeFormed;
        }

        // ─── Event Handler ─────────────────────────────────────────────────────

        /// <summary>
        /// Called by BondManager.OnMoleculeFormed whenever a valid bond reaction
        /// completes. Populates all text fields from the matched MoleculeData asset.
        /// </summary>
        private void HandleMoleculeFormed(MoleculeData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[MoleculeInfoPanel] Received a null MoleculeData event.");
                return;
            }

            // ── Show the panel if it was hidden ───────────────────────────────
            if (panelRoot != null) panelRoot.SetActive(true);

            // ── Name ──────────────────────────────────────────────────────────
            if (nameText != null)
                nameText.text = data.moleculeName;

            // ── Formula ───────────────────────────────────────────────────────
            if (formulaText != null)
                formulaText.text = data.formula;

            // ── Bond Type ─────────────────────────────────────────────────────
            if (bondTypeText != null)
                bondTypeText.text = FormatBondType(data.bondType);

            // ── Atom Composition ──────────────────────────────────────────────
            if (compositionText != null)
                compositionText.text = FormatComposition(data.requiredAtoms);

            Debug.Log($"[MoleculeInfoPanel] Panel updated for: {data.moleculeName} ({data.formula})");
        }

        // ─── Formatting Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Converts the BondType enum to a human-readable label.
        /// Example: BondType.Double → "Covalent — Double Bond"
        /// </summary>
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

        /// <summary>
        /// Converts a flat atom list into a grouped, readable string.
        /// Example: [Hydrogen, Hydrogen, Oxygen] → "H × 2,  O × 1"
        /// </summary>
        private string FormatComposition(List<AtomType> atoms)
        {
            if (atoms == null || atoms.Count == 0) return "—";

            // Group atoms by type and count occurrences
            var grouped = atoms
                .GroupBy(a => a)
                .Select(g => $"{AbbreviateAtom(g.Key)} × {g.Count()}");

            return string.Join(",   ", grouped);
        }

        /// <summary>
        /// Returns the chemical symbol for a known AtomType.
        /// Extend this switch as new AtomTypes are added to the enum.
        /// </summary>
        private string AbbreviateAtom(AtomType atomType)
        {
            return atomType switch
            {
                AtomType.Hydrogen => "H",
                AtomType.Oxygen   => "O",
                AtomType.Carbon   => "C",
                AtomType.Nitrogen => "N",
                _                 => atomType.ToString()
            };
        }
    }
}
