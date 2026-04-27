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
        public static float ignitionFailureBase = 0.01f;
        public static float srbGracePeriod = 10.0f;
        public static bool debugMode = false;

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

                float ignFailBase = 1f;
                if (node.TryGetValue("IgnitionFailureBase", ref ignFailBase)) ignitionFailureBase = ignFailBase / 100f;

                node.TryGetValue("SRBGracePeriod", ref srbGracePeriod);
                node.TryGetValue("DebugMode", ref debugMode);

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
        [KSPField(isPersistant = true)] public float performanceMultiplier = 1.0f;
        [KSPField(isPersistant = true)] public double srbIgnitionTime = -1.0;
        [KSPField(isPersistant = true)] public float lastCalculatedLemonProb = 0f;

        // --- CACHED REFERENCES (NON-PERSISTENT) ---
        private List<ModuleEngines> engineModules = new List<ModuleEngines>();
        private ModuleGimbal cachedGimbal;
        private PartResource cachedSolidFuel;
        private List<float> prefabMaxThrusts = new List<float>();
        private float prefabGimbalRange = 0f;
        private bool isCascadeVictim = false;

        // --- UI ---
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "S/N", groupName = "KED", groupDisplayName = "RELIABILITY REPORT")]
        public string uiSerialNumber = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Maturity", groupName = "KED")]
        public string uiMaturity = "";
        [KSPField(guiActive = true, guiName = "State", groupName = "KED")]
        public string uiState = "Nominal";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "ASI", groupName = "KED")]
        public string uiASI = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Batch Hint", groupName = "KED")]
        public string uiBatchHint = "Unknown";
        [KSPField(guiActive = true, guiName = "History", groupName = "KED")]
        public string uiHistory = "";

        // --- DEBUG UI ---
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "State", groupName = "KED_Debug", groupDisplayName = "DEBUG: ENGINE CONTROL")]
        public string uiDebugInternalState = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Clock", groupName = "KED_Debug")]
        public string uiDebugFailClock = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Fatigue", groupName = "KED_Debug")]
        public string uiDebugFatigue = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "RNG", groupName = "KED_Debug")]
        public string uiDebugRNG = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Archetype", groupName = "KED_Debug")]
        public string uiDebugArchetype = "";

        [KSPField(isPersistant = true)] public EngineArchetype archetype = EngineArchetype.Thermodynamic;
        private bool lastIgnited = false;

        private static readonly HashSet<string> HypergolicPropellants = new HashSet<string> { "Aerozine50", "NTO", "MMH", "UDMH", "NitricAcid", "Hydrazine" };
        private static readonly HashSet<string> ElectricPropellants = new HashSet<string> { "XenonGas", "ArgonGas", "LqdArgon", "KryptonGas", "LqdKrypton", "NeonGas", "LqdNeon", "Lithium" };

        public override void OnStart(StartState state)
        {
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
                var prefabEngine = part.partInfo.partPrefab.FindModulesImplementing<ModuleEngines>().Find(m => m.engineID == e.engineID);
                if (prefabEngine != null) prefabMaxThrusts.Add(prefabEngine.maxThrust);
                else prefabMaxThrusts.Add(e.maxThrust);
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
                
                if (isFailed) ApplyFailureState(failureMode);
            }
            RefreshUI();
            UpdateDebugUI();
        }

        public void Update()
        {
            if (KEDSettings.debugMode && HighLogic.LoadedSceneIsFlight)
            {
                uiDebugInternalState = $"Lemon: {isLemon} | Weak: {isWeakUnit} | Fail: {isFailed}";
                uiDebugFailClock = $"Target: {failureTriggerTime:F1}s | Current: {cumulativeBurnSeconds:F1}s";
                uiDebugFatigue = $"Fatigue: {ignitionFatigue:F4}";
                int seed = 0;
                if (KEDScenario.Instance.manufacturingSeeds.ContainsKey(part.partInfo.name)) seed = KEDScenario.Instance.manufacturingSeeds[part.partInfo.name];
                uiDebugRNG = $"Seed: {seed} | P(Lemon): {lastCalculatedLemonProb:P1}";
                uiDebugArchetype = $"Type: {archetype} | ASI: {atmSensitivityIndex:F2}";
            }
        }

        private void UpdateDebugUI()
        {
            bool show = KEDSettings.debugMode;
            Fields["uiDebugInternalState"].guiActive = show;
            Fields["uiDebugInternalState"].guiActiveEditor = show;
            Fields["uiDebugFailClock"].guiActive = show;
            Fields["uiDebugFailClock"].guiActiveEditor = show;
            Fields["uiDebugFatigue"].guiActive = show;
            Fields["uiDebugFatigue"].guiActiveEditor = show;
            Fields["uiDebugRNG"].guiActive = show;
            Fields["uiDebugRNG"].guiActiveEditor = show;
            Fields["uiDebugArchetype"].guiActive = show;
            Fields["uiDebugArchetype"].guiActiveEditor = show;

            Events["ForceLemon"].guiActive = show;
            Events["ForcePerfect"].guiActive = show;
            Events["ForceNewBatch"].guiActive = show;
            Events["ResetFatigue"].guiActive = show;
            Events["CycleFailure"].guiActive = show;
            Events["InstantRepair"].guiActive = show;
            Events["GrantRepairKits"].guiActive = show;
            Events["KillEngine"].guiActive = show;
            Events["AddMaturityDebug"].guiActive = show;
            Events["AddMaturityDebug"].guiActiveEditor = show;
            Events["ResetMaturityDebug"].guiActive = show;
            Events["ResetMaturityDebug"].guiActiveEditor = show;
            Events["DumpModuleInfo"].guiActive = show;
            Events["DumpModuleInfo"].guiActiveEditor = show;
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

        private string GetEngineBranding()
        {
            int lvl = GetMaturityLevel();
            if (lvl >= 3) return "Heritage Line";
            if (lvl >= 2) return "Block II";
            if (lvl >= 1) return "Block I";
            return "Prototype";
        }

        private void RefreshUI()
        {
            float mp = KEDScenario.GetMaturity(part.partInfo.name);
            int lvl = GetMaturityLevel();
            uiMaturity = $"{GetLevelName(lvl)} ({mp:F0} MP)";
            uiSerialNumber = serialNumber;
            
            float successRate = flightsCount > 0 ? (1f - (float)failuresCount / flightsCount) * 100f : 100f;
            uiHistory = $"Flights: {flightsCount} | Success: {successRate:F0}%";
        }

        public string GetModuleTitle() => $"{part.partInfo.title} ({GetEngineBranding()})";
        public string GetPrimaryField() => $"Maturity: {GetLevelName(GetMaturityLevel())}";

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
            if (bestMP > 0) KEDScenario.Instance.AddMaturity(part.partInfo.name, bestMP * KEDSettings.heritageTransferRate);
        }

        private void DetermineArchetype()
        {
            // 1. Nuclear (EnrichedUranium + non-Uranium propellant)
            if (part.Resources.Contains("EnrichedUranium")) 
            { 
                archetype = EngineArchetype.Nuclear; 
                return; 
            }

            ModuleEngines e = engineModules[0];
            float vacIsp = e.atmosphereCurve.Evaluate(0f);

            // 2. Exotic (ISP > 3000 or specific resources)
            if (vacIsp > 3000f || part.Resources.Contains("Antimatter") || part.Resources.Contains("Gravioli") || part.Resources.Contains("WarpDrive"))
            { 
                archetype = EngineArchetype.Exotic; 
                return; 
            }

            // 3. Advanced (ISP > 500)
            if (vacIsp > 500f) 
            { 
                archetype = EngineArchetype.Advanced; 
                return; 
            }

            // 4. Electric (Standard electric types or Noble gases)
            bool isElec = e.engineType == EngineType.Electric;
            if (!isElec)
            {
                foreach (var prop in e.propellants)
                {
                    if (prop.name == "ElectricCharge")
                    {
                        foreach (var p2 in e.propellants)
                            if (ElectricPropellants.Contains(p2.name)) { isElec = true; break; }
                    }
                }
            }
            if (isElec) { archetype = EngineArchetype.Electric; return; }

            // 5. Airbreathing (Intake resources)
            foreach (var prop in e.propellants)
                if (prop.name == "IntakeAir" || prop.name == "IntakeAtm") { archetype = EngineArchetype.Airbreathing; return; }

            // 6. Hypergolic (Chemical list)
            if (e.propellants.Count >= 2)
            {
                foreach (var prop in e.propellants)
                    if (HypergolicPropellants.Contains(prop.name)) { archetype = EngineArchetype.Hypergolic; return; }
            }

            // 7. Monopropellant (Solo fuel)
            if (e.propellants.Count == 1 && (e.propellants[0].name == "MonoPropellant" || e.propellants[0].name == "Hydrazine"))
            { archetype = EngineArchetype.Monopropellant; return; }

            // 8. Bipropellant (Oxidizer base)
            foreach (var prop in e.propellants)
                if (prop.name == "LqdOxygen" || prop.name == "Oxidizer") { archetype = EngineArchetype.Bipropellant; return; }

            // 9. Solid (SolidFuel / SolidBooster type)
            if (e.engineType == EngineType.SolidBooster || part.Resources.Contains("SolidFuel")) 
            { 
                archetype = EngineArchetype.Solid; 
                return; 
            }

            // 10. Thermodynamic (Standard liquid fallback)
            archetype = EngineArchetype.Thermodynamic;
        }

        private void CalculateASI()
        {
            if (engineModules.Count == 0) return;
            float vac = engineModules[0].atmosphereCurve.Evaluate(0f);
            float asl = engineModules[0].atmosphereCurve.Evaluate(1f);
            atmSensitivityIndex = vac / Mathf.Max(asl, 0.1f);
        }

        private void InitializeBatch()
        {
            // Batch ID is shared per part name on the vessel
            batchId = $"{vessel.id}_{part.partInfo.name}";
            
            // Generate Serial Number
            serialNumber = $"{DateTime.Now.Year}-{part.partInfo.name.Substring(0, Mathf.Min(3, part.partInfo.name.Length)).ToUpper()}-{UnityEngine.Random.Range(100, 999)}";

            // Roll for Lemon
            // P(Lemon) = MaturityAnchor + (Engine Count - 1) * 0.0167
            int engineCount = 0;
            foreach (Part p in vessel.parts) if (p.partInfo.name == part.partInfo.name) engineCount++;

            float anchor = GetMaturityAnchor();
            float lemonProb = (anchor + (engineCount - 1) * 0.0167f) * KEDSettings.globalRiskMultiplier;
            
            // Batch Lineage & Aging Factor
            if (KEDScenario.Instance != null)
            {
                // Lineage Influence
                if (KEDScenario.Instance.lineageRisk.TryGetValue(part.partInfo.name, out float risk))
                {
                    lemonProb += risk;
                }

                // Aging Factor (X+ flights)
                if (KEDSettings.enableAging && KEDScenario.Instance.globalFlightsCount.TryGetValue(part.partInfo.name, out int globalFlights))
                {
                    if (globalFlights > KEDSettings.agingFlightThreshold)
                    {
                        lemonProb += (globalFlights - KEDSettings.agingFlightThreshold) * KEDSettings.agingFactorInc; // Slow climb after threshold
                    }
                }

                KEDScenario.Instance.AddFlight(part.partInfo.name);
            }

            lastCalculatedLemonProb = lemonProb;
            isLemon = UnityEngine.Random.value < lemonProb;
            if (KEDScenario.Instance != null) KEDScenario.Instance.RecordBatchResult(part.partInfo.name, isLemon);

            if (isLemon)
            {
                uiBatchHint = UnityEngine.Random.value < 0.7f ? "Slight variance detected in turbopump alignment." : "Batch vibration signature above nominal.";
                // Controlled Chaos: 80% 1 weak unit, 15% 2, 5% degraded batch
                float roll = UnityEngine.Random.value;
                if (roll < 0.80f) AssignWeakUnit(1);
                else if (roll < 0.95f) AssignWeakUnit(2);
                else ApplyDegradedBatch();
            }
            else
            {
                uiBatchHint = "Factory QA: All systems nominal.";
            }

            // SRB Special Trigger: Set failure trigger time based on expected burn
            if (archetype == EngineArchetype.Solid && isWeakUnit)
            {
                // We'll use a specific fuel percentage check in FixedUpdate for Solids,
                // but let's set a flag or time offset to ensure it doesn't trigger in first 10s.
                failureTriggerTime = KEDSettings.srbGracePeriod + UnityEngine.Random.Range(5f, 60f); // Fallback if fuel check fails
            }

            // Update lineage seed
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
            
            // Simple logic: the first N found get it, or randomize. Let's randomize.
            for (int i = 0; i < count && modules.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, modules.Count);
                modules[idx].isWeakUnit = true;
                modules[idx].failureTriggerTime = UnityEngine.Random.Range(15f, 180f); // Failure window
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
                    if (m != null) m.failureMode = 4; // Thrust Drop
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

            bool isBurning = false;
            for (int i = 0; i < engineModules.Count; i++)
            {
                var e = engineModules[i];
                if (e.EngineIgnited && e.currentThrottle > 0.01f && !e.flameout) { isBurning = true; break; }
            }

            if (isBurning)
            {
                if (srbIgnitionTime < 0 && archetype == EngineArchetype.Solid) srbIgnitionTime = Planetarium.GetUniversalTime();
                cumulativeBurnSeconds += Time.fixedDeltaTime;
                
                // Maturity: Engine Start
                if (!maturityStartAwarded && (vessel.situation == Vessel.Situations.FLYING || vessel.altitude > 100))
                {
                    maturityStartAwarded = true;
                    KEDEvents.OnMaturityGained.Fire(part.partInfo.name, KEDSettings.startBonus);
                }

                // Maturity: Full Burn
                if (!maturityBurnAwarded && cumulativeBurnSeconds > 60f)
                {
                    maturityBurnAwarded = true;
                    KEDEvents.OnMaturityGained.Fire(part.partInfo.name, KEDSettings.burnBonus);
                }

                HandleReliabilityLogic();
            }

            // --- SENSORY CUES (JITTER) REMOVED ---

            // --- IGNITION TRACKING ---
            bool anyIgnited = false;
            for (int i = 0; i < engineModules.Count; i++) if (engineModules[i].EngineIgnited) { anyIgnited = true; break; }

            if (anyIgnited && !lastIgnited)
            {
                OnIgnition();
            }
            lastIgnited = anyIgnited;
        }

        private void OnIgnition()
        {
            if (isFailed) return;

            // Ignition Fatigue Increment
            float fatigueIncr = KEDSettings.ignitionFailureBase;
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);

            // Booster Penalty: High fatigue if starting in vacuum (low pressure)
            if (atmSensitivityIndex < 1.25f && pressure < 0.1f)
            {
                fatigueIncr *= 10f * (1.25f - atmSensitivityIndex);
            }

            ignitionFatigue += fatigueIncr;

            // Check for Ignition Failure
            float ignitionFailProb = ignitionFatigue * KEDSettings.globalRiskMultiplier;
            
            // Qualified level reduces fatigue influence
            if (maturityLevelAtLaunch >= 1) ignitionFailProb *= 0.5f;
            // Masterwork caps ignition failure
            if (maturityLevelAtLaunch >= 4) ignitionFailProb = Mathf.Min(0.01f, ignitionFailProb);

            if (UnityEngine.Random.value < ignitionFailProb)
            {
                TriggerFailure(1); // Ignition Fail
            }
        }

        private void HandleReliabilityLogic()
        {
            if (isFailed) return;

            // Weak Unit Trigger Window
            if (isWeakUnit && failureTriggerTime > 0)
            {
                if (cumulativeBurnSeconds >= failureTriggerTime)
                {
                    TriggerFailure();
                }
            }

            // SRB Specific logic: 10s safe, 40-70% fuel window
            if (archetype == EngineArchetype.Solid && isWeakUnit)
            {
                double timeSinceIgnition = Planetarium.GetUniversalTime() - srbIgnitionTime;
                if (timeSinceIgnition > (double)KEDSettings.srbGracePeriod && cachedSolidFuel != null)
                {
                    float fuelPct = (float)(cachedSolidFuel.amount / cachedSolidFuel.maxAmount);
                    if (fuelPct < 0.6f && fuelPct > 0.3f) // 40% to 70% used = 60% to 30% remaining
                    {
                        // Peak probability at 50% fuel (0.5 remaining)
                        float prob = 0.01f * (1.0f - Mathf.Abs(fuelPct - 0.5f) * 5f) * KEDSettings.globalRiskMultiplier;
                        if (UnityEngine.Random.value < prob) TriggerFailure(5); // Explode
                    }
                }
            }

            // ASI Operating Band Check & Reliability Modifiers
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);
            
            // Vacuum Bonus: Reliability improves as pressure drops below threshold
            if (atmSensitivityIndex > 1.25f && pressure < 0.1f)
            {
                // Reduce failure window probability or delay triggers
                // (Implementation detail: slightly shift failureTriggerTime forward)
                if (isWeakUnit) failureTriggerTime += Time.fixedDeltaTime * (atmSensitivityIndex - 1.25f);
            }

            // Atmospheric Band Warning in PAW
            if ((atmSensitivityIndex > 1.5f && pressure > 0.5f) || (atmSensitivityIndex < 1.15f && pressure < 0.1f))
            {
                uiState = isFailed ? uiState : "Outside Optimal Band";
            }
            else if (!isFailed && uiState == "Outside Optimal Band")
            {
                uiState = "Nominal";
            }

            if (atmSensitivityIndex > 1.5f && pressure > 0.5f) // Vacuum engine in thick air
            {
                if (UnityEngine.Random.value < 0.001f * (atmSensitivityIndex - 1.25f) * KEDSettings.globalRiskMultiplier) TriggerFailure(2); // Flameout
            }
        }

        private void TriggerFailure(int forcedMode = 0)
        {
            int mode = forcedMode;
            if (mode == 0)
            {
                if (archetype == EngineArchetype.Solid) mode = 5; // Explode
                else if (archetype == EngineArchetype.Electric || archetype == EngineArchetype.Nuclear) mode = (maturityLevelAtLaunch >= 1) ? 4 : 2;
                else mode = UnityEngine.Random.Range(1, 5);
            }
            ApplyFailureState(mode);
        }

        private void ApplyFailureState(int mode)
        {
            isFailed = true;
            failureMode = mode;
            failuresCount++;
            string msg = "";
            string color = "#FF3333";

            switch (mode)
            {
                case 1: // Ignition Fail
                    msg = "Ignition Failure";
                    uiState = "FAULT (Injector Lockout)";
                    for (int i = 0; i < engineModules.Count; i++) { engineModules[i].Shutdown(); engineModules[i].allowRestart = false; }
                    break;
                case 2: // Flameout
                    msg = "Sudden Flameout";
                    uiState = "FAULT (Flameout)";
                    for (int i = 0; i < engineModules.Count; i++) { engineModules[i].Shutdown(); engineModules[i].allowRestart = false; }
                    break;
                case 3: // Gimbal Lock
                    msg = "Gimbal Seized";
                    uiState = "DEGRADED (Actuators)";
                    color = "#FFCC00";
                    if (cachedGimbal != null) cachedGimbal.gimbalLock = true;
                    break;
                case 4: // Thrust Drop
                    msg = "Thrust Capped";
                    uiState = "DEGRADED (Valve Seep)";
                    color = "#FFA500";
                    for (int i = 0; i < engineModules.Count; i++) { engineModules[i].maxThrust = prefabMaxThrusts[i] * performanceMultiplier; }
                    break;
                case 5: // Explode
                    msg = "Casing Breach";
                    uiState = "DESTROYED";
                    color = "#CC0000";
                    if (maturityLevelAtLaunch < 2) part.explode();
                    else { for (int i = 0; i < engineModules.Count; i++) { engineModules[i].Shutdown(); engineModules[i].maxThrust = 0; } part.Die(); }
                    break;
            }

            if (KEDScenario.Instance != null)
                KEDScenario.Instance.PostQueuedMessage($"ALARM: {part.partInfo.title} ({msg})!", 7f, ScreenMessageStyle.UPPER_CENTER);
            else
                ScreenMessages.PostScreenMessage($"<color={color}>ALARM: {part.partInfo.title} ({msg})!</color>", 7f, ScreenMessageStyle.UPPER_CENTER);
            
            // Failure Cascade (5-10%)
            if (UnityEngine.Random.value < 0.08f) TriggerCascade();

            Events["RepairEngine"].active = (mode != 5 && (archetype != EngineArchetype.Exotic || maturityLevelAtLaunch >= 1));
            Events["RunDiagnostics"].active = true;
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

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Run Diagnostics", unfocusedRange = 3f)]
        public void RunDiagnostics()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];
            if (crew.experienceTrait.Title != "Engineer") { ScreenMessages.PostScreenMessage("Only Engineers can run diagnostics."); return; }
            
            if (archetype == EngineArchetype.Nuclear && crew.experienceLevel < 3)
            { ScreenMessages.PostScreenMessage("Nuclear diagnostics require Level 3 Engineer."); return; }

            diagnosticsRun = true;
            KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 5f); // Inspection Bonus
            string report = isWeakUnit ? "WEAK UNIT DETECTED. Critical failure imminent." : "Core systems nominal.";
            if (isFailed) report = $"FAILURE IDENTIFIED: {uiState}";
            
            ScreenMessages.PostScreenMessage($"[KED] DIAGNOSTICS ({serialNumber}): {report}", 8f, ScreenMessageStyle.LOWER_CENTER);
            Events["PreventativeMaintenance"].active = (isWeakUnit && !isFailed);
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Preventative Maintenance", unfocusedRange = 3f, active = false)]
        public void PreventativeMaintenance()
        {
            if (ConsumeKits(GetKitCost()))
            {
                isWeakUnit = false;
                failureTriggerTime = -1;
                ScreenMessages.PostScreenMessage("Preventative Maintenance Complete. Lemon flag cleared.", 5f, ScreenMessageStyle.UPPER_CENTER);
                Events["PreventativeMaintenance"].active = false;
            }
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Hardware Retrofit", unfocusedRange = 3f)]
        public void HardwareRetrofit()
        {
            if (vessel == null || !vessel.isEVA) return;
            var crew = vessel.GetVesselCrew()[0];
            if (crew.experienceLevel < 2) { ScreenMessages.PostScreenMessage("Requires Level 2 Engineer."); return; }
            
            if (archetype == EngineArchetype.Nuclear && crew.experienceLevel < 3)
            { ScreenMessages.PostScreenMessage("Nuclear retrofits require Level 3 Engineer."); return; }

            int kits = 5;
            if (archetype == EngineArchetype.Bipropellant || archetype == EngineArchetype.Airbreathing) kits = 7;
            if (archetype == EngineArchetype.Nuclear || archetype == EngineArchetype.Electric || archetype == EngineArchetype.Exotic) kits = 10;

            if (ConsumeKits(kits))
            {
                maturityLevelAtLaunch = GetMaturityLevel();
                ScreenMessages.PostScreenMessage($"Retrofit Complete: Standardized to Level {maturityLevelAtLaunch}", 5f, ScreenMessageStyle.UPPER_CENTER);
                RefreshUI();
            }
        }

        [KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiName = "Repair Engine Systems", unfocusedRange = 3f, active = false)]
        public void RepairEngine()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !v.isEVA) return;
            var crew = v.GetVesselCrew()[0];

            if (archetype == EngineArchetype.Nuclear && crew.experienceLevel < 3)
            { ScreenMessages.PostScreenMessage("Nuclear repairs require Level 3 Engineer."); return; }

            if (ConsumeKits(GetKitCost()))
            {
                isFailed = false;
                failureMode = 0;
                uiState = "Nominal";
                performanceMultiplier = 0.98f; // 2% penalty
                for (int i = 0; i < engineModules.Count; i++) 
                { 
                    engineModules[i].allowRestart = true; 
                    engineModules[i].maxThrust = prefabMaxThrusts[i] * performanceMultiplier; 
                }
                KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 10f); // Hard Lesson
                ScreenMessages.PostScreenMessage("Engine Systems Restored (Degraded).", 5f, ScreenMessageStyle.UPPER_CENTER);
                Events["RepairEngine"].active = false;
            }
        }

        private bool ConsumeKits(int count)
        {
            // 1. Check for modern inventory items (KSP 1.11+)
            int foundItems = 0;
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            
            foreach (var inv in inventories)
            {
                foreach (var slot in inv.storedParts.Values)
                {
                    if (slot != null && slot.partName == "evaRepairKit")
                    {
                        foundItems += slot.quantity;
                    }
                }
            }

            if (foundItems >= count)
            {
                int toRemove = count;
                foreach (var inv in inventories)
                {
                    // Copy keys to avoid modification issues during iteration
                    List<int> keys = new List<int>(inv.storedParts.Keys);
                    foreach (int key in keys)
                    {
                        var slot = inv.storedParts[key];
                        if (slot != null && slot.partName == "evaRepairKit")
                        {
                            int take = Math.Min(slot.quantity, toRemove);
                            slot.quantity -= take;
                            if (slot.quantity <= 0)
                            {
                                inv.storedParts.Remove(key);
                            }
                            toRemove -= take;
                        }
                        if (toRemove <= 0) break;
                    }
                    if (toRemove <= 0) break;
                }
                return true;
            }

            // 2. Fallback for legacy resource-based kits (if any)
            double foundResource = 0;
            foreach (Part p in vessel.parts)
            {
                if (p.Resources.Contains("EVARepairKit")) foundResource += p.Resources["EVARepairKit"].amount;
            }

            if (foundResource >= (double)count)
            {
                double toRemove = (double)count;
                foreach (Part p in vessel.parts)
                {
                    if (p.Resources.Contains("EVARepairKit"))
                    {
                        double take = Math.Min(p.Resources["EVARepairKit"].amount, toRemove);
                        p.Resources["EVARepairKit"].amount -= take;
                        toRemove -= take;
                    }
                    if (toRemove <= 0) break;
                }
                return true;
            }

            ScreenMessages.PostScreenMessage($"Missing {count} EVA Repair Kits!", 5f, ScreenMessageStyle.UPPER_CENTER);
            return false;
        }

        private int GetKitCost()
        {
            int cost = 5;
            if (archetype == EngineArchetype.Bipropellant || archetype == EngineArchetype.Airbreathing) cost = 7;
            if (archetype == EngineArchetype.Nuclear || archetype == EngineArchetype.Electric || archetype == EngineArchetype.Exotic) cost = 10;
            if (diagnosticsRun) cost = Mathf.Max(1, cost - 1);
            return cost;
        }

        // --- DEBUG EVENTS ---

        [KSPEvent(guiActive = true, guiName = "Force Lemon", groupName = "KED_Debug")]
        public void ForceLemon()
        {
            isLemon = true;
            isWeakUnit = true;
            failureTriggerTime = cumulativeBurnSeconds + 5f;
            ScreenMessages.PostScreenMessage("Debug: Force Lemon Active. Failure in 5s.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Force Perfect", groupName = "KED_Debug")]
        public void ForcePerfect()
        {
            isLemon = false;
            isWeakUnit = false;
            failureTriggerTime = -1;
            uiBatchHint = "Debug: Force Perfect.";
            ScreenMessages.PostScreenMessage("Debug: Unit Cleared.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Force New Batch", groupName = "KED_Debug")]
        public void ForceNewBatch()
        {
            InitializeBatch();
            ScreenMessages.PostScreenMessage("Debug: Rerolling Batch...", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Reset Fatigue", groupName = "KED_Debug")]
        public void ResetFatigue()
        {
            ignitionFatigue = 0f;
            ScreenMessages.PostScreenMessage("Debug: Fatigue Reset.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Cycle Failure", groupName = "KED_Debug")]
        public void CycleFailure()
        {
            int nextMode = (failureMode % 5) + 1;
            ApplyFailureState(nextMode);
            ScreenMessages.PostScreenMessage($"Debug: Cycled to Failure Mode {nextMode}.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Instant Repair", groupName = "KED_Debug")]
        public void InstantRepair()
        {
            isFailed = false;
            failureMode = 0;
            uiState = "Nominal";
            performanceMultiplier = 1.0f;
            for (int i = 0; i < engineModules.Count; i++) 
            { 
                engineModules[i].allowRestart = true; 
                engineModules[i].maxThrust = prefabMaxThrusts[i]; 
            }
            ScreenMessages.PostScreenMessage("Debug: Instant Repair Complete.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = true, guiName = "Grant Repair Kits", groupName = "KED_Debug")]
        public void GrantRepairKits()
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            if (inventories.Count > 0)
            {
                // TODO: Verify correct ModuleInventoryPart signature for user's KSP version
                // AvailablePart kitInfo = PartLoader.getPartInfoByName("evaRepairKit");
                // if (kitInfo != null && kitInfo.partPrefab != null)
                // {
                //     for (int i = 0; i < 5; i++)
                //     {
                //         inv.StorePart(kitInfo.partPrefab, true); 
                //     }
                //     ScreenMessages.PostScreenMessage("Debug: 5 Repair Kits added to inventory.", 5f, ScreenMessageStyle.UPPER_CENTER);
                // }
                ScreenMessages.PostScreenMessage("Debug: Grant Repair Kits (Inventory logic disabled due to API mismatch).", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                ScreenMessages.PostScreenMessage("Debug: No inventory found on vessel.", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
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

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Dump Module Info", groupName = "KED_Debug")]
        public void DumpModuleInfo()
        {
            string log = $"\n[KED DEBUG DUMP: {part.partInfo.name} ({serialNumber})]\n";
            log += $"Archetype: {archetype} | ASI: {atmSensitivityIndex:F4}\n";
            log += $"Maturity: {KEDScenario.GetMaturity(part.partInfo.name):F2} MP (Level {GetMaturityLevel()})\n";
            log += $"Batch ID: {batchId} | Lemon: {isLemon} | Weak: {isWeakUnit}\n";
            log += $"Failure: {isFailed} (Mode: {failureMode}) | Clock: {cumulativeBurnSeconds:F2}/{failureTriggerTime:F2}s\n";
            log += $"Fatigue: {ignitionFatigue:F6} | PerfMod: {performanceMultiplier:F3}\n";
            log += $"Vessel: {vessel.id} | Situation: {vessel.situation}\n";
            UnityEngine.Debug.Log(log);
            ScreenMessages.PostScreenMessage("Debug: Module Info dumped to KSP Log.", 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        public override string GetInfo()
        {
            int lvl = GetMaturityLevel();
            float mp = KEDScenario.GetMaturity(part.partInfo.name);
            string info = $"<color=#00e6e6><b>=== FACTORY SPECIFICATION ===</b></color>\n";
            info += $"<b>Class: <color=#ffaa00>{GetLevelName(lvl)}</color></b> ({mp:F0} MP)\n";
            info += $"<b>Archetype: <color=#00ff00>{archetype}</color></b>\n";
            info += $"<b>ASI: <color=#ffff00>{atmSensitivityIndex:F2}</color></b>\n";
            
            if (KEDScenario.Instance != null && KEDScenario.Instance.globalFlightsCount.TryGetValue(part.partInfo.name, out int flights))
                info += $"<b>Service History: <color=#ffffff>{flights} Flights</color></b>\n";

            info += $"\n<i>Place in workspace for Live Telemetry.</i>";
            return info;
        }

        public Callback<Rect> GetDrawModulePanelCallback() => null;
    }
}