# Kerbal Engine Dynamics (KED)
**A Total Overhaul of Engine Reliability for KSP**

Kerbal Engine Dynamics (KED) replaces the simple random failures of other mods with a **manufacturing-first model**. Every engine on your rocket has a serial number, a lineage, and a technical history.

> *"An engine feels nearly invincible — until it doesn't."*

---

## 🏗️ The Core Pillars

KED is built on four interactive systems that turn engine management into a strategic layer of your space program:

1.  **Batch Quality System (BQS)**: Engines are built in lots. Symmetry groups share a "Batch Leader" — if one is a Lemon, the whole group is at risk.
2.  **Operational Maturity System (OMS)**: Your agency gains experience with every flight. "Heritage" engines are nearly flawless; "Prototypes" are dangerous experiments.
3.  **Atmospheric Sensitivity (ASI)**: Engines are tuned for specific pressures. Using a vacuum engine at sea level is a recipe for a "Sudden Flameout."
4.  **Hardware Spin-up**: High-perf engines require **Liquid Nitrogen (LN2)** or **Electric Charge (EC)** to spin up their turbopumps before they can ignite.

---

## 📖 The Pilot's Guide: From Prototype to Heritage

### 1. Your First Launch (Maturity Level 0)
When you first unlock an engine (e.g., the "Reliant"), it is a **Prototype (Block 0)**. 
*   **The Risk**: Prototypes have a high "Lemon Anchor" (5% base failure rate).
*   **The Goal**: Fly them. Even short hops generate **Maturity Points (MP)**. 
*   **MP Gains**: 
    *   **Ignition**: +1 MP (Once per flight).
    *   **60s Burn**: +2 MP.
    *   **Recovery**: +5 MP (Bring the engine back home!).

### 2. Hunting for Lemons
Every launch rolls a new batch result. 
*   **The Hint**: Check the Part Action Window (PAW) in the VAB or on the Pad. Notes like *"Slight variance detected"* are a 70% match for a **Lemon Batch**.
*   **The Action**: Before you commit to a burn, have an Engineer perform an **EVA Inspection**. This has a 50% chance to reveal if the engine is a **Weak Unit**.
*   **Cycle Valves**: Hypergolic engines can be tested from the cockpit. "Cycling Valves" can reveal a Lemon but carries a 5% risk of accidental ignition or gimbal lock!

### 3. Ignition: The Resource Debt
If you are flying **Bipropellant** or **Advanced** engines, they won't just "start."
*   **Bipropellant**: Needs **Liquid Nitrogen (LN2)**. Ensure your service tanks are full. 
*   **Advanced**: Needs **Electric Charge (EC)**. High-performance Hydrolox/Methalox engines require a massive surge of power to spin up their high-torque pumps.
*   **Spin-Down Grace**: If you shut down and restart within 1.5 seconds, you keep your momentum and don't pay the spin-up cost again.

### 4. Handling a Failure
When an alarm sounds and the screen flashes red: **DON'T PANIC.**
*   **Thrust Drop**: The engine is still running but capped at 60%. You can usually finish the burn.
*   **Flameout**: The engine has died. You'll need an Engineer on EVA to perform a **Tactical Patch**.
*   **Performance Scars**: Every emergency patch adds **2 Scars**. Each scar is a permanent -1% penalty to Thrust and ISP.

---

## 👨‍🚀 The Engineer's Toolbox (EVA Guide)

Engineers are no longer just repair bots; they are your most valuable assets.

| Action | Cost | Effect | When to use? |
| :--- | :--- | :--- | :--- |
| **Inspect Engine** | Free* | Reveal Weak Unit status; +5 MP. | **Every** mission before the first major burn. |
| **Prev. Maintenance** | 1 Kit | Delays pending failure by 300s. | When a Weak Unit is found but you can't afford an overhaul. |
| **Tactical Patch** | 1 Kit | Clears a failure; adds 2 Scars. | Emergency mid-mission survival. |
| **Deep Overhaul** | SpecParts | Resets EVERYTHING (Scars, Fatigue, Lemons). | Between missions or during long-term station maintenance. |
| **Catalyst Swap** | 1 Kit + EC | Resets Monoprop burn clock; removes 1 Scar. | Deep space probes/landers after long missions. |
| **Nitrogen Purge** | 1 Kit | Resets Hypergolic chemical fatigue; removes 1 Scar. | High-utility orbital tugs. |
| **Turbine Prime** | 1 Kit | Makes the next start "free" (No LN2/EC). | When your vessel is out of spin-up resources. |

*\*Requires a Repair Kit in inventory as a tool, but does not consume it.*

---

## 🔬 Advanced Technical Details

### Performance Scars & Fatigue
*   **Structural Fatigue**: Mechanical wear that accumulates every time you press 'Z'. It **never** goes away except during a **Deep System Overhaul**.
*   **Chemical Fatigue**: Soot and residue. This causes "Ignition Failures." You can clear this with a **Nitrogen Purge** or **Catalyst Swap**.
*   **The "Block" System**:
    *   **Block I (Qualified)**: Lower ignition fatigue.
    *   **Block II (Proven)**: The first 8 seconds of every burn are immune to Weak Unit failures.
    *   **Heritage (Masterwork)**: Nearly immune to ignition failure; gimbal lock immunity.

