using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using System.Reflection;
using System.Linq;

namespace KerbalEngineDynamics
{
    // ==========================================
    // CUSTOM KSP EVENT BUS
    // ==========================================
    public static class KEDEvents
    {
        public static EventData<string, float> OnMaturityGained = new EventData<string, float>("KED_OnMaturityGained");
    }

    // ==========================================
    // GLOBAL MOD SETTINGS (WITH TRANSLATION LAYER)
    // ==========================================
    public static class KEDSettings
    {
        // Internal Units
        public static float globalRiskMultiplier = 1.0f;
        public static float maturityYieldMultiplier = 1.0f;
        public static float recoveryBonus = 5.0f;
        public static float startBonus = 1.0f;
        public static float burnBonus = 2.0f;
        public static float heritageTransferRate = 0.2f;
        public static float lineageLemonInc = 0.05f;
        public static float lineageLemonDec = 0.02f;
        public static float agingFactorInc = 0.001f;
        public static int agingFlightThreshold = 50;
        public static bool enableAging = true;
        public static bool enableCryoSpinUp = true;
        public static bool enableAdvancedSpinUp = true;
        public static float ignitionFailureBase = 0.01f;
        public static float srbGracePeriod = 10.0f;
        public static float catalystServiceLimit = 600f;
        public static float hypergolicFatigueStep = 0.005f;
        public static float cycleValvesRisk = 0.05f;
        public static float cycleValvesReveal = 0.5f;
        public static float cryoSpinDownGrace = 1.5f;
        public static float cryoSpinUpMultiplier = 1.0f;
        public static float advancedSpinUpMultiplier = 1.0f;
        public static bool debugMode = false;
        public static float batchLemonProbScale = 0.0167f;
        public static float vacuumReliabilityBonus = 0.1f;
        public static float asiFlameoutThreshold = 1.5f;
        public static float asiWarningThreshold = 1.25f;
        public static float asiPressureThreshold = 0.5f;
        public static Dictionary<string, int> engineerGates = new Dictionary<string, int>();
        public static float juryRigFatigueAdd    = 0.15f;
        public static float juryRigCascadeChance = 0.10f;
        public static float overhaulMassRatio    = 0.05f;

        // Maturity Thresholds
        public static float mono_L1 = 60, mono_L2 = 150, mono_L3 = 300;
        public static float hyper_L1 = 60, hyper_L2 = 150, hyper_L3 = 300;
        public static float biprop_L1 = 100, biprop_L2 = 250, biprop_L3 = 500, biprop_L4 = 1000;
        public static float nuc_L1 = 50, nuc_L2 = 120;
        public static float elec_L1 = 50, elec_L2 = 120;
        public static float air_L1 = 80, air_L2 = 200, air_L3 = 450;
        public static float exotic_L1 = 30;
        public static float adv_L1 = 100, adv_L2 = 250, adv_L3 = 500, adv_L4 = 1000;
        public static float solid_L1 = 40, solid_L2 = 80;
        public static float thermo_L1 = 100, thermo_L2 = 250, thermo_L3 = 500, thermo_L4 = 1000;
        
