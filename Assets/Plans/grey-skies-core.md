# Project Overview

- **Game Title:** Grey Skies
- **High-Level Concept:** A hardcore, post-apocalyptic open-world survival game set in South East England, beginning in the overgrown, damp, and fog-shrouded fields and coastal marshes of Kent. The game merges persistent dedicated server multiplayer (PVP/PVE) with hardcore mil-sim combat, seamless asynchronous terrain streaming, and realistic physical/somatic game feedback.
- **Players:** Persistent Dedicated Multiplayer Server (PVP/PVE) supporting up to 50 players per server.
- **Inspiration / Reference Games:** DayZ, S.T.A.L.K.E.R. (Shadow of Chernobyl / Anomaly), Escape from Tarkov, Arma 3.
- **Tone / Art Direction:** Melancholic, cold, wet, and overgrown. Visual style is characterized by weathered realism, cool desaturated autumn lighting, rotting Tudor wood, crumbling plaster, lichen-covered stone, and rusted British motoring relics.
- **Target Platform:** PC (StandaloneOSX and StandaloneWindows).
- **Screen Orientation / Resolution:** Landscape 1920x1080 (PC Native).
- **Render Pipeline:** URP (Universal Render Pipeline), optimized using high-fidelity Custom Volume Profiles (PC_High/PC_Low configurations).

---

# Game Mechanics

## Core Gameplay Loop

1. **Spawn & Entry:** Players awaken on the desolate, muddy shorelines or mist-choked orchards of Kent with only standard worn clothing, a dim flashlight, and minimal starting supplies.
2. **Exploration & Scavenging:** Players navigate narrow country lanes flanked by 2.5m-high overgrown Hawthorn and Blackthorn hedgerows. They search iconic landmarks such as Tudor Brick Pubs, abandoned red K6 telephone boxes, and rusted Rover Mini wreckage for food, fresh water, medication, protective apparel (like the Barbour Wax Jacket), and weapons.
3. **Hardcore Survival Management:** Constant regulation of body statistics: Health, Stamina, Hunger, Thirst, Temperature, and Bleeding. The environment is actively hostile—players risk hypothermia in damp conditions and blood loss from lacerations.
4. **Threat Engagement (PVE & PVP):**
   - **PVE:** Combating infected locals and hostile fauna wandering Kent's villages.
   - **PVP:** Navigating high-stakes, tense encounters with other armed players. Players must decide whether to negotiate, trade, or engage in lethal mil-sim firefights.
5. **Progression & Extraction:** Securing high-tier military loot from ruined Canterbury barracks, building hidden wooden stashes in deep Oak forests, and upgrading gear to increase overall carrying capacity and survival efficiency.

## Controls and Input Methods

The game utilizes the **Unity New Input System** to handle high-fidelity, responsive control mappings for PC (Keyboard/Mouse and Gamepad).

| Action | Keyboard/Mouse Binding | Gamepad Binding | Description |
| :--- | :--- | :--- | :--- |
| **Move** | `W/A/S/D` | Left Stick | Standard omnidirectional character movement. |
| **Look** | Mouse Delta | Right Stick | Rotates the first-person camera / character orientation. |
| **Sprint** | `Left Shift` (Hold) | Left Stick Click (L3) | Faster movement, drains Stamina rapidly. Disabled at low stamina or when heavily burdened. |
| **Jump** | `Space` | South Button (`A` / `Cross`) | Crosses minor obstacles; drains Stamina. |
| **Interact** | `E` | West Button (`X` / `Square`) | Open doors, search containers, drink from wells, pick up items. |
| **Aim Down Sights (ADS)** | `Right Mouse Button` (Hold/Toggle) | Left Trigger (`LT` / `L2`) | Tightens weapon spread, magnifies view, aligns iron sights. |
| **Attack / Shoot** | `Left Mouse Button` | Right Trigger (`RT` / `R2`) | Swings melee weapon or fires active firearm. |
| **Reload** | `R` | East Button (`B` / `Circle`) | Inserts next magazine; long press checks chamber. |
| **Inventory Overlay** | `Tab` | View/Select Button | Toggles full-screen blurred grid inventory overlay. |
| **Voice Chat (VOIP)** | `V` (Hold) | D-Pad Down | Broadcasts local, directional voice audio to nearby players. |

---

# UI

