using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Handles resetting the chemistry environment to its initial state.
    /// Can be wired to a UI Button (via XR Simple Interactable or Unity UI).
    /// </summary>
    public class ResetManager : MonoBehaviour
    {
        /// <summary>
        /// Clears all atoms and molecules currently in the scene, and forces
        /// all active AtomSpawners to instantly respawn a fresh atom.
        /// </summary>
        public void ResetSystem()
        {
            Debug.Log("[ResetManager] Reset triggered.");

            // 1. Find and destroy all active atoms
            AtomController[] atoms = FindObjectsByType<AtomController>(FindObjectsSortMode.None);
            foreach (AtomController atom in atoms)
            {
                if (atom != null && atom.gameObject != null)
                {
                    Destroy(atom.gameObject);
                }
            }

            // 2. Find and destroy all active molecules
            MoleculeController[] molecules = FindObjectsByType<MoleculeController>(FindObjectsSortMode.None);
            foreach (MoleculeController molecule in molecules)
            {
                if (molecule != null && molecule.gameObject != null)
                {
                    Destroy(molecule.gameObject);
                }
            }

            Debug.Log("[ResetManager] Respawning atoms.");

            // 3. Notify all spawners to respawn fresh atoms
            AtomSpawner[] spawners = FindObjectsByType<AtomSpawner>(FindObjectsSortMode.None);
            foreach (AtomSpawner spawner in spawners)
            {
                if (spawner != null)
                {
                    spawner.Respawn();
                }
            }
        }
    }
}
