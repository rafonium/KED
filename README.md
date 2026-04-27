# Kerbal Engine Dynamics (KED) 2.0

**Kerbal Engine Dynamics (KED)** is a comprehensive overhaul of engine reliability, progression, and maintenance for Kerbal Space Program. It transforms engines from simple fire-and-forget parts into complex pieces of aerospace hardware with unique histories, manufacturing pedigrees, and evolving certification standards.

---

## 🚀 Core Philosophy

KED 2.0 is built on five pillars to deepen the engineering loop of KSP:

1.  **Strategic Depth**: Choose between mass-produced **Industrial** hardware or high-performance, ultra-reliable **Exotic** masterpieces.
2.  **Reliability Earned**: Reliability isn't a static value. Through the **7-Block Certification Pathway**, your agency perfects designs over time.
3.  **Engineer Value**: Elevates the **Flight Test Engineer (FTE)** to a critical mission role, responsible for performing complex field diagnostics and maintenance.
4.  **Historical Persistence**: The **Snapshot Mechanic** ensures that older vessels remain "museum pieces" of your agency's history, reflecting the technology standards of their launch date.
5.  **Archetype-Driven Realism**: Engines are governed by their **fuel lines**, simulating the unique physical stresses of Thermodynamic, Monopropellant, Solid, and Hypergolic systems.

---

## 🛠️ The Universal Pedigree Index (UPI 2.0)

Every engine is assigned a **Universal Pedigree Index (UPI)**. This index represents the "technological density" of the part, ensuring that advanced propulsion (like Nuclear or Fusion) is correctly classified alongside chemical engines.

**Formula**: `Log10(Specific Cost) + Log10(Vac ISP) + Gimbal Bonus`

| Class | UPI 2.0 Range | Yield (Base) | Target CTU | Characteristics |
| :--- | :--- | :--- | :--- | :--- |
| **Exotic** | >= 8.5 | 10 CTU | 50 | High cost/Isp, rapid mastery. |
| **Advanced** | >= 7.0 | 40 CTU | 400 | Cutting-edge aerospace tech. |
| **Aerospace** | >= 5.0 | 50 CTU | 1,000 | Standard high-performance parts. |
| **Industrial** | >= 3.5 | 100 CTU | 2,000 | Mass-produced, long grind. |
| **Utility** | < 3.5 | 100 CTU | 5,000 | Bare-bones reliability. |

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
*   **Failure Mode**: **Catalyst Choke** (Soft Flameout). Requires an EVA **Catalyst Flush** to reset the clock.

### 3. Solid Rocket Motors (SRB)
Focuses on internal casing pressure and "point of no return" reliability.
*   **Grace Period**: 0% failure risk for the first **10–25s** of burn.
*   **Pressure Bell Curve**: Risk peaks exactly at **50% fuel consumption**, representing maximum internal stress.
*   **Failure Mode**: **Casing Breach** (Visual explosion, thrust zeroed). Guaranteed non-collateral at Block III.

### 4. Hypergolic (Contact Ignition)
Focuses on chemical corrosion and valve reliability.
*   **Contact Ignition**: 0% ignition failure risk and no ignition fatigue.
*   **Corrosive Wear**: Service life is limited by chemical degradation.
*   **Failure Mode**: **Valve Lockout** (Hard Lockout). Requires an EVA **Line Flush/Decon**.
*   **Block IV**: Gains **Valve Immunity** (prevents lockouts while the engine is already burning).

---

## 📈 The 7-Block Certification Pathway

As your agency gains **Cumulative Test Units (CTU)** through flights and inspections, engines evolve through seven certification blocks.

| Block | Major Milestones |
| :--- | :--- |
| **Block X-0 (Experimental)** | High risk, 2.0x risk penalty, maximum telemetry yield. |
| **Block I (Flight Qualified)** | Risk normalized; +5s SRB Grace Period; reduced Monoprop Pulse Wear. |
| **Block II (Field Certified)** | -30% Running Risk; +50% Monoprop/Hypergolic Service Limit. |
| **Block III (Human-Rated)** | **2% Ignition Failure Cap**; Guaranteed non-collateral SRB breaches. |
| **Block IV (Heritage)** | **Total Gimbal/Valve Immunity**; +15s SRB Grace Period. |
| **Block V (Up-Rated)** | **+5% Thrust Bonus**; Improved SRB Gimbal response. |
| **Block VI (Masterwork)** | **Total Ignition Immunity** (0% failure); 95% SRB Breach reduction. |

---

## 👨‍🚀 The Flight Test Engineer (EVA)

Engineers are vital for maintaining the fleet and advancing agency knowledge.

### Field Maintenance (Repair Kit Costs)
Costs scale with experience level. **Running Diagnostics** before a repair reduces costs by **1 Kit** (min 1).

| Task | Rookie (Lvl 0-1) | Veteran (Lvl 2-3) | Master (Lvl 4-5) |
| :--- | :--- | :--- | :--- |
| **Systems Overhaul (Thermo)** | 4 Kits | 2 Kits | 1 Kit |
| **Catalyst Flush (Mono)** | 3 Kits | 2 Kits | 1 Kit |
| **Line Flush / Decon (Hyper)**| 5 Kits | 3 Kits | 2 Kits |
| **Structural Patch (SRB)** | 6 Kits | 4 Kits | 2 Kits |
| **Gimbal / Ignition Repair** | 2 Kits | 1 Kit | 1 Kit |

### Data Harvesting & Fleet Knowledge
*   **Provenance Check**: Automatically awarded at **15s burn**. Validates manufacturing integrity.
    *   **SRBs**: Full Base Yield (Primary mastery path).
    *   **Others**: 20% Base Yield.
*   **Diagnostic Inspection**: Perform on EVA to harvest remaining telemetry. Yield is **doubled (2x)** on failed engines.
*   **Scrap Bulletin**: Failed parts broadcast safety data fleet-wide. All engines in the same **UPI Pedigree track** receive a CTU bonus.
*   **Endurance Bonus**: 1.5x CTU yield for engines with >180s burn time.

---

## 💾 Snapshot Persistence

Every engine is assigned a **Serial Number (S/N)** (e.g., `2026-AE-001`) and locks in its **Block Level** at the moment of launch.
*   **Legacy Hardware**: A vessel launched at Block I will *stay* at Block I performance and reliability, even if you unlock Block VI globally.
*   **In-Flight Retrofit**: To upgrade an active vessel to the current **Agency Standard**, an Engineer must perform a **Hardware Retrofit** on EVA.

---

## 📊 UI & UX
*   **Editor**: Tooltips show the **Universal Pedigree Index (UPI)** and mastery target.
*   **Part Action Window (PAW)**: Displays Serial Number, Certification Level, Build Version (Legacy/Standard), and live reliability data.
*   **Dynamic Labels**: Maintenance buttons now show the exact Repair Kit cost based on the active Engineer's level.
*   **HUD Alerts**: Real-time messages for telemetry recording, provenance validation, and casing breaches.

---

> "KED 2.0 ensures that as your agency grows, your older space stations and satellites become tangible artifacts of your early engineering history until a maintenance crew arrives to upgrade them."
