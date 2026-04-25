# Kerbal Engine Dynamics (KED) 2.0

**Kerbal Engine Dynamics (KED)** is a comprehensive overhaul of engine reliability, progression, and maintenance for Kerbal Space Program. It transforms engines from simple fire-and-forget parts into complex pieces of aerospace hardware with unique histories, manufacturing pedigrees, and evolving certification standards.

---

## 🚀 Core Philosophy

KED 2.0 is built on five pillars to deepen the engineering loop of KSP:

1.  **Strategic Depth**: Choose between cheap, mass-produced **Industrial** hardware or high-performance, ultra-reliable **Exotic** masterpieces.
2.  **Reliability Earned**: Reliability isn't a static value. Through the **7-Block Certification Pathway**, your agency perfects designs over time.
3.  **Engineer Value**: Elevates the **Flight Test Engineer (FTE)** to a critical mission role, responsible for harvesting telemetry and performing complex field maintenance.
4.  **Historical Persistence**: The **Snapshot Mechanic** ensures that older vessels remain "museum pieces" of your agency's history, reflecting the technology standards of their launch date.
5.  **Archetype-Driven Realism**: Engines are governed by their **fuel lines**, simulating the unique physical stresses of Solid, Monopropellant, and Thermodynamic (Liquid/Nuclear) systems.

---

## 🛠️ The Pedigree Index (PI)

Every engine is assigned a **Pedigree Index (PI)** based on its "financial density"—the ratio of its cost to its performance envelope (Thrust and Isp).

| Class | PI Range | Discovery Rate | Yield (EVA) | Target CTU |
| :--- | :--- | :--- | :--- | :--- |
| **Exotic** | > 2.0 | 20% | +10 CTU | 50 |
| **Advanced** | 1.5 - 2.0 | 10% | +40 CTU | 400 |
| **Aerospace** | 1.3 - 1.5 | 5% | +50 CTU | 1,000 |
| **Industrial** | < 1.3 | 2% | +100 CTU | 5,000 |

*   **Industrial** parts are cheap but require a massive "long grind" to master.
*   **Exotic** parts are expensive but reach peak reliability almost instantly.

---

## 🔬 Propulsion Archetypes

KED identifies hardware by its fuel architecture to simulate distinct physical failure points.

### 1. Thermodynamic (Liquid / Nuclear / Fusion)
Focuses on thermal shock and mechanical complexity.
*   **Ignition Fatigue**: Every start adds a **-2% reliability penalty**.
*   **Thermal Soak**: If restarted within 30s of shutdown (while "Hot"), the penalty is halved to **-1%**.
*   **Failure Mode**: **Turbopump Seizure** (Hard Lockout). Requires an EVA **Systems Overhaul** to restart and reset fatigue.

### 2. Monopropellant
Focuses on catalyst degradation over time.
*   **Service Limit**: Tracks cumulative burn time (Base: 600s).
*   **Pulse Wear**: Every ignition consumes **0.5s** of service life instantly.
*   **Failure Mode**: **Catalyst Choking** (Frequent Flameouts). Requires an EVA **Catalyst Flush** to reset the clock.

### 3. Solid Rocket Motors (SRB)
Focuses on internal casing pressure and "point of no return" reliability.
*   **Grace Period**: 0% failure risk for the first **10–25s** of burn.
*   **Pressure Bell Curve**: Risk peaks exactly at **50% fuel consumption**, representing maximum internal stress.
*   **Failure Mode**: **Casing Breach** (Visual explosion, thrust zeroed). Guaranteed non-collateral at Block III.

---

## 📈 The 7-Block Certification Pathway

As your agency gains **Cumulative Test Units (CTU)** through flights and inspections, engines evolve through seven certification blocks.

| Block | Major Milestones |
| :--- | :--- |
| **Block X-0 (Experimental)** | High risk, 2.0x risk penalty, maximum telemetry yield. |
| **Block I (Flight Qualified)** | Risk normalized; +5s SRB Grace Period; reduced Monoprop Pulse Wear. |
| **Block II (Field Certified)** | -30% Running Risk; +50% Monoprop Service Limit. |
| **Block III (Human-Rated)** | **2% Ignition Failure Cap**; Guaranteed non-collateral SRB breaches. |
| **Block IV (Heritage)** | **Total Gimbal Lock Immunity**; +15s SRB Grace Period. |
| **Block V (Up-Rated)** | **+5% Thrust Bonus**; Improved SRB Gimbal response. |
| **Block VI (Masterwork)** | **Total Ignition Immunity** (0% failure); 95% SRB Breach reduction. |

---

## 👨‍🚀 The Flight Test Engineer (EVA)

Engineers are vital for maintaining the fleet and advancing agency knowledge.

### Field Maintenance (Repair Kit Costs)
The cost to perform maintenance scales inversely with the Engineer's level.

| Task | Rookie (Lvl 0-1) | Veteran (Lvl 2-3) | Master (Lvl 4-5) |
| :--- | :--- | :--- | :--- |
| **Systems Overhaul** | 4 Kits | 2 Kits | 1 Kit |
| **Catalyst Flush** | 3 Kits | 2 Kits | 1 Kit |
| **Hardware Retrofit** | 8 Kits | 5 Kits | 3 Kits |

### Telemetry Harvesting
*   **Diagnostic Inspection**: Perform on EVA to gain CTU. Yield is **doubled (2x)** on failed engines.
*   **Endurance Bonus**: 1.5x CTU yield for engines with >180s burn time.
*   **Recovery**: Safe return of hardware to Kerbin automatically harvests any uncollected telemetry.

---

## 💾 Snapshot Persistence

Every engine is assigned a **Serial Number (S/N)** (e.g., `2026-AE-001`) and locks in its **Block Level** at the moment of launch.
*   **Legacy Hardware**: A vessel launched at Block I will *stay* at Block I performance and reliability, even if you unlock Block VI globally.
*   **In-Flight Retrofit**: To upgrade an active vessel to the current **Agency Standard**, an Engineer must perform a **Hardware Retrofit** on EVA.

---

## ⚙️ Configuration & Installation

### Requirements
*   **ModuleManager**: Required to inject KED logic into engine parts.

### Settings (`KED_Settings.cfg`)
You can tune the experience using the following multipliers:
*   `globalRiskMultiplier`: Scales all failure probabilities.
*   `ctuYieldMultiplier`: Adjusts how fast you progress through blocks.
*   `srbGracePeriodMultiplier`: Adjusts the safe window for SRB launches.
*   `monoServiceLimitMultiplier`: Adjusts monopropellant catalyst lifespan.

---

## 📊 UI & UX
*   **Editor**: Tooltips show the **Design Pedigree** (Exotic, Industrial, etc.).
*   **Part Action Window (PAW)**: Displays Serial Number, Certification Level, Build Version (Legacy/Standard), and live reliability data.
*   **HUD Alerts**: Real-time screen messages for telemetry recording, ignition failures, and casing breaches.

---

> "KED 2.0 ensures that as your agency grows, your older space stations and satellites become tangible artifacts of your early engineering history until a maintenance crew arrives to upgrade them."
