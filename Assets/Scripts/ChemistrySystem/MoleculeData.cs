using System.Collections.Generic;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Types of chemical bonds that can be formed.
    /// </summary>
    public enum BondType
    {
        Single,
        Double,
        Triple
    }

    /// <summary>
    /// Data container for defining valid molecule combinations.
    /// Adheres to Open-Closed Principle: New molecules can be created as assets
    /// without needing to modify any system code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMoleculeData", menuName = "Chemistry System/Molecule Data", order = 1)]
    public class MoleculeData : ScriptableObject
    {
        [Tooltip("Common name of the molecule (e.g., Water)")]
        public string moleculeName;

        [Tooltip("Chemical formula representation (e.g., H2O)")]
        public string formula;

        [Tooltip("The exact collection of atoms required to form this molecule. Order does not matter.")]
        public List<AtomType> requiredAtoms;

        [Tooltip("The primary bond type connecting these atoms (for informational or visual purposes)")]
        public BondType bondType;

        [Tooltip("The completed molecule prefab to instantiate upon successful bonding")]
        public GameObject moleculePrefab;
    }
}
