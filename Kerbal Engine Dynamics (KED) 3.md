# KED 3.0 — Engine Overhaul: Updated Implementation Plan

## Overview

This plan replaces the existing per-engine stochastic model with a **Batch Quality System (BQS)**. Engine categories are determined strictly by the archetype logic in `EngineFilterLogic.md`. The UPI-based class tiers are replaced by the **Operational Maturity System (OMS)**, where "Maturity" is the only metric that matters for reliability progression. Each archetype now has its own identity, batch behavior, and maturity roadmap.

> *"An engine feels nearly invincible — until it doesn't."*

---

## Resolved Design Decisions

| Question | Decision |
|---|---|
| **Batch Reveal Timing** | **Permanently hidden.** The player never sees the batch quality roll. Pure risk. |
| **Cross-Engine Batch Grouping** | **Per part name only.** Different part names = different batches, always. |
| **Weak Unit After Repair** | **Restored but degraded.** Performance penalties apply (ISP/Thrust) and future fatigue increases. |
| **Nuclear Repair Access** | **Engineer Level 3+ required** to repair a failed nuclear engine. No new item type needed. |
| **ASI for Modded Engines** | Retain Q3 as an open implementation detail — add a `[KED_ASI_OVERRIDE]` config field for mod compatibility. |
| **Batch Lineage** | **Linked across launches.** Batches are not fully independent; "manufacturingSeed" creates production runs. |

---

## Part 1 — The Batch Quality System (BQS)

### 1.1 What Is a Batch?

When a vessel is launched, every engine of the **same part name** on that rocket shares one **Batch ID** — a manufacturing lot number stamped at build time.

- Batch ID generated once at launch, stored in vessel persistent data.
- Engines of **different part names** are always separate batches.
- **Batch Lineage (Anti-Exploit)**: Each engine part has a hidden `manufacturingSeed`. Batches across launches are **not fully independent**. If a part name gets a "Lemon" result, the probability of a Lemon in the next launch of that same part is slightly increased temporarily (simulating a "bad production run"). This prevents farming safe batches with single-engine test flights.
- **Diegetic Pre-Launch Hints**: While the roll is hidden, the Factory Specification or Launchpad PAW may show subtle, non-deterministic hints:
  - *"Factory QA Note: Slight variance detected in turbopump alignment."*
  - *"Batch vibration signature above nominal."*
  - These hints are ~60-70% correlated with a Lemon status, allowing players to develop a "gut feeling" without UI-spam.

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

*   **Controlled Chaos (Variance):** Instead of exactly one Weak Unit, the system uses a probability spread:
    - **80% Cases:** Exactly 1 Weak Unit (current standard).
    - **15% Cases:** 2 Weak Units (Rare "double fault" event).
    - **5% Cases:** No Weak Unit, but **Degraded Batch** (All engines in the batch suffer soft failures/thrust drops).
*   **Cumulative Timer:** The Weak Unit trigger window tracks **cumulative running time** across all ignitions.
*   **SRB Spread Window:** SRBs are guaranteed safe for the first 10s. The failure window is **40% – 70% fuel**, with peak probability at 50%. This prevents "safe cheese timing" by staging exactly at the 50% mark.
*   **Non-Explicit Sensory Cues:** Impending failures provide subtle sensory feedback instead of UI warnings:
    - Slight thrust jitter.
    - Micro gimbal twitch.
    - Subtle sound distortion (pitch shifting or static).
*   **Repair Recovery:** Successfully performing an EVA repair on a Weak Unit clears the "Lemon" flag but applies a **Hidden Penalty** (e.g., -1-2% ISP or increased future ignition fatigue). Recovery is not a "perfect reset."





### 1.5 Why Multi-Engine Tests Fail

Same-part-name engines on one rocket share a batch. Testing a 4-engine cluster gives you one batch result, not four. To gather meaningful data across batches, the player must fly **separate missions** with the same engine part.

### 1.6 Good Batch Degradation

Even **Good** batches still wear over time:

