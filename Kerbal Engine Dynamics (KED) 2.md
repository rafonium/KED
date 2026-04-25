# **System Architecture: Hardware Identity, Versioning, And Failure Logics**

This section defines the core technical architecture of **Kerbal Engine Dynamics (KED) 2.0**. It establishes how the mod identifies engine prestige, manages real-time performance data, and preserves the historical integrity of every vessel in the fleet.

---

## **1\. The Mathematical Implementation**

**Goal:** To establish an objective, performance-based standard for engine "prestige" that scales with financial density, ensuring high-tech hardware is distinguished from mass-produced parts.

### **The Pedigree Index (PI) Formula**

The calculation uses a log-log relationship to isolate the financial density of a part relative to its performance envelope.

$$PI \= \\frac{\\log\_{10}(Cost)}{\\log\_{10}(Thrust\_{Vac}^{0.8} \\times \\log\_{10}(I\_{sp\\\_VAC}))}$$

### **Safety Clamps & Integrity**

To ensure compatibility with diverse mod-lists (e.g., *Realism Overhaul*), the implementation uses hard-coded floors to prevent calculation errors:

* **Cost Floor:** Mathf.Max(part.cost, 10f).  
* **Thrust Floor:** Mathf.Max(engine.maxThrust, 0.1f).  
* **Isp Floor:** Mathf.Max(engine.atmosphereCurve.Evaluate(0f), 2f).

---

## **2\. The Dynamic Pedigree Index (Lightweight)**

**Goal:** To maintain real-time compatibility with dynamic part modifications (like *TweakScale*) without impacting frame rates.

### **Trigger-Based Lifecycle**

Calculation is restricted to specific events to minimize CPU overhead:

* **Editor Event:** Recalculates during OnAwake or when part configurations (cost/performance) are changed in the VAB/SPH.  
* **Flight Initialization:** Calculated once during OnStart as the physics engine loads the vessel.

### **Refined Tier Hierarchy (Industrial Baseline)**

The "Junk" tier has been removed to ensure all hardware feels like professional aerospace equipment, with **Industrial** serving as the entry-level track.

| Class | PI Range | CTU Target | Discovery Rate | Yield (EVA) |
| :---- | :---- | :---- | :---- | :---- |
| **Exotic** | $\> 2.0$ | 50 | 20% | \+10 CTU |
| **Advanced** | $1.5 \- 2.0$ | 400 | 10% | \+40 CTU |
| **Aerospace** | $1.3 \- 1.5$ | 1,000 | 5% | \+50 CTU |
| **Industrial** | $\< 1.3$ | 5,000 | 2% | \+100 CTU |

---

## **3\. The "Snapshot" Persistence Mechanic**

**Goal:** To preserve the specific engineering state of hardware at its moment of launch, creating a meaningful distinction between "Legacy" and "Modern" agency standards.

### **Core Concepts**

* **Legacy Lockdown:** Every engine is permanently assigned a blockLevelAtLaunch integer. It will always use the failure rates and performance bonuses (e.g., **Block V \+5% Thrust**) that were active when it was first built.  
* **The Serial Number (S/N) System:** Each engine is stamped with a unique identifier (e.g., S/N: 2026-AE-001) that tracks its birth year and pedigree, making each part a unique piece of agency history.  
* **In-Flight Retrofit:** An Engineer on EVA can spend a **Repair Kit** to perform a "Hardware Retrofit," updating a legacy engine to match the current **Agency Standard** block level.

---

## **4\. Technical Logic for the Snapshot**

**Goal:** To ensure global progress does not "teleport" into active vessels, requiring active intervention for older hardware to benefit from new breakthroughs.

### **Three-Step Execution Logic**

1. **State Initialization:** On OnStart, the module checks for an existing blockLevelAtLaunch. If null, it generates a new Serial Number and pulls the current global mastery level from the EngineMasteryTracker.  
2. **Physics Enforcement:** The module applies physics modifiers (Thrust bonuses or Gimbal status) based **strictly** on the stored snapshot level.  
3. **Risk Mitigation:** Reliability checks use the risk multipliers associated with the snapshot. A **Block X-0** engine remains $2.0\\times$ more likely to fail than its modernized counterparts, even if the player has unlocked higher blocks globally.

---

\[\!TIP\]

