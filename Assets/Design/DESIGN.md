# Game Design

This design specification outlines a DayZ-inspired post-apocalyptic survival game set in South East England (Kent and Sussex). It establishes the creative vision, layout, aesthetic, and sensory feedback systems required to build a bleak, atmospheric, and highly immersive player experience. All specifications are divided into `(core)` and `(optional)` tiers to support incremental, prioritised development.

---

## UI Design

The UI must feel like a natural extension of the damp, cold, and unforgiving world of South East England, rather than a utility overlay. It uses intentional depth layering, a weathered survivalist aesthetic, and clear typographical hierarchy to communicate high stakes.

### Color System
All UI neutrals are tinted with a damp, desaturated forest green to integrate them into the wet countryside palette. Pure greys and pure blacks are strictly avoided.

- **Primary / Highlight (core):** `#4A6D55` (Active Moss Green) — Used to represent active selections, equipped items, and safe/optimal states.
- **Surface Neutral (core):** `#1E221F` (Deep Charcoal Green) — The base panel color, used for the main inventory backdrop and HUD base containers.
- **Surface Raised (core):** `#2A2F2C` (Medium Charcoal Green) — Used for nested panels, item cards, and container slots to establish structure without outlines.
- **Interactive Neutral (core):** `#38403B` (Lighter Charcoal Green) — The default color for buttons and clickable surfaces.
- **Accent - Low Stakes (optional):** `#D69E2E` (Dull Ochre / Hazard Yellow) — Used for warnings, mild hunger/thirst, and minor wear on items.
- **Accent - High Stakes / Error (core):** `#B23B3B` (Crimson Red) — Exclusively reserved for bleeding, critical status effects, damage, and death screens.
- **Typography Light (core):** `#E3E8E5` (Off-White) — Primary text color.
- **Typography Muted (core):** `#A0AAA4` (Desaturated Sage) — Secondary label and unit text.

### Typography
The type scale uses high contrast between bold, industrial displays and clean, readable body text to convey a military/utilitarian survival theme.

- **Headline / Display Font (core):** A bold, condensed, geometric sans-serif (e.g. Teko or heavy DIN style). Used for large status numbers, location markers, and inventory headers. Exaggerated proportions are used here (e.g. huge "45" for temperature, with a tiny "°C" label).
- **Body Font (core):** A modern, highly legible sans-serif (e.g. Inter or Roboto). Used for descriptions, item lore, and settings.
- **Label / Unit Font (core):** A clean, monospaced or narrow sans-serif (e.g. Roboto Mono). Used for technical stats (e.g., "7.62x39mm", "0.5L", "KG").

### Layout & Screen Structure

#### 1. Survival HUD (core)
Positioned in the bottom right corner, the HUD is minimalist and non-obtrusive, fading in intensity when the player is safe and fully hydrated/fed. Each survival indicator features a custom icon:
- **Health (core):** A stylized heart icon. Drains from primary green to yellow, then flashes crimson under 20%.
- **Stamina (core):** A thin horizontal bar that sits below the other icons. Depletes during sprinting, jumping, or heavy melee swings.
- **Hunger (core):** A rustic fork-and-knife icon. Turns from green to ochre, then flashes crimson as starvation sets in.
- **Thirst (core):** A water droplet icon. Depletes faster than hunger; flashes crimson when dehydrated.
- **Temperature (core):** A classic glass thermometer icon. Glows light blue when freezing (hypothermia risk) and deep red when hyperthermic.
- **Bleeding (core):** A blood droplet icon. Remains invisible unless the player has open wounds. Flashes crimson and displays a multiplier label (e.g., "x2") to indicate active bleeding sites.

#### 2. Inventory Screen (core)
An immersive, full-screen split overlay with a blurred background of the physical world.
- **Left Column - Equipped Gear (core):** A skeletal, physical layout of the player character's slot positions (Head, Torso, Legs, Feet, Hands, Back).
- **Center Column - Container Grid (core):** A slot-based 2D grid where items occupy specific physical blocks (e.g., a rusty cricket bat takes 1x4 slots; an apple takes 1x1). Nested containers (jacket, trousers, backpack) are displayed as distinct raised panels (`Surface Raised`) without hard borders, relying on spacing.
- **Right Column - Vicinity & Inspection (core):** Displays items laying on the ground nearby in a simple list. Selecting an item brings up a detailed card showing its physical 3D rotate preview, weight, status (pristine/damaged/ruined), and description.

### Component Design