| Degradation | Trigger | Effect |
|---|---|---|
| Ignition Fatigue | Each start/stop cycle | Cumulative ignition penalty per restart |
| Catalyst Decay | Cumulative burn seconds (Monoprop) | Exponential decay after service limit |
| Thermal Cycling | Cold restarts (Thermodynamic/Nuclear) | Increased gimbal and running instability |
| **Aging Factor** | Heritage engines (X+ flights) | Legacy engines eventually gain "Aging Risk," slowly increasing base failure probability again. |

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
    - Vacuum ISP > 3000 OR uses `Antimatter`, `Gravioli`, or `WarpDrive` related resources.
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
| **Ignition Fail** | Engine fails to start upon activation. | Bipropellant, Thermodynamic, Advanced. |
| **Flameout** | Sudden engine shutdown during a sustained burn. | All Liquid/Electric/Airbreathing archetypes. |
| **Gimbal Lock** | Vectoring actuators seize; steering is lost. | Any engine equipped with a gimbal. |
| **Thrust Drop** | Performance is capped; Limiter locked to <100%. | Hypergolic, Monopropellant, Electric. |
| **Explode** | Structural casing breach leading to part destruction. | **Solid (SRB) only**; triggered at 50% fuel peak (Guaranteed safe for first 10s). |


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
*   **Engine Start:** $+1$ MP (Once per flight; only if vessel state is `FLYING` or altitude $> 100$m).
*   **Full Burn:** $+2$ MP (Burn exceeds 60 seconds).
    *   **Bonus +5 MP:** Earned if an engineer performs an inspection on an engine from that batch, or if that batch is recovered (Recovery bonus only triggers if vessel reached `SubOrbital` or greater).
*   **The Hard Lesson:** $+10$ MP (Earned if you successfully perform an EVA repair on a failed engine).
*   **Archetype Heritage:** Newly unlocked parts receive **20% of the MP** from the most experienced part in the same archetype.
*   **Identity & Reputation (Immersion):** Engines gain personality as they mature.
    - **Renaming:** At certain thresholds, engine lines are renamed internally (e.g., "LV-T45 Block II", "LV-T45 Heritage Line").
    - **Historical Stats:** The PAW/Tooltip shows historical performance:
      - *Flights: 23*
      - *Failures: 2*
      - *Success Rate: 91%*
    - Players develop emotional attachment: *"This engine family has never failed me... until it does."*
*   **Aging & Legacy Risk:** Even "Heritage" engines are not immune to time. After X total flights (e.g., 50+), an "Aging Factor" applies, slowly increasing the failure probability again. This encourages lifecycle management: "Retire this engine line or risk it?"


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
| **0** | **Baseline** | 0 | **Hard Lockout:** Failure results in total thrust loss. |
| **1** | **Stabilized** | 60 | **Corrosion Resistance:** Lemon unit corrosion/decay rate penalty halved. |
| **2** | **Proven** | 150 | **Valve Seep:** Failures become "Soft" (5% thrust loss instead of total lockout). |
| **3** | **Heritage** | 300 | **Infinite Life:** Catalyst service limits doubled; first failure is always restorable via EVA. |

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
*   **Context**: Engines launched years ago may have outdated specs.
*   **Mechanic**: A Level 2+ Engineer can perform a "Hardware Retrofit" to bring an engine up to modern standards.
*   **Requirement**:
    *   **Engineer Level 2+**.
    *   **5-10 EVA Repair Kits** (Cost scales: 5 for Solid/Monoprop, 7 for Biprop/Airbreathing, 10 for Nuclear/Electric/Exotic).
    *   **Inventory Logic**: Repair kits can be pulled directly from the vessel's cargo storage if the Engineer is within 5m of the engine, bypassing Kerbal inventory limits.
*   **Effect**: Updates the `MaturityLevelAtLaunch` of the specific part instance to the current **Global Maturity Level** of that archetype.

*   **Use Case**: Upgrading legacy hardware on long-term orbital stations or interplanetary craft.

#### 2. Deep Diagnostics
*   **Context**: Expansion of "The Hard Lesson".
*   **Mechanic**: Before attempting a repair, an Engineer can "Run Diagnostics".
*   **Pre-Failure Effect**:
    *   **Requirement**: Maturity Level 2+ for that specific engine archetype.
    *   **Insight**: Can reveal if the engine is a **Weak Unit** (hidden Lemon status) before it ever fails.
    *   **Preventative Maintenance**: Once a Weak Unit is identified, an Engineer can perform "Preventative Maintenance". This costs the same as a standard repair (EVA kits) but clears the Lemon flag immediately, preventing the catastrophic failure from occurring.
    *   **SRB Note**: For Solid engines, diagnostics can identify a Weak Unit, but since they are non-repairable, the only action is to abort or stage early (Preventative Maintenance is not available for Solids).
*   **Post-Failure Effect**:

    *   **Insight**: Identifies the exact failure mode (from the Unified Failure Palette).
    *   **Efficiency**: Reduces the EVA Repair Kit cost by **1** (minimum 1) for the subsequent repair.
    *   **Reward**: Still awards the "+10 MP (Hard Lesson)" upon successful repair.

