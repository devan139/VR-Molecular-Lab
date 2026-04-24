using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Central manager responsible for validating bondable object combinations
    /// and spawning the resulting molecule prefab.
    ///
    /// INCREMENTAL BONDING DESIGN:
    /// Because VR users can only hold one object per hand, this manager works
    /// with IBondable objects — both raw AtomControllers AND already-formed
    /// MoleculeControllers. This means:
    ///   Step 1: H  + H  → H2   (two AtomControllers bond)
    ///   Step 2: H2 + O  → H2O  (a MoleculeController bonds with an AtomController)
    /// Each step uses the same evaluation pipeline regardless of input types.
    ///
    /// DUPLICATE TRIGGER PREVENTION:
    /// When two objects touch, Unity fires OnTriggerEnter on BOTH of them in the
    /// same physics frame. Without a guard, EvaluateCluster would run twice:
    /// the first call succeeds and destroys the objects; the second call then
    /// iterates over already-destroyed Unity objects → NullReferenceException.
    /// The `isReacting` flag blocks any re-entrant evaluation within the same
    /// reaction cycle. It is reset at the end of FormMolecule.
    /// </summary>
    public class BondManager : MonoBehaviour
    {
        public static BondManager Instance { get; private set; }

        /// <summary>
        /// Raised whenever a valid bond reaction completes and a molecule is formed.
        /// Subscribe to this from UI scripts (e.g., MoleculeInfoPanel) to display
        /// molecule details without creating a direct reference from BondManager to the UI.
        /// </summary>
        public static event System.Action<MoleculeData> OnMoleculeFormed;

        [Header("Molecule Database")]
        [Tooltip("List of all valid molecule configurations defined via ScriptableObjects.")]
        [SerializeField] private List<MoleculeData> validMolecules;

        // Guard flag — prevents duplicate EvaluateCluster calls within one reaction
        private bool isReacting = false;

        private void Awake()
        {
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[BondManager] Duplicate instance detected and destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Entry point called by AtomController or MoleculeController when a new
        /// proximity contact is detected. Runs the full cluster evaluation pipeline.
        ///
        /// GUARD LOGIC:
        /// Both participants of a trigger fire OnTriggerEnter in the same physics step.
        /// The `isReacting` flag ensures only the FIRST caller proceeds; subsequent
        /// calls within the same reaction are silently dropped.
        /// </summary>
        public void EvaluateCluster(IBondable startBondable)
        {
            // Prevent re-entrant calls while a reaction is already being processed
            if (isReacting) return;

            if (validMolecules == null || validMolecules.Count == 0)
            {
                Debug.LogWarning("[BondManager] No molecules in the database. Assign MoleculeData assets in the Inspector.");
                return;
            }

            // Safety: if the caller was already destroyed before we got here, abort
            if (startBondable == null || startBondable.BondableGameObject == null) return;

            // ── Step 1: Gather all contiguous touching bondables via BFS ──────
            HashSet<IBondable> cluster = GetBondableCluster(startBondable);

            // At least two live objects must be in contact to attempt a bond
            if (cluster.Count < 2)
            {
                Debug.Log($"[BondManager] Cluster too small ({cluster.Count} object). Waiting for more contacts.");
                return;
            }

            // ── DEBUG: Log every object in the cluster ─────────────────────────
            string clusterNames = string.Join(", ", System.Linq.Enumerable.Select(cluster,
                b => $"{b.BondableGameObject.name}({string.Join("+", b.GetComposition())})"));
            Debug.Log($"[BondManager] Cluster detected ({cluster.Count} objects): {clusterNames}");

            // ── Step 2: Merge all compositions into one flat atom list ─────────
            // HOW MERGING ENABLES INCREMENTAL BUILDING:
            // A MoleculeController for H2 reports composition [Hydrogen, Hydrogen].
            // An AtomController for O reports composition [Oxygen].
            // Merging gives [Hydrogen, Hydrogen, Oxygen] → matches H2O recipe.
            List<AtomType> mergedComposition = MergeCompositions(cluster);

            // ── DEBUG: Log the full merged composition ─────────────────────────
            Debug.Log($"[BondManager] Merged composition: [{string.Join(", ", mergedComposition)}]");

            // ── Step 3: Check merged composition against all molecule recipes ──
            MoleculeData matchedMolecule = FindMatchingMolecule(mergedComposition);

            // ── Step 4: React if a valid recipe was found ──────────────────────
            if (matchedMolecule != null)
            {
                Debug.Log($"[BondManager] MATCH FOUND → {matchedMolecule.moleculeName} ({matchedMolecule.formula}). Forming molecule...");
                isReacting = true; // Lock — prevents the partner's OnTriggerEnter from re-evaluating
                FormMolecule(cluster, matchedMolecule);
                isReacting = false; // Unlock after reaction is fully complete
            }
            else
            {
                Debug.Log($"[BondManager] No recipe match for [{string.Join(", ", mergedComposition)}]. " +
                          $"Check your MoleculeData ScriptableObjects.");
            }
        }

        // ─── Private Pipeline Methods ──────────────────────────────────────────

        /// <summary>
        /// BFS traversal that collects all IBondable objects reachable from the
        /// starting node through the ProximalBondables graph.
        ///
        /// DESTROYED OBJECT SAFETY:
        /// Unity's Destroy() is deferred — a C# reference to a destroyed MonoBehaviour
        /// is not null immediately. We guard against this by checking
        /// `bondable.BondableGameObject == null`, which Unity overrides to return
        /// true for destroyed objects even if the C# reference is not null.
        /// </summary>
        private HashSet<IBondable> GetBondableCluster(IBondable startNode)
        {
            HashSet<IBondable> cluster = new HashSet<IBondable>();
            Queue<IBondable> queue = new Queue<IBondable>();

            queue.Enqueue(startNode);
            cluster.Add(startNode);

            while (queue.Count > 0)
            {
                IBondable current = queue.Dequeue();

                // Skip if Unity has destroyed this object between frames
                if (current == null || current.BondableGameObject == null) continue;

                foreach (IBondable neighbor in current.ProximalBondables)
                {
                    // Skip C# null, Unity-destroyed objects, and already-visited nodes
                    if (neighbor == null || neighbor.BondableGameObject == null) continue;
                    if (cluster.Contains(neighbor)) continue;

                    cluster.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return cluster;
        }

        /// <summary>
        /// Flattens the composition lists of all bondables in the cluster into
        /// a single list of AtomTypes.
        /// </summary>
        private List<AtomType> MergeCompositions(HashSet<IBondable> cluster)
        {
            List<AtomType> merged = new List<AtomType>();
            foreach (IBondable bondable in cluster)
            {
                if (bondable != null && bondable.BondableGameObject != null)
                {
                    merged.AddRange(bondable.GetComposition());
                }
            }
            return merged;
        }

        /// <summary>
        /// Compares the merged atom list against every registered MoleculeData
        /// recipe. Returns the first exact match found, or null if none match.
        /// Comparison is order-independent (H,H,O == O,H,H).
        /// </summary>
        private MoleculeData FindMatchingMolecule(List<AtomType> mergedComposition)
        {
            foreach (var recipe in validMolecules)
            {
                if (recipe == null || recipe.requiredAtoms == null)
                {
                    Debug.LogWarning("[BondManager] A null or incomplete MoleculeData entry was skipped. Check the Molecule Database list.");
                    continue;
                }

                string recipeAtoms = string.Join(", ", recipe.requiredAtoms);
                Debug.Log($"[BondManager] Testing against recipe '{recipe.moleculeName}': [{recipeAtoms}]");

                if (AreAtomCollectionsEqual(mergedComposition, recipe.requiredAtoms))
                {
                    return recipe;
                }
            }

            return null;
        }

        /// <summary>
        /// Order-independent, count-exact comparison of two atom lists.
        /// Groups by AtomType and validates that counts match exactly.
        /// Example: [H, H, O] matches [O, H, H] → true
        ///          [H, H, O] matches [H, O]     → false (wrong count)
        /// </summary>
        private bool AreAtomCollectionsEqual(List<AtomType> list1, List<AtomType> list2)
        {
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            var dict1 = list1.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            var dict2 = list2.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

            if (dict1.Count != dict2.Count) return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Executes a successful bond reaction:
        /// 1. Snapshots the cluster into a list BEFORE any Destroy calls.
        /// 2. Calculates the center point of all involved objects.
        /// 3. Spawns the molecule prefab at that position.
        /// 4. Calls Initialize(moleculeData) on the new MoleculeController so it
        ///    reads composition directly from the ScriptableObject — no duplicate data.
        /// 5. Destroys all consumed bondable objects.
        /// </summary>
        private void FormMolecule(HashSet<IBondable> cluster, MoleculeData moleculeData)
        {
            if (moleculeData.moleculePrefab == null)
            {
                Debug.LogError($"[BondManager] Molecule prefab for '{moleculeData.moleculeName}' is not assigned in the ScriptableObject!");
                return;
            }

            // ── Snapshot the cluster into a plain list before any mutations ────
            List<IBondable> clusterSnapshot = cluster.ToList();

            // ── Compute spawn position at the geographic center of the cluster ──
            Vector3 centerPosition = Vector3.zero;
            int validCount = 0;
            foreach (var bondable in clusterSnapshot)
            {
                if (bondable?.BondableGameObject != null)
                {
                    centerPosition += bondable.BondableGameObject.transform.position;
                    validCount++;
                }
            }
            if (validCount > 0) centerPosition /= validCount;

            // ── Spawn the resulting molecule prefab ────────────────────────────
            GameObject spawnedMolecule = Instantiate(moleculeData.moleculePrefab, centerPosition, Quaternion.identity);

            // ── Initialize the MoleculeController with its MoleculeData ────────
            // SINGLE SOURCE OF TRUTH: We pass the matched MoleculeData asset directly.
            // MoleculeController.GetComposition() reads from moleculeData.requiredAtoms,
            // so there is no separate composition list to keep in sync.
            MoleculeController moleculeController = spawnedMolecule.GetComponent<MoleculeController>();
            if (moleculeController != null)
            {
                moleculeController.Initialize(moleculeData);
            }
            else
            {
                Debug.LogWarning($"[BondManager] Prefab '{moleculeData.moleculePrefab.name}' is missing a MoleculeController. " +
                                 "The spawned molecule will not be able to bond further.");
            }

            // ── Destroy all consumed bondable objects (from snapshot, not live set) ──
            foreach (var bondable in clusterSnapshot)
            {
                if (bondable?.BondableGameObject != null)
                {
                    Destroy(bondable.BondableGameObject);
                }
            }

            // ── Play bond sound via AudioManager ──────────────────────────────
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBondSound(centerPosition);

            // ── Raise the event — notifies any UI listeners (e.g. MoleculeInfoPanel) ──
            OnMoleculeFormed?.Invoke(moleculeData);

            Debug.Log($"[ChemistrySystem] Successfully synthesized: {moleculeData.moleculeName} ({moleculeData.formula})");
        }
    }
}
