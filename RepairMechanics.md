# Kerbal Engine Dynamics (KED): Engine Maintenance & Repair Systems

The KED repair system is built around a realistic **"Tools vs. Materials"** philosophy. Instead of abstract repair points, the system distinguishes between field-expedient emergency patches (Tools) and comprehensive structural rebuilds (Materials).

All maintenance actions must be performed by a Kerbal on EVA with the **Engineer** trait.

---

## 1. The Skill Gate System (`ENGINEER_GATES`)
Rather than scaling kit costs based on an Engineer's level, KED uses a flat gating system. If an Engineer meets the minimum level requirement for a specific engine archetype, they can perform advanced maintenance (**Deep System Overhauls** and **Preventative Maintenance**) on it. If they don't, they are restricted to emergency Tactical Patches and Diagnostics only.

*   **Nuclear Engines:** Level 3 Engineer required.
*   **Electric Engines:** Level 2 Engineer required.
*   **Exotic Engines:** Level 4 Engineer required.
*   **Standard Engines:** Level 0 Engineer required.

---

## 2. Maintenance Actions & Interventions

### A. Tactical Patch (Emergency Jury-Rig)
The Tactical Patch is a quick field repair designed to get a dead or degraded engine firing again. It bypasses complex skill gates in favor of immediate survival.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** 1 EVA Repair Kit.
*   **What it Fixes:** Clears one active failure mode (Ignition, Flameout, or Gimbal).
*   **Side Effects:** 
    *   Adds **+2 Performance Scars** (a permanent -2% penalty to thrust and ISP).
    *   Adds significant **Structural Fatigue** (3-8 cycles).
    *   **Cascade Risk:** 10% chance to propagate stress to symmetric parts.

### B. Deep System Overhaul
A comprehensive structural and internal rebuild of the engine. This is the only way to fully "clean" an engine's history.

*   **Skill Required:** Archetype minimum level (Level 3 for Nuclear, etc).
*   **Cost:** **5% of engine dry mass** in **`SpecializedParts`**.
*   **What it Fixes:** 
    *   Clears **all** active failure modes and **all Performance Scars**.
    *   Resets **Ignition Fatigue** and **Burn Clock** to 0.
    *   **Breaks the Batch Lineage**: Clears "Lemon" and "Weak Unit" status completely.
    *   **Retrofit**: Updates the part to the latest global Maturity Standards.

### C. Preventative Maintenance
A proactive measure taken once a defect is identified. It represents shimming tolerances and reinforcing weak components.

*   **Skill Required:** Archetype minimum level.
*   **Cost:** 1 EVA Repair Kit.
*   **What it Does:** Extends the failure countdown by **300 seconds** (Default).
*   **Constraint:** Does NOT cure the Lemon status. It only buys time. Can be performed up to **3 times** (Max +900s delay) before the hardware is too far gone for field shims.

### D. Inspect Engine (Diagnostics)
A deep inspection of the engine's telemetry and hardware state.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** **FREE** (but requires at least 1 EVA Repair Kit in inventory as tooling).
*   **What it Does:** 
    *   50% chance to reveal hidden factory defects (**Lemon/Weak Unit** status).
    *   Grants an immediate **+5 Maturity Points (MP)** bonus.
    *   Unlocks the ability to perform Preventative Maintenance if a defect is revealed.

### E. Minor Servicing (Tactical Resets)
Archetype-specific field maintenance to extend the operational life of specialized engines.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** 1 EVA Repair Kit. (Catalyst Swaps also consume 5 Electric Charge).
*   **What it Does:**
    *   **Catalyst Swap (Monopropellant):** Resets the burn-time clock and chemical fatigue. Removes 1 Performance Scar.
    *   **Nitrogen Purge (Hypergolic):** Fully resets **Chemical Fatigue**. Removes 1 Performance Scar.
    *   **Manual Turbine Prime**: Bypasses the next ignition's spin-up resource requirement (LN2/EC).

---

## 3. The Consequences of Damage

### Performance Scars
Every time an engine suffers a severe mechanical event or receives a hasty Tactical Patch, it accumulates **Performance Scars**.
*   **1 Performance Scar = -1% Engine Efficiency.**
*   Reduces both **Thrust** and **Specific Impulse (ISP)**.
*   Scars can only be fully removed by a **Deep System Overhaul**. Minor servicing (Purges/Swaps) removes 1 scar per action.

### Ignition Fatigue
Engines accumulate stress every time they are ignited:
1.  **Structural Fatigue**: Permanent micro-fractures in turbopumps and injectors. Only reset by Overhaul.
2.  **Chemical Fatigue**: Temporary soot, residue, or catalyst poisoning. Reset by Purges or Catalyst Swaps.
High combined fatigue significantly increases the chance of an **Ignition Failure** on the next start attempt.