## 1. Survival HUD (Bottom Right Corner)
The HUD is designed to stay minimalist and non-obtrusive, fading out by 50% opacity when the player is fully healthy, fed, hydrated, and warm, and glowing intensely during emergencies.

```
+-------------------------------------------------------------------------+
|                                                                         |
|                                                                         |
|                                                                         |
|                                                     [Bleeding: x2] (Red)|
|                                                     (Heart)   (Fork/Knf)|
|                                                     [ HP ]    [Hunger ] |
|                                                     (Drop)    (Thermom) |
|                                                     [Thrst]   [ Temp  ] |
|                                   [========= Stamina Bar =========]     |
+-------------------------------------------------------------------------+
```
- **Health Icon (Heart):** Active Moss Green (`#4A6D55`). Transitions to Yellow (`#D69E2E`) at 50% and flashes Crimson (`#B23B3B`) under 20%.
- **Hunger Icon (Fork/Knife):** Green to Yellow to Flashing Crimson.
- **Thirst Icon (Water Drop):** Depletes 1.5x faster than hunger. Flashes Crimson when critical.
- **Temperature Icon (Thermometer):** Glows Ice Blue when freezing (hypothermia risk) and Hot Orange-Red when overheated.
- **Bleeding Icon (Blood Drop):** Invisible by default. Appears and flashes Crimson when bleeding, with an indicator like "x3" signifying the number of active wound sites.
- **Stamina Bar:** A thin, high-contrast horizontal bar centered below the icons. Depletes from right to left while sprinting, jumping, or executing heavy swings.

## 2. Immersive Grid Inventory Screen (Tab Key)
When opened, the screen overlays a 90% opacity tint of Charcoal Dark (`#0E110F`) combined with a heavy real-time camera blur to separate the interface from active danger without breaking immersion.

```
+-------------------------------------------------------------------------+
|  [ VICINITY ]         [ EQUIPPED GEAR ]           [ CONTAINER GRID ]    |
|                       +---------------+           +------------------+  |
|  - Red Soda Can       | Head: Flatcap |           | Barbour Jacket   |  |
|  - Rusty Nails        +---------------+           | [X][X][ ][ ]     |  |
|  - Shotgun Shell x2   | Torso: Barbour|           | [ ][ ][ ][ ]     |  |
|                       +---------------+           | (8 Slots)        |  |
|  +-----------------+  | Legs: Trousers|           +------------------+  |
|  | SELECTED ITEM   |  +---------------+           | Backpack         |  |
|  | Cricket Bat     |  | Hands: Bare   |           | [X][X][X][ ]     |  |
|  | Condition:      |  +---------------+           | [X][X][ ][ ]     |  |
|  | Damaged         |  | Feet: Boots   |           | [ ][ ][ ][ ]     |  |
|  | Weight: 1.2 kg  |  +---------------+           | (12 Slots)       |  |
|  | [3D Preview]    |  | Active: Bat   |           +------------------+  |
|  +-----------------+  +---------------+           | Quick Slots: 1-4 |  |
+-------------------------------------------------------------------------+
```
- **Left Column (Vicinity & Item Inspection):** Lists loose items on the ground within a 2-meter radius. Selecting an item displays its detailed card: description, weight (kg), condition (Pristine, Worn, Damaged, Ruined), and a rotatable 3D model render preview.
- **Center Column (Equipped Gear):** Visual skeletal slots representing the player's worn garments and active hands.
- **Right Column (Container Grid):** A modular grid system. Items occupy physical dimensions (e.g. Cricket Bat is 1x4 slots, Ammo box is 2x2, Soda can is 1x1). Nested containers (jacket, trousers, pack) are stacked as distinct raised panels (`#2A2F2C`) separated by slight margins.

---

# Key Asset & Context

We will create and synchronize the following core technical assets and C# classes:

### 1. `SurvivalStats.cs` (Server-Authoritative)
Maintains player vitals over the network. Uses Netcode `NetworkVariable<float>` for replication to clients with strict delta decay based on physical activity (sprinting, temperature, bleeding rate).
```csharp
public class SurvivalStats : NetworkBehaviour
{
    public NetworkVariable<float> Health = new NetworkVariable<float>(100f);
    public NetworkVariable<float> Stamina = new NetworkVariable<float>(100f);
    public NetworkVariable<float> Hunger = new NetworkVariable<float>(100f); // 0 = starving
    public NetworkVariable<float> Thirst = new NetworkVariable<float>(100f); // 0 = dehydrated
    public NetworkVariable<float> Temperature = new NetworkVariable<float>(36.6f); // in Celsius
    public NetworkVariable<int> BleedingStacks = new NetworkVariable<int>(0); // number of wounds
    
    // Server-only update ticks
    [ServerRpc]
    public void ApplyDamageServerRpc(float amount, DamageType type) { ... }
    
    [ServerRpc]
    public void BandageWoundServerRpc() { ... }
}
```

