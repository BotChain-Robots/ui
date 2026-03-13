# Capstone

A modular robot control and visualization UI for a modular robotic system. Capstone provides a unified interface to visualize, control, and program robots built from connectable modules (servos, hubs, DC motors, sensors, etc.) through three main modes: Live View, Inverse Kinematics, and Programmed Movements.

---

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Technical Architecture](#technical-architecture)
- [Major Features](#major-features)
- [Data Formats](#data-formats)
- [Building](#building)

---

## Overview

Capstone is designed for:

- **Visualizing** robots built from modular components (servos, hubs, DC motors, grippers, sensors, etc.)
- **Controlling** modules in real time via a native C library (`libc_control`) or mock JSON configs
- **Programming** movement sequences via a timeline-based editor

The application supports both hardware-connected operation (with a native control library) and a **mock mode** for development and testing without physical hardware.

---

## Requirements


| Requirement  | Version               |
| ------------ | --------------------- |
| **Unity**    | 6000.3.6f1 (Unity 6)  |
| **Platform** | Windows, macOS, Linux |


### Supported Platforms

- Windows (x64)
- macOS (x64)
- Linux (x86_64)

---

## Getting Started

### 1. Clone and Open

```bash
git clone <repository-url>
cd ui
```

Open the project in Unity Hub using Unity 6000.3.6f1.

### 2. Run Without Hardware (Mock Mode)

To run without the native control library:

1. Open `ProjectSettings` or locate the `TopologyBuilder` component in your scene.
2. Enable **Use JSON File** and **Mock Control Library**.
3. The app will load topology from JSON files in `Assets/Resources/` (e.g. `mockData.json`, `mockDataSimple.json`, `mockDataWithDC.json`).

### 3. Run With Hardware

1. Ensure `libc_control` is built and available in the appropriate platform directory (e.g. `Plugins/`).
2. Disable mock mode in `TopologyBuilder`.
3. Use the **Discovery** overlay to select a connected robot leader.

### 4. Main Scene

- **SampleScene** (`Assets/Scenes/SampleScene.unity`) – primary scene for robot control and visualization.

---

## Project Structure

```
ui/
├── Assets/
│   ├── Scenes/                    # Main scenes (SampleScene, Playground)
│   ├── Resources/                 # JSON configs for mock mode
│   │   ├── mockData.json
│   │   ├── mockDataSimple.json
│   │   ├── mockDataWithDC.json
│   │   └── ...
│   ├── Module/                    # Topology, Hub, Discovery
│   ├── IK/                        # Inverse kinematics
│   ├── ProgrammedMovements/       # Timeline-based movement
│   │   └── SavedTimelines/        # Saved timeline JSON files
│   ├── ControlLibrary/            # P/Invoke bridge to libc_control
│   ├── FlatBuffers/               # FlatBuffers runtime
│   ├── Flatbuffers_generated/     # Generated types (RobotConfiguration, etc.)
│   ├── StarterAssets/             # Third-person controller, input
│   ├── StandaloneFileBrowser/     # Cross-platform file dialogs
│   ├── SyntyStudios/              # Polygon assets (PolygonCity, etc.)
│   └── Editor/                    # Build scripts
├── ProjectSettings/
├── Packages/
└── README.md
```

---

## Technical Architecture

### Unity Version & Packages


| Package                         | Version |
| ------------------------------- | ------- |
| Cinemachine                     | 2.10.3  |
| Input System                    | 1.14.0  |
| Universal Render Pipeline (URP) | 14.0.12 |
| TextMeshPro                     | 3.0.7   |
| Timeline                        | 1.7.7   |
| UGUI                            | 1.0.0   |
| Visual Scripting                | 1.9.4   |
| Collab Proxy                    | 2.8.2   |
| Development                     | 1.0.1   |


### Native Integration

The `ControlLibrary` communicates with the native C library `libc_control` via P/Invoke:


| Function                                 | Purpose                               |
| ---------------------------------------- | ------------------------------------- |
| `init()`                                 | Initialize the control library        |
| `cleanup()`                              | Cleanup resources                     |
| `send_angle_control(module_id, angle)`   | Send angle command to a servo module  |
| `send_string_control(module_id, s)`      | Send string command to a module       |
| `get_distance_control(module_id)`        | Get distance sensor reading           |
| `get_configuration(out size, leader_id)` | Get robot configuration (FlatBuffers) |
| `get_leaders(out length)`                | Get list of connected robot leaders   |
| `control_sentry_*`                       | Sentry error reporting integration    |


If `libc_control` is missing, native calls are disabled and the app falls back to mock/JSON mode.

### Data Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                     ViewBannerUI (3 modes)                       │
├─────────────────┬─────────────────────┬──────────────────────────┤
│   Live View     │ Inverse Kinematics  │  Programmed Movements    │
│ LiveViewPanel   │ IKController +      │  ProgrammedMovementsCtrl │
│ ModuleSelector  │ PositionControlGizmo│  Timeline (4 tracks)     │
└────────┬────────┴──────────┬──────────┴──────────────┬───────────┘
         │                   │                         │
         ▼                   ▼                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              ModuleBase (Servo, DC, Hub, Sensor, etc.)          │
│                    SendToControlLibrary()                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ControlLibrary (P/Invoke)  ←→  libc_control (native C library) │
│  getRobotConfiguration, send_angle_control, get_leaders, etc.   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Major Features

### 1. View Modes

The top banner (`ViewBannerUI`) provides three modes:


| Mode                     | Description                                                      |
| ------------------------ | ---------------------------------------------------------------- |
| **Live**                 | Real-time control of individual modules (sliders, buttons)       |
| **Inverse Kinematics**   | Drag a 3D gizmo to move the end-effector; IK solves joint angles |
| **Programmed Movements** | Timeline-based sequences (up to 10 seconds, 4 tracks)            |


### 2. Module System


| Module Type       | Description                                                                          |
| ----------------- | ------------------------------------------------------------------------------------ |
| **ModuleBase**    | Base class for all modules; sends commands via `ControlLibrary`                      |
| **Hub modules**   | `HubModule_MMMM`, `TriangleHubModule_MMF`, `TriangleHubModule_MMM`, `HubModule_MMMF` |
| **Servo modules** | `ServoBendModule`, `ServoStraightModule`, `GripperModule`                            |
| **Actuators**     | `DCMotorModule`                                                                      |
| **Sensors**       | `DistanceSensorModule`, `IMUSensorModule`                                            |
| **Other**         | `DisplayModule`, `SpeakerModule`, `PowerModule`, `Battery`                           |


### 3. Topology Builder

- Builds the robot graph from JSON or from `ControlLibrary.getRobotConfiguration()`.
- Uses `ModuleSpawner` to instantiate prefabs and connect them.
- Options: `useJsonFile`, `skipControlLibraryCalls`, `mockControlLibrary`.

### 4. Discovery

- Lists robot leaders from `ControlLibrary.getRobotLeaders()`.
- Mock mode uses JSON files in `Resources/`.
- Selecting a leader loads its topology.

### 5. Live View

- Side panel for the selected module.
- **Servo**: angle slider (0–180°).
- **DC motor**: direction and rotation controls.
- **Sensors**: readouts (e.g. distance).
- Uses `ModuleSelector` for selection.

### 6. Inverse Kinematics

- CCD-based IK solver for kinematic chains.
- Builds chain from selected module to root (`GeneratedTopology`).
- `PositionControlGizmo`: 3D gizmo for dragging end-effector position.
- Supports `ServoStraightModule` joints with configurable deadzone and smoothing.

### 7. Programmed Movements

- Timeline: 4 tracks, 0–10 seconds.
- Movement blocks per second per track.
- **Run**: plays timeline and sends commands to modules.
- **Save/Load**: JSON files in `Assets/ProgrammedMovements/SavedTimelines/`.
- Uses `StandaloneFileBrowser` for cross-platform file dialogs.

### 8. File Browser

- **StandaloneFileBrowser** – cross-platform open/save dialogs (Windows, Mac, Linux).

---

## Data Formats

### Topology JSON

```json
{
  "Modules": [
    { "Id": "splitter1", "Type": "Splitter4", "Degree": null },
    { "Id": "servo1", "Type": "Servo1", "Degree": 45 }
  ],
  "Connections": [
    {
      "FromModuleId": "splitter1",
      "ToModuleId": "servo1",
      "FromSocket": "MaleSocket1",
      "ToSocket": "FemaleSocket",
      "Orientation": 0
    }
  ]
}
```

### Programmed Movement Timeline JSON

```json
{
  "Timeline": [
    {
      "Second": 0,
      "Movements": [
        {
          "ModuleId": "11",
          "ModuleType": "Servo",
          "Degree": 180.0,
          "Direction": 1,
          "Track": 0
        }
      ]
    }
  ]
}
```

### Robot Configuration

- **FlatBuffers** format: `RobotConfiguration`, `RobotModule`, `MotorState`, `ModuleConnection`, etc.
- Generated types in `Assets/Flatbuffers_generated/`.

---

## Building

Build targets are configured in `Assets/Editor/BuildScript.cs`:


| Platform | Output                         |
| -------- | ------------------------------ |
| Windows  | `Builds/Windows/botchain.exe`  |
| Linux    | `Builds/Linux/botchain.x86_64` |
| macOS    | `Builds/macOS/botchain.app`    |


### Default Resolution

- 1920×1080 (configurable in Project Settings)

---

## Key Scripts Reference


| Script                             | Purpose                                                |
| ---------------------------------- | ------------------------------------------------------ |
| `ModuleBase.cs`                    | Base for all modules; sends commands to ControlLibrary |
| `ModuleSpawner.cs`                 | Spawns module prefabs by type                          |
| `TopologyBuilder.cs`               | Builds topology from JSON or native config             |
| `TopologyGraphModel.cs`            | Graph data structures                                  |
| `ModuleSelector.cs`                | Tracks selected module                                 |
| `ControlLibrary.cs`                | P/Invoke bridge to `libc_control`                      |
| `ViewBannerUI.cs`                  | Top banner and view switching                          |
| `LiveViewModulePanel.cs`           | Live view side panel                                   |
| `InverseKinematicsController.cs`   | CCD IK solver                                          |
| `PositionControlGizmo.cs`          | 3D drag gizmo for IK                                   |
| `ProgrammedMovementsController.cs` | Timeline UI and playback                               |
| `DiscoverOverlayController.cs`     | Discovery overlay UI                                   |