---

## Part 5 — Unified Failure Palette (Detailed Breakdown)

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

### Phase 1 — Data Layer
- Implement `KEDScenario` (`ScenarioModule`) to store `Dictionary<string, float> GlobalEngineMaturity`.
- Add `BatchQuality` enum and `batchId` / `batchQuality` per-vessel fields.
- Add `weakUnitDesignated` bool per engine module (cleared on EVA repair).
- Add `atmSensitivityIndex` float, computed from Isp curve at part load.
- Replace `DetermineArchetype()` to use `EngineFilterLogic.md` priority chain with no UPI class tiers.

### Phase 2 — Batch Roll Logic
- Implement `BatchManager`: rolls quality per unique `(vesselId, partName)` pair at launch.
- **Batch Lineage:** Integrate `manufacturingSeed` to influence Lemon probability based on previous launches of that part.
- **Pre-Launch Hints:** Implement the logic for non-deterministic "QA Notes" in the Factory Specification/PAW.
- **Controlled Chaos:** Implement the 80/15/5 variance for Weak Unit designation and Degraded Batches.
- Store results in vessel persistent data.

### Phase 3 — Failure Logic Rewire
- Replace per-engine RNG with batch-gated failure windows per archetype.
- Implement `weakUnitFailureWindow` timer for each archetype (Cumulative).
- **SRB Spread Window:** Implement the 40-70% failure window for Solids.
- **Sensory Cues:** Implement thrust jitter, gimbal twitch, and audio distortion modules.
- **Failure Cascades:** Implement the logic for rare chain reactions between parts.
- Implement **Screen Message Dispatcher** with queuing logic and KSP color-coding.
- Implement PAW `State` field with dynamic "EVA Repair Req" strings.
- Implement ASI-modulated ignition/running checks with PAW feedback.


### Phase 4 — Operational Maturity Tracks
- Implement 6 separate `MaturityRoadmap` objects (one per group defined in Part 4).
- Wire batch roll Anchor and Complexity shifts to maturity level thresholds.
- Implement Nuclear repair gate: check Engineer experience level before allowing interaction.
- **Implement Hardware Retrofit**: Logic for updating `MaturityLevelAtLaunch` and repair kit consumption.
- **Implement Deep Diagnostics**: Pre-failure "Weak Unit" reveal and post-failure cost reduction logic.


### Phase 5 — UI & UX
- **Remove all "Silent Anomaly" early warnings** and hidden pre-lockout cues (replaced by sensory cues).
- Implement the **Screen Message Protocol** (7s duration, top-center).
- **Identity & Reputation:** Add historical stats (Flights/Success) to tooltips and PAW. Implement engine line renaming.
- **ASI Feedback:** Add real-time "Outside Optimal Band" PAW warnings and staging UI efficiency zones.
- Add ASI value and maturity stats to Factory Specification tooltips.
- Implement the `State: <Status>` field in the PAW for all engines.
- Electric/Airbreathing: Retain live margin displays (Thrust/Power) as standard telemetry.


### Phase 6 — Config & Compatibility
- Add `[KED_ASI_OVERRIDE]` config key for mod-list compatibility.
- Test archetype detection against: stock engines, Kerbal Atomics, Near Future Propulsion, Near Future Aeronautics, Far Future Technologies.

### Phase 7 — Verification
- Seed RNG batch rolls and verify probability table output per archetype.
- **Test Batch Lineage:** Verify that Lemon results influence future launch probabilities.
- **Test SRB Timing:** Confirm failures occur within the 40-70% window, not just at 50%.
- **Test Sensory Cues:** Verify jitter/twitch/audio effects trigger before failures.
- **Test Cascades:** Verify that rare chain reactions occur as intended.
- **Test Repair Penalties:** Confirm that repaired engines suffer ISP/fatigue penalties.
- Test Weak Unit designation, failure window timing, and repair-clearing behavior.
- Test **Preventative Maintenance** flow for pre-failure Weak Units.
- Test maturity MP accumulation (Launchpad Farming check) and level unlock triggers.
- Test Nuclear Lvl 3+ gate: confirm Lvl 0-2 engineers cannot repair.
- **Strict Unit Test**: Verify that "Sepratron" style engines (Solid fuel, small thrust) are correctly classified as **Solid** (#9) and not accidentally caught by **Monopropellant** (#7).