- **Buttons (core):** 4px corner radius for a rugged, utilitarian look. 
  - *Structure:* Built using a soft ambient shadow (Offset 0px, 4px, 8px, `#0B0C0B`) and a darker bottom edge (3px offset, `#121413`) to simulate a thick, physical button.
  - *States:*
    - *Normal:* `#38403B` background, `#E3E8E5` text.
    - *Hover (optional):* `#4A6D55` background, slight 1px upward offset.
    - *Active / Pressed (core):* Visual thud effect. Background shifts to `#1E221F`, bottom edge thickness collapses to 1px, and the entire element offsets 2px downwards.
- **Cards (core):** Item and equipment slots. No internal lines or dividers. Uses a subtle background gradient of `#2A2F2C` (top) to `#1E221F` (bottom) to give a hollow, inset pocket feel. Equipped cards have a 1px soft highlight border of Active Moss `#4A6D55`.
- **Text Inputs (optional):** Recessed/inset appearance. Solid `#141715` background (darker than base panels) with a 1px lighter top edge highlight to make the field feel carved into the UI.
- **Overlays (core):** Full-screen panels use a 90% opacity tint of `#0E110F` combined with a soft real-time camera blur to separate the interface from active danger without fully breaking immersion.

---

## Asset Design

The art direction captures the melancholy, overgrown, and damp atmosphere of post-apocalyptic South East England. It is characterized by cool, overcast tones, rotting wood, cracked brick, oxidized metals, and creeping nature.

### Visual Identity
- **Style Consistency (core):** Gritty, weathered 3D realism. Texture detail focuses heavily on micro-wear: lichen growing on damp brickwork, water streaks running down crumbling plaster, rust eating through fender arches, and ivy strangling wooden signposts.
- **Color Temperature (core):** Dominantly cool and desaturated. Lighting mimics a permanent overcast autumn sky, casting soft, diffused shadows with low contrast. Warm light is extremely rare, restricted to player-made campfires or rare hand-cranked lanterns.
- **Silhouette Clarity (core):** Because scavenging is central, every interactive prop must have a highly readable silhouette from 10 meters away, even in dim conditions. A box of 12-gauge ammunition must look distinctly rectangular and sharp, while a canned tin of beans must have a perfect, smooth cylindrical profile.

### Color Palettes by Category

- **Architecture & Hard Surfaces (core):**
  - Dominant: `#52362C` (Weathered Tudor Timber / Clay Tile Red), `#757D75` (Damp Concrete/Slate)
  - Secondary: `#C2B29A` (Cracked Lime Plaster / Aged Limestone)
  - Accent (optional): `#A53F2B` (Chipped Royal Mail Red / Telephone Box)
- **Environment & Nature (core):**
  - Dominant: `#3A4138` (Damp Fern Green / English Oak Foliage), `#4E4237` (Saturated Wet Soil / Bark)
  - Secondary: `#2F332C` (Overgrown Brambles / Hawthorn Hedgerows)
  - Accent (optional): `#C8B168` (Dull Gorse Blossom Yellow / Lichen)
- **Scavengable Props & Weapons (core):**
  - Dominant: `#3E443F` (Faded Wax Canvas / Olive Drab military gear), `#4C4F56` (Cold Gunmetal Steel)
  - Secondary: `#8A8279` (Weathered Ash Wood / Faded Tan Corduroy)
  - Accent (core): `#D69E2E` (Hazard Orange / Industrial Tape)

### Architectural & Environmental Guidelines

#### 1. Tudor Brick Pubs (core)
Dotted along rural crossroads, these are critical landmarks and defensive structures.
- *Structure:* Dark, decaying oak support beams framing cracked cream plaster. The plaster must be missing in chunks, revealing damp, dark red Kentish bricks beneath.
- *Detailing:* Green moss climbing up the damp north-facing foundations. Roofs made of sagging clay tiles, with some tiles slipped and broken on the ground. Signboards (e.g., "The Oak & Ivy") must hang askew, creaking in the wind.

#### 2. Derelict British Vehicles (core)
Rusting hulks scattered across overgrown country lanes.
- *Models:* Classic 1990s Rover Minis, rusted Vauxhall Astra hatchbacks, and decayed Leyland tractors.
- *Condition:* Stripped of tyres, propped up on rusted brake discs. Paintwork is severely faded from acid rain, with heavy orange oxidation bubbles bursting along the wheel arches and sills. Windshields are either entirely shattered or covered in a thick layer of grime and pine needles.

