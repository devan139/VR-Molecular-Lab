using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Maintains an infinite supply of a single atom type at a fixed spawn point.
    ///
    /// HOW IT WORKS:
    /// On Start, one atom prefab is instantiated at this spawner's position.
    /// When the user grabs that atom, the XRGrabInteractable fires selectEntered.
    /// AtomSpawner listens to that event and schedules a new atom to appear after
    /// a short delay — ensuring only one atom is ever waiting at the spawner.
    /// </summary>
    public class AtomSpawner : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Atom Settings")]
        [Tooltip("The atom prefab to spawn. Must have AtomController and XRGrabInteractable.")]
        public GameObject atomPrefab;

        [Tooltip("Seconds to wait before spawning the next atom after the current one is grabbed.")]
        [Range(0.1f, 2f)]
        public float respawnDelay = 0.2f;

        [Tooltip("If true, spawns the first atom automatically on Start. " +
                 "Set to false when you want a button to trigger spawning via StartSpawning().")]
        public bool autoStart = false;

        // ─── Private State ─────────────────────────────────────────────────────

        /// <summary>
        /// Reference to the atom currently sitting at the spawner.
        /// Null while the user is holding it and a respawn is pending.
        /// </summary>
        private GameObject currentAtom;

        /// <summary>Prevents StartSpawning() from running more than once.</summary>
        private bool hasStarted = false;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            if (autoStart)
                StartSpawning();
        }

        /// <summary>
        /// Activates this spawner and places the first atom at the spawn point.
        /// Call this from a button's onClick event when autoStart is false.
        /// Safe to call multiple times — only the first call takes effect.
        /// </summary>
        public void StartSpawning()
        {
            if (hasStarted) return;

            if (atomPrefab == null)
            {
                Debug.LogError($"[AtomSpawner] {gameObject.name}: atomPrefab is not assigned in the Inspector!", this);
                return;
            }

            hasStarted = true;
            Debug.Log($"[AtomSpawner] {gameObject.name}: StartSpawning() called.");
            SpawnAtom();
        }

        /// <summary>
        /// Clears any pending respawns, destroys the current atom waiting at the spawner,
        /// and spawns a fresh one. Used by the ResetManager.
        /// </summary>
        public void Respawn()
        {
            StopAllCoroutines();

            if (currentAtom != null)
            {
                Destroy(currentAtom);
                currentAtom = null;
            }

            // Only spawn if this spawner is currently active
            if (hasStarted)
            {
                SpawnAtom();
            }
        }

        // ─── Spawn Logic ───────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates a fresh atom at this spawner's position and rotation,
        /// then subscribes to its selectEntered event to detect when it is grabbed.
        /// </summary>
        private void SpawnAtom()
        {
            // Instantiate at the spawner's exact world position and rotation
            currentAtom = Instantiate(atomPrefab, transform.position, transform.rotation);

            // Get the XRGrabInteractable on the spawned atom
            XRGrabInteractable grabInteractable = currentAtom.GetComponent<XRGrabInteractable>();

            if (grabInteractable == null)
            {
                Debug.LogError($"[AtomSpawner] Spawned atom '{currentAtom.name}' is missing an XRGrabInteractable component. " +
                               "Auto-respawn will not work.", this);
                return;
            }

            // Subscribe to selectEntered — fires the moment the user grabs the atom.
            // We use a local variable capture so the lambda always references the
            // correct grabInteractable even if multiple atoms exist at once.
            XRGrabInteractable capturedGrab = grabInteractable;
            capturedGrab.selectEntered.AddListener(_ => OnAtomGrabbed(capturedGrab));

            Debug.Log($"[AtomSpawner] Spawned '{currentAtom.name}' at {transform.position}");
        }

        /// <summary>
        /// Called when the user grabs the atom. Unsubscribes the listener to prevent
        /// duplicate events, clears the current reference, and schedules a new spawn.
        /// </summary>
        private void OnAtomGrabbed(XRGrabInteractable grabInteractable)
        {
            Debug.Log($"[AtomSpawner] Atom grabbed — scheduling respawn in {respawnDelay}s.");

            // Remove the listener immediately to prevent duplicate triggers
            // (e.g., if the atom is grabbed and released rapidly)
            grabInteractable.selectEntered.RemoveAllListeners();

            // Clear the reference — the user now owns this atom
            currentAtom = null;

            // Spawn the next atom after the configured delay
            StartCoroutine(RespawnAfterDelay());
        }

        /// <summary>
        /// Waits for respawnDelay seconds then spawns a new atom.
        /// The delay prevents an immediate duplicate appearing while the grabbed
        /// atom is still physically at the spawner position.
        /// </summary>
        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            // Guard: spawner may have been disabled/destroyed during the delay
            if (this == null || !gameObject.activeInHierarchy) yield break;

            SpawnAtom();
        }
    }
}
