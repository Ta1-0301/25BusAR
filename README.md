# AR Navigation System for Unity

This project is a high-precision AR navigation solution for mobile devices, developed with **Unity** and **AR Foundation**. It addresses common AR challenges such as positional drift, scale mismatch, and terrain alignment to provide a stable and intuitive navigation experience.

## 🚀 Key Features
- **Terrain-Aware AR Placement**: Implements Raycasting against OBJ environment models to ensure AR markers are grounded at the correct elevation.
- **Dynamic Drift Correction**: A real-time feedback loop that snaps the AR camera back to the planned route if the user deviates or the tracking drifts.
- **Scale Synchronization**: Features an adjustable `Scale Factor` to perfectly align physical walking distances with the digital coordinate system.
- **Hybrid Input Support**: Fully compatible with both the new Unity Input System and the legacy Input Manager.

---

## 📂 Script Breakdown

### 1. PathfindingManager.cs
The **Core Controller** of the navigation system.
- **Route Data Management**: Handles `NavigationInstruction` data, including coordinates, turn directions, and instruction text.
- **Marker Generation**: Dynamically spawns AR arrows and turn-point markers on the ground using Raycast detection.
- **Waypoint Tracking**: Monitors user progress and updates the navigation state as waypoints are reached.

### 2. ARNavigationAdjuster.cs
The **Stabilization Engine** of the system.
- **Auto Snapping**: Calculates the user's deviation from the path segment and smoothly corrects the `XR Origin` position.
- **Drift Prevention**: Ensures the AR visuals remain synchronized with the physical environment during long walks.

### 3. GPSLocationProvider.cs & MapDataConverter.cs
The **Geospatial Interface**.
- **GPS Integration**: Retrieves the initial real-world location to seed the navigation.
- **Coordinate Transformation**: Converts global latitude/longitude into local Unity world space coordinates for the OBJ model.

### 4. EditorFreeMove.cs
A **Developer Utility** script.
- Enables mouse and keyboard controls within the Unity Editor, allowing for rapid testing of navigation logic without the need for constant mobile deployment.

---

## 🛠 Setup & Requirements

### Unity Project Settings
1. **Active Input Handling**: Set to `Both` (found in *Project Settings > Player > Other Settings*).
2. **Layer Mask**: Assign your OBJ environment model to a specific layer (e.g., `Map`) and ensure the `PathfindingManager` is configured to Raycast against this layer.

### Component Configuration
- **XR Origin**: Attach `ARNavigationAdjuster`. Link the `Main Camera` and `PathfindingManager` to its reference slots.
- **PathfindingManager**: Set your arrow/marker prefabs and assign an empty GameObject as the `Ar Arrows Parent`.

---

## 📖 Technical Logic Flow
1. **Initialization**: The system performs a downward Raycast to find the ground level of the OBJ model and places navigation markers.
2. **Navigation**: As the user moves, the `ARNavigationAdjuster` calculates the shortest distance to the current path segment.
3. **Correction**: If the user's "Drift" exceeds the threshold, the `XR Origin` is subtly adjusted using a `Lerp` function to maintain path alignment without jarring visual jumps.