#### 3. Rusted Red K6 Telephone Boxes (optional)
An iconic British silhouette converted into a rustic landmark.
- *Condition:* The cast-iron structure is faded to a dull, matte brick-red. Most glass panes are smashed, with the remaining frames filled with dirt. Brambles and English ivy wrap tightly around the base and crawl up the door frame, anchoring it to the earth.

#### 4. Damp Green Forests & Country Lanes (core)
The connective tissue of the English landscape.
- *Woodlands:* Dominated by massive, ancient English Oaks and wet Silver Birches. The canopy should block out most light, creating a dark, damp floor covered in decaying brown leaves and rotting logs.
- *Country Lanes:* Narrow, single-vehicle asphalt roads cracked by roots. Flanked on both sides by dense, 2.5-meter-high overgrown Hawthorn and Blackthorn hedgerows, creating natural maze-like corridors that trap the player.

---

## Game Feedback Design (Polishment)

The feedback system balances high-tension silence with heavy, visceral feedback during survival emergencies and violent encounters. It follows a strict "Atmospheric Survival" hybrid profile, reserving major sensory impact for critical moments.

### Genre Profile
- **Tone Rationale:** Quiet, agonizing exploration punctuated by sudden, heart-stopping violence. Feedback is designed to make physical conditions (cold, bleeding, exhaustion) feel claustrophobic and internally distressing, while combat is messy and impactful.

### Interaction Feedback Map

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Input | Rationale |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Freezing / Severe Cold** | `(core)` | Medium | Slow, organic camera drift (0.15Hz) to simulate shivering focus. | — | Weapon/hands procedurally jitter in first-person view. | Cool blue vignette screen edges. Film grain intensity increased by 40% to blur focus. | Chattering teeth SFX and irregular, shivering breaths. | — | Warns player of hypothermia risk through bodily feedback instead of relying on a screen prompt. |
| **Active Bleeding** | `(core)` | Heavy | Sharp, vertical camera drops (1.2Hz pulse) on heartbeat. | — | Camera FOV pulses slightly out-and-in on heartbeats. | Pulsing dark-red screen vignette. Post-exposure lowered by 0.3 stops, darkening the world. | Loud, low-frequency heartbeat thuds. Squelching liquid drips. | Light, continuous controller rumble on heartbeat. | Creates a claustrophobic panic state during blood loss, driving immediate self-preservation. |
| **Low Health (Under 25%)** | `(core)` | Critical | Staggering camera bob cycle simulating a limping gait. | — | Weapon sway and tilt increased by 300%. Melee windups are 20% slower. | Near-total desaturation (grayscale world). Contrast increased to simulate tunnel-vision. | Low-pass filter (cutoff 400Hz) on all ambient sound. High-frequency tinnitus ring (10kHz). | Movement input responsiveness dampened by 10%. | Simulates fading consciousness and the terrifying transition of dying in the wilderness. |
| **Stamina Depletion** | `(core)` | Medium | Continuous high-frequency head-bob during running. | — | Weapon aiming sway heavily magnified. Hands visibly drop from ADS. | Subtle blurring of the peripheral screen edges using a shallow depth of field. | Desperate, heavy gasping and ragged breathing sounds (randomized pitch ±10%). | Disables sprinting and reduces jumping height by 50% until recovered. | — | Forces tactical pacing and stamina management via direct somatic feedback. |
| **Footsteps in Wet Mud** | `(core)` | Minor | — | — | — | Subtle dark mud splashes ejected behind the player's heels. | Squelching, organic mud-crush sounds. 6 alternate steps, pitch randomized by ±12%. | Subtle, sharp haptic tap on heel strike (optional). | Grounds the player physically in the wet, muddy English countryside, reinforcing the damp atmosphere. |
| **Melee Weapon Impact** | `(core)` | Heavy | Directional screen shake matching the swing angle. Exponential decay. | 0.04s hitstop freeze frame on successful impact. | Target mesh squashes 15% along strike vector, overshooting then settling. | Bright red 2-frame hit flash on target. Blood spray particles emit along the blade's exit path. | A deep, wet crunch or flesh-slice sound (pitch randomized ±10%). | Strong, instantaneous dual-motor controller rumble. | Conveys physical weight, making survival melee combat feel brutal, desperate, and heavy. |
| **Bullet Impact (Flesh)** | `(core)` | Critical | High-frequency, sharp camera recoil punch. Decay over 0.1s. | 0.02s hitstop on critical headshots. | Target head/torso reels back along bullet trajectory. | Mist of fine blood spray. Camera lens gets a tiny, transient red droplet spray. | Crisp, wet "thwack" of kinetic entry followed by high-altitude crack echo. | Sharp, asymmetric haptic jolt. | Makes gunshots feel lethal, high-stakes, and instantly recognizable. |

