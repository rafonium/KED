# KED — Engine Overhaul Design Document

## Overview

This plan replaces the existing per-engine stochastic model with a **Batch Quality System (BQS)**. Engine categories are determined strictly by the archetype logic in `EngineFilterLogic.md`. The UPI-based class tiers are replaced by the **Operational Maturity System (OMS)**, where "Maturity" is the only metric that matters for reliability progression. Each archetype now has its own identity, batch behavior, and maturity roadmap.

> *"An engine feels nearly invincible — until it doesn't."*

---

## Resolved Design Decisions

| Question | Decision |
|---|---|
| **Batch Reveal Timing** | **Hidden by default.** Player can reveal Lemon status via `CycleValves` (Hypergolic) or `Deep Diagnostics` (EVA). |
| **Cross-Engine Batch Grouping** | **Per part name only.** Different part names = different batches, always. |
| **Weak Unit After Repair** | **Restored but degraded.** Performance penalties apply (ISP/Thrust) and future fatigue increases (Scars). |
| **Repair System** | **Partial Repair Logic.** Engineers can invest kits over multiple sessions. Costs scale with Engineer Level. |
| **Nuclear Repair Access** | **Engineer Level 3+ required** to repair or diagnostic a nuclear engine. |
| **ASI for Modded Engines** | Automated detection via ISP curve — uses `[KED_ASI_OVERRIDE]` if needed. |
| **Batch Lineage** | **Linked across launches.** `LineageRisk` tracks production quality (Lemons increase risk, Goods decrease it). |

---

## Part 1 — The Batch Quality System (BQS)

### 1.1 What Is a Batch?

When a vessel is launched, every engine of the **same part name** on that rocket shares one **Batch ID** — a manufacturing lot number stamped at build time.

- Batch ID generated once at launch, stored in vessel persistent data.
- Engines of **different part names** are always separate batches.
- **Batch Lineage (Dynamic Risk)**: If a part name gets a "Lemon" result, the `LineageRisk` increases by 5%. Successful "Good" batches reduce it by 2%. This simulates production quality fluctuations.
- **Aging Factor**: Once an engine model passes its `AgingThreshold` (Default: 50 flights), its reliability starts to decay globally, increasing the base Lemon probability.
- **Diegetic Pre-Launch Hints**: While the roll is hidden, the Factory Specification or Launchpad PAW shows correlated hints:
  - *"Factory QA Note: Slight variance detected in turbopump alignment."*
  - *"Batch vibration signature above nominal."*
  - These hints are ~70% correlated with a Lemon status.
- **Controlled Chaos (Variance):** 
    - **80% Cases:** Exactly 1 Weak Unit.
    - **15% Cases:** 2 Weak Units (Rare "double fault").
    - **5% Cases:** **Degraded Batch** (All engines in the batch suffer immediate Thrust Drop penalties).

### 1.2 The Reliability Formula (Binary Batch System)
The **Binary Batch System (BQS)** for KED 3.0 replaces complex stochastic reliability with a streamlined manufacturing model. All engines of the same part name on a vessel share a single batch identity, which is rolled at launch to determine if the lot is **Good** or a **Lemon**.

The probability of a "Lemon" batch scales linearly with the number of engines in the cluster to represent the complexity of mass manufacturing.

**The Anchor Formula:** $$P(\text{Lemon}) = \text{MaturityAnchor} + (\text{Engine Count} - 1) \times 0.0167$$
*(Note: MaturityAnchor starts at 0.05 for most archetypes at Level 0).*

| Engine Count | Good Batch Chance | Lemon Batch Chance | Gameplay Feel |
| :--- | :--- | :--- | :--- |
| **1 (Solo)** | **95%** | **5%** | Highly reliable; failure is a rare anomaly. |
| **10 (Cluster)** | **80%** | **20%** | Standard heavy lifter risk; 1-in-5 chance of a dud. |
| **20 (Complex)** | **63.3%** | **36.7%** | Redundancy is required to survive a failure event. |
| **30 (Extreme)** | **46.6%** | **53.4%** | Statistical certainty that a failure will occur. |