This "Core Technical" foundation ensures that as your agency grows, your older space stations and satellites become tangible "museum pieces" of your early engineering history until a maintenance crew arrives to upgrade them.

## **5\. Failure Logics**

**Goal:** To define a high-performance, event-driven probability system that governs malfunctions based on engine history, physical stress, and agency mastery.

KED 2.0 utilizes a **Deterministic Stochastic Trigger** system. Instead of checking for failures every frame, the logic only "rolls the dice" during specific transition events or at set intervals to minimize CPU overhead.

---

### **A. Core Probability Modifiers**

The final failure chance for any logic check is a product of the engine's baseline pedigree and its historical "Snapshot" state.

* **Pedigree Baseline**: Exotic hardware has a lower baseline failure floor compared to Industrial hardware.  
* **Block Multipliers**: The blockLevelAtLaunch integer applies a global risk factor:  
  * **Block X-0**: 2.0x Risk Multiplier.  
  * **Block I**: 1.0x (Normalized Baseline).  
  * **Block II**: \-30% Running Risk (Flameout risk \-30%)  
  * **Block III**: 2.0% Maximum Hard-Cap on Ignition Failure probability.  
  * **Block VI**: 0% Ignition Failure probability (Immunity).

---

### **B. Archetype Logic Profiles**

Each propulsion type uses a unique set of triggers to simulate its specific physical failure points.

#### **1\. Thermodynamic Logic (Liquid/Nuclear/Fusion)**

* **Ignition Check (Single Event)**: A failure roll occurs exactly once when the engine transitions from 0% to \>0% throttle.  
  * **Penalty**: Accumulates \-2% Ignition Fatigue per start.  
  * **Mitigation**: Halved to \-1% if the engine is "Hot" (restarted within 30 seconds of shutdown).  
* **Running Check (Interval-Based)**: A reliability roll triggers every **10–15 seconds** of continuous burn.  
  * **Modifiers**: Probability is weighted by current TWR stress and cumulative fatigue.

#### **2\. Monopropellant Logic (Dedicated Engines)**

* **The Dormant Phase**: Zero failure checks are performed as long as CumulativeBurnSeconds \< ServiceLimit.  
* **The Decay Phase**: Once the limit is exceeded, a "Catalyst Choking" check triggers every **5 seconds** of active burn.  
* **Pulse Wear**: Every ignition event instantly deducts **0.5s to 0.2s** (depending on Block Level) from the Service Limit, simulating the shock of cold starts on the catalyst bed.

#### **3\. Solid Rocket Motor Logic (SRB)**

* **Grace Period**: 0% failure risk during the first **10–25 seconds** (depending on Block Level) to ensure safe pad departure.  
* **Fixed-Point Checks**: Logic checks occur only at five specific fuel-percentage markers: **20%, 40%, 50%, 60%, and 80%**.  
* **The Pressure Peak**: The highest probability roll is reserved for the **50% fuel marker**, simulating maximum internal casing pressure.

#### **4\. Hypergolic Logic (Chemical Corrosion)**

* **Ignition Immunity**: 0% failure risk during ignition (Contact Ignition).
* **The Corrosion Phase**: Unlike pulse-based wear, Hypergolic engines suffer from **Chemical Corrosion** during sustained burns.
* **Hard Lockout**: Failures represent corrosive leaks or hard valve seizures, resulting in a **Hard Lockout** requiring EVA decontamination.

---

### **C. Failure Mode Outcomes**

If a logic check fails, the system executes one of the following mechanical states based on the roll result:

| Failure Mode | Archetype | Resulting State |
| :---- | :---- | :---- |
| **Ignition Failure** | Thermodynamic | Starter fails; engine enters a **Hard Lockout** requiring EVA repair. |
| **Pump Seizure** | Thermodynamic | Mid-burn flameout; engine enters a **Hard Lockout** requiring EVA repair. |
| **Gimbal Lock** | Thermodynamic | Actuators freeze; engine provides thrust but zero vectoring (Immune at Block IV). |
| **Catalyst Choke** | Monoprop | Non-destructive flameout; frequency increases exponentially until a **Catalyst Flush**. |
| **Valve Lockout** | Hypergolic | Corrosive leak or hard valve failure; results in a **Hard Lockout**. |
| **Casing Breach** | SRB | Visual explosion; thrust zeroed; part removed via non-collateral cleanup. |

---

### **D. Optimization Strategy**

