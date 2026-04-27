# Engine Categorization Overhaul (Refined)

Overhaul the engine categorization logic in KED to provide precise archetypes based on fuel chemistry, propulsion technology, and performance metrics. This ensures that engines from complex mod suites (Kerbal Atomics, Near Future, Far Future) and standard airbreathing jets are correctly identified.

## User Review Required

> [!IMPORTANT]
> The classification logic now includes a specific "Nuclear" check that scans the part for radioactive material (`EnrichedUranium`) to distinguish NTRs from high-performance chemical engines.

> [!NOTE]
> The "Electric" category now includes a wide range of noble gases (Argon, Xenon, Krypton, Neon) and Lithium to support *Near Future Propulsion*.

## Proposed Changes

### KED Core Component

#### [MODIFY] [KED_Core.cs](file:///c:/Users/rafif/OneDrive/Documents/code-base/ksp-mod/KED/KED_Core.cs)

- **Update `EngineArchetype` Enum**:
    ```csharp
    public enum EngineArchetype { 
        Monopropellant, 
        Hypergolic, 
        Bipropellant, 
        Nuclear, 
        Electric, 
        Airbreathing, // New Category
        Exotic,     // ISP > 3000
        Advanced,   // ISP > 500
        Solid, 
        Thermodynamic 
    }
    ```
- **Refined `DetermineArchetype` Logic (Prioritized)**:
    1.  **Nuclear**: 
        - Part contains `EnrichedUranium` resource.
        - Engine propellants include at least one resource that is NOT `EnrichedUranium`.
    2.  **Electric**: 
        - `EngineType == EngineType.Electric` OR
        - Uses `ElectricCharge` AND one of: `XenonGas`, `ArgonGas`, `LqdArgon`, `KryptonGas`, `LqdKrypton`, `NeonGas`, `LqdNeon`, `Lithium`.
    3.  **Airbreathing**:
        - Uses `IntakeAir` or `IntakeAtm` as a propellant.
    4.  **Hypergolic**: 
        - Uses 2+ propellants.
        - At least one propellant is in the Hypergolic list: `Aerozine50`, `NTO`, `MMH`, `UDMH`, `NitricAcid`, `Hydrazine`.
    5.  **Monopropellant**: 
        - Uses exactly 1 propellant.
        - Propellant is `MonoPropellant` or `Hydrazine` (if used solo).
    6.  **Bipropellant**: 
        - Uses `LqdOxygen` or `Oxidizer`.
    7.  **Solid**: 
        - `EngineType == EngineType.SolidBooster` OR uses `SolidFuel`.
    8.  **Exotic**: 
        - Vacuum ISP > 3000.
    9.  **Advanced**: 
        - Vacuum ISP > 500.
    10. **Thermodynamic**: 
        - Catch-all fallback for standard liquid engines.

- **Mapping to Failure Logics**:
    - `Nuclear`, `Bipropellant`, `Advanced`, `Exotic`, `Thermodynamic`, `Airbreathing` -> Use **Thermodynamic Logic**.
    - `Monopropellant` -> Use **Monopropellant Logic**.
    - `Hypergolic` -> Use **Hypergolic Logic**.
    - `Solid` -> Use **Solid Logic**.
    - `Electric` -> Use **Thermodynamic Logic** (as a proxy).

## Verification Plan

### Manual Verification
- **Stock Part Tests**:
    - `J-X4 "Whiplash"`: Should classify as **Airbreathing**.
    - `LV-N "Nerv"`: Should classify as **Nuclear**.
    - `IX-6315 "Dawn"`: Should classify as **Electric**.
    - `O-10 "Puff"`: Should classify as **Monopropellant**.
- **Modded Part Tests**:
    - Verify that atmospheric engines from mods like *Near Future Aeronautics* are correctly identified.