### 1.3 The Weak Unit Mechanic
If a batch is rolled as a **Lemon**, the group is designated for potential failure.

*   **Controlled Chaos (Variance):** Instead of exactly one Weak Unit, the system uses a probability sp*   **Deterministic Triggers:** KED 3.0 uses a fuel-based or time-based **Failure Trigger Time**. This eliminates frame-rate dependence.
*   **Trigger Windows:**
    - **Liquid/Electric:** Randomly designated window between 15s and 50s of cumulative burn time.
    - **SRB Spread Window:** Guaranteed safe for the first 10s. The failure trigger is rolled as a **fuel threshold** between 40% – 70% remaining fuel, peaking at 50%.
*   **Impulse Failures:** Failures occur suddenly without explicit pre-failure sensory cues (Jitter removed).
*   **Repair Recovery:** Successfully performing an EVA repair clears the "Lemon" flag but applies a **Performance Scar** (-2% Thrust or ISP depending on archetype). Recovery is not a "perfect reset."

### 1.5 Why Multi-Engine Tests Fail

Same-part-name engines on one rocket share a batch. Testing a 4-engine cluster gives you one batch result, not four. To gather meaningful data across batches, the player must fly **separate missions** with the same engine part.

### 1.6 Good Batch Degradation

Even **Good** batches still wear over time:

| Degradation | Trigger | Effect |
|---|---|---|
| Ignition Fatigue | Each start/stop cycle | Stacking failure risk. Multiplied if starting "Booster" engines in vacuum. |
| Catalyst Decay | Cumulative burn seconds | Exponential decay after `CatalystServiceLimit`. Leads to Thrust Drops. |
| Hypergolic Fatigue | Each ignition | Stacking +0.5% failure risk per cycle. |
| **Aging Factor** | Heritage engines (50+ flights) | Base Lemon probability increases per flight after threshold. |

### 1.7 Pre-Launch Logistics
Players can make strategic decisions before launch to influence batch quality:
- **Extended QA (Cost/Time):** Increases funds/build time to reduce Lemon chance for that launch.
- **Rush Production:** Reduces funds/build time but increases Lemon chance.


---

## Part 2 — Atmospheric Sensitivity Index (ASI)

ASI is a continuous number replacing binary booster/vacuum labels.

```
ASI = Isp_VAC / Isp_ASL
```

| ASI Range | Role | Vacuum Behavior | Re-ignition at Altitude |
|---|---|---|---|
| `1.00 – 1.10` | Pure Booster | No bonus | Severe penalty |
| `1.11 – 1.25` | Booster-Biased | Minor bonus | High penalty |
| `1.26 – 1.50` | Sustainer (Sweet Spot) | Moderate bonus | Moderate penalty |
| `1.51 – 2.00` | Vacuum-Biased | Significant bonus | Low penalty |
| `2.01+` | Pure Vacuum | High bonus | No penalty |

- **Vacuum bonus**: Ignition and running reliability improve as pressure drops below threshold, scaled by `(ASI - 1.25)`.
- **Booster penalty**: Re-ignition fatigue multiplied by `(1.25 - ASI) × K` when pressure is low.
- **ASI Gameplay Visibility**: 
  - **Real-time feedback**: "Engine operating outside optimal atmospheric band" displayed in PAW.
  - **Efficiency Zones**: Staging UI provides subtle indicators of optimal pressure zones.
  - **Failure Tie-in**: Operating far outside the ASI sweet spot slightly increases failure probability.
- ASI computed once at part load. Stored as `float atmSensitivityIndex`.
- Modded engines with flat Isp curves can use a `[KED_ASI_OVERRIDE]` config value.

---

## Part 3 — Archetype System (Engine Categorization)

The old UPI-based class tiers are removed. Engine type is determined by the **priority-ordered archetype logic** to provide precise archetypes based on fuel chemistry, propulsion technology, and performance metrics.

> [!IMPORTANT]
> The classification logic now includes a specific "Nuclear" check that scans the part for radioactive material (`EnrichedUranium`) to distinguish NTRs from high-performance chemical engines.