To ensure the mod is lightweight, KED 2.0 employs **Delta-Time Accumulators**. The system sums up the time elapsed since the last check; the failure logic function only wakes up and executes when the sum reaches the required 5s, 10s, or 15s threshold.

Does the 5-point check system for the SRBs provide enough "tension" during the burn, or would you prefer a more frequent check as they approach that 50% pressure peak?

---

# **Propulsion Archetypes**

This deep dive focuses on the **Propulsion Archetypes** of KED 2.0. Rather than treating all engines the same, this architecture identifies hardware by its fuel lines to simulate the unique physical stresses of solid, monopropellant, and thermodynamic combustion.

---

## **1\. Solid Rocket Motors (SRB) — "The Fuse"**

**Goal:** To simulate the high-pressure, "point of no return" nature of solid fuel while ensuring early-launch stability.

* **The 10-Second Grace Period:** To prevent frustrating pad-explosions that ruin a mission before it begins, all SRBs have a $0\\%$ risk window for the first 10 seconds of burn time.  
* **The Internal Pressure Bell Curve:** Unlike liquid engines, SRB risk is not constant. The failure probability follows a **Bell Curve** that peaks exactly when **50% of the propellant** has been consumed, representing the moment of maximum internal casing stress.  
* **Non-Collateral Casing Breach:** A failure does not necessarily mean the end of the whole rocket.  
  * **The Event:** An audio and visual explosion effect is triggered, thrust is instantly set to zero, and the part is removed from the physics simulation.  
  * **Safety:** This breach is "non-collateral," meaning it will not trigger a chain reaction that destroys parent or child parts, allowing for "emergency abort" scenarios.

---

## **2\. Monopropellant — "The Workhorse"**

**Goal:** To provide high-reliability, long-term service for orbital maneuvering units, introducing a "maintenance debt" system that rewards proactive field servicing.

\[\!IMPORTANT\]

This archetype applies exclusively to **dedicated propulsion modules** (ModuleEngines) that utilize monopropellant as their primary fuel source. It does not govern standard ModuleRCS thrusters.

### **A. The Service Limit Logic**

Unlike thermodynamic engines that suffer from thermal shock per ignition, Monopropellant engines are limited by the degradation of their catalyst beds over time.

* **Cumulative Burn Tracking:** The module tracks the total number of seconds the engine has been active across the vessel's entire lifespan.  
* **The "Pulse" Wear Factor:** To simulate catalyst shock, every ignition event (transition from 0% to \>0% throttle) instantly consumes **0.5 seconds** of the Service Limit, even if the burn is shorter.  
* **Catalyst Expected Burn Time:** 600s (base)

### **B. Failure Profile: Exponential Decay**

* **The Safe Zone:** Reliability remains at $99.9\\%$ until the cumulative burn time exceeds the engine's specific Service Limit.  
* **The Decay Phase:** Once the limit is passed, the engine enters a state of **Exponential Decay**. The frequency of flameouts increases significantly for every additional second of burn time.  
* **Failure Mode (Catalyst Choking):** Failures result in sudden engine flameouts. Unlike SRBs, these are non-destructive but can occur at critical moments during docking or landing.

### **C. Maintenance & Endurance**

* **The Catalyst Flush:** An Engineer on EVA can perform a "Catalyst Flush" to reset the cumulative burn clock to zero. This action consumes **Repair Kit**.  
* **Block II (Field Certified) Bonus:** Engines that have reached **Block II** receive a permanent **\+50% extension** to their base Service Limit, representing improved catalyst bed durability.

---

## **3\. Thermodynamic — "The Sophisticates"**

Here is the expanded and highly detailed specification for the Thermodynamic propulsion archetype. This incorporates the "Thermal Soak" mechanic and defines the exact physical consequences of a failure, alongside the newly balanced, inventory-friendly Repair Kit requirements.

**Goal:** To simulate the complexity and thermal shock of high-performance Liquid, Nuclear, and Fusion propulsion systems.

### **A. The "No Safety" Reality**

Unlike Solid Rocket Boosters, Thermodynamic engines do not have a "grace period" during liftoff. Every single ignition—whether on the launchpad or deep in space—is a trial of the hardware's integrity. Risk is calculated dynamically based on the engine's TWR stress, Pedigree Index, and its current Certification Block.

### **B. Ignition Fatigue & "Thermal Soak"**

