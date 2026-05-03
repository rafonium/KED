# Kerbal Engine Dynamics (KED): Engine Maintenance & Repair Systems

The KED repair system is built around a realistic **"Tools vs. Materials"** philosophy. Instead of abstract repair points, the system distinguishes between field-expedient emergency patches (Tools) and comprehensive structural rebuilds (Materials).

All maintenance actions must be performed by a Kerbal on EVA with the **Engineer** trait.

---

## 1. The Skill Gate System (`ENGINEER_GATES`)
Rather than scaling kit costs based on an Engineer's level, KED uses a flat gating system. If an Engineer meets the minimum level requirement for a specific engine archetype, they can perform advanced maintenance (Overhauls and Preventative Maintenance) on it. If they don't, they are restricted to emergency Tactical Patches only.

These gates are defined in `KED_Settings.cfg` under the `ENGINEER_GATES` block.
*(Example: A nuclear engine might require a Level 3 Engineer to overhaul, while a solid rocket booster only requires a Level 1 Engineer).*

---

## 2. Maintenance Actions & Interventions

### A. Tactical Patch (Emergency Jury-Rig)
The Tactical Patch is a quick, dirty field repair designed to get a dead or degraded engine firing again. It bypasses complex skill gates in favor of immediate survival.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** 1 EVA Repair Kit (consumed from the Kerbal's inventory).
*   **What it Fixes:** It clears the **first available failure mode** in the following priority order:
    1. Ignition Failure (Injector Lockout)
    2. Sudden Flameout
    3. Gimbal Seizure (Actuator Lock)
    4. Thrust Drop (Valve Seep / Catalyst Decay)
*   **Side Effects:** 
    *   Adds **+2 Performance Scars** (a permanent -2% penalty to thrust and ISP).
    *   Increases the engine's internal **Ignition Fatigue**.
    *   **Cascade Risk:** Has a small chance to propagate stress to symmetrically attached engines, potentially triggering secondary failures on those engines.

### B. Deep System Overhaul (Hardware Retrofit)
This is a comprehensive structural and internal rebuild of the engine. It represents stripping the engine down and replacing broken hardware with fresh spares.

*   **Skill Required:** Archetype-specific minimum level (via `ENGINEER_GATES`).
*   **Cost:** **0** EVA Repair Kits. Instead, it consumes **`SpecializedParts`** from the vessel's cargo capacity equal to **5% of the engine's dry mass**.
*   **What it Fixes:** 
    *   Clears **all** active failure modes simultaneously.
    *   Resets **Ignition Fatigue** to 0.
    *   Clears **all Performance Scars**, restoring the engine to 100% nominal efficiency.
    *   Clears the engine's "Lemon" or "Weak Unit" lineage flags.
*   **Side Effects:** Completely restores the engine to factory specifications based on its current Maturity Level.

### C. Preventative Maintenance
A proactive measure taken before an engine fails, designed to catch factory defects and restore reliability before a catastrophic event occurs.

*   **Skill Required:** Archetype-specific minimum level (via `ENGINEER_GATES`).
*   **Cost:** 1 EVA Repair Kit.
*   **What it Fixes:** Removes the "Weak Unit" and "Lemon" flags from an engine, completely neutralizing the elevated failure risk associated with bad manufacturing batches.
*   **Condition:** The engine must not currently be in a failed state.

### D. Run Diagnostics
A deep inspection of the engine's telemetry and hardware state.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** 1 EVA Repair Kit. *(Note: If the engine has reached Maturity Level 3 "Heritage" or higher, Diagnostics are completely **FREE** as the hardware is well-understood).*
*   **What it Does:** 
    *   Reveals hidden factory defects (Lemon/Weak Unit status).
    *   Provides a Reliability Forecast (Optimal, Stretched, Poor, Critical).
    *   Grants an immediate **+5 Maturity Points (MP)** bonus for inspecting the engine.
    *   Unlocks the ability to perform Preventative Maintenance if a defect is found.

### E. Minor Servicing (Catalyst Swap & Nitrogen Purge)
Archetype-specific field maintenance to extend the operational life of specialized engines.

*   **Skill Required:** Level 0+ Engineer
*   **Cost:** 1 EVA Repair Kit. (Catalyst Swaps also consume 5 Electric Charge from the vessel).
*   **What it Does:**
    *   **Catalyst Swap (Monopropellant):** Resets the burn-time clock, preventing or delaying Catalyst Decay (Thrust Drop). Removes 1 Performance Scar.
    *   **Nitrogen Purge (Hypergolic):** Halves the current accumulated Ignition Fatigue. Removes 1 Performance Scar.

---

## 3. The Consequences of Damage

### Performance Scars
Every time an engine suffers a severe mechanical event or receives a hasty Tactical Patch, it accumulates **Performance Scars**.
*   **1 Performance Scar = -1% Engine Efficiency.**
*   This penalty scales down both the engine's maximum **Thrust** and its **Specific Impulse (ISP)** across all atmospheric curves.
*   For example, an engine with 4 Performance Scars will operate at a strict 96% of its rated capability.
*   Scars can only be fully removed by performing a **Deep System Overhaul**. Minor servicing (Purges/Swaps) can slowly buff them out (-1 Scar per service).

### Ignition Fatigue
Engines accumulate thermal and mechanical stress every time they are ignited, and slightly more if they undergo a Tactical Patch. High fatigue significantly increases the chance of an Ignition Failure on the next start attempt. Fatigue is reduced by Nitrogen Purges or completely reset by a Deep System Overhaul.