> [!NOTE]
> The "Electric" category now includes a wide range of noble gases (Argon, Xenon, Krypton, Neon) and Lithium to support *Near Future Propulsion*.

### 3.1 EngineArchetype Enum

```csharp
public enum EngineArchetype { 
    Monopropellant, 
    Hypergolic, 
    Bipropellant, 
    Nuclear, 
    Electric, 
    Airbreathing, 
    Exotic,     // ISP > 3000
    Advanced,   // ISP > 500
    Solid, 
    Thermodynamic 
}
```

### 3.2 DetermineArchetype Logic (Priority Order)

1.  **Nuclear**: 
    - Part contains `EnrichedUranium` resource.
2.  **Exotic**: 
    - Vacuum ISP > 2850 OR uses `Antimatter`, `Gravioli`, or `WarpDrive` related resources.
3.  **Advanced**: 
    - Vacuum ISP > 500 (Catches high-performance Hydrolox/Methalox before they hit the Bipropellant trap).
4.  **Electric**: 
    - `EngineType == EngineType.Electric` OR
    - Uses `ElectricCharge` AND one of: `XenonGas`, `ArgonGas`, `LqdArgon`, `KryptonGas`, `LqdKrypton`, `NeonGas`, `LqdNeon`, `Lithium`.
5.  **Airbreathing**:
    - Uses `IntakeAir` or `IntakeAtm` as a propellant.
6.  **Hypergolic**: 
    - Uses 2+ propellants.
    - At least one propellant is in the Hypergolic list: `Aerozine50`, `NTO`, `MMH`, `UDMH`, `NitricAcid`, `Hydrazine`.
7.  **Monopropellant**: 
    - Uses exactly 1 propellant.
    - Propellant is `MonoPropellant` or `Hydrazine` (if used solo).
8.  **Bipropellant**: 
    - Uses `LqdOxygen` or `Oxidizer`.
9.  **Solid**: 
    - `EngineType == EngineType.SolidBooster` OR uses `SolidFuel`.
10. **Thermodynamic**: 
    - Catch-all fallback for standard liquid engines.


### 3.3 Unified Failure Palette

All mechanical failures are consolidated into five distinct states. These replace the previous complex failure logics:

| Failure Mode | Description | Archetype Application |
| :--- | :--- | :--- |
| **Ignition Fail** | Engine fails to start; prevents further restart attempts until repaired. | Biprop, Thermo, Advanced, Nuclear, Electric, Exotic. |
| **Flameout** | Sudden engine shutdown during burn; prevents restart. | Biprop, Thermo, Advanced, Airbreathing. (Nuclear, Electric, Exotic are immune). |
| **Gimbal Lock** | Vectoring actuators seize in current position. | Any engine with a gimbal. |
| **Thrust Drop** | Performance capped at 60%; applies "Performance Scar". | Hypergolic, Monoprop. (Nuclear, Electric, Exotic are immune). |
| **Explode** | Casing breach leading to part destruction. | **Solid (SRB)**; Safe Abort maturity allows engine death without explosion. |

### 3.4 Failure Cascades
Failures have an **8% chance** to trigger a chain reaction in symmetry counterparts, representing shared vibration or fuel manifold issues.


### 3.4 Verification Plan (Archetype Accuracy)

- **Stock Part Tests**:
    - `J-X4 "Whiplash"`: Should classify as **Airbreathing**.
    - `LV-N "Nerv"`: Should classify as **Nuclear**.
    - `IX-6315 "Dawn"`: Should classify as **Electric**.
    - `O-10 "Puff"`: Should classify as **Monopropellant**.
- **Modded Part Tests**:
    - Verify that atmospheric engines from mods like *Near Future Aeronautics* are correctly identified.
    - Verify noble gas detection for *Near Future Propulsion*.


---

## Part 4 — Operational Maturity System (OMS)

This overhaul replaces the dense, 6-tier CTU spreadsheets with a streamlined maturity model. In this system, "Maturity" is the only metric that matters, and different archetypes have vastly different "learning curves" to reflect their technical complexity.