### 2. `SomaticFeedbackManager.cs` (Client-Side)
Drives all immersive post-processing visual, audio, and physical camera adjustments dynamically based on `SurvivalStats` values.
- **Freezing:** Triggers a 0.15Hz organic camera drift. Procedurally jitters the weapon model in first-person view. Dynamically increases a cool blue URP Volume vignette.
- **Bleeding:** Pulses a dark-red vignette and drops the first-person camera vertically by 5% at a rate of 1.2Hz (simulating heartbeat). Drives a low-frequency heart-thud SFX.
- **Critical Health (<25%):** Renders the screen near-grayscale using a desaturation Volume override. Sweeps a low-pass audio filter down to 400Hz and triggers a high-pitched 10kHz tinnitus ring.
- **Player Death Sequence:** Collapses camera to the ground, rotates it 45 degrees, fades exposure to -10.0 over 1.5 seconds, sweeps low-pass audio to 80Hz, and displays glowing crimson text: "You are dead."

### 3. `SeamlessTerrainStreamer.cs` (World-Management)
An asynchronous sector management script that calculates player position on the server and client, using `SceneManager.LoadSceneAsync` with `LoadSceneMode.Additive` to dynamically stream the Kent sectors (e.g., `Kent_Sector_0_0`, `Kent_Sector_0_1`) and unload distant ones, maintaining seamless coordinates.

### 4. `BallisticsSystem.cs` (Hardcore Physics)
Calculates realistic bullet physics on the server. Bypasses standard raycasting for active rounds, simulating real projectiles over time using drag coefficients, wind vectors, and gravity-induced bullet drop. Triggers client-side impact effects.

---

# Implementation Steps

```
+----------------------------------------------------------------------------+
|                               IMPLEMENTATION MAP                           |
|                                                                            |
|  [Step 1: Netcode Setup]                                                    |
|          │                                                                 |
|          ▼                                                                 |
|  [Step 2: Network Player] ───► [Step 3: Seamless Streaming]                 |
|          │                             │                                   |
|          ▼                             ▼                                   |
|  [Step 4: Survival Stats] <────────────┘                                   |
|          │                                                                 |
|          ▼                                                                 |
|  [Step 5: Somatic Feedback] ───► [Step 6: Grid Inventory]                  |
|          │                               │                                 |
|          ▼                               ▼                                 |
|  [Step 7: Combat/Ballistics] ──► [Step 8: Prop Spawners]                   |
|          │                               │                                 |
|          ▼                               ▼                                 |
|  [Step 9: HUD & UI Screens] ───► [Step 10: Final Playtests]                |
+----------------------------------------------------------------------------+
```

### Step 1: Install & Configure Netcode for GameObjects
- **Description:** Install the `com.unity.netcode.gameobjects` package. Set up the foundational network configuration, create the `NetworkManager` Prefab, configure transport (Unity Transport), and establish network-safe prefab registries.
- **Assigned Role:** Developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2: Implement Network Player Controller & Setup
- **Description:** Adapt the existing `StarterAssets.FirstPersonController` to support Netcode. Disable client inputs, main camera, and audio listeners for non-local players. Synchronize character movement, rotation, and animation state using `NetworkTransform` and `NetworkAnimator`.
- **Assigned Role:** Developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Implement Seamless Terrain Streaming Architecture
- **Description:** Create the `SeamlessTerrainStreamer.cs` script. Divide the Kent map scene structure into a grid of additive streaming sub-scenes. Write the streaming logic to load adjacent sectors asynchronously and unload old sectors based on player coordinate bounds.
- **Assigned Role:** Developer
- **Dependencies:** Step 2
- **Parallelizable:** Yes

