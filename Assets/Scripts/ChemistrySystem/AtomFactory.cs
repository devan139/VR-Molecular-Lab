using System.Collections.Generic;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// ScriptableObject that maps AtomType enum values to their corresponding prefabs.
    ///
    /// SINGLE ASSET, SHARED EVERYWHERE:
    /// Create ONE AtomFactory asset in your project and assign it to every
    /// MoleculeController prefab. Because it is a ScriptableObject, all references
    /// point to the same asset — no duplication, no sync issues.
    ///
    /// HOW TO CREATE:
    /// Right-click Project window → Create → Chemistry System → Atom Factory
    /// Then assign each AtomType → prefab pair in the list.
    /// </summary>
    [CreateAssetMenu(fileName = "AtomFactory", menuName = "Chemistry System/Atom Factory", order = 2)]
    public class AtomFactory : ScriptableObject
    {
        // ─── Inspector ─────────────────────────────────────────────────────────

        /// <summary>
        /// Maps each AtomType to its spawnable prefab.
        /// Extend this list when new atom types are added to the AtomType enum.
        /// </summary>
        [System.Serializable]
        public struct AtomPrefabEntry
        {
            [Tooltip("The atom type this entry represents.")]
            public AtomType atomType;

            [Tooltip("The prefab to instantiate for this atom type. " +
                     "Must have AtomController and XRGrabInteractable.")]
            public GameObject prefab;
        }

        [Tooltip("One entry per AtomType. Order does not matter.")]
        public List<AtomPrefabEntry> atomPrefabs = new List<AtomPrefabEntry>();

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the prefab registered for the given AtomType, or null if not found.
        /// </summary>
        public GameObject GetPrefab(AtomType atomType)
        {
            foreach (var entry in atomPrefabs)
            {
                if (entry.atomType == atomType) return entry.prefab;
            }

            Debug.LogWarning($"[AtomFactory] No prefab registered for AtomType '{atomType}'. " +
                             "Add it to the AtomFactory asset in the Inspector.");
            return null;
        }

        /// <summary>
        /// Spawns an atom of the given type at the specified world position and rotation.
        /// Returns the spawned GameObject, or null if no prefab is registered.
        /// </summary>
        public GameObject SpawnAtom(AtomType atomType, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefab(atomType);
            if (prefab == null) return null;

            GameObject spawned = Instantiate(prefab, position, rotation);
            Debug.Log($"[AtomFactory] Spawned '{atomType}' atom at {position}");
            return spawned;
        }
    }
}