### 4.1 The New Currency: Maturity Points (MP)
Instead of tracking thousands of points, we use a low-digit MP system.
*   **Engine Start:** $+1$ MP (Once per flight; only if vessel is `FLYING` or altitude $> 100$m).
*   **Full Burn:** $+2$ MP (Burn exceeds 60 cumulative seconds).
*   **Inspection/Recovery:** $+5$ MP (Earned if an engineer performs a Diagnostic or if the part is recovered from `SubOrbital`+).
*   **The Hard Lesson:** $+10$ MP (Earned if you successfully perform an EVA repair on a failed engine).
*   **Archetype Heritage:** Newly unlocked parts receive **20% of the MP** from the most experienced part in the same archetype.
*   **Identity & Reputation (Immersion):** Engines gain branding as they mature.
    - **Branding:** 0 MP = "Prototype", L1 = "Block I", L2 = "Block II", L3+ = "Heritage Line".
    - **Historical Stats:** PAW shows: *Flights: X | Success: Y%*


### 4.2 The Roadmaps

#### 1. EXOTIC (The Prototype)
**Complexity:** Very Low (1 Level).
**Goal:** High-tech reliability that is nearly perfect out of the box.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Prototype** | 0 | **Total Lockout:** Any failure is permanent and cannot be repaired via EVA. Dead weight on failure. |
| **1** | **Heritage** | 30 | **Modular Design:** Anomalies are now standard failures that can be cleared with an EVA Repair Kit. |



---

#### 2. ELECTRIC & NUCLEAR (The Long-Haul)
**Complexity:** Low (2 Levels).
**Goal:** Deep-space endurance and operator awareness.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Experimental** | 0 | **Volatile Core:** 3.0% Lemon Anchor. Failures result in immediate, hard flameouts. (Nuclear requires Lvl 3 Engineer). |
| **1** | **Flight Rated** | 50 | **Surge Protection:** 1.5% Lemon Anchor. Failures are "Soft" (Thrust Drop) instead of complete flameouts. |
| **2** | **Heritage** | 120 | **Indestructible:** Gimbal lock immunity; Lemon Anchor drops to 0.2%. |



---

#### 3. SOLID (The Fuse)
**Complexity:** Low (2 Levels).
**Goal:** Eliminating catastrophic "Mission Kill" events.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Batch Tested** | 0 | **Casing Breach:** Lemon unit guaranteed to explode at 50% fuel (Safe for first 10s of burn). |
| **1** | **Reinforced** | 40 | **Pressure Buffered:** Breach probability reduced by 50%. |
| **2** | **Safe Abort** | 80 | **Containment:** Breaches are now "non-collateral"—the engine dies, but does not destroy adjacent parts. |

---

#### 4. MONOPROPELLANT & HYPERGOLIC (The Tactical)
**Complexity:** Medium (3 Levels).
**Goal:** Managing corrosion and catalyst health.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Baseline** | 0 | **Hard Lockout:** Failure results in total shutdown. |
| **1** | **Stabilized** | 60 | **Maintenance Access:** Enables `CatalystSwap` and `NitrogenPurge` EVA actions. |
| **2** | **Proven** | 150 | **Valve Seep:** Failures become "Soft" (Thrust Drop) instead of flameouts. |
| **3** | **Heritage** | 300 | **Extended Life:** Catalyst limits doubled; first failure is restorable. |

**Tactical EVA Actions:**
- **Catalyst Swap (Monoprop):** Costs 1 Kit + 5 EC. Resets cumulative burn time (Catalyst life) and removes 1 Performance Scar.
- **Nitrogen Purge (Hypergolic):** Costs 1 Kit. Halves current Ignition Fatigue and removes 1 Performance Scar.
- **Cycle Valves (Hypergolic):** (Lvl 1+ Engineer) 50% chance to reveal Weak Unit; 5% risk of accidental ignition pulse or gimbal lock.

---