### Step 4: Implement Core Survival & Vital Stats System (Networked)
- **Description:** Create `SurvivalStats.cs`. Implement server-authoritative ticks for hunger, thirst, temperature, and stamina depletion. Link sprinting and jumping to stamina reduction. Replicate stats to the local client using `NetworkVariable` with custom sync rates.
- **Assigned Role:** Developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 5: Implement Somatic Game Feedback Systems & Volume Overrides
- **Description:** Create `SomaticFeedbackManager.cs`. Map local player stats to visual effects inside URP Volume Overrides (vignettes, desaturation, exposure). Hook up the procedural weapon/hand jitter for freezing and camera drops/heartbeats for bleeding.
- **Assigned Role:** Developer
- **Dependencies:** Step 4
- **Parallelizable:** Yes

### Step 6: Implement Grid-Based Networked Inventory System
- **Description:** Build `InventorySystem.cs` and matching UI controller. Create a data structure representing container capacities (Barbour Jacket with 8 slots, Backpack with 12 slots). Add networking to handle moving items between the ground (vicinity), equipment slots, and bag grids.
- **Assigned Role:** Developer
- **Dependencies:** Step 4
- **Parallelizable:** No

### Step 7: Implement Hardcore Ballistics & Melee Combat (Networked)
- **Description:** Write `BallisticsSystem.cs` for realistic bullet flight (drag, drop, wind). Build a network-synced melee system for the Cricket Bat featuring server hit registration, visual hitstops (0.04s freeze), and particle feedback (blood splatters or concrete sparks).
- **Assigned Role:** Developer
- **Dependencies:** Step 4
- **Parallelizable:** Yes

### Step 8: Build Environmental & Scavengable Prop Spawners
- **Description:** Place the custom stylized props (`Kentish Tudor Pub`, `Rusted Rover Mini`, `K6 Red Telephone Box`, and `Overgrown Hawthorn Hedgerows`) into the Kent terrain sub-scenes. Create a network-controlled spawn manager to populate containers with scavengable items on server startup.
- **Assigned Role:** Explorer (for asset organization) / Developer (for spawning logic)
- **Dependencies:** Step 3, Step 6
- **Parallelizable:** Yes

### Step 9: Integrate UI HUD & Screens
- **Description:** Design the final Survival HUD elements in the bottom-right corner. Configure UI text rendering to use the custom military typography scheme. Connect the HUD gauges directly to local `SurvivalStats` values and link the inventory grid UI.
- **Assigned Role:** Developer
- **Dependencies:** Step 5, Step 6
- **Parallelizable:** Yes

### Step 10: Perform Final Playtests & Server Verification
- **Description:** Deploy a dedicated server build. Test the seamless terrain streaming transitions under simulated latency, verify network inventory transactions, test ballistics projectile trajectories, and validate that somatic feedback triggers correctly upon damage or freezing.
- **Assigned Role:** Developer
- **Dependencies:** All Steps
- **Parallelizable:** No

---

# Verification & Testing

### 1. Network Synchronization & Replication Tests
- **Test:** Verify that player position, rotation, and active animations replicate accurately across a host and multiple clients.
- **Test:** Validate that server-authoritative vital statistics (hunger, thirst, bleeding) decrease on the server first, update on the client's HUD within 100ms, and cannot be modified by client-side memory hacks.

### 2. Seamless Terrain Streaming Tests
- **Test:** Walk across sector borders at high speed. Ensure the adjacent sector loads asynchronously and displays without dropping below 60 FPS (zero hitching).
- **Test:** Verify that distant sectors unload correctly, releasing memory and keeping the active RAM usage under 4GB on standard configurations.

### 3. Hardcore Somatic Feedback Calibration
- **Test (Bleeding):** Set bleeding stacks to 2. Confirm the screen vignette pulses dark-red at exactly 1.2Hz and the heartbeat SFX matches the tempo.
- **Test (Freezing):** Drop temperature below 35°C. Check that the first-person camera and weapon mesh exhibit organic shivering jitter and the cool blue screen vignette appears.
- **Test (Death Sequence):** Set player health to 0. Verify the 1.5-second collapse transition, sound low-pass sweep, screen fade to total blackness, and appearance of the glowing "You are dead." banner.

### 4. Hardcore Ballistics & Weapon Verification
- **Test:** Fire a projectile over 300 meters. Plot the path to ensure it matches the gravity drop curve and bullet velocity decays as expected.
- **Test (Melee Hitstop):** Strike a wall with the Cricket Bat. Check that the animation freezes for exactly 0.05 seconds, plays a stone clang sound, and emits high-velocity orange sparks.
