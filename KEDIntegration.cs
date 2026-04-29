using System;
using System.Reflection;
using UnityEngine;

namespace KerbalEngineDynamics
{
    /// <summary>
    /// Soft-dependency wrapper for BackgroundThrust (BT) and Persistent Thrust (PT).
    /// Uses Reflection so KED loads cleanly without either mod installed.
    /// </summary>
    public static class KEDIntegration
    {
        // --- Persistent Thrust ---
        public static bool HasPT { get; private set; }
        private static Type      _ptEngineType;
        private static FieldInfo _ptThrottleField; // best-effort; PT API is undocumented

        // --- Background Thrust ---
        // Assembly name: "BackgroundThrust"   (BackgroundThrust.dll)
        // Sub-assemblies: "BackgroundThrust.BRP", "BackgroundThrust.Kerbalism", etc.
        public static bool HasBT { get; private set; }

        // BackgroundEngine   (PartModule on each engine part)
        private static Type      _btEngineType;          // BackgroundThrust.BackgroundEngine
        private static FieldInfo _btIsEnabledField;      // public bool IsEnabled

        // BackgroundThrustVessel (VesselModule — authoritative active-burn check)
        private static Type      _btVesselModuleType;    // BackgroundThrust.BackgroundThrustVessel
        private static PropertyInfo _btActiveProperty;   // public bool Active
        private static PropertyInfo _btThrottleProperty; // public double Throttle

        private static bool _initialized = false;

        // -----------------------------------------------------------------------
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            foreach (var asm in AssemblyLoader.loadedAssemblies)
            {
                // --- Persistent Thrust ---
                if (!HasPT && asm.name.StartsWith("PersistentThrust", StringComparison.OrdinalIgnoreCase))
                {
                    _ptEngineType = asm.assembly.GetType("PersistentThrust.PersistentEngine");
                    if (_ptEngineType != null)
                    {
                        // PT API is undocumented; try common field/property names
                        _ptThrottleField =
                            _ptEngineType.GetField("isEnabled",     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                            _ptEngineType.GetField("vesselThrottle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        HasPT = true;
                    }
                }

                // --- Background Thrust ---
                // Match the core assembly and all integration sub-assemblies ("BackgroundThrust.BRP", etc.)
                if (asm.name.Equals("BackgroundThrust", StringComparison.OrdinalIgnoreCase) ||
                    asm.name.StartsWith("BackgroundThrust.", StringComparison.OrdinalIgnoreCase))
                {
                    HasBT = true;

                    // BackgroundEngine — per-part PartModule; IsEnabled is a public KSPField
                    if (_btEngineType == null)
                    {
                        _btEngineType = asm.assembly.GetType("BackgroundThrust.BackgroundEngine");
                        if (_btEngineType != null)
                        {
                            // Field name: "IsEnabled" (capital I) — confirmed from source BackgroundEngine.cs:26
                            _btIsEnabledField = _btEngineType.GetField(
                                "IsEnabled", BindingFlags.Instance | BindingFlags.Public);
                        }
                    }

                    // BackgroundThrustVessel — VesselModule; Active and Throttle are the authoritative burn state
                    if (_btVesselModuleType == null)
                    {
                        _btVesselModuleType = asm.assembly.GetType("BackgroundThrust.BackgroundThrustVessel");
                        if (_btVesselModuleType != null)
                        {
                            // bool Active { get }  — returns true when packed+thrusting or background+thrusting
                            _btActiveProperty   = _btVesselModuleType.GetProperty("Active",   BindingFlags.Instance | BindingFlags.Public);
                            // double Throttle { get } — readable even when vessel is packed
                            _btThrottleProperty = _btVesselModuleType.GetProperty("Throttle", BindingFlags.Instance | BindingFlags.Public);
                        }
                    }
                }
            }

            if (KEDSettings.debugMode)
            {
                Debug.Log($"[KED] Integration initialized. " +
                          $"HasPT={HasPT} (engine={_ptEngineType?.Name ?? "null"}) | " +
                          $"HasBT={HasBT} (engine={_btEngineType?.Name ?? "null"}, vessel={_btVesselModuleType?.Name ?? "null"})");
            }
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Returns true if Persistent Thrust is currently driving this part's engine on-rails.
        /// </summary>
        public static bool IsEngineActivePT(Part part)
        {
            if (!HasPT || _ptEngineType == null || part == null) return false;

            var module = part.Modules.GetModule(_ptEngineType.Name);
            if (module == null) return false;

            try
            {
                object val = _ptThrottleField?.GetValue(module);
                if (val is bool b)   return b;
                if (val is float f)  return f > 0.01f;
                if (val is double d) return d > 0.01;
            }
            catch { /* Reflection failure — fall through */ }

            return false;
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Returns true if Background Thrust is actively burning this vessel's engines
        /// (packed timewarp or background). Checks both the VesselModule (preferred)
        /// and the per-part BackgroundEngine toggle as a fallback.
        /// </summary>
        public static bool IsVesselBurningBT(Vessel vessel)
        {
            if (!HasBT || vessel == null) return false;

            // 1. Preferred path: query BackgroundThrustVessel.Active on the VesselModule
            VesselModule vesselModule = null;
            if (_btVesselModuleType != null)
            {
                vesselModule = FindVesselModule(vessel, _btVesselModuleType);
            }

            if (vesselModule != null && _btActiveProperty != null)
            {
                try
                {
                    object active = _btActiveProperty.GetValue(vesselModule);
                    if (active is bool b) return b;
                }
                catch { /* fall through to part-level check */ }
            }

            // 2. Fallback: check if any BackgroundEngine on the vessel has IsEnabled=true and throttle>0
            if (_btEngineType != null && _btIsEnabledField != null)
            {
                try
                {
                    foreach (Part p in vessel.parts)
                    {
                        var btMod = p.Modules.GetModule(_btEngineType.Name);
                        if (btMod == null) continue;

                        object enabled = _btIsEnabledField.GetValue(btMod);
                        if (enabled is bool isEnabled && isEnabled)
                        {
                            // Throttle check via VesselModule (using the cached module)
                            if (vesselModule != null && _btThrottleProperty != null)
                            {
                                object throttle = _btThrottleProperty.GetValue(vesselModule);
                                if (throttle is double d && d > 0.01) return true;
                            }
                            else
                            {
                                return true; // No throttle check available — assume burning if enabled
                            }
                        }
                    }
                }
                catch { /* Reflection failure */ }
            }

            return false;
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Linear scan over vessel.vesselModules to find a module by reflected Type.
        /// KSP's FindVesselModuleImplementing is generic-only; this is the non-generic fallback.
        /// </summary>
        private static VesselModule FindVesselModule(Vessel vessel, Type targetType)
        {
            foreach (VesselModule m in vessel.vesselModules)
                if (m != null && m.GetType() == targetType) return m;
            return null;
        }
    }
}
