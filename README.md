ItemGenerator

This project is an automated pipeline designed for RPG and Roguelike game designers to rapidly prototype dungeon layouts and complex item balances. By adjusting various constraints and parameters, designers can simulate thousands of gameplay scenarios and reward structures without manual asset placement.

Key Features
1. Procedural Dungeon Generation
BSP Algorithm: Utilizes Binary Space Partitioning to create non-overlapping rooms of randomized sizes.

BFS Leveling: Measures path depth from the Start Room using a Breadth-First Search algorithm to assign a roomLevel, which scales the power of rewards.

Visualization: Automatically renders floor and wall tiles using TilemapVisualizer, applying physical boundaries to the wall layer.

2. Item Spawning & Leveling
Exponential Scaling: As the roomLevel increases, the probability of high-tier items and the value of base stats grow exponentially.

Physics-Based Placement: Prevents items from spawning inside walls using Physics2D.OverlapCircle and maintains natural distribution via the minSpacing parameter.

Start Room Exclusion: Ensures the initial entry area remains clear by skipping any room designated as RoomType.Start.

3. Equipment & Trade-off System
Independent Slot Logic: Epic+ items receive randomized bonuses and penalties independently (Epic 2/1, Unique 3/2, Legendary 4/3).

Dynamic Stat Growth: Additional options scale based on depth using a formula that incorporates room level and penalty intensity.

Visual Debugging: Displays color-coded wireframe boxes in the Scene view based on rarity: Normal (White), Rare (Cyan), Epic (Purple), Unique (Yellow), and Legendary (Red).

4. Data Persistence & Analytics
Parameter-Aware Logging: Records session metadata including Global Difficulty and Penalty Intensity in the JSON header for comparison between different tuning sessions.

How to Use
Select the DungeonGenerator or ItemGenerator object in the Unity Inspector.

Adjust the Trade-Off Parameters such as Difficulty, Penalty Intensity, and Trade-off Chance.

Click "1. Spawn Trade-Off Items" to generate the dungeon and loot.

Click "2. Save Data to JSON" to record the session results in the _Scripts/Data folder.

Credits & Attribution
Map Generation Reference
The core map generation logic including BSP, room connection, and tilemap visualization was implemented by referencing the following tutorial:

Sunny Valley Studio: Procedural Dungeon Generation in Unity (https://www.youtube.com/watch?v=szOq1HSWtm0&t=395s)

Original Work & Design
All systems outside of the basic map generation were designed and implemented from scratch by me, including:

Item Placement Algorithm: Physics-based collision checking and spacing logic.

Level Scaling System: BFS-based depth leveling linked to loot power.

Trade-off Mechanics: The independent bonus/penalty slot logic and growth formulas.

Data Logging System: Parameter-aware metadata logging and incremental JSON file management.

Visual Debugging Tools: Rarity-based color coding and Gizmo implementations.
