# AR Navigation System for Unity

This project is a mobile AR navigation system built with Unity and AR Foundation. It is designed to provide accurate pathfinding at real-world scale while implementing features to mitigate common AR issues like positional drift.

## Key Features
- **Automatic Ground Detection**: Uses Raycasting to ensure AR markers are placed at the correct height relative to the OBJ terrain model.
- **Real-time Drift Correction**: Automatically calculates and corrects discrepancies between the user's physical movement and the AR coordinate system.

## System Architecture
### 1. PathfindingManager.cs
The central controller of the system.
- Manages fixed navigation route data.
- Dynamically spawns AR markers (arrows) aligned with the terrain.
- Monitors proximity to navigation waypoints.

### 2. ARNavigationAdjuster.cs
A stability script that ensures the AR session remains synchronized with the physical world.
- **Scale Factor**: Adjusts the scale mismatch between physical walking distance and the digital model.
- **Auto Snapping**: Smoothly corrects the camera's position if it deviates beyond a specified threshold from the planned route.

## Development Environment
- Unity 2022.3 (LTS recommended)
- AR Foundation
- Input System (Both mode enabled)