To discourage players from rapidly "flickering" the throttle for minor trajectory corrections, Thermodynamic engines track thermal and mechanical stress.

* **Ignition Fatigue:** Every time the engine transitions from an unlit state to burning, it accrues a cumulative **\-2% Ignition Penalty** to its overall reliability. This represents the microscopic fractures and violent thermal shock caused by extreme heating and cooling cycles.  
* **The "Thermal Soak" Factor:** If an engine is restarted while it is still "Hot" (defined as being within **30 seconds** of its last shutdown), the Ignition Fatigue penalty is halved to **\-1%**. This realistically simulates that restarting an already-warm engine is less stressful on the combustion chamber than a "cold start," rewarding players for executing rapid, sequential corrective burns.

### **C. Failure Mode: Turbopump Seizure**

Because Thermodynamic engines are highly complex networks of plumbing and pumps, a failure does not usually result in an immediate explosion, but rather a complete mechanical lockup.

* **The Event:** If an ignition check fails, the engine suffers a Turbopump Seizure. Thrust is instantly cut, and the engine flames out.  
* **The Consequence:** The engine enters a "Hard Lockout" state (engine.allowRestart \= false). It cannot be fired again, and staging will bypass it. It remains dead weight until a Kerbal on EVA intervenes.

### **D. Maintenance: Systems Overhaul (Variable Cost)**

A **Systems Overhaul** can be performed by an Engineer on EVA. This action un-seizes the turbopumps (if the engine has failed) and completely resets the Ignition Fatigue counter back to zero.

To reflect the difference in skill while respecting KSP's strict EVA inventory volume limits, the cost of an Overhaul scales inversely with the Engineer's level. All engineers can perform the task, but rookies are highly inefficient with their materials.

| Engineer Experience | Repair Kit Cost | Gameplay Implication |
| :---- | :---- | :---- |
| **Level 0 – 1 (Rookie)** | **4 Kits** | A logistical struggle. The Kerbal's inventory will be maxed out, and they may need to make multiple trips to a nearby cargo container to fetch enough parts. |
| **Level 2 – 3 (Veteran)** | **2 Kits** | Standard field maintenance. Efficient enough to perform the overhaul and still carry science data or other EVA tools. |
| **Level 4 – 5 (Master)** | **1 Kit** | Masterful efficiency. They know exactly which valve is stuck and only need a single kit to rebuild the pump system. |

---

## **4\. Hypergolic — "The Volatile"**

**Goal:** To provide near-perfect ignition reliability for critical maneuvers while introducing a high-stakes "Chemical Corrosion" mechanic.

### **A. Ignition Perfection**

Hypergolic engines utilize contact ignition, making them the most reliable starters in the agency’s arsenal.

* **0% Ignition Risk:** Every ignition event is guaranteed to succeed, regardless of block level or previous fatigue.

### **B. Chemical Corrosion (Sustained Burn Stress)**

While they excel at starting, Hypergolic engines "eat themselves" during operation.

* **Burn-Based Degradation:** Stress is calculated solely based on active burn time.
* **The Valve Lockout:** Failures result in a **Hard Lockout**. This represents a corrosive leak that has compromised the pressure plumbing or a valve that has seized shut due to propellant toxicity.

### **C. Maintenance: Line Flush / Decontamination**

Because of the toxicity of Aerozine50 and NTO, maintenance is a high-risk, resource-intensive task.

* **Decontamination:** Requires a full line flush and component replacement.
* **Logistical Debt:** Hypergolic engines have a higher base Repair Kit cost for rookie engineers compared to Monopropellant systems.

---

# **The Certification Pathway**

This section outlines the progression of engine reliability and performance through the **7-Block Certification Pathway**. Mastery is an agency-wide achievement measured in **Cumulative Test Units (CTU)**; as these thresholds are reached, every engine of that specific model receives permanent upgrades to its manufacturing and operational standards.

---

## **1\. The 7-Block Evolution**

This updated **7-Block Certification Pathway** ensures that every mastery tier provides tangible engineering improvements across all three propulsion archetypes. The progression focuses on stabilizing **SRB casing safety** and maximizing **monopropellant catalyst longevity** alongside standard thermodynamic reliability.

### **Block X-0 (Experimental)**

