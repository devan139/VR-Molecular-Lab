# VR Molecular Chemistry Lab

## Overview
A VR-based interactive molecular chemistry lab built using Unity and XR Interaction Toolkit. Users can grab atoms and combine them to form valid molecules in an immersive environment.

---

## Features
- XR-based atom interaction (grab & combine)
- Incremental molecule formation (H2 → H2O → etc.)
- Molecule discovery system with feedback
- World-space UI panels
- Molecule breaking system
- Infinite atom spawning

---

## Implemented Molecules
- H2 (Hydrogen Gas)
- O2 (Oxygen Gas)
- N2 (Nitrogen Gas)
- H2O (Water)
- NH3 (Ammonia)
- CO2 (Carbon Dioxide)
- CH4 (Methane)

---

## Architecture
- **ScriptableObjects** for molecule data
- **BondManager** for validation logic
- **AtomController / MoleculeController**
- **MoleculeDiscoveryManager**
- Modular and extensible design

---

## XR Setup
- Unity 6
- XR Interaction Toolkit
- OpenXR
- Meta Quest compatible (Android build)

---

## AI Tools Used
- ChatGPT → architecture planning & debugging
- Antigravity → script generation
- Meshy → 3D asset generation
- (Add others if used)

---

## Setup Instructions
1. Open project in Unity 6
2. Ensure XR Plugin Management is enabled
3. Build for Android (Quest)

---

## Demo
(Video link here)

---

## APK Download
(Link here)

---

## Notes
Designed with XR interaction constraints in mind, using incremental molecule construction for usability.
