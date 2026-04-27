using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalEngineDynamics
{
    // ==========================================
    // CUSTOM KSP EVENT BUS
    // ==========================================
    public static class KEDEvents
    {
        public static EventData<string, int> OnFlightRecorded = new EventData<string, int>("KED_OnFlightRecorded");
    }

    // ==========================================
    // GLOBAL MOD SETTINGS
    // ==========================================
    public static class KEDSettings
    {
        public static float globalRiskMultiplier = 1.0f;
        public static float srbGracePeriodMultiplier = 1.0f;
        public static float monoServiceLimitMultiplier = 1.0f;
        public static float ctuYieldMultiplier = 1.0f;
        
        private static bool initialized = false;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            ConfigNode[] nodes = GameDatabase.Instance?.GetConfigNodes("KED_SETTINGS");
            if (nodes != null && nodes.Length > 0)
            {
                ConfigNode node = nodes[0];
                node.TryGetValue("globalRiskMultiplier", ref globalRiskMultiplier);
                node.TryGetValue("srbGracePeriodMultiplier", ref srbGracePeriodMultiplier);
                node.TryGetValue("monoServiceLimitMultiplier", ref monoServiceLimitMultiplier);
                node.TryGetValue("ctuYieldMultiplier", ref ctuYieldMultiplier);
            }
            initialized = true;
        }
    }

    // ==========================================
    // THE CUMULATIVE TEST UNIT (CTU) TRACKER
    // ==========================================
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPACECENTER)]
    public class EngineMasteryTracker : ScenarioModule
    {
        public static EngineMasteryTracker Instance;
        public Dictionary<string, int> engineExperience = new Dictionary<string, int>();

        public override void OnAwake() 
        { 
            Instance = this;
            KEDSettings.EnsureInitialized();
            KEDEvents.OnFlightRecorded.Add(OnEngineMessageReceived);
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
        }

        private void OnDestroy() 
        { 
            KEDEvents.OnFlightRecorded.Remove(OnEngineMessageReceived);
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
        }

        private void OnVesselRecovered(ProtoVessel protoVessel, bool quick)
        {
            foreach (ProtoPartSnapshot pps in protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules)
                {
                    if (ppms.moduleName != "KEDModule") continue;
                    float burnSec = 0f; bool failed = false; bool harvested = false; bool provChecked = false;
                    ppms.moduleValues.TryGetValue("cumulativeBurnSeconds", ref burnSec);
                    ppms.moduleValues.TryGetValue("isFailed", ref failed);
                    ppms.moduleValues.TryGetValue("telemetryHarvested", ref harvested);
                    ppms.moduleValues.TryGetValue("provenanceChecked", ref provChecked);

                    if (burnSec < 15f) continue;
                    AvailablePart ap = PartLoader.getPartInfoByName(pps.partInfo.name);
                    if (ap?.partPrefab == null) continue;
                    ModuleEngines e = ap.partPrefab.FindModuleImplementing<ModuleEngines>();
                    if (e == null) continue;

                    float bMass = ap.partPrefab.mass;
                    float bCost = Mathf.Max(pps.partInfo.cost, 10f);
                    float bIsp = Mathf.Max(e.atmosphereCurve.Evaluate(0f), 2f);
                    float bGimbal = ap.partPrefab.FindModuleImplementing<ModuleGimbal>() != null ? 0.25f : 0f;

                    // Unified Classification via UPI 2.0
                    KEDModule.CalculateUPI(bMass, bCost, bIsp, bGimbal, out float upi, out int ctuT);
                    int baseYield = (ctuT == 50) ? 10 : (ctuT == 400) ? 40 : (ctuT == 1000) ? 50 : 100;
                    bool isSRB = e.engineType == EngineType.SolidBooster;
                    if (isSRB) baseYield = 100; // SRBs rely on recovery/provenance primarily

                    baseYield = Mathf.RoundToInt(baseYield * KEDSettings.ctuYieldMultiplier);

                    // PROVENANCE BONUS: Awarded at 15s burn if not already recorded in flight.
                    // SRBs get their full baseYield on Provenance because they are single-use and
                    // are almost never inspected mid-flight — Provenance Check is their primary
                    // CTU earning path.
                    if (!provChecked && burnSec >= 15f)
                    {
                        int provYield = isSRB ? baseYield : Mathf.Max(1, baseYield / 5);
                        AddCTU(pps.partInfo.name, provYield);
                    }

                    // AUTO-HARVEST: only if NOT already inspected in-flight AND >= 60s burn
                    if (!harvested && burnSec >= 60f)
                    {
                        int yield = baseYield;
                        if (failed) yield *= 2;
                        if (burnSec >= 180f) yield = Mathf.RoundToInt(yield * 1.5f);
                        AddCTU(pps.partInfo.name, yield);
                    }
                    // SCRAP BONUS: failed part broadcasts fleet-wide CTU to same-pedigree track
                    if (failed)
                    {
                        int scrapBonus = Mathf.Max(1, baseYield / 2);
                        foreach (var kvp in new Dictionary<string, int>(engineExperience))
                        {
                            if (kvp.Key == pps.partInfo.name) continue;
                            AvailablePart bp = PartLoader.getPartInfoByName(kvp.Key);
                            if (bp == null || bp.partPrefab == null) continue;

                            ModuleEngines be = bp.partPrefab.FindModuleImplementing<ModuleEngines>();
                            if (be == null) continue;

                            // Calculate standardized UPI 2.0 for this part prefab
                            float sMass = bp.partPrefab.mass;
                            float sCost = bp.cost;
                            float sIsp = Mathf.Max(be.atmosphereCurve.Evaluate(0f), 2f);
                            float sGimbal = (bp.partPrefab.FindModuleImplementing<ModuleGimbal>() != null) ? 0.25f : 0f;

                            // Use the same classification logic as KEDModule
                            KEDModule.CalculateUPI(sMass, sCost, sIsp, sGimbal, out float sUpi, out int sCtuT);

                            if (sCtuT == ctuT) AddCTU(kvp.Key, scrapBonus);
                        }
                        ScreenMessages.PostScreenMessage($"[KED] SCRAP BULLETIN: Fleet safety data distributed (+{scrapBonus} CTU to pedigree track)", 8f, ScreenMessageStyle.LOWER_CENTER);
                    }
                }
            }
        }

        private void OnEngineMessageReceived(string engineName, int amount)
        {
            // Standard increment using the provided amount
            AddCTU(engineName, amount);
        }

        public void AddCTU(string engineName, int amount)
        {
            if (engineExperience.ContainsKey(engineName)) engineExperience[engineName] += amount;
            else engineExperience.Add(engineName, amount);
        }

        public override void OnSave(ConfigNode node) 
        { 
            foreach (var kvp in engineExperience) node.AddValue(kvp.Key, kvp.Value.ToString());
        }

        public override void OnLoad(ConfigNode node) 
        {
            engineExperience.Clear();
            foreach (ConfigNode.Value val in node.values)
                if (int.TryParse(val.value, out int xp)) engineExperience[val.name] = xp;
        }

        public static int GetCTU(string name) => (Instance != null && Instance.engineExperience.ContainsKey(name)) ? Instance.engineExperience[name] : 0;
    }

    // ==========================================
    // THE ENGINE MODULE (KED 2.0 ARCHITECTURE)
    // ==========================================
    public class KEDModule : PartModule, IModuleInfo
    {
        // Settings / CFG Controlled
        [KSPField] public float globalSafety = 1.0f;
        [KSPField] public float srbGracePeriodBase = 10f;
        [KSPField] public float monoServiceLimitBase = 600f;

        // --- SNAPSHOT & PERSISTENCE ---
        [KSPField(isPersistant = true)] public int blockLevelAtLaunch = -1;
        [KSPField(isPersistant = true)] public string serialNumber = "";
        
        [KSPField(isPersistant = true)] public bool isFailed = false;
        [KSPField(isPersistant = true)] public bool gimbalFailed = false;
        [KSPField(isPersistant = true)] public int failureType = 0; // 2=Ignition, 3=Flameout/Choke/Seizure
        [KSPField(isPersistant = true)] public bool wasBurning = false; 
        [KSPField(isPersistant = true)] public bool gimbalBuffApplied = false;

        // Archetype Accumulators
        [KSPField(isPersistant = true)] public float cumulativeBurnSeconds = 0f;
        [KSPField(isPersistant = true)] public float ignitionFatigue = 0f;
        [KSPField(isPersistant = true)] public double lastShutdownUT = -1;
        [KSPField(isPersistant = true)] public int srbStagePassed = 0; 
        [KSPField(isPersistant = true)] public bool telemetryHarvested = false;
        [KSPField(isPersistant = true)] public float nextCheckInterval = 10f; // Randomized 10–15s for thermodynamic running checks
        [KSPField(isPersistant = true)] public bool diagnosticsRun = false;
        [KSPField(isPersistant = true)] public bool provenanceChecked = false;

        // UI Fields
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "S/N", groupName = "KED", groupDisplayName = "RELIABILITY REPORT")]
        public string uiSerialNumber = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Certification", groupName = "KED")]
        public string uiCertLevel = "";
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Build Version", groupName = "KED")]
        public string uiBuildVersion = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Data Harvest", groupName = "KED")]
        public string uiDataHarvest = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Ignition Rel.", groupName = "KED")]
        public string uiIgnitionReliability = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Thrust Rating", groupName = "KED")]
        public string uiThrustRating = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Archetype Status", groupName = "KED")]
        public string uiArchetypeStatus = "";

        private List<ModuleEngines> engines = new List<ModuleEngines>();
        private float checkTimer = 0f;
        private float uiUpdateTimer = 0f;
        private List<float> originalMaxThrusts = new List<float>();
        private static readonly HashSet<string> HypergolicPropellants = new HashSet<string> { "Aerozine50", "NTO", "MMH", "UDMH", "NitricAcid", "Hydrazine" };

        public enum EngineArchetype { Thermodynamic, Monopropellant, Solid, Hypergolic }
        private EngineArchetype currentArchetype;

        public override void OnStart(StartState state)
        {
            KEDSettings.EnsureInitialized();
            engines = part.FindModulesImplementing<ModuleEngines>();
            if (engines.Count > 0)
            {
                originalMaxThrusts.Clear(); // Duplication protection
                foreach (var e in engines)
                {
                    originalMaxThrusts.Add(e.maxThrust);
                }
                DetermineArchetype();
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (blockLevelAtLaunch == -1)
                {
                    blockLevelAtLaunch = GetGlobalCertTier();
                    string pedClass = GetPedigreeClass(out float upi, out int ctuT);
                    string pedCode = pedClass.Substring(0, 2).ToUpper();
                    float secondsPerYear = GameSettings.KERBIN_TIME ? (21600f * 426f) : (86400f * 365f);
                    int year = Mathf.FloorToInt((float)Planetarium.GetUniversalTime() / secondsPerYear) + 2026;
                    serialNumber = $"{year}-{pedCode}-{UnityEngine.Random.Range(100, 999)}";
                }
                
                ApplyBlockPhysics(blockLevelAtLaunch);
                nextCheckInterval = UnityEngine.Random.Range(10f, 15f);
            }

            if (isFailed) SetFailureState(true, failureType);
            if (gimbalFailed) SetFailureState(true, 1);
            RefreshLiveUI();
        }

        private void DetermineArchetype()
        {
            if (engines.Count == 0) return;
            // We use the first module to determine archetype for the whole part
            var e = engines[0];
            if (e.engineType == EngineType.SolidBooster) { currentArchetype = EngineArchetype.Solid; return; }
            
            // Hypergolic Detection: Check for specific propellant pairings
            foreach (var prop in e.propellants)
            {
                if (HypergolicPropellants.Contains(prop.name))
                {
                    currentArchetype = EngineArchetype.Hypergolic;
                    return;
                }
            }

            if (e.propellants.Count == 1 && e.propellants[0].name == "MonoPropellant") currentArchetype = EngineArchetype.Monopropellant;
            else currentArchetype = EngineArchetype.Thermodynamic;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || engines.Count == 0) return;

            uiUpdateTimer += Time.deltaTime;
            if (uiUpdateTimer > 1f) { uiUpdateTimer = 0f; RefreshLiveUI(); }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || engines.Count == 0) return;

            bool anyEngineBurning = false;
            foreach (var e in engines)
            {
                if (e.EngineIgnited && e.currentThrottle > 0.001f && !e.flameout)
                {
                    anyEngineBurning = true;
                    break;
                }
            }

            // SHUTDOWN TRANSITION
            if (!anyEngineBurning && wasBurning)
            {
                wasBurning = false;
                lastShutdownUT = Planetarium.GetUniversalTime();
                // Catalyst Choke is a soft flameout — clear on shutdown so engine can retry
                if (isFailed && failureType == 3 && currentArchetype == EngineArchetype.Monopropellant)
                {
                    isFailed = false;
                    failureType = 0;
                }
            }

            if (gimbalFailed)
            {
                var g = part.FindModuleImplementing<ModuleGimbal>();
                if (g != null)
                {
                    g.gimbalLock = true;
                    g.Events["LockGimbal"].active = false;
                    g.Events["FreeGimbal"].active = false;
                }
            }

            if (isFailed)
            {
                // Hard Lockout for Thermodynamic (Pump Seizure or Ignition Failure)
                if (currentArchetype == EngineArchetype.Thermodynamic && (failureType == 3 || failureType == 2))
                { 
                    foreach (var e in engines)
                    {
                        if (e.EngineIgnited) e.Shutdown(); 
                        e.allowRestart = false; 
                        e.Events["Activate"].active = false; 
                        e.Actions["OnAction"].active = false; // Disable Action Groups
                        e.requestedThrottle = 0f;
                    }
                }
                return;
            }

            // IGNITION TRANSITION
            if (anyEngineBurning && !wasBurning)
            {
                wasBurning = true;
                HandleIgnitionEvent();
            }

            // CONTINUOUS BURN ACCUMULATOR
            if (anyEngineBurning)
            {
                cumulativeBurnSeconds += Time.fixedDeltaTime;
                checkTimer += Time.fixedDeltaTime;

                // PROVENANCE CHECK (15s Burn Reward)
                // SRBs get their full baseYield on Provenance (their primary CTU path since
                // they are single-use and almost never get a mid-flight diagnostic inspection).
                if (!provenanceChecked && cumulativeBurnSeconds >= 15f)
                {
                    provenanceChecked = true;
                    GetPedigreeClass(out float upi, out int ctuT);
                    int baseYield = (ctuT == 50) ? 10 : (ctuT == 400) ? 40 : (ctuT == 1000) ? 50 : 100;
                    int provYield = (currentArchetype == EngineArchetype.Solid) ? baseYield : Mathf.Max(1, baseYield / 5);
                    KEDEvents.OnFlightRecorded.Fire(part.partInfo.name, provYield);
                    ScreenMessages.PostScreenMessage($"[KED] PROVENANCE RECORDED: +{provYield} CTU (Flight Integrity Validated)", 5f, ScreenMessageStyle.LOWER_CENTER);
                }

                HandleRunningEvent();
            }
        }

        // ==========================================
        // FAILURE LOGICS & TRIGGERS
        // ==========================================
        private float GetBaseIgnitionRisk(int block)
        {
            return (block == 0) ? 0.10f : 0.05f;
        }

        private void HandleIgnitionEvent()
        {
            int block = blockLevelAtLaunch;
            
            if (currentArchetype == EngineArchetype.Thermodynamic)
            {
                if (block == 6) return; // Block VI Immunity

                bool isHot = (lastShutdownUT > 0 && (Planetarium.GetUniversalTime() - lastShutdownUT) < 30);
                float fatiguePenalty = isHot ? 0.01f : 0.02f;
                ignitionFatigue += fatiguePenalty;

                float baseRisk = GetBaseIgnitionRisk(block) * KEDSettings.globalRiskMultiplier;
                // Block X-0: hard cap 20%; Block III+: hard cap 2%
                float cap = (block == 0) ? 0.20f : (block >= 3) ? 0.02f : 1.0f;
                float finalRisk = Mathf.Clamp(baseRisk + ignitionFatigue, 0f, cap);

                if (UnityEngine.Random.value < finalRisk) SetFailureState(false, 2); // Ignition Failure
                nextCheckInterval = UnityEngine.Random.Range(10f, 15f); // Re-randomize running check
            }
            else if (currentArchetype == EngineArchetype.Monopropellant)
            {
                float wear = (block >= 3) ? 0.2f : (block >= 1) ? 0.4f : 0.5f;
                cumulativeBurnSeconds += wear; 
            }
            else if (currentArchetype == EngineArchetype.Hypergolic)
            {
                // Contact Ignition: 0% risk, no fatigue
                nextCheckInterval = UnityEngine.Random.Range(10f, 15f);
            }
        }

        private void HandleRunningEvent()
        {
            int block = blockLevelAtLaunch;

            // SRB Fixed-Point Checks
            if (currentArchetype == EngineArchetype.Solid)
            {
                float grace = ((block >= 4) ? 25f : (block >= 1) ? 15f : 10f) * KEDSettings.srbGracePeriodMultiplier;
                if (cumulativeBurnSeconds < grace) return;

                if (engines[0]?.propellants == null || engines[0].propellants.Count == 0) return;
                float fuelPct = (float)(engines[0].propellants[0].totalResourceAvailable / engines[0].propellants[0].totalResourceCapacity);
                float[] stages = { 0.8f, 0.6f, 0.5f, 0.4f, 0.2f };

                if (srbStagePassed < stages.Length && fuelPct <= stages[srbStagePassed])
                {
                    bool isPeak = (stages[srbStagePassed] == 0.5f);
                    float risk = (block >= 6) ? 0.005f : (block >= 2) ? 0.075f : 0.10f;
                    if (!isPeak) risk *= 0.5f;               // Non-peak checks are half
                    if (isPeak && block >= 2) risk *= 0.75f; // Block II: -25% Bell Curve peak risk
                    if (block == 0) risk *= 2.0f;            // Block X-0: 2x multiplier
                    risk *= KEDSettings.globalRiskMultiplier;

                    if (UnityEngine.Random.value < risk) { SetFailureState(false, 3); return; } // Casing Breach
                    srbStagePassed++;
                }
            }
            // Thermodynamic Interval Check (randomized 10–15s)
            else if (currentArchetype == EngineArchetype.Thermodynamic && checkTimer >= nextCheckInterval)
            {
                checkTimer = 0f;
                nextCheckInterval = UnityEngine.Random.Range(10f, 15f);
                float risk = (0.02f + ignitionFatigue) * KEDSettings.globalRiskMultiplier;
                if (block == 0) risk *= 2.0f;
                if (block >= 2) risk *= 0.7f; // -30% running risk
                // TWR stress modifier: up to +50% additional risk at 5G
                risk *= (1f + Mathf.Clamp01((float)(vessel.geeForce - 1.0) / 4f) * 0.5f);

                if (UnityEngine.Random.value < risk)
                {
                    var g = part.FindModuleImplementing<ModuleGimbal>();
                    if (g != null && block < 4 && UnityEngine.Random.value < 0.5f) SetFailureState(false, 1); // Gimbal Lock
                    else SetFailureState(false, 3); // Pump Seizure
                }
            }
            // Monopropellant Decay Check (5s)
            else if (currentArchetype == EngineArchetype.Monopropellant && checkTimer >= 5f)
            {
                checkTimer = 0f;
                float limit = monoServiceLimitBase * ((block >= 6) ? 3.0f : (block >= 2) ? 1.5f : 1.0f) * KEDSettings.monoServiceLimitMultiplier;

                if (cumulativeBurnSeconds > limit)
                {
                    float decayRate = (block >= 4) ? 0.05f : 0.10f;
                    float excess = cumulativeBurnSeconds - limit;
                    float risk = Mathf.Clamp01(excess * decayRate) * KEDSettings.globalRiskMultiplier;
                    if (UnityEngine.Random.value < risk) SetFailureState(false, 3); // Catalyst Choke (soft)
                }
            }
            // Hypergolic Corrosion Check (10s)
            else if (currentArchetype == EngineArchetype.Hypergolic && checkTimer >= 10f)
            {
                checkTimer = 0f;
                if (block >= 6) return; // Block VI: Infinite Chemical Integrity

                float limit = monoServiceLimitBase * ((block >= 2) ? 1.5f : 1.0f); // Re-use base for simplicity or separate field
                if (cumulativeBurnSeconds > limit)
                {
                    float corrosionRate = (block >= 2) ? 0.02f : 0.05f;
                    if (block >= 4) corrosionRate = 0f; // Valve Immunity (No lockouts during burn)
                    
                    float excess = cumulativeBurnSeconds - limit;
                    float risk = Mathf.Clamp01(excess * corrosionRate) * KEDSettings.globalRiskMultiplier;
                    
                    if (UnityEngine.Random.value < risk) SetFailureState(false, 3); // Corrosive Leak / Valve Lockout
                }
            }
        }

        private void SetFailureState(bool silent, int mode)
        {
            if (mode == 1)
            {
                gimbalFailed = true;
                var g = part.FindModuleImplementing<ModuleGimbal>();
                if (g != null)
                {
                    g.gimbalLock = true;
                    g.Events["LockGimbal"].active = false;
                    g.Events["FreeGimbal"].active = false;
                }
                if (!silent) ScreenMessages.PostScreenMessage($"[!] Gimbal Lock: {uiSerialNumber}", 10f, ScreenMessageStyle.UPPER_CENTER);
                Events["RepairGimbal"].active = HighLogic.LoadedSceneIsFlight;
                return;
            }

            isFailed = true;
            failureType = mode;
            
            if (currentArchetype == EngineArchetype.Solid && mode == 3)
            {
                if (!silent) ScreenMessages.PostScreenMessage($"[FATAL] CASING BREACH DETECTED - ABORT IMMEDIATELY", 10f, ScreenMessageStyle.UPPER_CENTER);
                foreach (var e in engines)
                {
                    e.Shutdown();
                    e.maxThrust = 0;
                }
                if (blockLevelAtLaunch < 3) part.explode(); // Collateral if below Block III
                else part.Die(); // Safe, non-collateral cleanup
                return;
            }

            if (currentArchetype == EngineArchetype.Monopropellant)
            {
                // Catalyst Choke — soft flameout, allowRestart stays TRUE; isFailed cleared on shutdown
                foreach (var e in engines) e.Shutdown();
            }
            else if (currentArchetype == EngineArchetype.Thermodynamic || currentArchetype == EngineArchetype.Hypergolic)
            {
                // Hard Lockout
                foreach (var e in engines)
                {
                    e.Shutdown();
                    e.allowRestart = false;
                    e.Events["Activate"].active = true; // Wait, why was this false? specification says hard lockout.
                    // Actually, let's keep the existing logic but just fix the gimbal part.
                    e.Events["Activate"].active = false;
                    e.Actions["OnAction"].active = false;
                }
            }

            Events["RepairEngine"].active = !(mode == 3 && currentArchetype == EngineArchetype.Monopropellant);
            Events["RunDiagnostics"].active = HighLogic.LoadedSceneIsFlight;
            if (!silent) ScreenMessages.PostScreenMessage($"[!] {GetFailureName(mode)}: {uiSerialNumber}", 10f, ScreenMessageStyle.UPPER_CENTER);
        }

        // ==========================================
        // EVA & MAINTENANCE MATRIX
        // ==========================================
        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Run Diagnostics", unfocusedRange = 3f, active = false)]
        public void RunDiagnostics()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];
            if (crew.experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can run diagnostics."); return; }
            diagnosticsRun = true;
            string report;
            if (currentArchetype == EngineArchetype.Solid)
                report = $"Casing: {GetSRBPressureStatus()}. Burn: {cumulativeBurnSeconds:F0}s.";
            else if (currentArchetype == EngineArchetype.Monopropellant)
                report = $"Catalyst Health: {GetCatalystHealthPct():F0}%. Burn: {cumulativeBurnSeconds:F0}s.";
            else
                report = $"Ignition Reliability: {GetIgnitionReliabilityPct():F1}%. Fatigue: {(ignitionFatigue * 100f):F1}%.";
            ScreenMessages.PostScreenMessage($"[KED] DIAGNOSTICS ({uiSerialNumber}): {report}", 8f, ScreenMessageStyle.LOWER_CENTER);
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Hardware Retrofit", unfocusedRange = 3f)]
        public void HardwareRetrofit()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];
            if (crew.experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can perform a Retrofit."); return; }

            int lvl = crew.experienceLevel;
            int kitsNeeded = (lvl >= 4) ? 3 : (lvl >= 2) ? 5 : 8;

            var inv = v.rootPart.FindModuleImplementing<ModuleInventoryPart>();
            if (inv != null && inv.TotalAmountOfPartStored("evaRepairKit") >= kitsNeeded) 
            {
                inv.RemoveNPartsFromInventory("evaRepairKit", kitsNeeded);
                blockLevelAtLaunch = GetGlobalCertTier();
                ApplyBlockPhysics(blockLevelAtLaunch);
                RefreshLiveUI();
                ScreenMessages.PostScreenMessage($"Retrofit Complete: Standardized to Block {blockLevelAtLaunch}", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else ScreenMessages.PostScreenMessage($"Requires {kitsNeeded} EVA Repair Kits.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Repair Engine Systems", unfocusedRange = 3f, active = false)]
        public void RepairEngine()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];
            if (crew.experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can perform maintenance."); return; }

            int lvl = crew.experienceLevel;
            int kitsNeeded = GetKitCost(failureType, lvl);

            var inv = v.rootPart.FindModuleImplementing<ModuleInventoryPart>();
            if (inv != null && inv.TotalAmountOfPartStored("evaRepairKit") >= kitsNeeded) 
            {
                inv.RemoveNPartsFromInventory("evaRepairKit", kitsNeeded);
                isFailed = false; failureType = 0; wasBurning = false; diagnosticsRun = false;
                foreach (var e in engines)
                {
                    e.allowRestart = true; 
                    e.Events["Activate"].active = true; 
                    e.Actions["OnAction"].active = true;
                }
                if (currentArchetype == EngineArchetype.Thermodynamic) ignitionFatigue = 0f;
                if (currentArchetype == EngineArchetype.Monopropellant || currentArchetype == EngineArchetype.Hypergolic) cumulativeBurnSeconds = 0f;
                
                Events["RepairEngine"].active = false;
                ScreenMessages.PostScreenMessage("Engine Systems Repaired!", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else ScreenMessages.PostScreenMessage($"Requires {kitsNeeded} EVA Repair Kits.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Repair Gimbal", unfocusedRange = 3f, active = false)]
        public void RepairGimbal()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];
            if (crew.experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can repair the gimbal."); return; }

            int lvl = crew.experienceLevel;
            int kitsNeeded = GetKitCost(1, lvl);

            var inv = v.rootPart.FindModuleImplementing<ModuleInventoryPart>();
            if (inv != null && inv.TotalAmountOfPartStored("evaRepairKit") >= kitsNeeded) 
            {
                inv.RemoveNPartsFromInventory("evaRepairKit", kitsNeeded);
                gimbalFailed = false;
                var g = part.FindModuleImplementing<ModuleGimbal>();
                if (g != null)
                {
                    g.gimbalLock = false;
                    g.Events["LockGimbal"].active = true;
                    g.Events["FreeGimbal"].active = true;
                }
                Events["RepairGimbal"].active = false;
                ScreenMessages.PostScreenMessage("Gimbal Repaired!", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else ScreenMessages.PostScreenMessage($"Requires {kitsNeeded} EVA Repair Kits.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private int GetKitCost(int fType, int lvl)
        {
            int cost = 8;
            if (currentArchetype == EngineArchetype.Thermodynamic) {
                if (fType == 2) cost = (lvl >= 2) ? 1 : 2; // Ignition
                else if (fType == 3) cost = (lvl >= 4) ? 1 : (lvl >= 2) ? 2 : 4; // Overhaul
                else cost = (lvl >= 2) ? 1 : 2; // Gimbal
            }
            else if (currentArchetype == EngineArchetype.Monopropellant) cost = (lvl >= 4) ? 1 : (lvl >= 2) ? 2 : 3; // Flush
            else if (currentArchetype == EngineArchetype.Hypergolic) cost = (lvl >= 4) ? 2 : (lvl >= 2) ? 3 : 5; // Decon
            else if (currentArchetype == EngineArchetype.Solid) cost = (lvl >= 4) ? 2 : (lvl >= 2) ? 4 : 6; // Patch
            
            if (diagnosticsRun) cost = Mathf.Max(1, cost - 1);
            return cost;
        }


        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Diagnostic Inspection", unfocusedRange = 3f)]
        public void HarvestTelemetry()
        {
            if (telemetryHarvested) return;
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            if (v.GetVesselCrew()[0].experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can perform inspections.", 5f, ScreenMessageStyle.UPPER_CENTER); return; }

            if (cumulativeBurnSeconds >= 60f || isFailed)
            {
                GetPedigreeClass(out float upi, out int ctuT);
                int yield = (ctuT == 50) ? 10 : (ctuT == 400) ? 40 : (ctuT == 1000) ? 50 : 100;
                yield = Mathf.RoundToInt(yield * KEDSettings.ctuYieldMultiplier);
                
                if (isFailed) yield *= 2;
                if (cumulativeBurnSeconds >= 180f) yield = Mathf.RoundToInt(yield * 1.5f);

                EngineMasteryTracker.Instance.AddCTU(part.partInfo.name, yield);
                telemetryHarvested = true;
                Events["HarvestTelemetry"].active = false;
                ScreenMessages.PostScreenMessage($"[KED] TELEMETRY: +{yield} Test Units Recorded", 5f, ScreenMessageStyle.LOWER_CENTER);
            }
        }

        // ==========================================
        // MATH, UI, AND HELPERS
        // ==========================================
        private float GetIgnitionReliabilityPct()
        {
            int block = blockLevelAtLaunch >= 0 ? blockLevelAtLaunch : GetGlobalCertTier();
            if (block == 6) return 100f;
            float baseRisk = GetBaseIgnitionRisk(block) * KEDSettings.globalRiskMultiplier;
            float cap = (block == 0) ? 0.20f : (block >= 3) ? 0.02f : 1.0f;
            return (1f - Mathf.Clamp(baseRisk + ignitionFatigue, 0f, cap)) * 100f;
        }

        private float GetCatalystHealthPct()
        {
            int block = blockLevelAtLaunch >= 0 ? blockLevelAtLaunch : GetGlobalCertTier();
            float limit = monoServiceLimitBase * ((block >= 6) ? 3.0f : (block >= 2) ? 1.5f : 1.0f) * KEDSettings.monoServiceLimitMultiplier;
            return Mathf.Clamp01(1f - (cumulativeBurnSeconds / limit)) * 100f;
        }

        private float GetChemicalIntegrityPct()
        {
            int block = blockLevelAtLaunch >= 0 ? blockLevelAtLaunch : GetGlobalCertTier();
            if (block >= 6) return 100f;
            float limit = monoServiceLimitBase * ((block >= 2) ? 1.5f : 1.0f);
            return Mathf.Clamp01(1f - (cumulativeBurnSeconds / limit)) * 100f;
        }

        private string GetSRBPressureStatus()
        {
            if (engines.Count == 0 || engines[0]?.propellants == null || engines[0].propellants.Count == 0) return "Stable";
            float fuelPct = (float)(engines[0].propellants[0].totalResourceAvailable / engines[0].propellants[0].totalResourceCapacity);
            if (fuelPct > 0.65f || fuelPct < 0.35f) return "Stable";
            if (fuelPct > 0.55f || fuelPct < 0.45f) return "Nominal";
            return "PEAK RISK";
        }

        private string GetBlockName(int b) => b == 0 ? "X-0" : b == 1 ? "I" : b == 2 ? "II" : b == 3 ? "III" : b == 4 ? "IV" : b == 5 ? "V" : "VI";
        private string GetBlockSubtitle(int b) => b == 0 ? "Experimental" : b == 1 ? "Flight Qualified" : b == 2 ? "Field Certified" : b == 3 ? "Human-Rated" : b == 4 ? "Heritage" : b == 5 ? "Up-Rated" : "Masterwork";

        // ==========================================
        // UNIVERSAL PEDIGREE INDEX (UPI 2.0)
        // Formula: log10(SpecificCost) + log10(Isp_VAC) + GimbalBonus
        // Additive scaling ensures exotic/nuclear engines are not penalised
        // for their high Isp. Self-initialising so it is safe to call from
        // GetInfo() / GetPrimaryField() before OnStart() has run.
        // ==========================================
        private string GetPedigreeClass(out float upi, out int ctuTarget)
        {
            // Use prefab/dry mass to ensure classification remains stable regardless of fuel state
            float mass = (part != null && part.partInfo != null) ? part.partInfo.partPrefab.mass : 1.0f;
            float cost = (part != null && part.partInfo != null) ? part.partInfo.cost : 100f;
            
            ModuleEngines e = (engines != null && engines.Count > 0) ? engines[0] 
                : part?.FindModuleImplementing<ModuleEngines>();

            float isp = (e != null) ? Mathf.Max(e.atmosphereCurve.Evaluate(0f), 2f) : 2f;
            float gimbalBonus = (part != null && part.FindModuleImplementing<ModuleGimbal>() != null) ? 0.15f : 0f;

            return CalculateUPI(mass, cost, isp, gimbalBonus, out upi, out ctuTarget);
        }

        public static string CalculateUPI(float mass, float cost, float isp, float gimbalBonus, out float upi, out int ctuTarget)
        {
            upi = 0f;
            ctuTarget = 5000;

            float specificCost = cost / Mathf.Max(mass, 0.01f);
            
            // UPI 2.0 Formula: Log10(Specific Cost) + Log10(Vac ISP) + Gimbal Bonus
            upi = Mathf.Log10(Mathf.Max(specificCost, 1f)) + Mathf.Log10(isp) + gimbalBonus;

            if (upi >= 8.5f) { ctuTarget = 50;   return "Exotic"; }
            if (upi >= 7.0f) { ctuTarget = 400;  return "Advanced"; }
            if (upi >= 5.0f) { ctuTarget = 1000; return "Aerospace"; }
            if (upi >= 3.5f) { ctuTarget = 2000; return "Industrial"; }

            return "Utility";
        }

        private int GetGlobalCertTier()
        {
            int ctu = EngineMasteryTracker.GetCTU(part.partInfo?.name ?? "");
            GetPedigreeClass(out float upi, out int target);
            
            float[] thresholds = { 0f, 0.10f, 0.25f, 0.45f, 0.65f, 0.85f, 1.0f };
            for (int i = 6; i >= 0; i--) if (ctu >= (target * thresholds[i])) return i;
            return 0;
        }

        private void ApplyBlockPhysics(int block)
        {
            for (int i = 0; i < engines.Count; i++)
            {
                if (originalMaxThrusts.Count > i)
                    engines[i].maxThrust = (block >= 5) ? originalMaxThrusts[i] * 1.05f : originalMaxThrusts[i];
            }

            if (currentArchetype == EngineArchetype.Solid && block >= 5 && !gimbalBuffApplied)
            {
                var g = part.FindModuleImplementing<ModuleGimbal>();
                if (g != null)
                {
                    g.gimbalResponseSpeed *= 1.2f;
                    gimbalBuffApplied = true;
                }
            }
        }

        private void RefreshLiveUI()
        {
            if (part == null || part.partInfo == null) return;

            // Update display values
            try
            {
                uiSerialNumber = serialNumber != "" ? serialNumber : "VAB-PREVIEW";
                int displayBlock = HighLogic.LoadedSceneIsFlight ? blockLevelAtLaunch : GetGlobalCertTier();
                uiCertLevel = $"Block {GetBlockName(displayBlock)} - {GetBlockSubtitle(displayBlock)}";
                uiBuildVersion = (displayBlock < GetGlobalCertTier()) ? "Legacy Hardware" : "Agency Standard";

                int ctu = EngineMasteryTracker.GetCTU(part.partInfo.name);
                GetPedigreeClass(out float upi, out int target);
                uiDataHarvest = ctu >= target ? "MAX MASTERY" : $"{ctu} / {target} CTU";

                // Ignition Reliability %
                if (currentArchetype == EngineArchetype.Thermodynamic)
                    uiIgnitionReliability = displayBlock == 6 ? "100% (Immune)" : $"{GetIgnitionReliabilityPct():F1}%";
                else
                    uiIgnitionReliability = "N/A";

                // Thrust Rating %
                uiThrustRating = displayBlock >= 5 ? "105% (Up-Rated)" : "100% (Baseline)";

                // Archetype Status
                if (currentArchetype == EngineArchetype.Solid)
                    uiArchetypeStatus = $"Casing Pressure: {GetSRBPressureStatus()}";
                else if (currentArchetype == EngineArchetype.Monopropellant)
                    uiArchetypeStatus = $"Catalyst Health: {GetCatalystHealthPct():F0}%";
                else if (currentArchetype == EngineArchetype.Hypergolic)
                    uiArchetypeStatus = $"Chemical Integrity: {GetChemicalIntegrityPct():F0}%";
                else
                    uiArchetypeStatus = $"Ignition Fatigue: {(ignitionFatigue * 100):F1}%";

                if (isFailed || gimbalFailed)
                {
                    string status = "FAILED: ";
                    if (isFailed) status += GetFailureName(failureType);
                    if (isFailed && gimbalFailed) status += " & ";
                    if (gimbalFailed) status += "Gimbal Lock";
                    uiArchetypeStatus = status;
                }

                // Dynamic button labels with kit cost hint
                int lvlHint = 2; // Default to standard cost
                if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA)
                {
                    var crew = FlightGlobals.ActiveVessel.GetVesselCrew();
                    if (crew != null && crew.Count > 0 && crew[0]?.experienceTrait?.Title == "Engineer") 
                        lvlHint = crew[0].experienceLevel;
                }

                int gKitCost = GetKitCost(1, lvlHint);
                int eKitCost = GetKitCost(failureType > 0 ? failureType : 3, lvlHint);

                Events["RepairGimbal"].guiName = $"Repair Gimbal ({gKitCost} Kits)";
                Events["RepairGimbal"].active = HighLogic.LoadedSceneIsFlight && gimbalFailed;

                Events["RepairEngine"].active = HighLogic.LoadedSceneIsFlight && isFailed && !(failureType == 3 && currentArchetype == EngineArchetype.Monopropellant);
                if (currentArchetype == EngineArchetype.Thermodynamic) Events["RepairEngine"].guiName = $"Perform Systems Overhaul ({eKitCost} Kits)";
                else if (currentArchetype == EngineArchetype.Monopropellant) Events["RepairEngine"].guiName = $"Perform Catalyst Flush ({eKitCost} Kits)";
                else if (currentArchetype == EngineArchetype.Hypergolic) Events["RepairEngine"].guiName = $"Perform Line Flush/Decon ({eKitCost} Kits)";
                else Events["RepairEngine"].guiName = $"Apply Structural Patch ({eKitCost} Kits)";

                Events["RunDiagnostics"].active = HighLogic.LoadedSceneIsFlight;
                Events["HarvestTelemetry"].active = HighLogic.LoadedSceneIsFlight && !telemetryHarvested && (cumulativeBurnSeconds >= 60f || isFailed || gimbalFailed);
                Events["HardwareRetrofit"].active = HighLogic.LoadedSceneIsFlight && blockLevelAtLaunch < GetGlobalCertTier();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KED] UI Refresh Failure: {ex.Message}");
            }
        }

        private string GetFailureName(int m) => m == 1 ? "Gimbal Lock" : m == 2 ? "Ignition Failure" : currentArchetype == EngineArchetype.Monopropellant ? "Catalyst Choke" : currentArchetype == EngineArchetype.Hypergolic ? "Valve Lockout" : "Pump Seizure";
        
        public string GetModuleTitle() => "Engine Dynamics (KED)";
        
        public string GetPrimaryField()
        {
            if (part != null && part.partInfo != null)
                return $"Pedigree: {GetPedigreeClass(out float upi, out int c)} ({upi:F2} UPI)";
            return "Pedigree: Evaluated on Placement";
        }

        public Callback<Rect> GetDrawModulePanelCallback() => null;

        public override string GetInfo()
        {
            if (part != null && part.partInfo != null)
                return $"<color=#00e6e6><b>=== FACTORY SPECIFICATION ===</b></color>\n<b>Class: <color=#ffaa00>{GetPedigreeClass(out float upi, out int c)}</color></b>\n<i>Place in workspace for Live Telemetry.</i>";
            return $"<color=#00e6e6><b>=== FACTORY SPECIFICATION ===</b></color>\n<b>Class: <color=#ffaa00>Evaluated on Placement</color></b>\n<i>Place in workspace for Live Telemetry.</i>";
        }
    }
}