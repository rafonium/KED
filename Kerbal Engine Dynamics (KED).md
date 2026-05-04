# KED — Engine Overhaul Design Document

## Overview

This plan replaces the existing per-engine stochastic model with a **Batch Quality System (BQS)**. Engine categories are determined strictly by the archetype logic. The UPI-based class tiers are replaced by the **Operational Maturity System (OMS)**, where "Maturity" is the only metric that matters for reliability progression. Each archetype now has its own identity, batch behavior, and maturity roadmap.

> *"An engine feels nearly invincible — until it doesn't."*

---

## Resolved Design Decisions

| Question | Decision |
|---|---|
| **Batch Reveal Timing** | **Hidden by default.** Player can reveal Lemon status via `CycleValves` (Hypergolic) or `Inspect Engine` (EVA). |
| **Cross-Engine Batch Grouping** | **Symmetry-Linked.** All engines in a symmetry group share one batch result (Batch Leader pattern). |
| **Weak Unit After Repair** | **Restored but degraded.** Performance penalties apply (ISP/Thrust) as "Performance Scars" (1% drop per scar). |
| **Repair System** | **Tiered Maintenance.** Engineers can perform "Tactical Patches" (consumes 1 kit, adds scars) or "Deep System Overhauls" (resets everything). |
| **Nuclear Repair Access** | **Engineer Level 3+ required** to perform deep overhauls or preventative maintenance on nuclear engines. |
| **ASI for Modded Engines** | Automated detection via ISP curve — uses `[KED_ASI_OVERRIDE]` if needed. |
| **Batch Lineage** | **Linked across launches.** `LineageRisk` tracks production quality (Lemons increase risk by 5%, Goods decrease it by 2%). |

---

## Part 1 — The Batch Quality System (BQS)

### 1.1 What Is a Batch?

When a vessel is launched, engines of the **same part name** are grouped. To ensure consistent behavior in clusters, KED uses a **Batch Leader** pattern.

- **Batch ID** generated once at launch.
- **Symmetry Synchronization**: All engines in a symmetry group are guaranteed to share the same batch result (Lemon vs Good).
- **Batch Lineage (Dynamic Risk)**: If a part name gets a "Lemon" result, the `LineageRisk` increases by 5%. Successful "Good" batches reduce it by 2%.
- **Aging Factor**: Once an engine model passes its `AgingThreshold` (Default: 50 flights), its reliability starts to decay globally.
- **Diegetic Pre-Launch Hints**: The Factory Specification or Launchpad PAW shows correlated hints:
  - *"Factory QA Note: Slight variance detected in turbopump alignment."*
  - *"Batch vibration signature above nominal."*
  - These hints are ~70% correlated with a Lemon status.

### 1.2 The Reliability Formula

The probability of a "Lemon" batch scales with the number of engines in the cluster.

**The Anchor Formula:** $$P(\text{Lemon}) = \text{MaturityAnchor} + (\text{Engine Count} - 1) \times 0.0167$$
*(Note: MaturityAnchor starts at 0.05 for most archetypes at Level 0).*

| Engine Count | Good Batch Chance | Lemon Batch Chance | Gameplay Feel |
| :--- | :--- | :--- | :--- |
| **1 (Solo)** | **95%** | **5%** | Highly reliable; failure is a rare anomaly. |
| **10 (Cluster)** | **80%** | **20%** | Standard heavy lifter risk. |
| **20 (Complex)** | **63.3%** | **36.7%** | Redundancy is required. |

### 1.3 The Weak Unit Mechanic

If a batch is rolled as a **Lemon**, specific engines in the cluster are designated as **Weak Units**.

*   **Deterministic Triggers:** Failures occur at a specific **Failure Trigger Time** (randomly designate between 15s and 50s of cumulative burn).
*   **SRB Spread Window:** Guaranteed safe for the first 10s. The failure trigger is a **fuel threshold** between 40% – 70% remaining fuel.
*   **Performance Scars:** Any "Tactical" repair or high-stress event applies a **Performance Scar** (-1% Thrust and ISP). These stack until a Deep System Overhaul is performed.

---

## Part 2 — Atmospheric Sensitivity Index (ASI)

ASI = Isp_VAC / Isp_ASL

| ASI Range | Role | Vacuum Behavior | Re-ignition at Altitude |
|---|---|---|---|
| `1.00 – 1.10` | Pure Booster | No bonus | Severe penalty |
| `1.26 – 1.50` | Sustainer | Moderate bonus | Moderate penalty |
| `2.01+` | Pure Vacuum | High bonus | No penalty |

- **Flow Separation Risk**: High-ASI engines (Vacuum engines) operated in thick atmosphere (>0.5 atm) risk flameout or gimbal seizure.

---

## Part 3 — Archetype System

### 3.1 DetermineArchetype Logic (Priority Order)

