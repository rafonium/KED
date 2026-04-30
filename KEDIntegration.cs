using System;
using System.Reflection;
using System.Linq.Expressions;
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

        // Fast Delegates
        private static Func<PartModule, object> _ptThrottleGetter;
        private static Func<PartModule, bool>   _btIsEnabledGetter;
        private static Func<VesselModule, bool> _btActiveGetter;
        private static Func<VesselModule, double> _btThrottleGetter;

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
                        _ptThrottleField =
                            _ptEngineType.GetField("isEnabled",      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                            _ptEngineType.GetField("vesselThrottle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        
                        if (_ptThrottleField != null)
                        {
                            _ptThrottleGetter = CreateFieldGetter<PartModule, object>(_ptEngineType, _ptThrottleField);
                            HasPT = true;
                        }
                    }
                }

                // --- Background Thrust ---
                if (asm.name.Equals("BackgroundThrust", StringComparison.OrdinalIgnoreCase) ||
                    asm.name.StartsWith("BackgroundThrust.", StringComparison.OrdinalIgnoreCase))
                {
                    HasBT = true;

                    if (_btEngineType == null)
                    {
                        _btEngineType = asm.assembly.GetType("BackgroundThrust.BackgroundEngine");
                        if (_btEngineType != null)
                        {
                            _btIsEnabledField = _btEngineType.GetField("IsEnabled", BindingFlags.Instance | BindingFlags.Public);
                            if (_btIsEnabledField != null)
                                _btIsEnabledGetter = CreateFieldGetter<PartModule, bool>(_btEngineType, _btIsEnabledField);
                        }
                    }

                    if (_btVesselModuleType == null)
                    {
                        _btVesselModuleType = asm.assembly.GetType("BackgroundThrust.BackgroundThrustVessel");
                        if (_btVesselModuleType != null)
                        {
                            _btActiveProperty   = _btVesselModuleType.GetProperty("Active",   BindingFlags.Instance | BindingFlags.Public);
                            _btThrottleProperty = _btVesselModuleType.GetProperty("Throttle", BindingFlags.Instance | BindingFlags.Public);
                            
                            if (_btActiveProperty != null)
                                _btActiveGetter = CreatePropertyGetter<VesselModule, bool>(_btVesselModuleType, _btActiveProperty);
                            if (_btThrottleProperty != null)
                                _btThrottleGetter = CreatePropertyGetter<VesselModule, double>(_btVesselModuleType, _btThrottleProperty);
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

        private static Func<TTarget, TValue> CreateFieldGetter<TTarget, TValue>(Type actualType, FieldInfo field)
        {
            var targetExp = Expression.Parameter(typeof(TTarget), "target");
            var castExp = Expression.TypeAs(targetExp, actualType);
            var fieldExp = Expression.Field(castExp, field);
            var resultExp = Expression.Convert(fieldExp, typeof(TValue));
            return Expression.Lambda<Func<TTarget, TValue>>(resultExp, targetExp).Compile();
        }

        private static Func<TTarget, TValue> CreatePropertyGetter<TTarget, TValue>(Type actualType, PropertyInfo prop)
        {
            var targetExp = Expression.Parameter(typeof(TTarget), "target");
            var castExp = Expression.TypeAs(targetExp, actualType);
            var propExp = Expression.Property(castExp, prop);
            var resultExp = Expression.Convert(propExp, typeof(TValue));
            return Expression.Lambda<Func<TTarget, TValue>>(resultExp, targetExp).Compile();
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Returns true if Persistent Thrust is currently driving this part's engine on-rails.
        /// Use the overload that takes a cached PartModule for performance.
        /// </summary>
        public static bool IsEngineActivePT(Part part)
        {
            if (!HasPT || _ptEngineType == null || part == null) return false;
            var module = part.Modules.GetModule(_ptEngineType.Name);
            return IsEngineActivePT(module);
        }

        public static bool IsEngineActivePT(PartModule module)
        {
            if (_ptThrottleGetter == null || module == null) return false;

            try
            {
                object val = _ptThrottleGetter(module);
                if (val is bool b)   return b;
                if (val is float f)  return f > 0.01f;
                if (val is double d) return d > 0.01;
            }
            catch { }

            return false;
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Returns true if Background Thrust is actively burning this vessel's engines.
        /// Optimized version that avoids vessel-wide part iteration.
        /// </summary>
        public static bool IsVesselBurningBT(Vessel vessel, PartModule cachedBTEngine, VesselModule cachedVesselModule = null)
        {
            if (!HasBT || vessel == null) return false;

            // Enforce Part-Level Checking to avoid false positives for unstaged engines.
            // We only count the engine as burning if its specific BackgroundEngine is enabled 
            // AND the vessel's background throttle is active.
            if (cachedBTEngine != null && _btIsEnabledGetter != null)
            {
                try
                {
                    if (_btIsEnabledGetter(cachedBTEngine))
                    {
                        // Now verify that the vessel actually has a non-zero background throttle
                        VesselModule vesselModule = cachedVesselModule ?? FindVesselModule(vessel, _btVesselModuleType);
                        if (vesselModule != null && _btThrottleGetter != null)
                        {
                            return _btThrottleGetter(vesselModule) > 0.01;
                        }

                        // If we have no throttle info but the vessel is packed, 
                        // we assume the 'IsEnabled' state is authoritative.
                        return vessel.packed;
                    }
                }
                catch { }
            }

            return false;
        }

        // -----------------------------------------------------------------------
        /// <summary>
        /// Linear scan over vessel.vesselModules to find a module by reflected Type.
        /// KSP's FindVesselModuleImplementing is generic-only; this is the non-generic fallback.
        /// </summary>
        public static VesselModule FindVesselModule(Vessel vessel, Type targetType)
        {
            if (vessel == null || targetType == null) return null;
            foreach (VesselModule m in vessel.vesselModules)
                if (m != null && m.GetType() == targetType) return m;
            return null;
        }

        public static Type GetBTVesselModuleType() => _btVesselModuleType;
        public static string GetPTEngineTypeName() => _ptEngineType?.Name;
        public static string GetBTEngineTypeName() => _btEngineType?.Name;
    }
}