* **General:** $2.0\\times$ Base Risk penalty; $20\\%$ Pad Failure (Ignition) cap.  
* **SRB Safety:** Standard 10s Grace Period; standard volatility peak at 50% fuel.  
* **Monoprop Catalyst:** Standard Service Limit; standard 0.5s "Pulse Wear" penalty per ignition.  
* **Status:** High-risk testing phase; maximum telemetry yield.

### **Block I (Flight Qualified)**

* **General:** $2.0\\times$ risk penalty is removed; reliability is normalized to factory baseline.  
* **SRB Safety:** **"Thermal Coating"** — The 10s Grace Period is extended to **15 seconds**.  
* **Monoprop Catalyst:** **"Stabilized Bed"** — Pulse Wear penalty is reduced from 0.5s to **0.4 seconds**.  
* **Status:** Validated for standard unmanned missions.

### **Block II (Field Certified)**

* **General:** Running Phase risk (all types) is reduced by **30%**.  
* **SRB Safety:** **"Pressure Buffering"** — The peak risk probability of the 50% Bell Curve is reduced by **25%**.  
* **Monoprop Catalyst:** **"Endurance Grade"** — Base Service Limit (cumulative burn seconds) is extended by **50%**.  
* **Status:** Proven for long-duration orbital and interplanetary maneuvers.

### **Block III (Human-Rated)**

* **General:** Ignition Failure probability is hard-capped at a maximum of **2.0%**.  
* **SRB Safety:** **"Safe Abort Logic"** — Casing breaches are now guaranteed to be **non-collateral**, ensuring zero damage to the rest of the stack.  
* **Monoprop Catalyst:** **"Precision Injection"** — Pulse Wear penalty is further reduced to **0.2 seconds**.  
* **Status:** Certified for crewed launches and high-stakes docking.

### **Block IV (Heritage)**

* **General:** Actuator maturity grants total **Immunity to Gimbal Lock**.  
* **SRB Safety:** **"Reinforced Casing"** — The Grace Period is extended to **25 seconds**, covering almost the entire high-pressure launch phase.  
* **Monoprop Catalyst:** **"Pure Catalyst"** — The Exponential Decay rate (after exceeding the limit) is **halved**, allowing for more "emergency" use.  
* **Status:** Mechanical systems are considered statistically flawless.

### **Block V (Up-Rated)**

* **General:** Optimized fuel injectors and chamber overclocking provide a permanent **\+5% Thrust Bonus**.  
* **SRB Safety:** **"Vector Precision"** — If the SRB has a gimbal, its response speed is increased by 20%.  
* **Monoprop Catalyst:** **"High-Flow Valves"** — The \+5% Thrust Bonus is applied while maintaining baseline Service Limit stability.  
* **Status:** Pushing the absolute physical limits of the design.

### **Block VI (Masterwork)**

* **General:** **Total Ignition Immunity** (0% ignition failure rate).  
* **SRB Safety:** **"Casing Perfection"** — The Bell Curve failure chance is reduced by **95%**, making mid-flight breaches nearly impossible.  
* **Monoprop Catalyst:** **"Indestructible Catalyst"** — Total Service Limit is increased by **200%**; maintenance is rarely required.  
* **Status:** Perfected engineering; the pinnacle of the agency’s technical history.

---

### **Mastery Impact Summary Table**

| Block | Thermodynamic Focus | SRB Safety Focus | Monoprop Catalyst Focus | Hypergolic Focus |
| :---- | :---- | :---- | :---- | :---- |
| **X-0** | Baseline Prototype | Standard Volatility | Standard Catalyst | Standard Corrosion |
| **I** | Risk Normalization | \+5s Grace Period | \-20% Pulse Wear | Baseline Stability |
| **II** | \-30% Running Risk | \-25% Peak Risk | \+50% Service Limit | **Reduced Corrosion Rate** |
| **III** | 2% Ignition Cap | Guaranteed Safe Abort | \-60% Pulse Wear | Human-Rated Valves |
| **IV** | Gimbal Lock Immunity | \+15s Grace Period | **Cycle Immunity** | **Valve Immunity** |
| **V** | \+5% Thrust Bonus | Improved Response | \+5% Thrust Bonus | \+5% Thrust Bonus |
| **VI** | 0% Ignition Failure | 95% Breach Reduction | \+200% Service Limit | **Infinite Integrity** |

*

---

## **2\. Mastery Threshold Matrix (CTU)**

