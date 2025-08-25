# ValetAgent: ML-Agents Parking Environment
![Alt text](Images/image.png)
## Overview
ValetAgent is a Unity-based simulation project designed to train reinforcement learning agents to park a car in a dynamic parking lot environment using Unity ML-Agents. The environment features multiple parking spots, obstacles, and a target parking goal, providing a challenging scenario for both AI and heuristic control.

## Solution Structure
- **CarAgent.cs**: Implements the main agent logic, including observation collection, reward shaping, action handling, and environment resets.
- **ParkingSpot.cs**: Represents individual parking spots in the environment.
- **CarBody.cs**: Handles car body interactions and physics.
- **ControlInterface.cs**: Abstracts car control inputs for both AI and manual control.
- **PrometeoCarController.cs**: Provides realistic car physics and movement.
- **CameraFollow.cs**: Smoothly follows the agent car for visualization.

## Key Features
- **Dynamic Obstacle Placement**: Obstacles are placed adjacent to the target parking spot, increasing the difficulty and realism of the parking task.
- **Randomized Start and Target**: Each episode randomizes the agent's spawn point and the target parking spot, ensuring diverse training scenarios.
- **Reward Shaping**: The agent receives rewards for approaching the target, aligning with the parking spot, and penalties for collisions or inefficient actions.
- **Heuristic Controls**: Manual control is supported for debugging and demonstration purposes.

## Challenges & Solutions
### 1. Obstacle Placement Logic
**Challenge:** Ensuring obstacles are placed only next to the target spot, not filling the entire parking lot or blocking the target.

**Solution:**
- Implemented logic to identify the target spot's index and place obstacles only at adjacent indices (if available), deactivating unused obstacle cars.

### 2. Reward Shaping for Realistic Parking
**Challenge:** Designing a reward system that encourages not just reaching the target, but also proper alignment, low speed, and minimal angular velocity for realistic parking.

**Solution:**
- Added alignment, speed, and angular velocity checks at the goal, with significant bonuses for well-aligned, slow, and stable parking.
- Penalized misalignment, excessive speed, and collisions.

### 3. Randomization and Generalization
**Challenge:** Preventing overfitting to specific spawn or target locations.

**Solution:**
- Randomized both the agent's spawn point and the target spot each episode.
- Ensured obstacles are placed dynamically based on the current target.

### 4. Efficient Obstacle Management
**Challenge:** Avoiding performance issues and visual clutter from too many obstacles.

**Solution:**
- Limited the number of obstacle cars instantiated.
- Deactivated unused obstacles each episode.

## Getting Started
1. Open the project in Unity (ensure ML-Agents is installed).
2. Assign the required prefabs and references in the CarAgent inspector.
3. Train the agent using ML-Agents or control it manually with the keyboard.

## Controls (Heuristic Mode)
- **W/S**: Gas/Brake
- **A/D**: Steer Left/Right
- **Space**: Handbrake

