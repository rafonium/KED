# Kerbal Engine Dynamics (KED)

**Kerbal Engine Dynamics (KED)** is a total overhaul of engine reliability for Kerbal Space Program. It introduces a **manufacturing-first model**, where engines are defined by their production batches, operational history, and technological maturity rather than simple random numbers.

> *"An engine feels nearly invincible — until it doesn't."*

---

## 🏗️ The Core Systems

KED 1.0 is built on three interactive pillars that transform your engineering workflow:

### 1. Batch Quality System (BQS)
Engines are no longer independent actors. When you launch a vessel, every engine of the same part name belongs to a single **Manufacturing Batch**.
*   **Good Batches**: Nominal performance; failures are nearly impossible unless pushed to extreme limits.
*   **Lemon Batches**: A "bad lot" where one or more **Weak Units** are hidden in the cluster, destined to fail during the mission.
*   **Cluster Complexity**: The chance of a "Lemon" batch increases with the number of engines in the cluster. Massive heavy lifters require redundancy to survive.

### 2. Operational Maturity System (OMS)
Progression is tracked via **Maturity Points (MP)**. Maturity is the global "experience level" your agency has with a specific engine archetype. 
*   Earn MP through successful launches, long burns, and recovering hardware.
*   Perform **EVA Repairs** to gain "The Hard Lesson" (+10 MP), accelerating your mastery of the hardware.
*   Higher maturity levels unlock better "Lemon Anchors" (base reliability) and unique archetype perks.

### 3. Atmospheric Sensitivity Index (ASI)
Reliability is tied to the operating environment. The ratio between an engine's Vacuum and Sea-Level ISP determines its **ASI**.
*   **Vacuum Engines** struggle with ignitions at high pressure (sea level).
*   **Booster Engines** suffer increased fatigue when restarted in a vacuum.
*   Operating outside an engine's optimal "Atmospheric Band" increases the risk of mechanical failure.

---

## 🔬 Engine Archetypes

KED 1.0 identifies hardware through prioritized resource detection, ensuring modded engines (Near Future, Kerbal Atomics) are correctly classified.

| Archetype | Identity & Key Failure Modes | Maturity Focus |
| :--- | :--- | :--- |
| **Nuclear** | Heavy thermal load; requires Lvl 3+ Engineer for repairs. (Flameout Immune). | Core Stability |
| **Electric** | Ion/Plasma thrusters; prone to ignition faults. (Flameout/Thrust Immune). | Deep Space Endurance |
| **Exotic** | Antimatter/Warp tech; Nearly perfect. (Flameout/Thrust Immune). | Advanced Materials |
| **Solid (SRB)** | High-stress casing; guaranteed safe for first 10s; explodes at 50%. | Structural Integrity |
| **Airbreathing** | Jet turbines; sensitive altitude/velocity "Safety Envelopes." | Turbine Health |
| **Hypergolic** | Corrosion-heavy; "Soft" valve failures vs "Hard" lockouts. | Valve Durability |
| **Monopropellant** | Pulse-heavy; catalyst degradation over cumulative burn time. | Catalyst Life |
| **Bipropellant** | Standard liquid; high ignition fatigue and turbopump risk. | Combustion Mastery |
| **Advanced** | High-performance Hydrolox/Methalox (ISP > 500). | Precise Tolerances |
| **Thermodynamic** | Standard liquid engines and mechanical fallbacks. | Mechanical Ruggedness |

---

## 👨‍🚀 Engineering Operations (EVA)

Engineers are no longer just repair technicians; they are vital field assets.

*   **Deep Diagnostics**: A Level 2+ Engineer can inspect an engine pre-failure. With enough maturity, they can identify a **Weak Unit** before it fails and perform **Preventative Maintenance**.
*   **Hardware Retrofit**: Upgrade active vessels to current Agency Standards. Perform a retrofit on EVA to update an engine's Maturity Level to the latest global stats.
*   **The Unified Failure Palette**:
    *   🔴 **Ignition Fail**: Total injector lockout at start.
    *   🔴 **Flameout**: Sudden shutdown during burn. (Rocket/Airbreathing only).
    *   🔴 **Explode**: Catastrophic casing breach (SRB Only).
    *   🟠 **Thrust Drop**: Performance capped.
    *   🟡 **Gimbal Lock**: Vectoring actuators seized.

---

## 📊 UI & Immersion

*   **Manufacturing Hints**: The Factory Specification and Launchpad PAW may show subtle, non-deterministic notes like *"Slight variance detected in turbopump alignment"*—a hint that your batch might be a Lemon.
*   **Identity & Reputation**: As an engine line matures, it gains history. The Tooltip/PAW tracks **Total Flights**, **Failures**, and **Success Rate**.
*   **Real-time Alerts**: Screen messages (Red/Orange/Yellow) notify you of failures instantly, allowing for split-second abort decisions.
*   **Heritage Hardware**: Vessels remain "locked" to the maturity standards of their launch date, serving as historical artifacts until a crew arrives to retrofit them.

---

> "KED 1.0 ensures that every engine tells a story. From the first experimental prototypes that fail on the pad to the heritage workhorses that carry your agency to the stars."