To maintain the distinction between "Exotic" and "Industrial" hardware, CTU requirements are distributed across the blocks as a percentage of the total track target.

| Block | Progress | Exotic (50) | Advanced (400) | Aerospace (1k) | Industrial (5k) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **X-0** | **0%** | 0 | 0 | 0 | 0 |
| **I** | **10%** | 5 | 40 | 100 | 500 |
| **II** | **25%** | 13 | 100 | 250 | 1,250 |
| **III** | **45%** | 23 | 180 | 450 | 2,250 |
| **IV** | **65%** | 33 | 260 | 650 | 3,250 |
| **V** | **85%** | 43 | 340 | 850 | 4,250 |
| **VI** | **100%** | 50 | 400 | 1,000 | 5,000 |

---

# **The Flight Test Engineer (EVA & Recovery)**

This section defines the active role of the **Flight Test Engineer (FTE)**. In KED 2.0, engineers are not just mechanics; they are field scientists responsible for harvesting telemetry and managing the "maintenance debt" of the fleet.

---

## **1\. Maintenance Task Matrix (Variable Costs)**

This table defines the resource requirements for the **Flight Test Engineer (FTE)** field maintenance program. In accordance with agency standards, all engineers are capable of performing every task regardless of rank, but resource consumption (Repair Kits) scales inversely with their specific field experience.

**Maximum Logistical Load:** 8 Repair Kits.

| Engine Type | Task / Failure Mode | Rookie (Lvl 0-1) | Veteran (Lvl 2-3) | Master (Lvl 4-5) |
| :---- | :---- | :---- | :---- | :---- |
| **Thermodynamic** | **Ignition Failure** (Clear Injectors) | 2 Kits | 1 Kit | 1 Kit |
| **Thermodynamic** | **Flameout / Seizure** (Systems Overhaul\*) | 4 Kits | 2 Kits | 1 Kit |
| **Thermodynamic** | **Gimbal Lock** (Actuator Reset) | 2 Kits | 1 Kit | 1 Kit |
| **Monopropellant** | **Catalyst Choking** (Catalyst Flush\*\*) | 3 Kits | 2 Kits | 1 Kit |
| **Hypergolic** | **Valve Lockout** (Line Flush/Decon) | 5 Kits | 3 Kits | 2 Kits |
| **SRB** | **Casing Breach** (Structural Patch\*\*\*) | 6 Kits | 4 Kits | 2 Kits |
| **All Engines** | **Hardware Retrofit** (Version Update) | **8 Kits** | 5 Kits | 3 Kits |

---

### **Task Impact Descriptions**

* **(\*) Systems Overhaul:** Beyond un-seizing the turbopumps, this action completely resets the engine's accumulated **Ignition Fatigue** counter to zero.  
* (**) Catalyst Flush:** This procedure cleans the catalyst bed and resets the engine's **Cumulative Burn Clock** (Service Limit) back to zero.  
* **(***) Structural Patch:*\* While most SRB breaches result in the part being discarded, this high-cost action represents the materials required to patch a breached casing if the part was successfully recovered or stayed attached.  
* **Hardware Retrofit:** Updates the engine’s blockLevelAtLaunch snapshot to match the current **Agency Standard** Block level. This is the most complex task, requiring a maximum of 8 Kits for a rookie due to the intensive recalibration of internal hardware.

### **Logistical Considerations**

* **Efficiency:** Master-level engineers (Lvl 4–5) are highly optimized, often requiring only a single kit for major repairs where a rookie would struggle with a heavy inventory load.  
* **The "University" Rule:** All Kerbal Engineers are equally intelligent; the cost difference represents the rookie's tendency to use more materials during "trial and error" in the field compared to the precise, single-kit adjustments of a veteran.  
* **Inventory Management:** Because Rookie costs for **Retrofits** (8 Kits) and **Overhauls** (4 Kits) are so high, they may need to operate near a cargo container or make multiple trips, as their standard EVA volume is limited.

---

## **2\. The Field Research Loop (Inspections)**

**Goal:** To reward players for "in-situ" data collection during missions, emphasizing the value of manned flight tests over automated probes.

### **Telemetry Harvesting (Free Actions)**

These actions do not consume Repair Kits but require a Kerbal Engineer to be on EVA within 3 meters of the part.

