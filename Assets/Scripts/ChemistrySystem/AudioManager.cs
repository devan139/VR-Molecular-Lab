using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Singleton audio manager for the Chemistry System.
    ///
    /// SINGLE REFERENCE POINT:
    /// Instead of assigning AudioClips on every individual atom and molecule prefab,
    /// assign them once here. Any script calls AudioManager.Instance.Play___()
    /// and this manager handles playback.
    ///
    /// SOUNDS MANAGED:
    /// - Bond formed       (BondManager → FormMolecule)
    /// - Molecule broken   (MoleculeController → BreakMolecule)
    /// - Checkpoint found  (MoleculeDiscoveryManager → first discovery)
    /// - All checkpoints complete
    ///
    /// SETUP:
    /// Add this script to a persistent scene GameObject (e.g., "AudioManager").
    /// Assign AudioClip assets in the Inspector. Adjust volumes per clip.
    /// An AudioSource component will be automatically added to play exclusive sounds.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // ─── Singleton ─────────────────────────────────────────────────────────

        public static AudioManager Instance { get; private set; }

        private AudioSource mainAudioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Grab the AudioSource that RequireComponent ensures is attached
            mainAudioSource = GetComponent<AudioSource>();
        }

        // ─── Inspector: Clips & Volumes ────────────────────────────────────────

        [Header("Bond Formed")]
        [Tooltip("Plays when two atoms or molecules successfully bond.")]
        [SerializeField] private AudioClip bondSound;
        [Range(0f, 1f)]
        [SerializeField] private float bondVolume = 1f;

        [Header("Molecule Broken")]
        [Tooltip("Plays when a molecule is broken apart by the player.")]
        [SerializeField] private AudioClip breakSound;
        [Range(0f, 1f)]
        [SerializeField] private float breakVolume = 1f;

        [Header("Checkpoint Discovered")]
        [Tooltip("Plays the first time a checkpoint molecule is formed.")]
        [SerializeField] private AudioClip checkpointSound;
        [Range(0f, 1f)]
        [SerializeField] private float checkpointVolume = 1f;

        [Header("All Checkpoints Complete")]
        [Tooltip("Plays when every checkpoint molecule has been discovered.")]
        [SerializeField] private AudioClip allCompleteSound;
        [Range(0f, 1f)]
        [SerializeField] private float allCompleteVolume = 1f;

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Call from BondManager after a molecule is successfully formed.
        /// Plays at the position where the molecule spawned.
        /// </summary>
        public void PlayBondSound(Vector3 position)
        {
            PlayAtPoint(bondSound, bondVolume, position, "Bond");
        }

        /// <summary>
        /// Call from MoleculeController.BreakMolecule() just before Destroy.
        /// Plays at the molecule's world position so the clip survives destruction.
        /// </summary>
        public void PlayBreakSound(Vector3 position)
        {
            PlayAtPoint(breakSound, breakVolume, position, "Break");
        }

        /// <summary>
        /// Call from MoleculeDiscoveryManager when a new checkpoint molecule is found.
        /// </summary>
        public void PlayCheckpointSound()
        {
            PlayAtPoint(checkpointSound, checkpointVolume, Vector3.zero, "Checkpoint");
        }

        /// <summary>
        /// Call from MoleculeDiscoveryManager when all checkpoints are complete.
        /// </summary>
        public void PlayAllCompleteSound()
        {
            PlayAtPoint(allCompleteSound, allCompleteVolume, Vector3.zero, "AllComplete");
        }

        /// <summary>
        /// Plays the given AudioClip exclusively.
        /// If another clip is currently playing on the main AudioSource, it will be stopped immediately.
        /// </summary>
        public void Play(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] Tried to Play() a null AudioClip.");
                return;
            }

            mainAudioSource.Stop();
            mainAudioSource.clip = clip;
            mainAudioSource.Play();
        }

        // ─── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Uses AudioSource.PlayClipAtPoint so sounds at a world position survive
        /// even if the calling GameObject is destroyed in the same frame.
        /// For sounds without a meaningful world position (checkpoints, UI), plays
        /// at the camera position so spatial audio is neutral.
        /// </summary>
        private void PlayAtPoint(AudioClip clip, float volume, Vector3 position, string label)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] '{label}' sound is not assigned in the Inspector.");
                return;
            }

            // For position-less sounds, use the main camera position (neutral spatial)
            Vector3 playPosition = (position == Vector3.zero && Camera.main != null)
                ? Camera.main.transform.position
                : position;

            AudioSource.PlayClipAtPoint(clip, playPosition, volume);
        }
    }
}
