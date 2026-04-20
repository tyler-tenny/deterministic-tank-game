# Deterministic Tank Game
A multiplayer physics-based tank game built in Unity using Photon Quantum's deterministic rollback networking framework. Developed as a team project of three by Aaron Hill, Derek Preston, and Tyler Tenny.

## About 
Built to explore the challenges of networked multiplayer game development, specifically the determinism requirements and architecture constraints of rollback netcode. Uses Photon Quantum's Entity Component System (ECS) architecture and deterministic simulation model.

## Technical Architecture

### Rollback Netcode
Traditional multiplayer games use lockstep or server-authoritative models 
that introduce input lag or require constant server communication. Rollback 
netcode solves this by predicting opponent inputs locally and "rolling back" 
the simulation to correct mispredictions when the real inputs arrive, resulting in responsive local gameplay despite network latency.

### Photon Quantum Framework
Photon Quantum enforces deterministic simulation. Identical inputs must 
produce identical game state on every client, every frame. This constraint 
shapes every architectural decision: no floating point operations, no 
random values outside the deterministic context, no client-side state 
that diverges from the simulation.

### ECS Architecture
Game logic is implemented using Quantum's Entity Component System:
- **Entities** — lightweight identifiers for game objects (tanks, projectiles)
- **Components** — pure data structs attached to entities (position, health, physics) - qtn files
- **Systems** — stateless logic that operates on component data each frame

This separation of data and behavior produces a simulation that is 
predictable, testable, and inherently network-safe.

## Tech Stack
- **Engine:** Unity 2022.3.49f1
- **Networking:** Photon Quantum 3.0.0
- **Language:** C#
- **Backend:** Firebase for live leaderboard
- **Version Control:** Unity Version Control/Plastic SCM

## My Contributions
- Implemented full 3D rigidbody vehicle physics, movement, and player controller, player join/disconnect, network integration, Firebase live leaderboard
- Wrote PlayerSystem, GameplaySystem, DriveSystem among others

## Team
Built collaboratively by Aaron Hill, Derek Preston, and Tyler Tenny as a university project at Oregon Institute of Technology. Presented at IDEAFest Student project symposium. Poster available here: https://docs.google.com/presentation/d/1944x7UYJs0akk54sLAtNJTkmwWrEZpL_/edit?usp=sharing&ouid=103544743046226241431&rtpof=true&sd=true

Gameplay video available here: https://youtu.be/p7En69X2ucc

## What I Learned
- Deterministic simulation constraints and why they matter for networked games
- ECS architecture patterns and stateless system design
- Working from SDK documentation to implement against a framework with limited outside resources/ AI help
- The tradeoffs between different multiplayer networking models
- 3D Rigidbody vehicle physics simulating suspension and weight
- Collaborative development on a shared Unity codebase with Unity Version Control
- At the time of development, Quantum 3.0 had no examples or documentation for rigidbody physics simulation, only kinematic character controllers. I ended up reaching out to the developers of Photon Quantum for assistance, and this contributed to Photon subsequently releasing an official rigidbody vehicle physics example project for the framework. 

## Running The Project
1. Clone the repository
2. Install Unity 2022.3.49f1
3. Import Photon Quantum SDK from https://www.photonengine.com
4. Open in Unity and run. Online services are disabled due to free tier constraints but can be activated on request.

## Known Issues / Future Work
- Additional maps, tank skins, more FX
- More weapon types and game modes
- Rework/further tuning of vehicle physics
- Spectator mode