* **Diagnostic Inspection:** Available after **60s of burn time** or following an **engine failure**. It grants a lump-sum of CTU based on the engine's Pedigree Discovery Rate.  
* **Failure Analysis:** If an inspection is performed on a *failed* engine, the resulting CTU yield is **doubled (2x)**.  
* **Endurance Bonus:** Inspections on engines with a cumulative burn time exceeding **180s** receive a **1.5x yield multiplier**.

---

## **3\. Post-Flight Intelligence (Recovery)**

**Goal:** To incentivize the safe return of hardware to Kerbin, turning "trash" into organizational knowledge.

* **Provenance Check:** An engine only contributes to agency knowledge if it has "proven" itself with at least **15s of active burn time**.  
* **The Auto-Harvest:** Any engine recovered after a **60s burn** that was *not* inspected in flight will automatically yield its "Deep Inspection" CTU upon recovery.  
* **Scrap Bonus:** Recovering a failed part (e.g., a breached SRB casing) grants a **Global CTU bonus** to every engine within that same Pedigree track, simulating a fleet-wide safety bulletin.

---

# **UI And UX Design**

This section defines the interactive and visual framework for **Kerbal Engine Dynamics (KED) 2.0**. The design focuses on "Information on Demand"—keeping the workspace clean while providing deep, physics-driven data for players who engage with the engineering loop.

---

## **1\. Static Catalog Info (Editor Tooltip)**

**Goal:** To establish the engine’s "financial density" at a glance without cluttering the part selection menu.

* **Header:** \<color=\#00e6e6\>\<b\>KED FACTORY SPECIFICATION\</b\>\</color\>  
* **Design Pedigree:** Displays the Class (Industrial, Aerospace, Advanced, or Exotic) calculated via the Dynamic PI logic.  
* **Mastery Status:** **\[HIDDEN\]** (Mastery is an agency-wide secret until the part is actively workspace-tested).  
* **UX Note:** \> *“Note: Place this part in the workspace and Right-Click to view live Certification levels, S/N data, and Telemetry reports.”*

---

## **2\. Dynamic Part Action Window (PAW)**

**Goal:** To provide a "Live Telemetry" experience, grouping all KED variables under a single, organized header.

| UI Field | Availability | Description |
| :---- | :---- | :---- |
| **Serial Number** | VAB / Flight | Displays the unique S/N: \[Year\]-\[Pedigree\]-\[ID\]. |
| **Certification** | VAB / Flight | Displays the current block name (e.g., Block III \- Human Rated). |
| **Build Version** | Flight Only | Indicates if the part is "Legacy" or "Agency Standard". |
| **Data Harvest** | VAB / Flight | A visual progress bar showing CTU gains toward the next Block. |
| **Ignition Rel.** | VAB / Flight | Live % chance of successful start, factoring in fatigue and block caps. |
| **Thrust Rating** | VAB / Flight | Displays current output (e.g., 105% for Block V Up-Rated parts). |

---

## **3\. Archetype-Specific UI**

**Goal:** To provide visual feedback for the unique failure risks associated with different fuel architectures.

### **Solid Rocket Motors (SRB)**

* **Pressure Status:** A dynamic readout tracking internal casing stress.  
* **Visual States:** Transitions from **Stable** to **Nominal** to **PEAK RISK** as fuel levels approach the 50% mark.  
* **Logic:** Directly tied to the 50% fuel consumption Bell Curve.

### **Monopropellant Engines**

* **Catalyst Health:** A percentage display showing the integrity of the catalyst bed.  
* **Clean Window:** Visualizes the remaining **Service Limit** (seconds) before entering exponential decay.  
* **Logic:** Tracks the "maintenance debt" required for the next **Catalyst Flush**.

---

## **4\. EVA Interaction & Contextual UI**

**Goal:** To guide the Flight Test Engineer (FTE) through field procedures using conditional buttons.

### **The EVA Context Menu**

Visible only to Kerbal Engineers within 3 meters of the part:

* **\[Run Diagnostics\]:** (Free) Optional check that may reduce the Repair Kit cost of subsequent repairs.  
* **\[Perform Maintenance\]:** (Variable Kit Cost) Dynamically changes to **Catalyst Flush** or **Systems Overhaul** based on archetype.  
* **\[Hardware Retrofit\]:** (Variable Kit Cost) Visible only if the blockLevelAtLaunch is lower than the current Agency Standard.  
* **\[Diagnostic Inspection\]:** (Free) Visible after 60s of burn or a failure; triggers CTU telemetry rewards.