### Core Assets Needed for Feedback & Environment

- **Kentish Tudor Pub (core):**
  - *Type:* Structural Environment Asset.
  - *Style Reference:* Rotting oak beams, mossy cream plaster, slipping clay tiles.
  - *Palette:* Dominant: `#52362C` (Timber), Accent: `#3A4138` (Moss).
  - *Context:* Village landmark and high-tier defensive loot location.
  - *Size:* 15m x 12m x 7.5m.
- **Rusted Rover Mini (core):**
  - *Type:* Vehicle Prop Asset.
  - *Style Reference:* Heavily oxidized 1990s British compact car.
  - *Palette:* Dominant: `#424E43` (Racing Green), Accent: `#7A664B` (Rust Orange).
  - *Context:* Street obstacle, engine component scavenger spot.
  - *Size:* 3.1m x 1.4m x 1.3m.
- **Overgrown Hawthorn Hedgerow (core):**
  - *Type:* Foliage Barrier Asset.
  - *Style Reference:* Dense, thorny, wild British field boundary.
  - *Palette:* Dominant: `#2F332C` (Bramble Green), Accent: `#4E4237` (Damp Bark).
  - *Context:* Lines country lanes to channel player movement.
  - *Size:* 3.0m x 1.2m x 2.5m.
- **K6 Red Telephone Box (optional):**
  - *Type:* Hard Surface Prop Asset.
  - *Style Reference:* Chipped, ivy-choked iconic cast-iron phone booth.
  - *Palette:* Dominant: `#A53F2B` (Faded Crimson), Accent: `#3A4138` (Ivy Green).
  - *Context:* Rural roadside landmark and emergency supply stash point.
  - *Size:* 1.0m x 1.0m x 2.4m.
- **Cricket Bat (core):**
  - *Type:* Melee Weapon Asset.
  - *Style Reference:* Weathered willow bat wrapped in decaying black electrical tape.
  - *Palette:* Dominant: `#8A8279` (Ash Wood), Accent: `#1E221F` (Charcoal Tape).
  - *Context:* Primary starting defensive weapon.
  - *Size:* 0.1m x 0.05m x 0.95m.
- **British Wax Barbour Jacket (core):**
  - *Type:* Apparel / Clothing Asset.
  - *Style Reference:* Heavily stained, torn, waxed-cotton hunting jacket.
  - *Palette:* Dominant: `#3E443F` (Wax Olive), Accent: `#D69E2E` (Brass snaps).
  - *Context:* Torso equipment providing weather protection and 8 inventory slots.
  - *Size:* Adjusted to fit human character mesh.

### Sequences

#### Player Death Sequence (core)
`[Event]: Player health drops to 0`
- **0ms:** Player's camera vertical position drops sharply to the floor (simulating a physical collapse). Controls are locked.
- **100ms:** Camera rotates 45 degrees sideways to rest on the ground, partially buried in grass.
- **200ms:** Post-exposure on the screen volume declines from `0.0` to `-10.0` over 1.5 seconds, fading the world to black.
- **300ms:** Low-pass filter cutoff on all game audio sweeps down from `20000Hz` to `80Hz` over 1.0 second, muffling the surrounding world.
- **1500ms:** High-frequency tinnitus ring dies out. Complete silence.
- **2000ms:** Deep crimson text "You are dead." slowly fades into the center screen over 2.0 seconds, glowing with soft ambient light.

#### Melee Weapon Deflect / Surface Strike Sequence (optional)
`[Event]: Melee strike hits concrete, stone, or metal instead of flesh`
- **0ms:** Player's arms rebound backwards. Camera angles recoil upwards by 5 degrees.
- **10ms:** Hitstop of 0.05s triggers, freezing the animation and camera mid-impact to convey extreme density.
- **60ms:** Sharp metallic or stone "clang" audio played. Pitch is randomized by ±15% to keep repeated deflections varied.
- **70ms:** A small shower of high-velocity orange sparks is emitted from the impact point, accompanied by gray dust particles.
- **120ms:** Visual "rebound" animation plays out, returning the weapon model to its idle stance over 0.25 seconds.
