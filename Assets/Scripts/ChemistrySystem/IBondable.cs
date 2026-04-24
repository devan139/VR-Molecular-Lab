using System.Collections.Generic;
using UnityEngine;

namespace VRMolecularLab.ChemistrySystem
{
    /// <summary>
    /// Shared contract for anything that can participate in a chemical bond.
    ///
    /// WHY AN INTERFACE?
    /// In XR, users can pick up both individual atoms AND partially-built molecules
    /// (e.g. an H2 that was already formed). The BondManager must evaluate all
    /// nearby objects uniformly, regardless of whether they are raw atoms or
    /// pre-formed molecules. An interface lets us achieve this polymorphism
    /// without forcing an inheritance chain that would conflict with Unity's
    /// MonoBehaviour architecture.
    /// </summary>
    public interface IBondable
    {
        /// <summary>
        /// Returns the full list of atom types this object represents.
        /// - An individual Hydrogen atom returns: [Hydrogen]
        /// - An H2 molecule returns:              [Hydrogen, Hydrogen]
        /// - A water molecule returns:             [Hydrogen, Hydrogen, Oxygen]
        ///
        /// This is what gets MERGED by BondManager when evaluating a new cluster.
        /// </summary>
        List<AtomType> GetComposition();

        /// <summary>
        /// The root GameObject of this bondable object.
        /// Used by BondManager to destroy it after a successful bond.
        /// </summary>
        GameObject BondableGameObject { get; }

        /// <summary>
        /// The set of other IBondable objects currently within this object's
        /// trigger collider range. Maintained by each implementing class.
        /// </summary>
        HashSet<IBondable> ProximalBondables { get; }
    }
}