---

## **5\. Visual UX & Audio Feedback**

**Goal:** To ensure critical engineering data is felt by the player even when not looking at menus.

### **Adaptive Nomenclature (The Suffix)**

Engine names automatically update based on their specific build level to distinguish legacy vessels from modern launches:

* **Experimental Phase:** \[Part Name\] \- X  
* **Standard Certification:** \[Part Name\] \- \[Block\] (e.g., LV-909 \- BIII)  
* **Peak Performance:** \[Part Name\] \- MAX

### **Screen Messaging**

* **Telemetry (LOWER\_CENTER):** *"\[KED\] TELEMETRY: \+50 Test Units Recorded (Aerospace Class)"*.  
* **Failures (UPPER\_CENTER):** *"\[\!\] IGNITION FAILURE: S/N: 2026-ID-042 \- Check Catalyst Health"*.  
* **Explosions (UPPER\_CENTER):** *"\[FATAL\] CASING BREACH DETECTED \- ABORT IMMEDIATELY"*.

### **UX Note for the PAW (Part Action Window)**

When an engine suffers a Turbopump Seizure, the PAW should display a high-visibility red status: Status: PUMP SEIZURE (Overhaul Required). The **\[Perform Systems Overhaul\]** button should dynamically display the required kit cost based on the level of the Kerbal currently active on EVA (e.g., *"\[Perform Systems Overhaul: 2 Kits Required\]"*).

Does this layout solve the mechanical gap for liquid engines while keeping the inventory logistics realistic for your players?

---

## **Summary of Gameplay Goals**

### **1\. Strategic Depth in Hardware Selection**

The primary goal is to force players to make meaningful choices based on **Financial Density** rather than just performance stats.

* **Pedigree Balance:** Players must choose between **Industrial** hardware, which requires a massive "long grind" to master, and **Exotic** masterpieces that offer near-instant reliability at a much higher cost.  
* **Objective Standards:** The **Pedigree Index (PI)** ensures that high-tech hardware is mathematically distinguished from mass-produced parts, removing arbitrary tiering.

### **2\. Reliability Earned Through Heritage**

Reliability is no longer a static value; it is a hard-earned agency achievement.

* **Iterative Progression:** Through the **7-Block Certification Pathway**, players transform volatile **Block X-0** prototypes into perfected **Block VI Masterworks**.  
* **Universal Improvements:** Mastery provides tangible upgrades across all engine types, such as improved **SRB casing safety** and **monopropellant catalyst longevity**.

### **3\. Meaningful Maintenance & Engineer Value**

KED 2.0 elevates the **Flight Test Engineer (FTE)** from a simple mechanic to a critical mission scientist.

* **Maintenance Debt:** Players must proactively manage hardware degradation through **Catalyst Flushes** and **Systems Overhauls**.  
* **Experience Scaling:** Veteran engineers are incentivized through a **Variable Cost Matrix**, allowing them to perform complex repairs using significantly fewer **Repair Kits** than rookies.  
* **In-Situ Science:** The **Field Research Loop** rewards manned missions by allowing engineers to harvest telemetry directly from active or failed engines.

### **4\. Historical Persistence & Legacy**

The mod aims to give every vessel a unique identity and place in the agency’s history.

* **Legacy Lockdown:** The **Snapshot Mechanic** ensures that older satellites and stations remain "museum pieces" that utilize the engineering standards active at their time of launch.  
* **Operational Maturity:** Global breakthroughs do not "teleport" to active vessels; players must perform **In-Flight Retrofits** to bring older hardware up to modern standards.

### **5\. Archetype-Driven Realism**

By identifying hardware by its **fuel lines**, the mod simulates distinct physical stresses for different propulsion technologies.

* **Solid Volatility:** Simulating the high-pressure "point of no return" with the **SRB Bell Curve**.  
* **Catalyst Decay:** Introducing the **Pulse Wear** factor for Monopropellant engines to simulate the shock of rapid engine cycling.  
* **Thermal Shock:** Penalizing "engine flickering" in Thermodynamic engines through cumulative **Ignition Fatigue**.

---

**Summary Quote:** "KED 2.0 ensures that as your agency grows, your older space stations and satellites become tangible artifacts of your early engineering history until a maintenance crew arrives to upgrade them".