### Failure Cascades
Failures have an **8% chance** to trigger a chain reaction. 
*   A vibrating, dying engine can shake its symmetry counterpart into a **Flameout**. 
*   **Pro Tip**: If one engine fails in a cluster, consider shutting down the whole cluster to prevent a cascade if you are in a stable orbit.

### Atmospheric Sensitivity (ASI)
The PAW displays your engine's ASI.
*   **ASI < 1.1**: Pure Booster. High altitude restarts are risky.
*   **ASI > 1.5**: Vacuum Specialist. Do **not** use at sea level; the backpressure will cause a flameout.

---

## 🚀 The Mission Lifecycle: A Detailed Walkthrough

### Stage 1: The Assembly (VAB & Launchpad)
Before you even ignite, your mission's reliability is being determined.
*   **Batch Selection**: In the VAB, look at the **KED Telemetry** in the PAW. If you see notes about *"Vibration signatures"* or *"Turbopump alignment,"* you are looking at a potential **Lemon**. Consider swapping the engine or bringing an extra Engineer.
*   **Symmetry Logic**: All engines in a symmetry group share the same batch. If you have 4 engines in a cluster, they all live or die together.
*   **Loadout**: Ensure your vessel has **SpecializedParts** (for overhauls) and **Liquid Nitrogen (LN2)** (for bipropellant spin-up).

### Stage 2: Ascent (The Spin-Up)
You've pressed Space.
*   **Turbine Spin-Up**: Bipropellant and Advanced engines will enter a "Spin-Up" phase. You'll see a resource drain in the PAW. If you lack LN2 or EC, the engine will fail to ignite.
*   **Protected Window**: If your engine is **Block II** or higher, the first 8 seconds of flight are "Fail-Safe." Use this time to clear the pad and establish a safe abort trajectory.
*   **ASI Monitoring**: As you climb, watch the **Atmospheric Sensitivity Index**. If you are using a Vacuum engine, don't ignite it until you are out of the "Warning" band (>0.5 atm) to avoid immediate flameout.

### Stage 3: Orbit & Deep Space (Fatigue & Resets)
Once in vacuum, the rules change.
*   **Ignition Fatigue**: Every time you restart, you add **Structural Stress**. 
    *   *Tip*: Only shut down when necessary. Long, continuous burns are safer than many short pulses.
*   **Catalyst Service**: Monopropellant engines have a "Service Limit" (default 600s). Once exceeded, they begin to lose thrust. Perform a **Catalyst Swap** on EVA to reset this.
*   **Nitrogen Purge**: For Hypergolic engines (OAMS/RCS), chemical fatigue builds up fast. A **Nitrogen Purge** clears this residue and removes a performance scar.

### Stage 4: Recovery (The MP Harvest)
*   **The Bonus**: Bringing an engine back to Kerbin (or any body) grants a massive **+5 MP** bonus. 
*   **Heritage Unlock**: Once an engine reaches **Heritage (Block IV)**, it becomes the "Gold Standard." Any new engines of that same archetype you unlock in the future will start with a portion of this experience.

---

## 🧠 Mastering the Archetypes: Professional Strategies

| Archetype | The "Gotcha" | Pro Strategy |
| :--- | :--- | :--- |
| **Nuclear** | Heavy core stress; Lvl 3 Engineer gate. | Always perform a **Deep Overhaul** every 10 missions. The parts are too expensive to lose. |
| **Electric** | Resource hungry; prone to ignition failure. | These engines are **Flameout Immune**. If it starts, it stays running. Focus on EC reserves. |
| **Solid** | No shutdown; casing breach risk. | SRBs are guaranteed safe for the first 10s. If they're going to blow, it's at the 50% fuel mark. |
| **Airbreather** | Turbine health; altitude envelopes. | Jets "self-clean" while running. If you have high chemical fatigue, let the engine idle at low thrust to flush the lines. |
| **Hypergolic** | Corrosion; no natural decay. | These engines never self-clean. **Never** fly a long-duration mission without a Nitrogen Purge kit. |

---

## 🛠️ Advanced Tuning (Configuring KED)

KED is fully customizable via `KED_Settings.cfg`.
*   **ProgressionRate**: Want a harder career? Set this to 50 for half MP gains.
*   **GlobalFailureRisk**: For a "Realism Overhaul" feel, bump this to 200%.
*   **EnableAging**: If true, engines flown 50+ times globally start to lose their base reliability as the "molds get old."
*   **JuryRigCascadeChance**: Control how often a "Tactical Patch" breaks neighboring engines.

---

## 📖 Case Study: The "Apollo 13" Scenario
**Situation**: Your center engine flamed out during ascent due to a Lemon Batch.
1.  **Immediate**: KED Screen Alerts flash red. The symmetry logic ensures your other engines stay running (unless a Cascade triggers).
2.  **Assessment**: Check the PAW. If it's a **Thrust Drop**, keep burning. If it's a **Flameout**, it's dead.
3.  **EVA Fix**: Send your Engineer. A **Tactical Patch** consumes 1 kit and gets it firing again.
4.  **The Scar**: You now have -2% Thrust on that engine. You'll need to compensate for the torque or adjust your thrust limiters.
5.  **The Lesson**: That mission just earned you **+10 MP** (The Hard Lesson) for successfully repairing a failed unit in flight.

---

> "KED ensures that every engine tells a story. From the first experimental prototypes to the heritage workhorses that carry your agency to the stars."
