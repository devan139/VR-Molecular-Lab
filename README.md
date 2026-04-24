# 🧪 VR Molecular Chemistry Lab

## 🎯 Overview
This project is an interactive **VR Molecular Chemistry Lab** built using Unity and XR Interaction Toolkit. Users can grab atoms and combine them to form valid molecules through intuitive VR interactions.

The system is designed with XR constraints in mind, enabling **incremental molecule construction**, allowing users to build complex molecules step by step.

---

## 🎮 Features

- XR-based atom interaction (grab, move, combine)
- Incremental molecule formation (H → H₂ → H₂O)
- Data-driven molecule system using ScriptableObjects
- Molecule discovery system with feedback (UI + audio)
- World-space UI panels optimized for VR
- Molecule breaking system (trigger-based)
- Infinite atom spawning system
- Reset system for recovering from edge cases
- Teleport-based onboarding with spatial audio
- Molecule cards with generated visual representations
- Audio feedback for bonding, breaking, and completion

---

## ⚛️ Implemented Molecules

### Required Molecules:
- H₂ (Hydrogen Gas)
- O₂ (Oxygen Gas)
- N₂ (Nitrogen Gas)
- H₂O (Water)
- NH₃ (Ammonia)
- CO₂ (Carbon Dioxide)
- CH₄ (Methane)

### Intermediate Molecules (for incremental building):
- NH
- NH₂

---

## 🧠 Architecture

The system is modular and scalable:

- **MoleculeData (ScriptableObject)**  
  Defines molecule composition and prefab reference

- **BondManager**  
  Handles molecule validation and formation logic

- **AtomController / MoleculeController**  
  Manage interaction and composition data

- **MoleculeDiscoveryManager**  
  Tracks discovered molecules and triggers events

- **AtomSpawner**  
  Provides infinite atom supply

- **ResetManager**  
  Safely resets the scene

---

## 🎨 XR UI/UX Design

- World-space UI panels placed ergonomically (~1.5m)
- High-contrast readable text
- Ray-based interaction using XR Ray Interactor
- Dynamic molecule info panel
- Spatial onboarding using teleport-triggered audio

---

## 🔊 Audio Design

- Success sound on molecule formation
- Break sound on molecule decomposition
- Tutorial voice guidance
- Completion feedback sound

---

## 🤖 AI Tools Used

AI tools were used to accelerate development and improve productivity:

- **ChatGPT** → Architecture planning, debugging, XR design guidance  
- **Antigravity** → Script generation and system refinement  
- **Google Gemini** → Molecule image generation for UI cards  
- **ElevenLabs** → Voiceover and audio generation  
- **Meshy.ai** → 3D asset generation (environment/table)  

---

## ⚙️ Technical Stack

- Unity 6 (6000.3.8f1)
- XR Interaction Toolkit
- OpenXR Plugin
- Android (Meta Quest compatible)
- IL2CPP scripting backend

---

## ▶️ Demo Video

https://drive.google.com/drive/folders/1VGRYVBn7z2tZ6ALphoH9QYooEMPt3e_Z?usp=sharing

---

## 📦 APK Download

https://drive.google.com/drive/folders/1Bvi5SvJqjjyDxwKsrwqxmnK7EQd80h3t?usp=sharing

---

## 🚀 Setup Instructions

1. Open project in Unity 6
2. Ensure XR Plugin Management is enabled
3. Set platform to Android
4. Connect Meta Quest device
5. Build and run APK

---

## 🧠 Design Considerations

- Incremental molecule construction improves usability in VR
- ScriptableObject-driven system avoids hardcoding
- Minimalistic environment improves performance and clarity
- Feedback systems enhance user engagement

---

## ⚖️ License

This project is licensed under the MIT License.

---

## 🧾 Third-Party Assets

This project uses Unity technologies including XR Interaction Toolkit and OpenXR.

All Unity assets and packages are subject to Unity's licensing terms.

Only the original scripts and implementation are covered under the MIT License.

---

## 👤 Author

**Devanarayanan MP**