#### 5. AIRBREATHING (The Turbine)
**Complexity:** Medium (3 Levels).
**Goal:** Expanding the "Safety Envelope" in the atmosphere.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Prototype** | 0 | **Fragile Envelope:** Weak Unit has a 20% narrower operating altitude/velocity. |
| **1** | **Bench Tested** | 80 | **Envelope Buffer:** Weak Unit penalty reduced to 10%. |
| **2** | **Cleared** | 200 | **Flameout Recovery:** Auto-relight time halved if engine returns to envelope. |
| **3** | **Heritage** | 450 | **Masterwork:** Envelope penalty eliminated; Lemon Anchor drops to 1.0%. |

---

#### 6. BIPROPELLANT, THERMODYNAMIC, & ADVANCED (The Vanguard)
**Complexity:** High (4 Levels).
**Goal:** Mastering the chaos of high-pressure liquid combustion.

| Level | Name | MP Req | Reliability Impact |
| :--- | :--- | :--- | :--- |
| **0** | **Experimental** | 0 | **Full Risk:** High Ignition Fatigue; Lemon Anchor at 5.0%. |
| **1** | **Qualified** | 100 | **QC Certified:** Ignition fatigue reduced; Anchor drops to 4.0%. |
| **2** | **Proven** | 250 | **Flight Standard:** First 8s of burn protected from Weak Unit triggers. |
| **3** | **Heritage** | 500 | **Fail-Safe:** ASI (Atmospheric) penalties halved; Gimbal lock immunity. |
| **4** | **Masterwork** | 1000 | **Golden Batch:** Anchor 0.5%; Ignition failures hard-capped at 1%. |

---

### 4.3 Implementation Note
To keep this elegant and persistent across launches, maturity is tracked globally. A custom `ScenarioModule` (e.g., `KEDScenario`) saves a persistent dictionary: `Dictionary<string, float> GlobalEngineMaturity`. The `PartModule` on the engine acts as a reader of this global node at launch to set its `MaturityLevelAtLaunch`.


---

### 4.4 Advanced Engineering Actions (EVA Only)

#### 1. Hardware Retrofit
*   **Mechanic**: Brings an old part instance up to current Global Maturity standards.
*   **Cost**: Scales by archetype (Default 5 kits, Hypergolic 7, Nuclear/Exotic 10-15).
*   **Requirement**: Level 2+ Engineer.

#### 2. Deep Diagnostics
*   **Mechanic**: Identifies Weak Unit status (Lemon) before failure.
*   **Requirement**: Level 2+ Engineer (Level 3+ for Nuclear).
*   **Preventative Maintenance**: Once revealed, the Lemon flag can be cleared for the cost of a standard maintenance action (Default 2 kits), preventing the failure.

#### 3. Partial Repair Logic
*   **Mechanic**: Engineers do not need to finish a repair in one go. Kits are "invested" into the part.
*   **PAW Feedback**: `Repair: [Component] (X/Y kits invested)`.
*   **Multi-Session**: Repairs can be resumed by different engineers or after a reload.

---

## Part 5 — System Integrations

### 5.1 Background & Persistent Thrust
KED 3.0 is fully integrated with **BackgroundThrust** and **PersistentThrust** for unloaded vessel support.

*   **Offline Catch-up**: When a vessel is loaded, KED calculates the time elapsed since the last update.
*   **Wear Processing**: If the engine was burning in the background, KED applies cumulative burn wear and catalyst decay.
*   **Deterministic Failure**: If a Weak Unit's trigger time was passed during the background burn, the engine is set to the failure state upon loading, with a notification explaining the event.
*   **Maturity Gains**: MP for "Full Burn" can be earned during background operations.

---

## Part 6 — Unified Failure Palette (Detailed Breakdown)