        private static bool initialized = false;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            ConfigNode[] nodes = GameDatabase.Instance?.GetConfigNodes("KED_SETTINGS");
            if (nodes != null && nodes.Length > 0)
            {
                ConfigNode node = nodes[0];
                
                // Progression Translation
                float progressionRate = 100f;
                if (node.TryGetValue("ProgressionRate", ref progressionRate)) maturityYieldMultiplier = progressionRate / 100f;
                node.TryGetValue("RecoveryMP", ref recoveryBonus);
                node.TryGetValue("IgnitionStartMP", ref startBonus);
                node.TryGetValue("FullBurnMP", ref burnBonus);
                
                float heritageEfficiency = 20f;
                if (node.TryGetValue("HeritageEfficiency", ref heritageEfficiency)) heritageTransferRate = heritageEfficiency / 100f;

                // Reliability Translation
                float globalRisk = 100f;
                if (node.TryGetValue("GlobalFailureRisk", ref globalRisk)) globalRiskMultiplier = globalRisk / 100f;

                float lemonInc = 5f;
                if (node.TryGetValue("BatchFailureStep", ref lemonInc)) lineageLemonInc = lemonInc / 100f;

                float lemonDec = 2f;
                if (node.TryGetValue("BatchImprovementStep", ref lemonDec)) lineageLemonDec = lemonDec / 100f;

                float agingSens = 5f;
                if (node.TryGetValue("AgingSensitivity", ref agingSens)) agingFactorInc = agingSens * 0.0002f;

                node.TryGetValue("AgingThreshold", ref agingFlightThreshold);
                node.TryGetValue("EnableAging", ref enableAging);
                node.TryGetValue("EnableCryoSpinUp", ref enableCryoSpinUp);
                node.TryGetValue("EnableAdvancedSpinUp", ref enableAdvancedSpinUp);

                float ignFailBase = 1f;
                if (node.TryGetValue("IgnitionFailureBase", ref ignFailBase)) ignitionFailureBase = ignFailBase / 100f;

                node.TryGetValue("SRBGracePeriod", ref srbGracePeriod);
                node.TryGetValue("CatalystServiceLimit", ref catalystServiceLimit);
                
                float hyperFatigue = 0.5f;
                if (node.TryGetValue("HypergolicFatigueStep", ref hyperFatigue)) hypergolicFatigueStep = hyperFatigue / 100f;
                
                float valveRisk = 5f;
                if (node.TryGetValue("CycleValvesRisk", ref valveRisk)) cycleValvesRisk = valveRisk / 100f;
                
                float valveReveal = 50f;
                if (node.TryGetValue("CycleValvesReveal", ref valveReveal)) cycleValvesReveal = valveReveal / 100f;

                node.TryGetValue("CryoSpinDownGrace", ref cryoSpinDownGrace);

                float cryoMultiplier = 100f;
                if (node.TryGetValue("CryoSpinUpMultiplier", ref cryoMultiplier)) cryoSpinUpMultiplier = cryoMultiplier / 100f;

                float advMultiplier = 100f;
                if (node.TryGetValue("AdvancedSpinUpMultiplier", ref advMultiplier)) advancedSpinUpMultiplier = advMultiplier / 100f;

                float batchScale = 1.67f;
                if (node.TryGetValue("BatchLemonProbScale", ref batchScale)) batchLemonProbScale = batchScale / 100f;

                float vacBonus = 10f;
                if (node.TryGetValue("VacuumReliabilityBonus", ref vacBonus)) vacuumReliabilityBonus = vacBonus / 100f;

                node.TryGetValue("ASI_FlameoutThreshold", ref asiFlameoutThreshold);
                node.TryGetValue("ASI_WarningThreshold", ref asiWarningThreshold);
                node.TryGetValue("ASI_PressureThreshold", ref asiPressureThreshold);

                node.TryGetValue("DebugMode", ref debugMode);

                float juryFatigue = 15f;
                if (node.TryGetValue("JuryRigFatigueAdd", ref juryFatigue)) juryRigFatigueAdd = juryFatigue / 100f;
                float juryRigCasc = 10f;
                if (node.TryGetValue("JuryRigCascadeChance", ref juryRigCasc)) juryRigCascadeChance = juryRigCasc / 100f;
                float massRatio = 5f;
                if (node.TryGetValue("OverhaulMassRatio", ref massRatio)) overhaulMassRatio = massRatio / 100f;

                // Thresholds Loading
                if (node.HasNode("MATURITY_THRESHOLDS"))
                {
                    ConfigNode tNode = node.GetNode("MATURITY_THRESHOLDS");
                    if (tNode.HasNode("MONOPROP")) { var n = tNode.GetNode("MONOPROP"); n.TryGetValue("L1", ref mono_L1); n.TryGetValue("L2", ref mono_L2); n.TryGetValue("L3", ref mono_L3); }
                    if (tNode.HasNode("HYPERGOLIC")) { var n = tNode.GetNode("HYPERGOLIC"); n.TryGetValue("L1", ref hyper_L1); n.TryGetValue("L2", ref hyper_L2); n.TryGetValue("L3", ref hyper_L3); }
                    if (tNode.HasNode("BIPROP")) { var n = tNode.GetNode("BIPROP"); n.TryGetValue("L1", ref biprop_L1); n.TryGetValue("L2", ref biprop_L2); n.TryGetValue("L3", ref biprop_L3); n.TryGetValue("L4", ref biprop_L4); }
                    if (tNode.HasNode("NUCLEAR")) { var n = tNode.GetNode("NUCLEAR"); n.TryGetValue("L1", ref nuc_L1); n.TryGetValue("L2", ref nuc_L2); }
                    if (tNode.HasNode("ELECTRIC")) { var n = tNode.GetNode("ELECTRIC"); n.TryGetValue("L1", ref elec_L1); n.TryGetValue("L2", ref elec_L2); }
                    if (tNode.HasNode("AIRBREATHER")) { var n = tNode.GetNode("AIRBREATHER"); n.TryGetValue("L1", ref air_L1); n.TryGetValue("L2", ref air_L2); n.TryGetValue("L3", ref air_L3); }
                    if (tNode.HasNode("EXOTIC")) { var n = tNode.GetNode("EXOTIC"); n.TryGetValue("L1", ref exotic_L1); }
                    if (tNode.HasNode("ADVANCED")) { var n = tNode.GetNode("ADVANCED"); n.TryGetValue("L1", ref adv_L1); n.TryGetValue("L2", ref adv_L2); n.TryGetValue("L3", ref adv_L3); n.TryGetValue("L4", ref adv_L4); }
                    if (tNode.HasNode("SOLID")) { var n = tNode.GetNode("SOLID"); n.TryGetValue("L1", ref solid_L1); n.TryGetValue("L2", ref solid_L2); }
                    if (tNode.HasNode("THERMO")) { var n = tNode.GetNode("THERMO"); n.TryGetValue("L1", ref thermo_L1); n.TryGetValue("L2", ref thermo_L2); n.TryGetValue("L3", ref thermo_L3); n.TryGetValue("L4", ref thermo_L4); }
                }

                if (node.HasNode("ENGINEER_GATES"))
                {
                    ConfigNode gNode = node.GetNode("ENGINEER_GATES");
                    foreach (ConfigNode.Value val in gNode.values)
                    {
                        if (int.TryParse(val.value, out int level))
                        {
                            engineerGates[val.name.ToUpper()] = level;
                        }
                    }
                }
            }
            initialized = true;
        }
    }

    public enum EngineArchetype 
    { 
        Monopropellant, 
        Hypergolic, 
        Bipropellant, 
        Nuclear, 
        Electric, 
        Airbreathing, 
        Exotic, 
        Advanced, 
        Solid, 
        Thermodynamic 
    }

    public enum BatchQuality { Good, Lemon }

    public enum FailureType { None = 0, Ignition = 1, Flameout = 2, Gimbal = 3, Thrust = 4, Explosion = 5 }
    public enum ServiceType { Maintenance = 0, Ignition = 1, Flameout = 2, Gimbal = 3, Thrust = 4, Diagnostics = 5, Retrofit = 6, CatalystSwap = 7, NitrogenPurge = 8 }

    // ==========================================
    // THE OPERATIONAL MATURITY SYSTEM (OMS)
    // ==========================================
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.SPACECENTER)]
    public class KEDScenario : ScenarioModule
    {
        public static KEDScenario Instance;
        public Dictionary<string, float> globalEngineMaturity = new Dictionary<string, float>();
        public Dictionary<string, int> manufacturingSeeds = new Dictionary<string, int>();
        public Dictionary<string, int> globalFlightsCount = new Dictionary<string, int>();
        public Dictionary<string, float> lineageRisk = new Dictionary<string, float>();

        private Queue<ScreenMessage> messageQueue = new Queue<ScreenMessage>();
        private float lastMessageTime = 0f;

        public override void OnAwake() 
        { 
            Instance = this;
            KEDSettings.EnsureInitialized();
            KEDIntegration.Initialize();
            KEDEvents.OnMaturityGained.Add(AddMaturity);
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
        }

        private void OnDestroy() 
        { 
            KEDEvents.OnMaturityGained.Remove(AddMaturity);
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
        }

        public void AddMaturity(string partName, float amount)
        {
            float yield = amount * KEDSettings.maturityYieldMultiplier;
            if (globalEngineMaturity.ContainsKey(partName)) globalEngineMaturity[partName] += yield;
            else globalEngineMaturity.Add(partName, yield);
        }

        public void AddFlight(string partName)
        {
            if (globalFlightsCount.ContainsKey(partName)) globalFlightsCount[partName]++;
            else globalFlightsCount.Add(partName, 1);
        }

        public void RecordBatchResult(string partName, bool lemon)
        {
            if (!lineageRisk.ContainsKey(partName)) lineageRisk.Add(partName, 0f);
            
            if (lemon) lineageRisk[partName] = Mathf.Min(0.2f, lineageRisk[partName] + KEDSettings.lineageLemonInc);
            else lineageRisk[partName] = Mathf.Max(0f, lineageRisk[partName] - KEDSettings.lineageLemonDec);
        }

        public void PostQueuedMessage(string text, float duration, ScreenMessageStyle style)
        {
            messageQueue.Enqueue(new ScreenMessage(text, duration, style));
        }

        public void Update()
        {
            if (Time.frameCount % 20 != 0) return;
            if (messageQueue.Count > 0 && Time.time - lastMessageTime > 1.5f)
            {
                var msg = messageQueue.Dequeue();
                ScreenMessages.PostScreenMessage(msg);
                lastMessageTime = Time.time;
            }
        }

        private void OnVesselRecovered(ProtoVessel pv, bool quick)
        {
            if (pv == null || pv.vesselRef == null) return;
            
            // SubOrbital or greater check
            if (pv.vesselRef.orbit.ApA < pv.vesselRef.mainBody.atmosphereDepth && pv.vesselRef.situation != Vessel.Situations.SUB_ORBITAL && pv.vesselRef.situation != Vessel.Situations.ORBITING && pv.vesselRef.situation != Vessel.Situations.ESCAPING)
                return;

            for (int i = 0; i < pv.protoPartSnapshots.Count; i++)
            {
                ProtoPartSnapshot p = pv.protoPartSnapshots[i];
                for (int j = 0; j < p.modules.Count; j++)
                {
                    if (p.modules[j].moduleName == "KEDModule")
                    {
                        AddMaturity(p.partName, KEDSettings.recoveryBonus); // Recovery Bonus
                        break;
                    }
                }
            }
        }

        public static float GetMaturity(string name) => (Instance != null && Instance.globalEngineMaturity.ContainsKey(name)) ? Instance.globalEngineMaturity[name] : 0f;

        public override void OnSave(ConfigNode node) 
        { 
            ConfigNode matNode = node.AddNode("MaturityData");
            foreach (var kvp in globalEngineMaturity) matNode.AddValue(kvp.Key, kvp.Value.ToString());
            
            ConfigNode seedNode = node.AddNode("ManufacturingSeeds");
            foreach (var kvp in manufacturingSeeds) seedNode.AddValue(kvp.Key, kvp.Value.ToString());

            ConfigNode flightsNode = node.AddNode("GlobalFlightsCount");
            foreach (var kvp in globalFlightsCount) flightsNode.AddValue(kvp.Key, kvp.Value.ToString());

            ConfigNode riskNode = node.AddNode("LineageRisk");
            foreach (var kvp in lineageRisk) riskNode.AddValue(kvp.Key, kvp.Value.ToString());
        }

        public override void OnLoad(ConfigNode node) 
        {
            globalEngineMaturity.Clear();
            manufacturingSeeds.Clear();
            if (node.HasNode("MaturityData"))
            {
                foreach (ConfigNode.Value val in node.GetNode("MaturityData").values)
                    if (float.TryParse(val.value, out float mp)) globalEngineMaturity[val.name] = mp;
            }
            if (node.HasNode("ManufacturingSeeds"))
            {
                foreach (ConfigNode.Value val in node.GetNode("ManufacturingSeeds").values)
                    if (int.TryParse(val.value, out int seed)) manufacturingSeeds[val.name] = seed;
            }
            if (node.HasNode("GlobalFlightsCount"))
            {
                foreach (ConfigNode.Value val in node.GetNode("GlobalFlightsCount").values)
                    if (int.TryParse(val.value, out int count)) globalFlightsCount[val.name] = count;
            }
            if (node.HasNode("LineageRisk"))
            {
                foreach (ConfigNode.Value val in node.GetNode("LineageRisk").values)
                    if (float.TryParse(val.value, out float risk)) lineageRisk[val.name] = risk;
            }
        }
    }

    // ==========================================
    // THE ENGINE MODULE (KED 3.0 ARCHITECTURE)
    // ==========================================
    public class KEDModule : PartModule, IModuleInfo
    {
        // --- PERSISTENCE ---
        [KSPField(isPersistant = true)] public int maturityLevelAtLaunch = 0;
        [KSPField(isPersistant = true)] public string serialNumber = "";
        [KSPField(isPersistant = true)] public string batchId = "";
        [KSPField(isPersistant = true)] public bool isLemon = false;
        [KSPField(isPersistant = true)] public bool isWeakUnit = false;
        [KSPField(isPersistant = true)] public bool isFailed = false;
        [KSPField(isPersistant = true)] public int failureMode = 0; // 1=Ignition, 2=Flameout, 3=Gimbal, 4=ThrustDrop, 5=Explode
        [KSPField(isPersistant = true)] public float cumulativeBurnSeconds = 0f;
        [KSPField(isPersistant = true)] public float ignitionFatigue = 0f;
        [KSPField(isPersistant = true)] public bool maturityStartAwarded = false;
        [KSPField(isPersistant = true)] public bool maturityBurnAwarded = false;
        [KSPField(isPersistant = true)] public bool diagnosticsRun = false;
        [KSPField(isPersistant = true)] public float atmSensitivityIndex = 1.0f;
        [KSPField(isPersistant = true)] public double failureTriggerTime = -1;
        [KSPField(isPersistant = true)] public int flightsCount = 0;
        [KSPField(isPersistant = true)] public int failuresCount = 0;
        [KSPField(isPersistant = true)] public bool wasRecovered = false;
        public float performanceMultiplier = 1.0f; // Calculated at runtime, non-persistent to avoid double-dipping
        [KSPField(isPersistant = true)] public int performanceScars = 0;
        [KSPField(isPersistant = true)] public int ignitionCount = 0;
        [KSPField(isPersistant = true)] public double srbIgnitionTime = -1.0;
        [KSPField(isPersistant = true)] public float lastCalculatedLemonProb = 0f;
        [KSPField(isPersistant = true)] public float srbFailureFuelThreshold = -1f;
        [KSPField(isPersistant = true)] public string activeFailuresMask = "0,0,0,0,0"; // Fix: Use commas to match parser
        [KSPField(isPersistant = true)] public double lastKEDUpdateUT = -1.0;
        [KSPField(isPersistant = true)] public double lastKnownFuelMass = -1.0;
        [KSPField(isPersistant = true)] public double turbineShutdownUT = -1.0;
        [KSPField(isPersistant = true)] public bool hasManualPrime = false;
        [KSPField(isPersistant = true)] public double pendingSpinUpDebt = 0;

        // --- INTERNAL OPTIMIZED STATE ---
        private int[] _activeFailures = new int[5];
        private VesselModule _cachedBTVesselModule;
        private PartModule _cachedBTEngine;
        private PartModule _cachedPTEngine;
        private float _fuelSyncTimer = 0f;

        // --- CACHED REFERENCES (NON-PERSISTENT) ---
        private List<ModuleEngines> engineModules = new List<ModuleEngines>();
        private ModuleGimbal cachedGimbal;
        private PartResource cachedSolidFuel;
        private List<float> prefabMaxThrusts = new List<float>();
        private List<FloatCurve> prefabAtmosphereCurves = new List<FloatCurve>();
        private float prefabGimbalRange = 0f;
        private bool isCascadeVictim = false;
        private bool isBurning = false;
        private bool lastFrameBurning = false;

        // --- UI ---
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Part S/N", groupName = "KED", groupDisplayName = "ENGINE TELEMETRY")]
        public string uiSerialNumber = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Tech Rating", groupName = "KED")]
        public string uiMaturity = "";
        [KSPField(guiActive = true, guiName = "Status", groupName = "KED")]
        public string uiState = "Nominal";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "ASI", groupName = "KED")]
        public string uiASI = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "QA Report", groupName = "KED")]
        public string uiBatchHint = "Unknown";
        [KSPField(guiActive = true, guiName = "Logbook", groupName = "KED")]
        public string uiHistory = "";
        [KSPField(guiActive = true, guiName = "Yield", groupName = "KED")]
        public string uiPerformance = "100%";
        [KSPField(guiActive = true, guiName = "Turbine", groupName = "KED")]
        public string uiTurbineStatus = "Ready";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Spin-up Req", groupName = "KED")]
        public string uiSpinUpReq = "0 units";

        // --- DEBUG UI ---
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Debug Status", groupName = "KED_Debug", groupDisplayName = "DEBUG: ENGINE CONTROL")]
        public string uiDebugStatusBlock = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Debug System", groupName = "KED_Debug")]
        public string uiDebugSystemBlock = "";

        private int debugTickOffset;

        [KSPField(isPersistant = true)] public EngineArchetype archetype = EngineArchetype.Thermodynamic;
        private bool lastIgnited = false;
        public bool IsFailed => HasFailure(1) || HasFailure(2) || HasFailure(3) || HasFailure(4) || HasFailure(5);

        private void SyncMasksToArrays()
        {
            if (_activeFailures == null) _activeFailures = new int[5];
            string[] fSplit = (activeFailuresMask ?? "0,0,0,0,0").Split(',');
            for (int i = 0; i < _activeFailures.Length; i++)
            {
                if (i < fSplit.Length && int.TryParse(fSplit[i], out int val)) _activeFailures[i] = val;
                else _activeFailures[i] = 0;
            }
        }

        private void SyncArraysToMasks()
        {
            activeFailuresMask = string.Join(",", System.Array.ConvertAll(_activeFailures, x => x.ToString()));
        }

        public int GetFailureCount(int mode)
        {
            if (mode < 1 || mode > 5) return 0;
            return _activeFailures[mode - 1];
        }

        public bool HasFailure(int mode) => GetFailureCount(mode) > 0;

        private void SetFailure(int mode, bool active)
        {
            if (mode < 1 || mode > 5) return;
            if (active) _activeFailures[mode - 1]++;
            else _activeFailures[mode - 1] = UnityEngine.Mathf.Max(0, _activeFailures[mode - 1] - 1);
            SyncArraysToMasks();
            isFailed = false;
            for (int i = 0; i < _activeFailures.Length; i++) if (_activeFailures[i] != 0) { isFailed = true; break; }
        }

        private static readonly HashSet<string> HypergolicPropellants = new HashSet<string> { "Aerozine50", "NTO", "MMH", "UDMH", "NitricAcid", "Hydrazine" };
        private static readonly HashSet<string> ElectricPropellants = new HashSet<string> { "XenonGas", "ArgonGas", "LqdArgon", "KryptonGas", "LqdKrypton", "NeonGas", "LqdNeon", "Lithium" };

        public override void OnStart(StartState state)
        {
            debugTickOffset = UnityEngine.Random.Range(0, 30);
            SyncMasksToArrays();
            if (KEDIntegration.HasBT && vessel != null)
            {
                _cachedBTVesselModule = KEDIntegration.FindVesselModule(vessel, KEDIntegration.GetBTVesselModuleType());
                string btName = KEDIntegration.GetBTEngineTypeName();
                if (!string.IsNullOrEmpty(btName)) _cachedBTEngine = part.Modules.GetModule(btName);
            }
            if (KEDIntegration.HasPT)
            {
                string ptName = KEDIntegration.GetPTEngineTypeName();
                if (!string.IsNullOrEmpty(ptName)) _cachedPTEngine = part.Modules.GetModule(ptName);
            }
            engineModules = part.FindModulesImplementing<ModuleEngines>();
            if (engineModules.Count == 0) return;

            cachedGimbal = part.FindModuleImplementing<ModuleGimbal>();
            if (cachedGimbal != null)
            {
                var prefabGimbal = part.partInfo.partPrefab.FindModuleImplementing<ModuleGimbal>();
                if (prefabGimbal != null) prefabGimbalRange = prefabGimbal.gimbalRange;
            }

            foreach (var e in engineModules)
            {
                var prefabEngine = part.partInfo.partPrefab?.FindModulesImplementing<ModuleEngines>().Find(m => m.engineID == e.engineID);
                if (prefabEngine != null)
                {
                    prefabMaxThrusts.Add(prefabEngine.maxThrust);
                    prefabAtmosphereCurves.Add(prefabEngine.atmosphereCurve);
                }
                else
                {
                    prefabMaxThrusts.Add(e.maxThrust);
                    prefabAtmosphereCurves.Add(e.atmosphereCurve);
                }
            }

            if (part.Resources.Contains("SolidFuel")) cachedSolidFuel = part.Resources["SolidFuel"];

            // Archetype & ASI logic: 
            // If they are at their default "unset" values (Thermodynamic/1.0), 
            // we run the automatic detection logic.
            if (archetype == EngineArchetype.Thermodynamic) DetermineArchetype();
            if (Math.Abs(atmSensitivityIndex - 1.0f) < 0.001f) CalculateASI();

            ApplyHeritageMP();

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (string.IsNullOrEmpty(batchId)) 
                {
                    InitializeBatch();
                    flightsCount++;
                }
                maturityLevelAtLaunch = GetMaturityLevel();
                uiSerialNumber = serialNumber;
                uiASI = atmSensitivityIndex.ToString("F2");
                
                ApplyPerformanceScars();

                if (isFailed) ApplyFailureState(failureMode);

                // --- CATCH-UP MECHANIC ---
                double now = Planetarium.GetUniversalTime();
                if (lastKEDUpdateUT > 0)
                {
                    double offlineSeconds = now - lastKEDUpdateUT;
                    double currentFuel = GetVesselPropellantMass();

                    // Only process a meaningful gap; BT must be present for engine to have burned offline
                    if (offlineSeconds > 5.0 && KEDIntegration.HasBT)
                    {
                        if (CheckFuelConsumedSinceLastUT(currentFuel))
                        {
                            ProcessOfflineBurn(offlineSeconds);
                        }
                    }
                    lastKEDUpdateUT = now;
                    lastKnownFuelMass = currentFuel;
                }
                else
                {
                    lastKnownFuelMass = GetVesselPropellantMass();
                    lastKEDUpdateUT = now; // Sanitize timer for new vessels to avoid phantom catch-up
                }
            }
            RefreshUI();
            UpdateDebugUI();
        }

        private double GetVesselPropellantMass()
        {
            if (engineModules.Count == 0 || vessel == null || vessel.resourcePartSet == null) return 0;
            double totalAmount = 0;
            foreach (var prop in engineModules[0].propellants)
            {
                vessel.GetConnectedResourceTotals(prop.id, out double amount, out double max);
                totalAmount += amount;
            }
            return totalAmount;
        }

        private bool CheckFuelConsumedSinceLastUT(double currentFuel)
        {
            // If fuel has decreased since last known state, assume a burn occurred
            if (lastKnownFuelMass >= 0 && currentFuel < lastKnownFuelMass - 0.001) return true;
            return false;
        }

        private void ProcessOfflineBurn(double burnSeconds)
        {
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);
            // 1. Accumulate wear (exact UT-delta)
            cumulativeBurnSeconds += (float)burnSeconds;

            // 2. Maturity awards (re-evaluated during catch-up)
            if (!maturityStartAwarded && (vessel.situation == Vessel.Situations.FLYING || vessel.altitude > 100))
            {
                AwardVesselMaturity(KEDSettings.startBonus, true);
            }
            if (!maturityBurnAwarded && burnSeconds >= 60.0)
            {
                AwardVesselMaturity(KEDSettings.burnBonus, false);
            }

            // 3. Weak Unit deterministic trigger
            if (isWeakUnit)
            {
                // Timer-based trigger
                if (failureTriggerTime > 0 && cumulativeBurnSeconds >= failureTriggerTime)
                {
                    TriggerFailure();
                    ScreenMessages.PostScreenMessage(
                        $"<color=#FF3333>[KED] {part.partInfo.title}: Engine failed during offline burn at T+{cumulativeBurnSeconds:F0}s.</color>",
                        8f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                // SRB Fuel-threshold trigger
                if (archetype == EngineArchetype.Solid && srbFailureFuelThreshold > 0)
                {
                    double currentFuel = GetVesselPropellantMass();
                    double maxFuel = 0;
                    if (cachedSolidFuel != null) maxFuel = cachedSolidFuel.maxAmount;
                    else
                    {
                        vessel.GetConnectedResourceTotals(engineModules[0].propellants[0].id, out double amount, out double max);
                        maxFuel = max;
                    }

                    if (maxFuel > 0 && (currentFuel / maxFuel) <= srbFailureFuelThreshold)
                    {
                        TriggerFailure(5); // Explode
                        ScreenMessages.PostScreenMessage(
                            $"<color=#FF3333>[KED] {part.partInfo.title}: SRB breach detected during offline burn!</color>",
                            8f, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                }
            }

            // 4. Monopropellant catalyst exhaustion (binomial-scaled)
            if (archetype == EngineArchetype.Monopropellant)
            {
                float limit = KEDSettings.catalystServiceLimit;
                if (maturityLevelAtLaunch >= 3) limit *= 2f;
                if (cumulativeBurnSeconds > limit)
                {
                    float overtime = cumulativeBurnSeconds - limit;
                    float failProbPerSec = (overtime / 10f) * 0.01f * KEDSettings.globalRiskMultiplier;
                    double p_survival = Math.Pow(1.0 - (double)failProbPerSec, burnSeconds);
                    if (UnityEngine.Random.value > p_survival) TriggerFailure(4);
                }
            }

            // 5. ASI Flow Separation Check
            if (atmSensitivityIndex > KEDSettings.asiFlameoutThreshold && pressure > KEDSettings.asiPressureThreshold && archetype != EngineArchetype.Nuclear && archetype != EngineArchetype.Electric && archetype != EngineArchetype.Exotic) 
            {
                float severity = (atmSensitivityIndex - 1.25f) * pressure;
                float damageProbPerSec = 0.05f * severity * KEDSettings.globalRiskMultiplier;
                double p_survival = Math.Pow(1.0 - (double)damageProbPerSec, burnSeconds);
                
                if (UnityEngine.Random.value > p_survival) 
                {
                    TriggerFailure(UnityEngine.Random.value > 0.5f ? 2 : 3); 
                }
            }

            RefreshUI();
        }

        private void ApplyPerformanceScars()
        {
            performanceMultiplier = 1.0f;
            float penalty = (1.0f - (performanceScars * 0.01f)) * performanceMultiplier;

            for (int i = 0; i < engineModules.Count; i++)
            {
                var e = engineModules[i];
                var baseCurve = prefabAtmosphereCurves[i];
                FloatCurve newCurve = new FloatCurve();
                foreach (var key in baseCurve.Curve.keys)
                {
                    newCurve.Add(key.time, key.value * penalty);
                }
                e.atmosphereCurve = newCurve;
                e.maxThrust = prefabMaxThrusts[i] * performanceMultiplier;
            }
        }

        public void Update()
        {
            if (KEDSettings.debugMode && HighLogic.LoadedSceneIsFlight)
            {
                if ((Time.frameCount + debugTickOffset) % 30 == 0)
                {
                    int seed = 0;
                    if (KEDScenario.Instance.manufacturingSeeds.ContainsKey(part.partInfo.name)) seed = KEDScenario.Instance.manufacturingSeeds[part.partInfo.name];
                    
                    float ignProb = ignitionFatigue * KEDSettings.globalRiskMultiplier;
                    if (maturityLevelAtLaunch >= 1) ignProb *= 0.5f;
                    if (maturityLevelAtLaunch >= 4) ignProb = Mathf.Min(0.01f, ignProb);

                    uiDebugStatusBlock = $"Lemon: {isLemon} | Weak: {isWeakUnit} | Fail: {isFailed}\n" +
                                         $"Clock: {cumulativeBurnSeconds:F1}s / {failureTriggerTime:F1}s\n" +
                                         $"Fatigue: {ignitionFatigue:F4} | P(Ignition): {(1f - ignProb):P1}\n" +
                                         $"Seed: {seed} | P(Lemon): {lastCalculatedLemonProb:P1}";

                    string tactInfo = "";
                    if (archetype == EngineArchetype.Monopropellant) tactInfo = $"\nService: {cumulativeBurnSeconds:F0}s / {KEDSettings.catalystServiceLimit:F0}s";
                    if (archetype == EngineArchetype.Hypergolic) tactInfo = $"\nIgnitions: {ignitionCount}";

                    string batchStatus = "";
                    if (string.IsNullOrEmpty(batchId)) batchStatus = "QA: Uninitialized";
                    else batchStatus = isLemon ? "QA: <color=#FFCC00>LEMON BATCH</color>" : "QA: <color=#00FF00>PRISTINE BATCH</color>";

                    string unitStatus = "";
                    if (string.IsNullOrEmpty(batchId)) unitStatus = "Unit: Uninitialized";
                    else unitStatus = isWeakUnit ? "Unit: <color=#FF3333>WEAK UNIT</color>" : "Unit: <color=#00FF00>NOMINAL UNIT</color>";

                    uiDebugSystemBlock = $"Type: {archetype} | ASI: {atmSensitivityIndex:F2} | Scars: {performanceScars}\n" +
                                         $"Perf: {performanceMultiplier:P0}{tactInfo}\n" +
                                         $"{batchStatus} | {unitStatus}";
                }
            }

            // --- TURBINE DYNAMIC UI ---
            bool isBiprop = archetype == EngineArchetype.Bipropellant;
            bool isAdv = archetype == EngineArchetype.Advanced;
            
            if (((KEDSettings.enableCryoSpinUp && isBiprop) || (KEDSettings.enableAdvancedSpinUp && isAdv)) && engineModules.Count > 0)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    float currentThrust = engineModules[0].maxThrust;
                    float maturityMod = (GetMaturityLevel() >= 3) ? 0.8f : (GetMaturityLevel() >= 1 ? 1.0f : 1.2f);
                    float pAtm = (float)vessel.mainBody.GetPressure(vessel.altitude) / 101.325f;

                    if (isBurning) uiTurbineStatus = "<color=#00FF00>Active</color>";
                    else if (Planetarium.GetUniversalTime() - turbineShutdownUT < KEDSettings.cryoSpinDownGrace) uiTurbineStatus = "<color=#FFFF00>Spin-down</color>";
                    else uiTurbineStatus = "Ready";

                    if (isBiprop)
                    {
                        double ln2Cost = (currentThrust / 100f) * (1f + pAtm) * maturityMod * KEDSettings.cryoSpinUpMultiplier;
                        uiSpinUpReq = hasManualPrime ? "0.0 LN2 (Primed)" : $"{ln2Cost:F2} LN2";
                    }
                    else if (isAdv)
                    {
                        double ecCost = (currentThrust / 10f) * (1f + pAtm) * maturityMod * KEDSettings.advancedSpinUpMultiplier;
                        uiSpinUpReq = hasManualPrime ? "0.0 EC (Primed)" : $"{ecCost:F2} EC";
                    }
                }
                else if (HighLogic.LoadedSceneIsEditor)
                {
                    float currentThrust = engineModules[0].maxThrust;
                    float maturityMod = (GetMaturityLevel() >= 3) ? 0.8f : (GetMaturityLevel() >= 1 ? 1.0f : 1.2f);
                    
                    if (isBiprop)
                    {
                        double ln2Cost = (currentThrust / 100f) * 1.0f * maturityMod * KEDSettings.cryoSpinUpMultiplier;
                        uiSpinUpReq = $"{ln2Cost:F2} LN2";
                    }
                    else if (isAdv)
                    {
                        double ecCost = (currentThrust / 10f) * 1.0f * maturityMod * KEDSettings.advancedSpinUpMultiplier;
                        uiSpinUpReq = $"{ecCost:F2} EC";
                    }
                }
            }
        }

        private void UpdateDebugUI()
        {
            bool show = KEDSettings.debugMode;
            Fields["uiDebugStatusBlock"].guiActive = show;
            Fields["uiDebugStatusBlock"].guiActiveEditor = show;
            Fields["uiDebugSystemBlock"].guiActive = show;
            Fields["uiDebugSystemBlock"].guiActiveEditor = show;

            Events["ForceLemon"].guiActive = (show && !isLemon);
            Events["ForcePerfect"].guiActive = show;
            Events["ForceNewBatch"].guiActive = show;
            Events["ResetFatigue"].guiActive = (show && ignitionFatigue > 0);
            Events["CycleFailure"].guiActive = (show && !isFailed);
            Events["InstantRepair"].guiActive = (show && (isFailed || performanceScars > 0));
            Events["GrantRepairKits"].guiActive = show;
            Events["KillEngine"].guiActive = show;
            Events["AddMaturityDebug"].guiActiveEditor = show;
            Events["ResetMaturityDebug"].guiActive = show;
            Events["ResetMaturityDebug"].guiActiveEditor = show;
            Events["AddScarDebug"].active = show;
            Events["ClearScarsDebug"].active = (show && performanceScars > 0);
            Events["AddBurnTimeDebug"].active = (show && archetype == EngineArchetype.Monopropellant);
            Events["ForceExhaustionDebug"].active = (show && archetype == EngineArchetype.Monopropellant);
            Events["DumpModuleInfo"].guiActive = show;
            Events["DumpModuleInfo"].guiActiveEditor = show;
            Events["ToggleCryoSpinUp"].guiActive = show;
            Events["ToggleCryoSpinUp"].guiActiveEditor = show;
        }

        private string GetLevelName(int lvl)
        {
            if (archetype == EngineArchetype.Exotic) 
                return lvl >= 1 ? "Heritage" : "Prototype";
            
            if (archetype == EngineArchetype.Electric || archetype == EngineArchetype.Nuclear) 
                return lvl >= 2 ? "Heritage" : (lvl == 1 ? "Flight Rated" : "Experimental");
            
            if (archetype == EngineArchetype.Solid) 
                return lvl >= 2 ? "Safe Abort" : (lvl == 1 ? "Reinforced" : "Batch Tested");
            
            if (archetype == EngineArchetype.Monopropellant || archetype == EngineArchetype.Hypergolic) 
                return lvl >= 3 ? "Heritage" : (lvl == 2 ? "Proven" : (lvl == 1 ? "Stabilized" : "Baseline"));
            
            if (archetype == EngineArchetype.Airbreathing) 
                return lvl >= 3 ? "Heritage" : (lvl == 2 ? "Cleared" : (lvl == 1 ? "Bench Tested" : "Prototype"));
            
            // Bipropellant, Thermodynamic, Advanced
            if (lvl >= 4) return "Masterwork";
            if (lvl == 3) return "Heritage";
            if (lvl == 2) return "Proven";
            if (lvl == 1) return "Qualified";
            return "Experimental";
        }

        private int GetMaturityLevel()
        {
            float mp = KEDScenario.GetMaturity(part.partInfo.name);
            
            switch (archetype)
            {
                case EngineArchetype.Exotic:
                    return mp >= KEDSettings.exotic_L1 ? 1 : 0;

                case EngineArchetype.Nuclear:
                    if (mp >= KEDSettings.nuc_L2) return 2;
                    if (mp >= KEDSettings.nuc_L1) return 1;
                    return 0;

                case EngineArchetype.Electric:
                    if (mp >= KEDSettings.elec_L2) return 2;
                    if (mp >= KEDSettings.elec_L1) return 1;
                    return 0;

                case EngineArchetype.Solid:
                    if (mp >= KEDSettings.solid_L2) return 2;
                    if (mp >= KEDSettings.solid_L1) return 1;
                    return 0;

                case EngineArchetype.Monopropellant:
                    if (mp >= KEDSettings.mono_L3) return 3;
                    if (mp >= KEDSettings.mono_L2) return 2;
                    if (mp >= KEDSettings.mono_L1) return 1;
                    return 0;

                case EngineArchetype.Hypergolic:
                    if (mp >= KEDSettings.hyper_L3) return 3;
                    if (mp >= KEDSettings.hyper_L2) return 2;
                    if (mp >= KEDSettings.hyper_L1) return 1;
                    return 0;

                case EngineArchetype.Airbreathing:
                    if (mp >= KEDSettings.air_L3) return 3;
                    if (mp >= KEDSettings.air_L2) return 2;
                    if (mp >= KEDSettings.air_L1) return 1;
                    return 0;

                case EngineArchetype.Bipropellant:
                    if (mp >= KEDSettings.biprop_L4) return 4;
                    if (mp >= KEDSettings.biprop_L3) return 3;
                    if (mp >= KEDSettings.biprop_L2) return 2;
                    if (mp >= KEDSettings.biprop_L1) return 1;
                    return 0;

                case EngineArchetype.Advanced:
                    if (mp >= KEDSettings.adv_L4) return 4;
                    if (mp >= KEDSettings.adv_L3) return 3;
                    if (mp >= KEDSettings.adv_L2) return 2;
                    if (mp >= KEDSettings.adv_L1) return 1;
                    return 0;

                case EngineArchetype.Thermodynamic:
                    if (mp >= KEDSettings.thermo_L4) return 4;
                    if (mp >= KEDSettings.thermo_L3) return 3;
                    if (mp >= KEDSettings.thermo_L2) return 2;
                    if (mp >= KEDSettings.thermo_L1) return 1;
                    return 0;

                default:
                    return 0;
            }
        }



        public override void OnLoad(ConfigNode node) { base.OnLoad(node); SyncMasksToArrays(); }
        public override void OnSave(ConfigNode node) { SyncArraysToMasks(); base.OnSave(node); }

        private void RefreshUI()
        {
            int lvl = GetMaturityLevel();
            if (maturityLevelAtLaunch != lvl)
                uiMaturity = $"Mk.{maturityLevelAtLaunch} (Live: Mk.{lvl})";
            else
                uiMaturity = $"Mk.{lvl} ({GetLevelName(lvl)})";
            
            uiSerialNumber = serialNumber;
            
            float successRate = flightsCount > 0 ? (1f - (float)failuresCount / flightsCount) * 100f : 100f;
            uiHistory = $"{flightsCount} Flights | {successRate:F0}% Yield";
            uiPerformance = $"{performanceMultiplier:P0}";
            
            // Smart Visibility
            Fields["uiPerformance"].guiActive = (performanceMultiplier < 0.99f);
            Fields["uiBatchHint"].guiActive = false;
            Fields["uiBatchHint"].guiActiveEditor = true;
            Fields["uiASI"].guiActive = false;
            Fields["uiASI"].guiActiveEditor = false;

            bool showSpinUp = (KEDSettings.enableCryoSpinUp && archetype == EngineArchetype.Bipropellant) || (KEDSettings.enableAdvancedSpinUp && archetype == EngineArchetype.Advanced);
            Fields["uiTurbineStatus"].guiActive = showSpinUp;
            Fields["uiSpinUpReq"].guiActive = showSpinUp;
            Fields["uiSpinUpReq"].guiActiveEditor = showSpinUp;

            RefreshFaultUI();
        }

        public string GetModuleTitle() => "Kerbal Engine Dynamics";
        public string GetPrimaryField() => $"Archetype: {archetype}";

        private void RefreshFaultUI()
        {
            if (!IsFailed) 
            { 
                uiState = "<color=#00FF00>Nominal</color>"; 
                return; 
            }

            List<string> faults = new List<string>();
            if (GetFailureCount(1) > 0) faults.Add("Ignition");
            if (GetFailureCount(2) > 0) faults.Add("Flameout");
            if (GetFailureCount(3) > 0) faults.Add("Gimbal");
            
            if (HasFailure(5)) 
            {
                uiState = "<color=#FF3333>CRITICAL (Explosion)</color>";
            }
            else if (HasFailure(1) || HasFailure(2)) 
            {
                uiState = $"<color=#FF3333>FAULT ({string.Join(", ", faults.ToArray())})</color>";
            }
            else 
            {
                uiState = $"<color=#FFCC00>DEGRADED ({string.Join(", ", faults.ToArray())})</color>";
            }

            Events["JuryRig"].active = HasFailure(1) || HasFailure(2) || HasFailure(3);
            Events["CatalystSwap"].active = (archetype == EngineArchetype.Monopropellant && maturityLevelAtLaunch >= 1);
            Events["NitrogenPurge"].active = (archetype == EngineArchetype.Hypergolic && maturityLevelAtLaunch >= 1);
            Events["CycleValves"].active = (archetype == EngineArchetype.Hypergolic && maturityLevelAtLaunch >= 1);
        }

        private void ApplyHeritageMP()
        {
            if (KEDScenario.GetMaturity(part.partInfo.name) > 0) return;

            // Find best part in SAME archetype
            float bestMP = 0;
            foreach (var kvp in KEDScenario.Instance.globalEngineMaturity)
            {
                AvailablePart ap = PartLoader.getPartInfoByName(kvp.Key);
                if (ap == null || ap.partPrefab == null) continue;
                
                var m = ap.partPrefab.FindModuleImplementing<KEDModule>();
                if (m != null)
                {
                    // Force archetype determination on prefab if needed
                    m.DetermineArchetype(); 
                    if (m.archetype == this.archetype)
                    {
                        if (kvp.Value > bestMP) bestMP = kvp.Value;
                    }
                }
            }
            if (bestMP > 0) 
            {
                float amount = bestMP * KEDSettings.heritageTransferRate;
                if (KEDScenario.Instance.globalEngineMaturity.ContainsKey(part.partInfo.name)) 
                    KEDScenario.Instance.globalEngineMaturity[part.partInfo.name] += amount;
                else 
                    KEDScenario.Instance.globalEngineMaturity.Add(part.partInfo.name, amount);
            }
        }

        private void DetermineArchetype()
        {
            // Use local find instead of cached list to support prefab calls
            var engines = part.FindModulesImplementing<ModuleEngines>();
            if (engines.Count == 0) return;

            // 1. Nuclear (EnrichedUranium + non-Uranium propellant)
            if (part.Resources.Contains("EnrichedUranium")) 
            { 
                archetype = EngineArchetype.Nuclear; 
                return; 
            }

            // 2. Hybrid / Multi-mode Advanced
            // If an engine has both an air-breathing mode and a vacuum/bipropellant mode (e.g. RAPIER), 
            // it is categorized as Advanced.
            if (engines.Count > 1)
            {
                bool hasAir = false;
                bool hasNonAir = false;
                foreach (var eMod in engines)
                {
                    bool isAir = false;
                    foreach (var prop in eMod.propellants)
                        if (prop.name == "IntakeAir" || prop.name == "IntakeAtm") { isAir = true; break; }
                    
                    if (isAir) hasAir = true;
                    else hasNonAir = true;
                }
                if (hasAir && hasNonAir) 
                { 
                    archetype = EngineArchetype.Advanced; 
                    return; 
                }
            }

            // 3. Airbreathing (Intake resources)
            // Priority over Exotic/Advanced because Jets have high ISP (3000+) in KSP
            foreach (var eMod in engines)
            {
                foreach (var prop in eMod.propellants)
                {
                    if (prop.name == "IntakeAir" || prop.name == "IntakeAtm") 
                    { 
                        archetype = EngineArchetype.Airbreathing; 
                        return; 
                    }
                }
            }

            // 4. Electric (Standard electric types or Noble gases)
            // Priority over Exotic because Ion engines have high ISP (4200)
            foreach (var eMod in engines)
            {
                bool isElec = eMod.engineType == EngineType.Electric;
                if (!isElec)
                {
                    bool hasEC = false;
                    bool hasNoble = false;
                    foreach (var prop in eMod.propellants)
                    {
                        if (prop.name == "ElectricCharge") hasEC = true;
                        if (ElectricPropellants.Contains(prop.name)) hasNoble = true;
                    }
                    if (hasEC && hasNoble) isElec = true;
                }
                if (isElec) { archetype = EngineArchetype.Electric; return; }
            }

            ModuleEngines e = engines[0];
            float vacIsp = e.atmosphereCurve.Evaluate(0f);

            // 5. Exotic (ISP > 2850 or specific resources)
            if (vacIsp > 3000f || part.Resources.Contains("Antimatter") || part.Resources.Contains("Gravioli") || part.Resources.Contains("WarpDrive"))
            { 
                archetype = EngineArchetype.Exotic; 
                return; 
            }

            // 6. Advanced (ISP > 500)
            if (vacIsp > 500f) 
            { 
                archetype = EngineArchetype.Advanced; 
                return; 
            }

            // 7. Hypergolic (Chemical list)
            if (e.propellants.Count >= 2)
            {
                foreach (var prop in e.propellants)
                    if (HypergolicPropellants.Contains(prop.name)) { archetype = EngineArchetype.Hypergolic; return; }
            }

            // 8. Monopropellant (Solo fuel)
            if (e.propellants.Count == 1 && (e.propellants[0].name == "MonoPropellant" || e.propellants[0].name == "Hydrazine"))
            { archetype = EngineArchetype.Monopropellant; return; }

            // 9. Bipropellant (Oxidizer base)
            foreach (var prop in e.propellants)
                if (prop.name == "LqdOxygen" || prop.name == "Oxidizer") { archetype = EngineArchetype.Bipropellant; return; }

            // 10. Solid (SolidFuel / SolidBooster type)
            if (e.engineType == EngineType.SolidBooster || part.Resources.Contains("SolidFuel")) 
            { 
                archetype = EngineArchetype.Solid; 
                return; 
            }

            // 11. Thermodynamic (Standard liquid fallback)
            archetype = EngineArchetype.Thermodynamic;
        }

        private void CalculateASI()
        {
            var engines = part.FindModulesImplementing<ModuleEngines>();
            if (engines.Count == 0) return;
            float vac = engines[0].atmosphereCurve.Evaluate(0f);
            float asl = engines[0].atmosphereCurve.Evaluate(1f);
            atmSensitivityIndex = vac / Mathf.Max(asl, 0.1f);
        }

        private void InitializeBatch()
        {
            // 1. Look for siblings on the vessel
            List<KEDModule> siblings = new List<KEDModule>();
            foreach (Part p in vessel.parts)
            {
                if (p.partInfo.name == part.partInfo.name)
                {
                    var m = p.FindModuleImplementing<KEDModule>();
                    if (m != null) siblings.Add(m);
                }
            }

            // 2. Check if a sibling has ALREADY initialized the batch for this vessel
            foreach (var m in siblings)
            {
                if (!string.IsNullOrEmpty(m.batchId) && m != this)
                {
                    // We are part of an existing batch. Copy the batch-wide stats and exit.
                    this.batchId = m.batchId;
                    this.isLemon = m.isLemon;
                    this.uiBatchHint = m.uiBatchHint;
                    this.lastCalculatedLemonProb = m.lastCalculatedLemonProb;
                    return; 
                }
            }

            // 3. If no sibling has a batchId, WE are the Batch Leader. 
            // Generate the master Batch ID.
            batchId = $"{vessel.id}_{part.partInfo.name}";
            
            // Generate Serial Number (kept local to this specific engine)
            double ut = Planetarium.GetUniversalTime();
            int year = (int)(ut / KSPUtil.dateTimeFormatter.Year) + 1;
            int day = (int)((ut % KSPUtil.dateTimeFormatter.Year) / KSPUtil.dateTimeFormatter.Day) + 1;
            string partSlug = part.partInfo.name.Substring(0, Mathf.Min(3, part.partInfo.name.Length)).ToUpper();
            serialNumber = $"Y{year}-D{day}-{partSlug}-{UnityEngine.Random.Range(100, 999)}";

            // 4. Roll for the entire batch's fate
            int engineCount = siblings.Count;
            float anchor = GetMaturityAnchor();
            float lemonProb = (anchor + (engineCount - 1) * KEDSettings.batchLemonProbScale) * KEDSettings.globalRiskMultiplier;
            
            if (KEDScenario.Instance != null)
            {
                if (KEDScenario.Instance.lineageRisk.TryGetValue(part.partInfo.name, out float risk)) lemonProb += risk;
                if (KEDSettings.enableAging && KEDScenario.Instance.globalFlightsCount.TryGetValue(part.partInfo.name, out int globalFlights))
                {
                    if (globalFlights > KEDSettings.agingFlightThreshold)
                        lemonProb += (globalFlights - KEDSettings.agingFlightThreshold) * KEDSettings.agingFactorInc;
                }
                KEDScenario.Instance.AddFlight(part.partInfo.name);
            }

            lastCalculatedLemonProb = lemonProb;
            isLemon = UnityEngine.Random.value < lemonProb;
            
            if (KEDScenario.Instance != null) 
                KEDScenario.Instance.RecordBatchResult(part.partInfo.name, isLemon);

            if (isLemon) uiBatchHint = UnityEngine.Random.value < 0.7f ? "Slight variance detected in turbopump alignment." : "Batch vibration signature above nominal.";
            else uiBatchHint = "Factory QA: All systems nominal.";

            // 5. SYNC THE BATCH FATE TO ALL SIBLINGS immediately
            foreach (var m in siblings)
            {
                m.batchId = this.batchId;
                m.isLemon = this.isLemon;
                m.uiBatchHint = this.uiBatchHint;
                m.lastCalculatedLemonProb = this.lastCalculatedLemonProb;
                // Do NOT sync isWeakUnit here. We want controlled chaos.
            }

            // 6. Finally, distribute the physical "Weak Unit" curses among the synced batch
            if (isLemon)
            {
                float roll = UnityEngine.Random.value;
                if (roll < 0.80f) AssignWeakUnit(1);
                else if (roll < 0.95f) AssignWeakUnit(2);
                else ApplyDegradedBatch();
            }
            
            KEDScenario.Instance.manufacturingSeeds[part.partInfo.name] = UnityEngine.Random.Range(0, 1000000);
        }

        private void AssignWeakUnit(int count)
        {
            List<KEDModule> modules = new List<KEDModule>();
            foreach (Part p in vessel.parts)
            {
                if (p.partInfo.name == part.partInfo.name)
                {
                    var m = p.FindModuleImplementing<KEDModule>();
                    if (m != null) modules.Add(m);
                }
            }
            
            for (int i = 0; i < count && modules.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, modules.Count);
                KEDModule m = modules[idx];
                m.isWeakUnit = true;

                if (m.archetype == EngineArchetype.Solid)
                {
                    float r1 = UnityEngine.Random.Range(0.3f, 0.6f);
                    float r2 = UnityEngine.Random.Range(0.3f, 0.6f);
                    m.srbFailureFuelThreshold = (r1 + r2) / 2f;
                    m.failureTriggerTime = -1;
                }
                else
                {
                    m.failureTriggerTime = UnityEngine.Random.Range(15f, 50f);
                }

                modules.RemoveAt(idx);
            }
        }

        private void ApplyDegradedBatch()
        {
            foreach (Part p in vessel.parts)
            {
                if (p.partInfo.name == part.partInfo.name)
                {
                    var m = p.FindModuleImplementing<KEDModule>();
                    if (m != null)
                    {
                        m.ApplyFailureState(4); // Apply Thrust Drop penalty and UI immediately
                    }
                }
            }
        }

        private float GetMaturityAnchor()
        {
            int lvl = GetMaturityLevel();
            
            // Default Anchor Logic based on Roadmaps
            if (archetype == EngineArchetype.Exotic) 
                return 0.002f; // Nearly perfect out of the box (Exotic doesn't have anchor shifts in design, but 0.2% is mentioned for Elec/Nuc)
            
            if (archetype == EngineArchetype.Electric || archetype == EngineArchetype.Nuclear)
                return lvl >= 2 ? 0.002f : (lvl == 1 ? 0.015f : 0.03f);
            
            if (archetype == EngineArchetype.Solid)
                return lvl >= 1 ? 0.025f : 0.05f; // Pressure Buffered
            
            if (archetype == EngineArchetype.Airbreathing)
                return lvl >= 3 ? 0.01f : 0.05f;

            if (archetype == EngineArchetype.Monopropellant || archetype == EngineArchetype.Hypergolic)
                return lvl >= 3 ? 0.005f : 0.03f;

            // Bipropellant, Thermodynamic, Advanced (The Vanguard)
            if (lvl >= 4) return 0.005f; // Golden Batch
            if (lvl >= 1) return 0.04f;
            return 0.05f;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || engineModules.Count == 0) return;

            // --- MULTI-FAULT ENFORCEMENT ---
            if (HasFailure(1) || HasFailure(2) || HasFailure(5))
            {
                for (int i = 0; i < engineModules.Count; i++) 
                { 
                    if (engineModules[i].EngineIgnited) engineModules[i].Shutdown(); 
                    if (engineModules[i].allowRestart) engineModules[i].allowRestart = false; 
                    if (!engineModules[i].flameout) engineModules[i].flameout = true;
                }
            }

            if (HasFailure(3) && cachedGimbal != null) { cachedGimbal.gimbalLock = true; }

            if (HasFailure(4))
            {
                for (int i = 0; i < engineModules.Count; i++) { engineModules[i].maxThrust = prefabMaxThrusts[i] * performanceMultiplier; }
            }

            isBurning = false;
            for (int i = 0; i < engineModules.Count; i++)
            {
                var e = engineModules[i];
                if (e.EngineIgnited && e.currentThrottle > 0.01f && !e.flameout) { isBurning = true; break; }
            }

            // Background/Persistent Thrust Overrides:
            // In the active flight scene (unpacked), we only trust BT/PT if the engine has been 
            // ignited/staged at least once. If packed, we assume the mods are handling the on-rails burn.
            bool anyIgnited = false;
            for (int i = 0; i < engineModules.Count; i++) if (engineModules[i].EngineIgnited) { anyIgnited = true; break; }
            bool canBurnInMod = vessel.packed || anyIgnited; 

            // Persistent Thrust: engine may be driving thrust on-rails when vanilla appears off
            if (!isBurning && KEDIntegration.HasPT && canBurnInMod)
            {
                isBurning = KEDIntegration.IsEngineActivePT(_cachedPTEngine);
            }

            // Background Thrust: vessel is packed+thrusting
            if (!isBurning && KEDIntegration.HasBT && canBurnInMod)
            {
                isBurning = KEDIntegration.IsVesselBurningBT(this.vessel, _cachedBTEngine, _cachedBTVesselModule);
            }

            double now = Planetarium.GetUniversalTime();
            double deltaT = (lastKEDUpdateUT < 0) ? Time.fixedDeltaTime : now - lastKEDUpdateUT;
            lastKEDUpdateUT = now;
            
            // Keep fuel mass in sync while vessel is active (Throttled to 5s)
            _fuelSyncTimer -= (float)deltaT;
            if (_fuelSyncTimer <= 0f)
            {
                lastKnownFuelMass = GetVesselPropellantMass();
                _fuelSyncTimer = 5.0f;
            }

            // --- TURBINE STATE MACHINE ---
            bool turbineArchetype = (archetype == EngineArchetype.Bipropellant && KEDSettings.enableCryoSpinUp) || (archetype == EngineArchetype.Advanced && KEDSettings.enableAdvancedSpinUp);
            if (turbineArchetype && engineModules.Count > 0)
            {
                // Startup Event (Rising Edge)
                if (isBurning && !lastFrameBurning)
                {
                    bool gracePeriod = (Planetarium.GetUniversalTime() - turbineShutdownUT < KEDSettings.cryoSpinDownGrace);
                    if (!gracePeriod && !hasManualPrime)
                    {
                        float currentThrust = engineModules[0].maxThrust;
                        float pAtm = (float)vessel.mainBody.GetPressure(vessel.altitude) / 101.325f;
                        float maturityMod = (GetMaturityLevel() >= 3) ? 0.8f : (GetMaturityLevel() >= 1 ? 1.0f : 1.2f);
                        
                        string resName = (archetype == EngineArchetype.Bipropellant) ? "LqdNitrogen" : "ElectricCharge";
                        double baseCost = (archetype == EngineArchetype.Bipropellant) ? (currentThrust / 100f) : (currentThrust / 10f);
                        float spinUpMultiplier = (archetype == EngineArchetype.Bipropellant) ? KEDSettings.cryoSpinUpMultiplier : KEDSettings.advancedSpinUpMultiplier;
                        double cost = baseCost * (1f + pAtm) * maturityMod * spinUpMultiplier;

                        double amountTaken = 0;
                        int resId = PartResourceLibrary.Instance.GetDefinition(resName).id;

                        if (vessel.packed || TimeWarp.CurrentRateIndex > 0)
                        {
                            vessel.GetConnectedResourceTotals(resId, out double totalRes, out double maxRes);
                            if (totalRes >= cost) { amountTaken = cost; pendingSpinUpDebt += cost; }
                        }
                        else amountTaken = part.RequestResource(resId, cost, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                        
                        if (amountTaken < cost * 0.99) 
                        {
                            isBurning = false;
                            foreach (var e in engineModules) e.Shutdown();
                            ignitionFatigue += 0.05f;
                            string shortRes = (archetype == EngineArchetype.Bipropellant) ? "LN2" : "EC";
                            ScreenMessages.PostScreenMessage($"<color=#FF3333>Ignition Aborted: Insufficient {shortRes} for turbopump spin-up ({amountTaken:F1}/{cost:F1})</color>", 5f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                    if (hasManualPrime && !gracePeriod) hasManualPrime = false;
                }

                // Shutdown Event (Falling Edge)
                if (!isBurning && lastFrameBurning)
                {
                    turbineShutdownUT = Planetarium.GetUniversalTime();
                }
            }
            if (!vessel.packed && pendingSpinUpDebt > 0)
            {
                string resName = (archetype == EngineArchetype.Bipropellant) ? "LqdNitrogen" : "ElectricCharge";
                double taken = part.RequestResource(PartResourceLibrary.Instance.GetDefinition(resName).id, pendingSpinUpDebt, ResourceFlowMode.STAGE_PRIORITY_FLOW);
                pendingSpinUpDebt -= taken;
                if (pendingSpinUpDebt < 0.01) pendingSpinUpDebt = 0;
            }

            lastFrameBurning = isBurning;

            if (isBurning)
            {
                if (srbIgnitionTime < 0 && archetype == EngineArchetype.Solid) srbIgnitionTime = Planetarium.GetUniversalTime();
                cumulativeBurnSeconds += (float)deltaT;
                
                // Maturity: Engine Start
                if (!maturityStartAwarded && (vessel.situation == Vessel.Situations.FLYING || vessel.altitude > 100))
                {
                    AwardVesselMaturity(KEDSettings.startBonus, true);
                }

                // Maturity: Full Burn
                if (!maturityBurnAwarded && cumulativeBurnSeconds > 60f)
                {
                    AwardVesselMaturity(KEDSettings.burnBonus, false);
                }

                HandleReliabilityLogic(deltaT);
            }

            // --- SENSORY CUES (JITTER) REMOVED ---

            // --- IGNITION TRACKING ---
            if (anyIgnited && !lastIgnited)
            {
                OnIgnition();
            }
            lastIgnited = anyIgnited;
        }

        private void AwardVesselMaturity(float amount, bool isStart)
        {
            if (vessel == null) return;
            
            bool alreadyAwarded = false;
            foreach (Part p in vessel.parts)
            {
                var m = p.FindModuleImplementing<KEDModule>();
                if (m != null && p.partInfo.name == part.partInfo.name)
                {
                    if (isStart ? m.maturityStartAwarded : m.maturityBurnAwarded)
                    {
                        alreadyAwarded = true;
                        break;
                    }
                }
            }

            if (!alreadyAwarded)
            {
                KEDEvents.OnMaturityGained.Fire(part.partInfo.name, amount);
            }

            // Sync flags to all siblings so they don't try to award it again
            foreach (Part p in vessel.parts)
            {
                var m = p.FindModuleImplementing<KEDModule>();
                if (m != null && p.partInfo.name == part.partInfo.name)
                {
                    if (isStart) m.maturityStartAwarded = true;
                    else m.maturityBurnAwarded = true;
                }
            }
        }

        private void OnIgnition()
        {
            if (HasFailure(1) || HasFailure(2) || HasFailure(5)) return;

            // Ignition Fatigue Increment
            float fatigueIncr = KEDSettings.ignitionFailureBase;
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);

            if (archetype == EngineArchetype.Hypergolic)
            {
                fatigueIncr = KEDSettings.hypergolicFatigueStep;
                ignitionCount++;
            }


            ignitionFatigue += fatigueIncr;

            // Check for Ignition Failure
            float ignitionFailProb = ignitionFatigue * KEDSettings.globalRiskMultiplier;
            
            // Pristine Vacuum Bonus: 10x reliability for Good batches in space
            if (!isLemon && atmSensitivityIndex > KEDSettings.asiWarningThreshold && pressure < KEDSettings.vacuumReliabilityBonus) ignitionFailProb *= KEDSettings.vacuumReliabilityBonus;

            // Qualified level reduces fatigue influence
            if (maturityLevelAtLaunch >= 1) ignitionFailProb *= 0.5f;
            // Masterwork caps ignition failure
            if (maturityLevelAtLaunch >= 4) ignitionFailProb = Mathf.Min(0.01f, ignitionFailProb);

            if (UnityEngine.Random.value < ignitionFailProb)
            {
                TriggerFailure(1); // Ignition Fail
            }
        }

        private void HandleReliabilityLogic(double deltaT)
        {
            if (HasFailure(5)) return;

            // Trigger failures scheduled by timer (Weak Units or Secondary Failures)
            if (failureTriggerTime > 0)
            {
                if (cumulativeBurnSeconds >= failureTriggerTime)
                {
                    TriggerFailure();
                }
            }

            // SRB Specific logic: Guaranteed safe for first 10s, then check fuel threshold
            if (archetype == EngineArchetype.Solid && isWeakUnit && srbFailureFuelThreshold > 0)
            {
                double timeSinceIgnition = Planetarium.GetUniversalTime() - srbIgnitionTime;
                if (timeSinceIgnition > (double)KEDSettings.srbGracePeriod && cachedSolidFuel != null)
                {
                    float fuelPct = (float)(cachedSolidFuel.amount / cachedSolidFuel.maxAmount);
                    if (fuelPct <= srbFailureFuelThreshold)
                    {
                        TriggerFailure(5); // Explode
                    }
                }
            }

            // ASI Operating Band Check & Reliability Modifiers
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);
            
            // Vacuum Bonus: Reliability improves as pressure drops below threshold
            if (atmSensitivityIndex > KEDSettings.asiWarningThreshold && pressure < 0.1f)
            {
                // [DEPRECATED] failureTriggerTime += deltaT * (atmSensitivityIndex - 1.25f);
                // Now handled via weighted failure modes in TriggerFailure()
            }

            // Flow Separation Warning in PAW (Vacuum nozzle in thick atmosphere)
            if (atmSensitivityIndex > KEDSettings.asiFlameoutThreshold && pressure > KEDSettings.asiPressureThreshold)
            {
                uiState = isFailed ? uiState : "DANGER: Flow Separation Risk";
            }
            else if (!isFailed && (uiState == "DANGER: Flow Separation Risk" || uiState == "Outside Optimal Band"))
            {
                uiState = "Nominal";
            }

            if (atmSensitivityIndex > KEDSettings.asiFlameoutThreshold && pressure > KEDSettings.asiPressureThreshold && archetype != EngineArchetype.Nuclear && archetype != EngineArchetype.Electric && archetype != EngineArchetype.Exotic) 
            {
                // Severe vibration from flow separation (Mach disks) damages the engine structure
                float severity = (atmSensitivityIndex - 1.25f) * pressure;
                float damageProbPerSec = 0.05f * severity * KEDSettings.globalRiskMultiplier;
                double p_survival = Math.Pow(1.0 - (double)damageProbPerSec, deltaT);
                
                if (UnityEngine.Random.value > p_survival) 
                {
                    // 50% chance the shockwaves blow out the combustion (Flameout)
                    // 50% chance the asymmetrical vibration seizes the actuators (Gimbal Lock)
                    TriggerFailure(UnityEngine.Random.value > 0.5f ? 2 : 3); 
                }
            }

            // Monopropellant Exhaustion Phase
            if (archetype == EngineArchetype.Monopropellant)
            {
                float limit = KEDSettings.catalystServiceLimit;
                if (maturityLevelAtLaunch >= 3) limit *= 2f; // Heritage doubles limit

                if (cumulativeBurnSeconds > limit)
                {
                    uiState = isFailed ? uiState : "DEGRADED (Catalyst Decay)";
                    
                    // 10% chance per second to add a scar (averages 1 scar per 10 seconds)
                    double p_survival = Math.Pow(1.0 - 0.1, deltaT);
                    if (UnityEngine.Random.value > p_survival)
                    {
                        performanceScars++;
                        ApplyPerformanceScars();
                        ScreenMessages.PostScreenMessage($"<color=#FFA500>⚠ {part.partInfo.title}: Catalyst decaying! (-1% efficiency)</color>", 3f, ScreenMessageStyle.LOWER_CENTER);
                    }
                }
            }
        }

        private void TriggerFailure(int forcedMode = 0, bool scheduleNext = true)
        {
            failureTriggerTime = -1; // Reset timer to prevent double-triggering
            int mode = forcedMode;
            bool isHighTech = (archetype == EngineArchetype.Nuclear || archetype == EngineArchetype.Electric || archetype == EngineArchetype.Exotic);

            if (mode == 0)
            {
                List<int> validModes = new List<int>();
                
                // Archetype Validation
                if (archetype == EngineArchetype.Solid) { validModes.Add(5); }
                else
                {
                    // Allow up to 3 failures per mode for separate buttons
                    if (GetFailureCount(1) < 3) validModes.Add(1);
                    if (!isHighTech && GetFailureCount(2) < 3) validModes.Add(2);
                    if (!isHighTech) validModes.Add(4); // Mode 4 (Valve Seep) doesn't use GetFailureCount anymore since it's an event
                    if (cachedGimbal != null && GetFailureCount(3) < 3) validModes.Add(3);
                }

                if (validModes.Count > 0)
                {
                    // --- VACUUM MODE SHIFT ---
                    float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);
                    if (atmSensitivityIndex > KEDSettings.asiWarningThreshold && pressure < 0.1f)
                    {
                        List<int> weightedPool = new List<int>();
                        foreach (int m in validModes)
                        {
                            int weight = 10;
                            if (m == 4) weight = 80; // High bias for Thrust Drop
                            if (m == 2) weight = 1;  // Low bias for Flameout
                            for (int i = 0; i < weight; i++) weightedPool.Add(m);
                        }
                        mode = weightedPool[UnityEngine.Random.Range(0, weightedPool.Count)];
                    }
                    else
                    {
                        mode = validModes[UnityEngine.Random.Range(0, validModes.Count)];
                    }
                }
                else return; // All slots full
            }
            else
            {
                // Forbidden modes for high-tech engines
                if (isHighTech && (mode == 2 || mode == 4)) return;
            }

            // --- IMMEDIATE SCAR EVENT: VALVE SEEP ---
            if (mode == 4)
            {
                performanceScars += 10;
                ApplyPerformanceScars();
                
                string msg = (archetype == EngineArchetype.Monopropellant) ? "Catalyst Failure" : "Valve Seal Bleedthrough";
                if (KEDScenario.Instance != null)
                    KEDScenario.Instance.PostQueuedMessage($"ALARM: {part.partInfo.title} ({msg})! -10% Efficiency", 7f, ScreenMessageStyle.UPPER_CENTER);
                else
                    ScreenMessages.PostScreenMessage($"<color=#FFA500>ALARM: {part.partInfo.title} ({msg})! -10% Efficiency</color>", 7f, ScreenMessageStyle.UPPER_CENTER);

                // Setup next failure timer since this was a soft event
                if (scheduleNext) failureTriggerTime = cumulativeBurnSeconds + UnityEngine.Random.Range(30f, 100f);
                return; // Stop here, no formal failure flag applied
            }

            // --- WARP DROP LOGIC ---
            bool isHardFailure = (mode == 1 || mode == 2 || mode == 5);

            if (isHardFailure && TimeWarp.CurrentRateIndex > 0)
            {
                TimeWarp.SetRate(0, true);
                ScreenMessages.PostScreenMessage(
                    $"<color=#FF3333>⚠ EMERGENCY WARP DROP: {part.partInfo.title} — Critical Failure!</color>",
                    6f, ScreenMessageStyle.UPPER_CENTER);
            }
            else if (!isHardFailure && TimeWarp.CurrentRateIndex > 0)
            {
                // Soft failure during warp — warn but do not interrupt (10s extended duration)
                ScreenMessages.PostScreenMessage(
                    $"<color=#FFCC00>⚠ {part.partInfo.title}: Degradation detected during warp.</color>",
                    10f, ScreenMessageStyle.UPPER_CENTER);
            }

            // Secondary Failures: If soft fail, set up next timer (unless suppressed by debug)
            if (scheduleNext && (mode == 3 || mode == 4))
            {
                failureTriggerTime = cumulativeBurnSeconds + UnityEngine.Random.Range(30f, 100f);
            }

            ApplyFailureState(mode);
        }

        private void ApplyFailureState(int mode)
        {
            SetFailure(mode, true);
            failureMode = mode;
            failuresCount++;
            diagnosticsRun = false; // New fault reopens the diagnostic window
            string msg = "";
            string color = "#FF3333";

            switch (mode)
            {
                case 1: // Ignition Fail
                    msg = "Ignition Failure";
                    uiState = "FAULT (Injector Lockout)";
                    for (int i = 0; i < engineModules.Count; i++) 
                    { 
                        engineModules[i].Shutdown(); 
                        engineModules[i].allowRestart = false; 
                        engineModules[i].flameout = true;
                    }
                    break;
                case 2: // Flameout
                    msg = "Sudden Flameout";
                    uiState = "FAULT (Flameout)";
                    for (int i = 0; i < engineModules.Count; i++) 
                    { 
                        engineModules[i].Shutdown(); 
                        engineModules[i].flameout = true;
                    }
                    ignitionFatigue += 0.30f; // Massive fatigue spike, making restart dangerous
                    break;
                case 3: // Gimbal Lock
                    msg = "Gimbal Seized";
                    uiState = "DEGRADED (Actuators)";
                    color = "#FFCC00";
                    if (cachedGimbal != null) cachedGimbal.gimbalLock = true;
                    break;
                case 5: // Explode
                    msg = "Casing Breach";
                    uiState = "DESTROYED";
                    color = "#CC0000";
                    if (maturityLevelAtLaunch < 2) part.explode();
                    else 
                    { 
                        for (int i = 0; i < engineModules.Count; i++) 
                        { 
                            engineModules[i].Shutdown(); 
                            engineModules[i].maxThrust = 0; 
                            engineModules[i].flameout = true;
                        } 
                        part.Die(); 
                    }
                    break;
            }

            if (KEDScenario.Instance != null)
                KEDScenario.Instance.PostQueuedMessage($"ALARM: {part.partInfo.title} ({msg})!", 7f, ScreenMessageStyle.UPPER_CENTER);
            else
                ScreenMessages.PostScreenMessage($"<color={color}>ALARM: {part.partInfo.title} ({msg})!</color>", 7f, ScreenMessageStyle.UPPER_CENTER);
            
            // Failure Cascade (5-10%)
            if (UnityEngine.Random.value < 0.08f) TriggerCascade();

            Events["RunDiagnostics"].active = true;
            RefreshUI();
        }

        private void TriggerCascade()
        {
            if (isCascadeVictim) return; // Prevent infinite cascade loops
            
            foreach (Part p in part.symmetryCounterparts)
            {
                var m = p.FindModuleImplementing<KEDModule>();
                if (m != null && !m.isFailed)
                {
                    m.isCascadeVictim = true;
                    m.TriggerFailure(UnityEngine.Random.Range(2, 5));
                }
            }
        }
        private bool TryGetValidEngineer(out ProtoCrewMember engineer, out int engineerLevel, int minLevel = 0)
        {
            engineer = null;
            engineerLevel = 0;
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null) return false;
            
            if (!v.isEVA)
                return false;

            var crew = v.GetVesselCrew();
            if (crew == null || crew.Count == 0)
            {
                KEDLog.Debug("TryGetValidEngineer: EVA Vessel has no crew data.");
                return false;
            }

            engineer = crew[0];
            if (engineer.experienceTrait.Title != "Engineer")
            {
                ScreenMessages.PostScreenMessage("Only Engineers on EVA can perform this action.");
                return false;
            }

            if (engineer.experienceLevel < minLevel)
            {
                ScreenMessages.PostScreenMessage($"[KED] Advanced training required (Level {minLevel}+ Engineer needed).");
                return false;
            }

            engineerLevel = engineer.experienceLevel;
            return true;
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Run Diagnostics", unfocusedRange = 3f)]
        public void RunDiagnostics()
        {
            if (!TryGetValidEngineer(out _, out int engLvl)) return;

            if (diagnosticsRun)
            {
                ScreenMessages.PostScreenMessage("Diagnostics already performed on this unit.", 5f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }
            
            int cost = (GetMaturityLevel() >= 3) ? 0 : 1; // Free for highly mature engines
            int consumed = ConsumeKits(cost, FlightGlobals.ActiveVessel);
            if (consumed < cost)
            {
                ScreenMessages.PostScreenMessage($"[KED] Insufficient EVA Repair Kits for Diagnostics (Need {cost}).");
                return;
            }

            diagnosticsRun = true;
            KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 5f); // Inspection Bonus
            
            List<string> details = new List<string>();
            if (HasFailure(1)) details.Add($"Ignition System: {GetFailureCount(1)} fault(s) detected.");
            if (HasFailure(2)) details.Add($"Combustion Stability: {GetFailureCount(2)} flameout event(s) recorded.");
            if (HasFailure(3)) details.Add($"Thrust Vectoring: {GetFailureCount(3)} actuator(s) seized.");
            if (HasFailure(4)) details.Add($"Flow Regulation: {GetFailureCount(4)} valve(s) degraded. Efficiency: {performanceMultiplier:P1}");
            
            string statusReport = (details.Count > 0) ? string.Join("\n", details.ToArray()) : "Core systems within nominal tolerances.";
            
            // Reliability Forecast
            string forecast = "Reliability Forecast: OPTIMAL";
            if (isWeakUnit) forecast = "Reliability Forecast: CRITICAL (Structural Weakness)";
            else if (ignitionFatigue > 0.2f) forecast = "Reliability Forecast: POOR (Thermal Fatigue)";
            else if (ignitionFatigue > 0.05f) forecast = "Reliability Forecast: STRETCHED";
            
            string finalReport = $"[KED] DIAGNOSTICS REPORT - S/N {serialNumber}\n" +
                                 $"--------------------------------------------------\n" +
                                 $"{statusReport}\n" +
                                 $"--------------------------------------------------\n" +
                                 $"{forecast}";

            ScreenMessages.PostScreenMessage(finalReport, 12f, ScreenMessageStyle.LOWER_CENTER);
            Events["PreventativeMaintenance"].active = (isWeakUnit && !IsFailed);
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Preventative Maintenance", unfocusedRange = 3f, active = false)]
        public void PreventativeMaintenance()
        {
            KEDSettings.engineerGates.TryGetValue(archetype.ToString().ToUpper(), out int minLevel);
            if (!TryGetValidEngineer(out _, out _, minLevel)) return;

            int consumed = ConsumeKits(1, FlightGlobals.ActiveVessel);
            if (consumed < 1)
            {
                ScreenMessages.PostScreenMessage($"[KED] Preventative Maintenance requires 1 EVA Repair Kit.");
                return;
            }

            isWeakUnit = false;
            isLemon = false;
            failureTriggerTime = -1;
            srbFailureFuelThreshold = -1f;
            ScreenMessages.PostScreenMessage("Preventative Maintenance Complete. Unit reliability restored.", 5f, ScreenMessageStyle.UPPER_CENTER);
            Events["PreventativeMaintenance"].active = false;
            RefreshUI();
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Deep System Overhaul", unfocusedRange = 3f)]
        public void HardwareRetrofit() // Name kept for save-game KSPEvent compatibility
        {
            KEDSettings.engineerGates.TryGetValue(archetype.ToString().ToUpper(), out int minLevel);
            if (!TryGetValidEngineer(out _, out _, minLevel)) return;

            // --- SpecializedParts phase: 5% of engine dry mass ---
            double specCost = Math.Ceiling(part.mass * KEDSettings.overhaulMassRatio);
            if (specCost > 0.5)
            {
                double taken = ConsumeSpecializedParts(specCost);
                if (taken < specCost * 0.99)
                {
                    ScreenMessages.PostScreenMessage(
                        $"[KED] Overhaul needs {specCost:F0} SpecializedParts on vessel " +
                        $"({part.mass * 1000:F0} kg engine × {KEDSettings.overhaulMassRatio * 100:F0}%).", 6f);
                    return;
                }
            }

            // --- Success: wipe everything ---
            maturityLevelAtLaunch   = GetMaturityLevel();
            performanceScars        = 0;
            ignitionFatigue         = 0f;
            batchId                 = "";  // Break lemon lineage
            isLemon                 = false;
            isWeakUnit              = false;
            failureTriggerTime      = -1;
            srbFailureFuelThreshold = -1f;

            for (int i = 1; i <= 5; i++) while (HasFailure(i)) SetFailure(i, false);

            for (int i = 0; i < engineModules.Count; i++)
            {
                engineModules[i].allowRestart = true;
                engineModules[i].flameout     = false;
                engineModules[i].maxThrust    = prefabMaxThrusts[i];
            }
            if (cachedGimbal != null) cachedGimbal.gimbalLock = false;

            ApplyPerformanceScars();
            ScreenMessages.PostScreenMessage(
                $"[KED] Deep Overhaul Complete. All scars cleared, lineage reset. ({specCost:F0} SpecParts consumed)",
                7f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Tactical Patch (Emergency)", unfocusedRange = 3f, active = false)]
        public void JuryRig()
        {
            if (!TryGetValidEngineer(out _, out _)) return;

            int consumed = ConsumeKits(1, FlightGlobals.ActiveVessel);
            if (consumed < 1)
            {
                ScreenMessages.PostScreenMessage($"[KED] Tactical Patch requires 1 EVA Repair Kit.", 4f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // Clear first available failure
            if      (HasFailure(1)) SetFailure(1, false);
            else if (HasFailure(2)) SetFailure(2, false);
            else if (HasFailure(3)) SetFailure(3, false);

            // Restore engine to minimum operable state
            for (int i = 0; i < engineModules.Count; i++)
            {
                engineModules[i].flameout     = false;
                engineModules[i].allowRestart = true;
            }
            if (cachedGimbal != null) cachedGimbal.gimbalLock = false;

            // Flat penalty
            performanceScars += 2;
            ignitionFatigue  += KEDSettings.juryRigFatigueAdd;
            ApplyPerformanceScars();

            // Cascade risk
            if (UnityEngine.Random.value < KEDSettings.juryRigCascadeChance)
            {
                ScreenMessages.PostScreenMessage(
                    "<color=#FF3333>[KED] WARNING: Patch stress propagated to symmetric parts!</color>",
                    6f, ScreenMessageStyle.UPPER_CENTER);
                TriggerCascade();
            }

            ScreenMessages.PostScreenMessage(
                "<color=#FFCC00>[KED] Tactical Patch applied. Engine operable but permanently scarred.</color>",
                6f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Catalyst Swap", unfocusedRange = 3f, active = false)]
        public void CatalystSwap()
        {
            if (!TryGetValidEngineer(out _, out _)) return;

            // Requires 1 kit + 5 EC
            int consumed = ConsumeKits(1, FlightGlobals.ActiveVessel);
            if (consumed < 1)
            {
                ScreenMessages.PostScreenMessage("Insufficient EVA Repair Kits for Catalyst Swap.");
                return;
            }

            // Vessel-wide EC check
            double availableEC; double maxEC;
            part.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out availableEC, out maxEC);
            if (availableEC < 5.0)
            {
                ScreenMessages.PostScreenMessage("Insufficient Electric Charge for Catalyst Swap (Needs 5).");
                return;
            }

            part.RequestResource("ElectricCharge", 5.0);
            cumulativeBurnSeconds = 0;
            if (performanceScars > 0) performanceScars--;
            isWeakUnit = false;
            isLemon = false;
            
            ApplyPerformanceScars();
            ScreenMessages.PostScreenMessage("Catalyst Swap Complete. Service life restored.", 5f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Nitrogen Purge", unfocusedRange = 3f, active = false)]
        public void NitrogenPurge()
        {
            if (!TryGetValidEngineer(out _, out _)) return;

            int consumed = ConsumeKits(1, FlightGlobals.ActiveVessel);
            if (consumed < 1)
            {
                ScreenMessages.PostScreenMessage("Insufficient EVA Repair Kits for Nitrogen Purge.");
                return;
            }

            ignitionFatigue *= 0.5f;
            if (performanceScars > 0) performanceScars--;
            
            ApplyPerformanceScars();
            ScreenMessages.PostScreenMessage("Nitrogen Purge & Seal Reseat Complete. Fatigue reduced.", 5f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Manual Turbine Prime", unfocusedRange = 3f)]
        public void ManualTurbinePrime()
        {
            if (!TryGetValidEngineer(out _, out _)) return;

            int consumed = ConsumeKits(1, FlightGlobals.ActiveVessel);
            if (consumed < 1)
            {
                ScreenMessages.PostScreenMessage("Insufficient EVA Repair Kits for Turbine Priming.");
                return;
            }

            hasManualPrime = true;
            ScreenMessages.PostScreenMessage("Turbine Primed Manually. Next start is free.", 5f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActive = true, guiName = "Cycle Valves", groupName = "KED", active = false)]
        public void CycleValves()
        {
            // Level 1+ Engineers only
            if (!TryGetValidEngineer(out _, out _, 1))
            {
                if (FlightGlobals.ActiveVessel == null || !FlightGlobals.ActiveVessel.isEVA)
                    ScreenMessages.PostScreenMessage("Only Engineers on EVA can Cycle Valves.");
                return;
            }

            // Reveal 50%
            if (isWeakUnit && UnityEngine.Random.value < KEDSettings.cycleValvesReveal)
            {
                ScreenMessages.PostScreenMessage("Valves cycled: Unusual resistance detected. Unit may be faulty.", 6f, ScreenMessageStyle.LOWER_CENTER);
            }
            else
            {
                ScreenMessages.PostScreenMessage("Valves cycled: Response normal.", 4f, ScreenMessageStyle.LOWER_CENTER);
            }

            // Risk 5%
            if (UnityEngine.Random.value < KEDSettings.cycleValvesRisk)
            {
                if (UnityEngine.Random.value < 0.5f) TriggerFailure(3); // Gimbal Lock
                else
                {
                    // Sputter
                    ScreenMessages.PostScreenMessage("WARNING: Inadvertent ignition pulse during test!", 3f, ScreenMessageStyle.UPPER_CENTER);
                    if (part.Rigidbody != null) 
                    {
                        part.Rigidbody.AddForce(-part.transform.up * 50f, ForceMode.Impulse); 
                    }
                }
            }
        }

        private double ConsumeSpecializedParts(double count)
        {
            var def = PartResourceLibrary.Instance.GetDefinition("SpecializedParts");
            if (def == null || count <= 0) return 0;
            vessel.GetConnectedResourceTotals(def.id, out double available, out _);
            if (available < count - 0.01) return 0;
            return part.RequestResource(def.id, count, ResourceFlowMode.ALL_VESSEL);
        }

        private int GetMaxMaturityTier()
        {
            switch (archetype)
            {
                case EngineArchetype.Exotic:                                              return 1;
                case EngineArchetype.Electric: case EngineArchetype.Nuclear:             return 2;
                case EngineArchetype.Solid:    case EngineArchetype.Monopropellant:
                case EngineArchetype.Hypergolic: case EngineArchetype.Airbreathing:      return 3;
                default: return 4;
            }
        }

        private int ConsumeKits(int count, Vessel evaKerbal)
        {
            if (count <= 0 || evaKerbal == null || evaKerbal.parts == null) return 0;

            int toRemove = count;
            int consumed = 0;

            List<ModuleInventoryPart> inventories =
                evaKerbal.FindPartModulesImplementing<ModuleInventoryPart>();

            foreach (ModuleInventoryPart inv in inventories)
            {
                if (toRemove <= 0) break;
                if (inv.storedParts == null) continue;

                // Snapshot keys - DictionaryValueList is not directly iterable as key-value pairs
                List<int> allKeys = new List<int>(inv.storedParts.Keys);
                List<int> matchingKeys = new List<int>();
                foreach (int k in allKeys)
                {
                    StoredPart sp = inv.storedParts[k];
                    if (sp != null &&
                        (sp.partName == "evaRepairKit" ||
                         sp.partName.ToLower().Contains("repairkit")))
                        matchingKeys.Add(k);
                }

                foreach (int key in matchingKeys)
                {
                    if (toRemove <= 0) break;
                    if (!inv.storedParts.Keys.Contains(key)) continue;

                    StoredPart slot = inv.storedParts[key];
                    int qty  = slot.quantity;
                    int take = Mathf.Min(qty, toRemove);

                    if (take >= qty)
                    {
                        // Remove entire stack via the vanilla method
                        inv.RemoveNPartsFromInventory(slot.partName, qty);
                    }
                    else
                    {
                        // Partial deduction — mutate quantity directly
                        slot.quantity -= take;
                    }

                    consumed += take;
                    toRemove -= take;
                }

                MonoUtilities.RefreshContextWindows(inv.part);
            }

            if (consumed > 0)
                ScreenMessages.PostScreenMessage(
                    $"[KED] Consumed {consumed} Repair Kit(s).", 4f, ScreenMessageStyle.LOWER_CENTER);
            else if (count > 0)
                ScreenMessages.PostScreenMessage(
                    "[KED] No Repair Kits found in EVA inventory.", 5f, ScreenMessageStyle.UPPER_CENTER);

            return consumed;
        }

        // --- DEBUG EVENTS ---

        [KSPEvent(guiActive = true, guiName = "Force Lemon", groupName = "KED_Debug")]
        public void ForceLemon()
        {
            isLemon = true;
            isWeakUnit = true;
            uiBatchHint = UnityEngine.Random.value < 0.7f ? "Slight variance detected in turbopump alignment." : "Batch vibration signature above nominal.";
            failureTriggerTime = cumulativeBurnSeconds + UnityEngine.Random.Range(15f, 50f);
            KEDLog.DebugScreenAndLog("Force Lemon - Unit flagged as Weak. Delayed failure scheduled.");
            RefreshUI();
        }

        [KSPEvent(guiActive = true, guiName = "Force Perfect", groupName = "KED_Debug")]
        public void ForcePerfect()
        {
            isLemon = false;
            isWeakUnit = false;
            failureTriggerTime = -1;
            uiBatchHint = "Debug: Force Perfect.";
            KEDLog.DebugScreenAndLog("Unit Cleared.");
        }

        [KSPEvent(guiActive = true, guiName = "Force New Batch", groupName = "KED_Debug")]
        public void ForceNewBatch()
        {
            // Reset all similar engines first
            List<KEDModule> siblings = new List<KEDModule>();
            foreach (Part p in vessel.parts)
            {
                if (p.partInfo.name == part.partInfo.name)
                {
                    var m = p.FindModuleImplementing<KEDModule>();
                    if (m != null)
                    {
                        m.batchId = "";
                        m.isLemon = false;
                        m.isWeakUnit = false;
                        m.failureTriggerTime = -1;
                        m.srbFailureFuelThreshold = -1f;
                        siblings.Add(m);
                    }
                }
            }

            // Perform a single initialization which will now automatically distribute effects
            InitializeBatch();

            KEDLog.DebugScreenAndLog($"Rerolled batch for {siblings.Count} engines.");
        }

        [KSPEvent(guiActive = true, guiName = "Reset Fatigue", groupName = "KED_Debug")]
        public void ResetFatigue()
        {
            ignitionFatigue = 0f;
            ScreenMessages.PostScreenMessage("Debug: Fatigue Reset.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Force Random Failure", groupName = "KED_Debug")]
        public void CycleFailure()
        {
            TriggerFailure(0, false); // Immediate failure, no scheduling
            ScreenMessages.PostScreenMessage("Debug: Forced immediate archetype-appropriate failure.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Instant Repair", groupName = "KED_Debug")]
        public void InstantRepair()
        {
            activeFailuresMask = "0,0,0,0,0";
            isFailed = false;
            isLemon = false;
            isWeakUnit = false;
            failureMode = 0;
            failureTriggerTime = -1;
            uiState = "Nominal";
            performanceMultiplier = 1.0f;
            performanceScars = 0;

            ApplyPerformanceScars();

            for (int i = 0; i < engineModules.Count; i++) 
            { 
                engineModules[i].allowRestart = true; 
                engineModules[i].flameout = false;
            }

            if (cachedGimbal != null) cachedGimbal.gimbalLock = false;

            ScreenMessages.PostScreenMessage("Debug: Instant Repair Complete. Unit restored to factory specs.", 5f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        [KSPEvent(guiActive = true, guiName = "Grant Repair Kits", groupName = "KED_Debug")]
        public void GrantRepairKits()
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            if (inventories.Count > 0)
            {
                AvailablePart kitInfo = PartLoader.getPartInfoByName("evaRepairKit");
                if (kitInfo != null && kitInfo.partPrefab != null)
                {
                    ModuleInventoryPart targetInv = inventories[0];
                    int added = 0;
                    MethodInfo storeMethod = targetInv.GetType().GetMethod("StorePart", new Type[] { typeof(Part), typeof(bool) }) ?? 
                                             targetInv.GetType().GetMethod("StorePart", new Type[] { typeof(AvailablePart), typeof(bool) });

                    for (int i = 0; i < 5; i++)
                    {
                        if (storeMethod != null)
                        {
                            object target = storeMethod.GetParameters()[0].ParameterType == typeof(AvailablePart) ? (object)kitInfo : (object)kitInfo.partPrefab;
                            if ((bool)storeMethod.Invoke(targetInv, new object[] { target, true })) added++;
                        }
                    }
                    KEDLog.DebugScreenAndLog($"{added} Repair Kits added to inventory.");
                    return;
                }
            }
            KEDLog.DebugScreenAndLog("No valid inventory found on vessel.");
        }

        [KSPEvent(guiActive = true, guiName = "Kill Engine", groupName = "KED_Debug")]
        public void KillEngine()
        {
            ScreenMessages.PostScreenMessage("Debug: Triggering explosion...", 2f, ScreenMessageStyle.UPPER_CENTER);
            part.explode();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Add 100 MP", groupName = "KED_Debug")]
        public void AddMaturityDebug()
        {
            KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 100f / KEDSettings.maturityYieldMultiplier);
            RefreshUI();
            ScreenMessages.PostScreenMessage("Debug: Added 100 MP.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Reset Maturity", groupName = "KED_Debug")]
        public void ResetMaturityDebug()
        {
            if (KEDScenario.Instance != null && KEDScenario.Instance.globalEngineMaturity.ContainsKey(part.partInfo.name))
            {
                KEDScenario.Instance.globalEngineMaturity[part.partInfo.name] = 0f;
                RefreshUI();
                ScreenMessages.PostScreenMessage("Debug: Maturity Reset to 0.", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Add Performance Scar", groupName = "KED_Debug")]
        public void AddScarDebug()
        {
            performanceScars++;
            ApplyPerformanceScars();
            ScreenMessages.PostScreenMessage($"Debug: Added performance scar (Current: {performanceScars})", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Clear All Scars", groupName = "KED_Debug")]
        public void ClearScarsDebug()
        {
            performanceScars = 0;
            performanceMultiplier = 1.0f;
            ApplyPerformanceScars();
            ScreenMessages.PostScreenMessage("Debug: Scars cleared. Performance restored to nominal.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Add 100s Burn Time", groupName = "KED_Debug")]
        public void AddBurnTimeDebug()
        {
            cumulativeBurnSeconds += 100f;
            ScreenMessages.PostScreenMessage($"Debug: Added 100s burn time (Total: {cumulativeBurnSeconds:F0}s)", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Force Exhaustion", groupName = "KED_Debug")]
        public void ForceExhaustionDebug()
        {
            float limit = KEDSettings.catalystServiceLimit;
            if (maturityLevelAtLaunch >= 3) limit *= 2f;
            cumulativeBurnSeconds = limit + 1f;
            ScreenMessages.PostScreenMessage("Debug: Forced Catalyst Exhaustion.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Dump Module Info", groupName = "KED_Debug")]
        public void DumpModuleInfo()
        {
            string log = $"\n[KED DEBUG DUMP: {part.partInfo.name} ({serialNumber})]\n";
            log += $"Archetype: {archetype} | ASI: {atmSensitivityIndex:F4}\n";
            log += $"Maturity: {KEDScenario.GetMaturity(part.partInfo.name):F2} MP (Level {GetMaturityLevel()})\n";
            log += $"Batch ID: {batchId} | Lemon: {isLemon} | Weak: {isWeakUnit}\n";
            log += $"Failure: {isFailed} (Mode: {failureMode}) | Clock: {cumulativeBurnSeconds:F2}/{failureTriggerTime:F2}s\n";
            log += $"Tactical: Scars={performanceScars} | Ignitions={ignitionCount} | Fatigue={ignitionFatigue:F4}\n";
            log += $"Vessel: {vessel.id} | Situation: {vessel.situation}\n";
            KEDLog.Info(log);
            KEDLog.DebugScreenAndLog("Module Info dumped to KSP Log.");
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Cryo Spin-up", groupName = "KED_Debug")]
        public void ToggleCryoSpinUp()
        {
            KEDSettings.enableCryoSpinUp = !KEDSettings.enableCryoSpinUp;
            ScreenMessages.PostScreenMessage($"Debug: Cryogenic Spin-up {(KEDSettings.enableCryoSpinUp ? "ENABLED" : "DISABLED")}", 5f, ScreenMessageStyle.UPPER_CENTER);
            RefreshUI();
        }

        public override string GetInfo()
        {
            DetermineArchetype();
            string info = $"<color=#00e6e6><b>KED SPECIFICATION</b></color>\nArchetype: {archetype}";

            // Add Spin-up baseline to tooltip
            if ((KEDSettings.enableCryoSpinUp && archetype == EngineArchetype.Bipropellant) || (KEDSettings.enableAdvancedSpinUp && archetype == EngineArchetype.Advanced))
            {
                var prefabEngine = part.partInfo.partPrefab.FindModuleImplementing<ModuleEngines>();
                if (prefabEngine != null)
                {
                    // Base calculation: vacuum (P=0), level 0 maturity (Mod=1.2)
                    if (archetype == EngineArchetype.Bipropellant)
                    {
                        double baseLn2 = (prefabEngine.maxThrust / 100f) * 1.0f * 1.2f * KEDSettings.cryoSpinUpMultiplier; 
                        info += $"\n<color=#66ff66>Base Spin-up:</color> {baseLn2:F1} LN2";
                    }
                    else if (archetype == EngineArchetype.Advanced)
                    {
                        double baseEc = (prefabEngine.maxThrust / 10f) * 1.0f * 1.2f * KEDSettings.advancedSpinUpMultiplier; 
                        info += $"\n<color=#66ff66>Base Spin-up:</color> {baseEc:F1} EC";
                    }
                }
            }
            
            return info;
        }

        public Callback<Rect> GetDrawModulePanelCallback() => null;
    }
}