1.  **Nuclear**: Contains `EnrichedUranium`.
2.  **Exotic**: Vacuum ISP > 2850 OR uses `Antimatter`, etc.
3.  **Advanced**: Vacuum ISP > 500 (High-perf Hydrolox/Methalox).
4.  **Electric**: `EngineType == Electric` OR uses `ElectricCharge` + Noble Gases.
5.  **Airbreathing**: Uses `IntakeAir`.
6.  **Hypergolic**: Uses 2+ propellants, at least one from the Hypergolic list.
7.  **Monopropellant**: Uses exactly 1 propellant (`MonoPropellant` or `Hydrazine`).
8.  **Bipropellant**: Uses `LqdOxygen` or `Oxidizer`.
9.  **Solid**: `EngineType == SolidBooster` OR uses `SolidFuel`.
10. **Thermodynamic**: Fallback for standard liquid engines.

### 3.2 Unified Failure Palette

| Failure Mode | Description | Archetype Application |
| :--- | :--- | :--- |
| **Ignition Fail** | Engine fails to start; requires `Tactical Patch`. | Most archetypes. |
| **Flameout** | Sudden shutdown during burn. | Biprop, Thermo, Advanced, Airbreathing. |
| **Gimbal Lock** | Vectoring actuators seize. | Any engine with a gimbal. |
| **Thrust Drop** | Performance capped at 60%; applies 1 Scar. | Hypergolic, Monoprop. |
| **Explode** | Casing breach (SRB Only). | Solid; Maturity Lvl 2 allows "Safe Abort". |

### 3.3 Failure Cascades
Failures have an **8% chance** to trigger a chain reaction in symmetry counterparts.

---

## Part 4 — Operational Maturity System (OMS)

### 4.1 Maturity Points (MP)
*   **Engine Start:** $+1$ MP (Once per flight; altitude $> 100$m).
*   **Full Burn:** $+2$ MP (Burn exceeds 60s).
*   **Inspection:** $+5$ MP (Engineer performs `Inspect Engine` on EVA).
*   **Recovery:** $+5$ MP (Vessel recovered from `SubOrbital`+).
*   **Archetype Heritage:** New parts receive **20% MP** from the most experienced part in their archetype.

### 4.2 Roadmap Overview

| Archetype | Complexity | Max Level | Key Perks |
| :--- | :--- | :--- | :--- |
| **Biprop / Thermo** | High | 4 | Fail-safe zones (first 8s protected), reduced fatigue. |
| **Advanced** | High | 4 | Golden Batch (0.5% Anchor), capped ignition failures. |
| **Monoprop / Hyper** | Medium | 3 | Tactical EVA actions (Catalyst Swap / Nitrogen Purge). |
| **Nuclear / Electric** | Low | 2 | Soft failures instead of flameouts, gimbal immunity. |
| **Solid** | Low | 2 | Safe Abort (non-catastrophic casing breach). |

---

## Part 5 — Engineering Operations (EVA)

### 5.1 Maintenance Actions
1.  **Inspect Engine**: (Lvl 0 Engineer). Requires Repair Kits in inventory. 50% chance to reveal Weak Unit. Awards 5 MP.
2.  **Preventative Maintenance**: (Lvl Req). Consumes 1 Kit. Adds +300s to failure countdown. Max 3 stacks. Does NOT cure Lemon status.
3.  **Deep System Overhaul**: (Lvl Req). Consumes `SpecializedParts` (5% of dry mass). **Resets everything**: Scars, Fatigue, and Lemon/Weak status.
4.  **Tactical Patch**: (Lvl 0 Engineer). Consumes 1 Kit. Clears failure. Adds **2 Scars** and high structural fatigue. 10% Cascade risk.
5.  **Manual Turbine Prime**: (Lvl 0 Engineer). Consumes 1 Kit. Bypasses the next Spin-up cost.

---

## Part 6 — Technical Systems

### 6.1 Spin-up Requirements
Modern engines require active spin-up before ignition:
*   **Cryo Spin-up (Bipropellant)**: Consumes **Liquid Nitrogen (LN2)**.
*   **Advanced Spin-up (Advanced)**: Consumes **Electric Charge (EC)**.
*   Costs are higher in thick atmosphere and lower with high maturity.

### 6.2 Ignition Fatigue
Engines accumulate two types of stress:
*   **Structural**: Permanent mechanical wear per ignition. Only reset by Overhaul.
*   **Chemical**: Temporary residue/soot. Cleared by `Nitrogen Purge` or `Catalyst Swap`.
*   **Natural Decay**: Some engines (Airbreathing) self-clean while running.

### 6.3 Performance Scars
Every scar reduces **Thrust and ISP by 1%**.
*   Accumulated from: Tactical Patches (+2), Stress events, or failures.
*   Cleared by: Deep System Overhaul (All) or Tactical Resets (-1).

### 6.4 Catch-up Logic
KED processes wear and maturity during background burns (e.g., Persistent Thrust). If an engine fails while unloaded, it will be flagged as failed upon vessel load.
