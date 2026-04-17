# 🤖 Iron Cradle

## A RimWorld BioTech Mod

![RimWorld](https://img.shields.io/badge/RimWorld-1.6-8B4513?style=flat-square)
![BioTech DLC](https://img.shields.io/badge/BioTech_DLC-Required-6A0DAD?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-9.0-239120?style=flat-square&logo=csharp)
![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)

**Automated repair docking station for BioTech mechanoids.**
When damaged, your mechanoids will autonomously seek out the station and self-repair — no micromanagement required.

> 📖 **Documentación en español** disponible en [`docs/es/README.md`](docs/es/README.md)
> 📖 **Documentação em português** disponível em [`docs/pt/README.md`](docs/pt/README.md)

---

## ✨ Features

- **Autonomous repair** — Mechanoids detect when their health drops below a configurable threshold and navigate to the nearest available station without player input
- **Resource consumption** — Repair consumes steel managed by `CompRefuelable`; colonists haul steel automatically when reserves drop below 25%
- **Power-gated** — Requires an active electrical connection; station shuts off cleanly when power is lost
- **Manual eject** — A gizmo button lets the player forcibly remove a mechanoid mid-repair
- **Station priority** — Each station has a configurable priority (1–9); mechanoids prefer the station with the lowest priority number
- **Fully configurable** — All parameters (health threshold, repair speed, steel cost, detection range) are editable in the XML with no recompile needed
- **Research-gated** — Unlocked by *Mechanoid Repair Systems* (Spacer tier, 1200 pts), requiring *Mechanoid Basics* first
- **Breakdown-able** — Requires periodic maintenance, consistent with vanilla industrial buildings
- **Save/load safe** — All state (occupant, station priority, repair threshold) is serialized with `Scribe_References` and `Scribe_Values`
- **No lost body parts** — Repairs active injuries only; permanent damage is not restored (by design)
- **No Harmony dependency** — All AI integration is done via XML ThinkTree patching, keeping compatibility risk low

---

## 🏗️ Architecture Overview

The mod is built around four interconnected systems:

```text
┌─────────────────────────────────────────────────────────────────┐
│                        AI LAYER (ThinkTree)                     │
│                                                                 │
│  ThinkNode_ConditionalNeedsRepair                               │
│    └─ Checks: is mechanoid? player-owned? health < threshold?   │
│       └─ JobGiver_GoToIronCradle                                │
│            └─ Emits: IC_GoToIronCradle job                      │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        JOB LAYER                                │
│                                                                 │
│  JobDriver_GoToIronCradle                                       │
│    1. GotoThing → walk to InteractionCell                       │
│    2. dock (Instant) → TryAcceptOccupant → enqueue repair job   │
│                                                                 │
│  JobDriver_RepairAtIronCradle                                   │
│    - Wait (ToilCompleteMode.Never)                              │
│    - Ends when CurrentOccupant becomes null                     │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BUILDING LAYER                             │
│                                                                 │
│  Building_IronCradle                                            │
│    - Manages occupant (TryAcceptOccupant / EjectOccupant)       │
│    - Tick: TryConsumeSteel every repairTickInterval             │
│    - Steel managed by CompRefuelable (cap. 50 units)            │
│    - Gizmos, InspectString, save/load                           │
│                                                                 │
│  CompIronCradle (ThingComp)                                     │
│    - CompTick: ApplyRepairTick every repairTickInterval         │
│    - Heals all active (non-permanent) Hediff_Injury instances   │
│    - Calls OnRepairComplete when health ≥ 99%                   │
│    - Stores per-instance repairThreshold (player-adjustable)    │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     REGISTRY LAYER                              │
│                                                                 │
│  IronCradleTracker (MapComponent)                               │
│    - O(1) register/deregister on SpawnSetup / DeSpawn           │
│    - ThinkNodes iterate this list instead of searching the map  │
│    - Declared in MapComponentDefs.xml; auto-instantiated by RW  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📁 Folder Structure

```text
IronCradle/
│
├── About/
│   └── About.xml                        ← Mod metadata, packageId (RexThar.IronCradle), BioTech dependency
│
├── Assemblies/
│   └── IronCradle.dll                   ← Compiled output (do not edit manually)
│
├── 1.6/
│   ├── Defs/
│   │   ├── JobDefs/
│   │   │   └── JobDefs_IronCradle.xml       ← IC_GoToIronCradle + IC_RepairAtIronCradle
│   │   ├── MapComponentDefs/
│   │   │   └── MapComponentDefs.xml         ← Registers IronCradleTracker with RimWorld
│   │   ├── ResearchProjectDefs/
│   │   │   └── ResearchDefs.xml             ← IC_IronCradleResearch (Spacer, 1200 pts)
│   │   └── ThingDefs/
│   │       └── Buildings_IronCradle.xml     ← ThingDef IC_IronCradle: size, cost, comps, research
│   │
│   ├── Languages/
│   │   ├── English/Keyed/
│   │   │   └── IronCradle.xml               ← All player-visible strings (base locale)
│   │   ├── Spanish (Español)/Keyed/
│   │   │   └── IronCradle.xml               ← Spanish (Spain) translation
│   │   ├── SpanishLatin (Español(Latinoamérica))/Keyed/
│   │   │   └── IronCradle.xml               ← Spanish (Latin America) translation
│   │   └── Portuguese (Português)/Keyed/
│   │       └── IronCradle.xml               ← Portuguese translation
│   │
│   └── Patches/
│       └── MechanoidThinkTree.xml           ← Injects repair node into MechanoidPlayerControlled
│
├── Source/
│   ├── IC_Mod.cs                            ← StaticConstructorOnStartup bootstrap + texture check
│   ├── IC_JobDefOf.cs                       ← [DefOf] static job references
│   ├── Building_IronCradle.cs               ← Main building: occupant, CompRefuelable steel, UI
│   ├── CompProperties_IronCradle.cs         ← CompProperties_IronCradle + CompIronCradle (healing tick)
│   ├── JobDriver_GoToIronCradle.cs          ← Walk-to-station job driver
│   ├── JobDriver_RepairAtIronCradle.cs      ← Docked repair job driver
│   ├── ThinkNode_ConditionalNeedsRepair.cs  ← AI conditional + JobGiver_GoToIronCradle + RepairStationUtility
│   └── IronCradleTracker.cs                 ← MapComponent station registry
│
├── Textures/
│   └── Things/
│       └── Buildings/
│           └── IronCradle.png               ← 128×128 building sprite (must be added)
│
├── docs/
│   ├── es/
│   │   └── README.md                        ← Full documentation in Spanish
│   └── pt/
│       └── README.md                        ← Documentação completa em português
│
└── .vscode/
    ├── mod.csproj                           ← Project file (net480, RootNamespace & AssemblyName: IronCradle)
    ├── tasks.json                           ← Build tasks (Windows + Linux)
    ├── launch.json                          ← Launch & attach debugger configs
    └── extensions.json                      ← Recommended VS Code extensions
```

---

## ⚙️ Configuration Reference

All parameters are tunable directly in `1.6/Defs/ThingDefs/Buildings_IronCradle.xml` inside the `<li Class="IronCradle.CompProperties_IronCradle">` block — no recompile needed.

| Property | Default | Description |
| --- | --- | --- |
| `repairHealthThreshold` | `0.5` | Default health fraction (0–1) below which a mechanoid seeks repair. Acts as the initial value per station; the player can override it in-game via gizmo. |
| `repairSpeedPerTick` | `0.0005` | HP restored per game tick to each active injury. |
| `steelPerRepairCycle` | `1` | Units of steel consumed per repair interval. At default settings, ~7.2 units/hour. |
| `repairTickInterval` | `500` | Ticks between each steel consumption and healing cycle (~8.3s at ×1 speed). Controls both resource granularity and CPU cost. |
| `maxRepairRange` | `30` | Maximum cell distance for a mechanoid to detect and path to this station. |

> **Tuning tip:** `repairSpeedPerTick` and `repairTickInterval` are coupled. The effective HP healed per second is `repairSpeedPerTick × 60`.

---

## 🔬 How Repair Works (Step by Step)

1. Every AI tick, `ThinkNode_ConditionalNeedsRepair.Satisfied()` checks each player mechanoid in this order (cheapest checks first):
   - Is it a mechanoid? Is it player-owned?
   - Is it already running a repair job (`IC_RepairAtIronCradle` or `IC_GoToIronCradle`)?
   - Is its health already at or above 99%? *(cheap pre-filter to skip healthy mechanoids)*
   - Is there a powered, unoccupied, reachable station within `maxRepairRange`? *(most expensive — runs last)*
   - Is health below the `repairThreshold` configured on that specific station?

2. If all conditions pass, `JobGiver_GoToIronCradle` emits an `IC_GoToIronCradle` job targeting the nearest valid station (lowest `StationPriority`, then closest), after verifying no other pawn of the same faction has already reserved it.

3. `JobDriver_GoToIronCradle` walks the mechanoid to the station's `InteractionCell`, then calls `Building_IronCradle.TryAcceptOccupant()` and enqueues `IC_RepairAtIronCradle`. If a race condition fills the station between walking and docking, the job ends as `Incompletable` and the mechanoid will retry.

4. Every `repairTickInterval` ticks while docked:
   - **Building tick:** `TryConsumeSteel()` deducts from `CompRefuelable`. If there is not enough steel, the mechanoid is ejected and the player is notified. Colonists will automatically haul more steel when reserves drop below 25%.
   - **Comp tick:** `ApplyRepairTick()` calls `injury.Heal(repairSpeedPerTick)` on every active (non-permanent) `Hediff_Injury`. Healing is skipped if steel ran out during the building tick.

5. When `SummaryHealthPercent ≥ 0.99`, `OnRepairComplete()` fires: the player receives a positive letter, a light-glow fleck is spawned on the mechanoid, `CurrentOccupant` is set to `null`, and `JobDriver_RepairAtIronCradle`'s `tickAction` detects the change and ends the job cleanly.

6. On load, `PostMapInit()` validates that the serialized occupant still has an active or queued repair job. If not (e.g. save edited externally), the occupant reference is cleared and a warning is logged, preventing the station from being permanently locked.

---

## 🧱 Building Stats

| Stat | Value |
| --- | --- |
| DefName | `IC_IronCradle` |
| Size | 2×2 tiles |
| Max HP | 300 |
| Work to Build | 4,000 ticks |
| Flammability | 50% |
| Beauty | −2 |
| Power Draw | 250W |
| Cost | 150 Steel + 4 Industrial Components + 1 Spacer Component |
| Research | `IC_IronCradleResearch` — Mechanoid Repair Systems (Spacer, 1200 pts) |
| Prerequisite Research | Mechanoid Basics (BioTech DLC) |
| Steel capacity (CompRefuelable) | 50 units |
| Auto-refuel threshold | 25% (12 units) |

---

## 🔧 Building & Compiling

### Prerequisites

- .NET SDK targeting `net480` (Visual Studio 2022 / JetBrains Rider or `dotnet` CLI)
- RimWorld 1.6 installed via Steam
- `Krafs.Rimworld.Ref` NuGet package (resolved automatically on build)

### Visual Studio / Rider

1. Open `.vscode/mod.csproj`.
2. Build → Release. The DLL is automatically output to `1.6/Assemblies/` as `IronCradle.dll`.

### Command Line

```bash
cd .vscode
dotnet build -c Release
```

### VS Code

Use the **Build & Run** task (`Ctrl+Shift+B`) defined in `.vscode/tasks.json`. A separate **Attach Debugger** launch configuration connects Mono on port `56000` for live debugging.

### Default RimWorld Paths

| OS | Path |
| --- | --- |
| Windows | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` |
| Linux | `~/.steam/steam/steamapps/common/RimWorld` |
| macOS | `~/Library/Application Support/Steam/steamapps/common/RimWorld` |

---

## 🖼️ Adding the Texture

Place a **128 × 128 px** PNG at:

```text
Textures/Things/Buildings/IronCradle.png
```

**Style guide:** Match the BioTech aesthetic — dark gunmetal panels with teal/blue accent lighting. The building is 2×2 tiles; keep the sprite visually centered with a subtle docking arm or cradle motif. The ThingDef uses `Graphic_Single`, so the texture is not rotated — design for a top-down, south-facing perspective. The startup bootstrap in `IC_Mod.cs` will log a warning if this file is missing.

---

## 📦 Installation

1. Copy the `IronCradle/` folder to your RimWorld mods directory:

    | OS | Mods Directory |
    | --- | --- |
    | Windows | `%APPDATA%\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods\` |
    | Linux | `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Mods/` |
    | macOS | `~/Library/Application Support/RimWorld/Mods/` |

2. Enable the mod in-game. Ensure the **BioTech DLC** is active.

3. Research *Mechanoid Basics*, then *Mechanoid Repair Systems* to unlock the building.

---

## 🌍 Localization

All player-visible strings live in `1.6/Languages/<Language>/Keyed/IronCradle.xml`. All keys use the `IC_` prefix to avoid collisions with other mods. Keys containing `{0}` are format strings filled at runtime (e.g. with the mechanoid's short label).

| Language | Path |
| --- | --- |
| English | `1.6/Languages/English/Keyed/` |
| Spanish (Spain) | `1.6/Languages/Spanish (Español)/Keyed/` |
| Spanish (Latin America) | `1.6/Languages/SpanishLatin (Español(Latinoamérica))/Keyed/` |
| Portuguese | `1.6/Languages/Portuguese (Português)/Keyed/` |

To add a new translation:

1. Create `1.6/Languages/<LanguageName>/Keyed/IronCradle.xml`
2. Copy the English file and replace the values (keep the keys identical)
3. RimWorld falls back to English automatically for any missing keys

---

## ⚠️ Known Limitations & Design Decisions

- **No regeneration of lost body parts** — `HediffComp_GetsPermanent` injuries are intentionally skipped in `ApplyRepairTick()`. Regrowing limbs is out of scope for this building tier.
- **Single occupant per station** — The station supports exactly one mechanoid at a time. Place multiple stations for larger mechanoid squads.
- **Steel managed by CompRefuelable** — The steel gizmo (capacity and current level) is shown automatically by the comp. Colonists haul steel to the station like any refuelable building; no manual configuration needed.
- **ThinkTree patch targets `MechanoidPlayerControlled`** — This is the tree used by player-owned BioTech mechanoids. If Ludeon restructures it in a future update, the XPath in `1.6/Patches/MechanoidThinkTree.xml` may need updating. Symptom: mechanoids never seek the station autonomously.
- **`MechanoidConstant` patch is commented out** — A secondary patch for that tree is present but disabled by default. Uncomment it in `MechanoidThinkTree.xml` only if you need compatibility with third-party mods that reuse that tree for allied mechanoids.
- **No Harmony patches** — All AI integration is done via XML ThinkTree patching, keeping compatibility risk low.
- **Assembly name matches namespace** — Both `RootNamespace` and `AssemblyName` in `.vscode/mod.csproj` are set to `IronCradle`, matching the C# namespace throughout the source.

---

## 📜 License

MIT — free to use, modify, and distribute with attribution.

---

Built for RimWorld 1.6 · Requires BioTech DLC · Author: RexThar