| Failure Mode | Screen Message Example | KSP Text Color | PAW State Display | Trigger Mechanism | Repair / Recovery |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Ignition Fail** | `ALARM: <Name> (Ignition Failure)!` | **Red** (#FF3333) | `FAULT (Injector Lockout)` | Weak Unit at activation. | EVA: Clear Injectors |
| **Flameout** | `ALARM: <Name> (Sudden Flameout)!` | **Red** (#FF3333) | `FAULT (Flameout)` | Weak Unit in window. | EVA: Systems Overhaul |
| **Explode** | `CRITICAL: <Name> (Casing Breach)!` | **Dark Red** (#CC0000) | `State: DESTROYED` | 50% fuel peak (SRB). | **None** |
| **Thrust Drop** | `WARNING: <Name> (Thrust Capped)!` | **Orange** (#FFA500) | `DEGRADED (Valve Seep)` | Stress / Lvl 1 Surge Prot. | EVA: Valve Flush |
| **Gimbal Lock** | `WARNING: <Name> (Gimbal Seized)!` | **Yellow** (#FFCC00) | `DEGRADED (Actuators)` | Weak Unit / Random. | EVA: Actuator Reset |

### 5.1 Failure Cascades (Rare Chain Reactions)
Failures are usually isolated, but there is a **5-10% chance** of a cascade event:
- **Gimbal Lock → Control Instability:** The seized gimbal causes severe torque, leading to vibration that triggers a **Flameout** in a neighboring engine.
- **Explode → Structural Damage:** In a dense cluster, an SRB explosion may cause a **Thrust Drop** in adjacent liquid engines due to fuel line disruption.
- **Flameout → Pressure Spike:** A sudden shutdown causes a water-hammer effect, leading to an **Ignition Fail** on the next attempt for that engine.
*These create "story moments" rather than just mechanical hurdles.*

---

## Part 6 — UI & Notification Protocol

To maintain a native feel, KED 3.0 uses standard KSP UI patterns for all failure feedback.

### 6.1 Screen Message Protocol
When a failure triggers, a `ScreenMessage` is displayed at the top-center for 7 seconds.

*   **Message Dispatcher**: To prevent overlapping messages from overwriting each other (e.g., multiple failures during staging), KED uses a message queue that displays notifications sequentially or uses distinct vertical screen slots if multiple triggers occur within a 1-second window.
*   **Format**: `<Color Code> ALARM: <Engine Name> (<Failure Mode>)! </Color>`
*   **Color Logic**: Red for hard failures, Orange/Yellow for degraded states, Dark Red for explosions.

### 6.2 PAW Integration
The Part Action Window field `State: <Status>` sits directly below "Specific Impulse".

*   **Dynamic Notes**: If the engine can be fixed, the state automatically appends `- EVA Repair Req.`
*   **Color-Coded Status**: The PAW text matches the severity (e.g., Red for `FAULT`, Yellow for `DEGRADED`).



---

## Part 7 — Implementation Phases

### Phase 1 — Data Layer [COMPLETE]
- `KEDScenario` implemented with global dictionaries.
- `BatchQuality` and `isLemon`/`isWeakUnit` fields active.
- `atmSensitivityIndex` automated detection active.

### Phase 2 — Batch Roll Logic [COMPLETE]
- `BatchManager` rolls per part name at launch.
- `LineageRisk` and `AgingFactor` influences active.
- Controlled Chaos (80/15/5) implemented.

### Phase 3 — Failure Logic Rewire [COMPLETE]
- Deterministic `failureTriggerTime` (Liquid) and `fuelThreshold` (SRB).
- Multi-mode `activeFailuresMask` active.
- Failure Cascades (8% chance) active.

### Phase 4 — Operational Maturity Tracks [COMPLETE]
- 6 Roadmap objects implemented.
- Nuclear Lvl 3+ gates active.
- Hardware Retrofit and Deep Diagnostics active.

### Phase 5 — UI & UX [COMPLETE]
- Screen Message Dispatcher (Queued) active.
- PAW updated with ASI, History, and State.
- Branding (Block I/II/Heritage) active.

### Phase 6 — Config & Compatibility [COMPLETE]
- `KED_Settings.cfg` with full repair matrix.
- `[KED_ASI_OVERRIDE]` support.

### Phase 7 — Background Integration [COMPLETE]
- UT-based catch-up logic for Background/Persistent Thrust.
- Offline failure and wear processing.
