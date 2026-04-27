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
    // GLOBAL MOD SETTINGS
    // ==========================================
    public static class KEDSettings
    {
        public static float globalRiskMultiplier = 1.0f;
        public static float maturityYieldMultiplier = 1.0f;
        
        private static bool initialized = false;

        public static void EnsureInitialized()
        {
            if (initialized) return;
            ConfigNode[] nodes = GameDatabase.Instance?.GetConfigNodes("KED_SETTINGS");
            if (nodes != null && nodes.Length > 0)
            {
                ConfigNode node = nodes[0];
                node.TryGetValue("globalRiskMultiplier", ref globalRiskMultiplier);
                node.TryGetValue("maturityYieldMultiplier", ref maturityYieldMultiplier);
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
            
            if (lemon) lineageRisk[partName] = Mathf.Min(0.2f, lineageRisk[partName] + 0.05f);
            else lineageRisk[partName] = Mathf.Max(0f, lineageRisk[partName] - 0.02f);
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

            foreach (ProtoPartSnapshot p in pv.protoPartSnapshots)
            {
                if (p.modules.Find(m => m.moduleName == "KEDModule") != null)
                {
                    AddMaturity(p.partName, 5f); // Recovery Bonus
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
        [KSPField(isPersistant = true)] public bool preFailureJitter = false;
        [KSPField(isPersistant = true)] public float srbIgnitionTime = -1f;

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

        private List<ModuleEngines> engines = new List<ModuleEngines>();
        private EngineArchetype archetype;
        private float jitterTimer = 0f;
        private bool lastIgnited = false;

        private static readonly HashSet<string> HypergolicPropellants = new HashSet<string> { "Aerozine50", "NTO", "MMH", "UDMH", "NitricAcid", "Hydrazine" };
        private static readonly HashSet<string> ElectricPropellants = new HashSet<string> { "XenonGas", "ArgonGas", "LqdArgon", "KryptonGas", "LqdKrypton", "NeonGas", "LqdNeon", "Lithium" };

        public override void OnStart(StartState state)
        {
            engines = part.FindModulesImplementing<ModuleEngines>();
            if (engines.Count == 0) return;

            DetermineArchetype();
            CalculateASI();
            
            // ASI Override support
            if (part.partInfo.partConfig != null && part.partInfo.partConfig.HasValue("KED_ASI_OVERRIDE"))
            {
                if (float.TryParse(part.partInfo.partConfig.GetValue("KED_ASI_OVERRIDE"), out float overrideAsi))
                {
                    atmSensitivityIndex = overrideAsi;
                }
            }

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
            
            if (archetype == EngineArchetype.Exotic)
                return mp >= 30 ? 1 : 0;

            if (archetype == EngineArchetype.Electric || archetype == EngineArchetype.Nuclear)
            {
                if (mp >= 120) return 2;
                if (mp >= 50) return 1;
                return 0;
            }

            if (archetype == EngineArchetype.Solid)
            {
                if (mp >= 80) return 2;
                if (mp >= 40) return 1;
                return 0;
            }

            if (archetype == EngineArchetype.Monopropellant || archetype == EngineArchetype.Hypergolic)
            {
                if (mp >= 300) return 3;
                if (mp >= 150) return 2;
                if (mp >= 60) return 1;
                return 0;
            }

            if (archetype == EngineArchetype.Airbreathing)
            {
                if (mp >= 450) return 3;
                if (mp >= 200) return 2;
                if (mp >= 80) return 1;
                return 0;
            }

            // Bipropellant, Thermodynamic, Advanced
            if (mp >= 1000) return 4;
            if (mp >= 500) return 3;
            if (mp >= 250) return 2;
            if (mp >= 100) return 1;
            return 0;
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
            if (bestMP > 0) KEDScenario.Instance.AddMaturity(part.partInfo.name, bestMP * 0.2f);
        }

        private void DetermineArchetype()
        {
            // 1. Nuclear (EnrichedUranium + non-Uranium propellant)
            if (part.Resources.Contains("EnrichedUranium")) 
            { 
                archetype = EngineArchetype.Nuclear; 
                return; 
            }

            ModuleEngines e = engines[0];
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
            if (engines.Count == 0) return;
            float vac = engines[0].atmosphereCurve.Evaluate(0f);
            float asl = engines[0].atmosphereCurve.Evaluate(1f);
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
            float lemonProb = anchor + (engineCount - 1) * 0.0167f;
            
            // Batch Lineage & Aging Factor
            if (KEDScenario.Instance != null)
            {
                // Lineage Influence
                if (KEDScenario.Instance.lineageRisk.TryGetValue(part.partInfo.name, out float risk))
                {
                    lemonProb += risk;
                }

                // Aging Factor (X+ flights)
                if (KEDScenario.Instance.globalFlightsCount.TryGetValue(part.partInfo.name, out int globalFlights))
                {
                    if (globalFlights > 50)
                    {
                        lemonProb += (globalFlights - 50) * 0.001f; // Slow climb after 50 flights
                    }
                }

                KEDScenario.Instance.AddFlight(part.partInfo.name);
            }

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
                failureTriggerTime = 10f + UnityEngine.Random.Range(5f, 60f); // Fallback if fuel check fails
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
            if (!HighLogic.LoadedSceneIsFlight || engines.Count == 0) return;

            bool isBurning = false;
            foreach (var e in engines) if (e.EngineIgnited && e.currentThrottle > 0.01f && !e.flameout) { isBurning = true; break; }

            if (isBurning)
            {
                if (srbIgnitionTime < 0 && archetype == EngineArchetype.Solid) srbIgnitionTime = (float)Planetarium.GetUniversalTime();
                cumulativeBurnSeconds += Time.fixedDeltaTime;
                
                // Maturity: Engine Start
                if (!maturityStartAwarded && (vessel.situation == Vessel.Situations.FLYING || vessel.altitude > 100))
                {
                    maturityStartAwarded = true;
                    KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 1f);
                }

                // Maturity: Full Burn
                if (!maturityBurnAwarded && cumulativeBurnSeconds > 60f)
                {
                    maturityBurnAwarded = true;
                    KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 2f);
                }

                HandleReliabilityLogic();
            }

            // --- SENSORY CUES (JITTER) ---
            if (preFailureJitter && !isFailed)
            {
                jitterTimer += Time.fixedDeltaTime;
                if (jitterTimer > 0.05f)
                {
                    jitterTimer = 0f;
                    float jitterFactor = UnityEngine.Random.Range(0.92f, 1.08f);
                    foreach (var e in engines) e.maxThrust = e.part.partInfo.partPrefab.FindModuleImplementing<ModuleEngines>().maxThrust * performanceMultiplier * jitterFactor;
                    
                    // Subtle gimbal twitch
                    var g = part.FindModuleImplementing<ModuleGimbal>();
                    if (g != null && !g.gimbalLock) g.gimbalRange = g.part.partInfo.partPrefab.FindModuleImplementing<ModuleGimbal>().gimbalRange * UnityEngine.Random.Range(0.8f, 1.2f);

                    // Sensory Audio Cues (Placeholder for future audio implementation)
                    // (Audio modification requires complex Effect navigation in KSP)
                }
            }
            else if (isFailed && failureMode == 4) // Thrust Drop / Jitter
            {
                jitterTimer += Time.fixedDeltaTime;
                if (jitterTimer > 0.1f)
                {
                    jitterTimer = 0f;
                    foreach (var e in engines) 
                    {
                        e.maxThrust = e.part.partInfo.partPrefab.FindModuleImplementing<ModuleEngines>().maxThrust * performanceMultiplier * UnityEngine.Random.Range(0.85f, 0.95f);
                    }
                }
            }
            else if (performanceMultiplier < 1.0f && !isFailed)
            {
                // Apply permanent degradation
                foreach (var e in engines) e.maxThrust = e.part.partInfo.partPrefab.FindModuleImplementing<ModuleEngines>().maxThrust * performanceMultiplier;
            }

            // --- IGNITION TRACKING ---
            bool anyIgnited = false;
            foreach (var e in engines) if (e.EngineIgnited) { anyIgnited = true; break; }

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
            float fatigueIncr = 0.01f;
            float pressure = (float)vessel.mainBody.GetPressure(vessel.altitude);

            // Booster Penalty: High fatigue if starting in vacuum (low pressure)
            if (atmSensitivityIndex < 1.25f && pressure < 0.1f)
            {
                fatigueIncr *= 10f * (1.25f - atmSensitivityIndex);
            }

            ignitionFatigue += fatigueIncr;

            // Check for Ignition Failure
            float ignitionFailProb = ignitionFatigue;
            
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
                // Pre-failure jitter triggers 10s before failure
                if (cumulativeBurnSeconds >= failureTriggerTime - 10f) preFailureJitter = true;

                if (cumulativeBurnSeconds >= failureTriggerTime)
                {
                    TriggerFailure();
                    preFailureJitter = false;
                }
            }

            // SRB Specific logic: 10s safe, 40-70% fuel window
            if (archetype == EngineArchetype.Solid && isWeakUnit)
            {
                double timeSinceIgnition = Planetarium.GetUniversalTime() - srbIgnitionTime;
                if (timeSinceIgnition > 10.0)
                {
                    float fuelPct = (float)(part.Resources["SolidFuel"].amount / part.Resources["SolidFuel"].maxAmount);
                    if (fuelPct < 0.6f && fuelPct > 0.3f) // 40% to 70% used = 60% to 30% remaining
                    {
                        // Peak probability at 50% fuel (0.5 remaining)
                        float prob = 0.01f * (1.0f - Mathf.Abs(fuelPct - 0.5f) * 5f);
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
                if (UnityEngine.Random.value < 0.001f * (atmSensitivityIndex - 1.25f)) TriggerFailure(2); // Flameout
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
                    foreach (var e in engines) { e.Shutdown(); e.allowRestart = false; }
                    break;
                case 2: // Flameout
                    msg = "Sudden Flameout";
                    uiState = "FAULT (Flameout)";
                    foreach (var e in engines) { e.Shutdown(); e.allowRestart = false; }
                    break;
                case 3: // Gimbal Lock
                    msg = "Gimbal Seized";
                    uiState = "DEGRADED (Actuators)";
                    color = "#FFCC00";
                    var g = part.FindModuleImplementing<ModuleGimbal>();
                    if (g != null) g.gimbalLock = true;
                    break;
                case 4: // Thrust Drop
                    msg = "Thrust Capped";
                    uiState = "DEGRADED (Valve Seep)";
                    color = "#FFA500";
                    break;
                case 5: // Explode
                    msg = "Casing Breach";
                    uiState = "DESTROYED";
                    color = "#CC0000";
                    if (maturityLevelAtLaunch < 2) part.explode();
                    else { foreach (var e in engines) { e.Shutdown(); e.maxThrust = 0; } part.Die(); }
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
            foreach (Part p in part.symmetryCounterparts)
            {
                var m = p.FindModuleImplementing<KEDModule>();
                if (m != null && !m.isFailed) m.TriggerFailure(UnityEngine.Random.Range(2, 5));
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
                foreach (var e in engines) { e.allowRestart = true; e.maxThrust = e.part.partInfo.partPrefab.FindModuleImplementing<ModuleEngines>().maxThrust * performanceMultiplier; }
                KEDEvents.OnMaturityGained.Fire(part.partInfo.name, 10f); // Hard Lesson
                ScreenMessages.PostScreenMessage("Engine Systems Restored (Degraded).", 5f, ScreenMessageStyle.UPPER_CENTER);
                Events["RepairEngine"].active = false;
            }
        }

        private bool ConsumeKits(int count)
        {
            // Simplified: Check vessel for EVA Repair Kits
            // In a real implementation, this would iterate through PartModule Inventory
            // For now, we'll check the whole vessel for the resource "EVARepairKit" if it exists,
            // or just assume success if in sandbox for testing purposes.
            
            double found = 0;
            foreach (Part p in vessel.parts)
            {
                if (p.Resources.Contains("EVARepairKit")) found += p.Resources["EVARepairKit"].amount;
            }

            if (found >= count)
            {
                double toRemove = count;
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

            // Fallback for stock inventory system (ModuleInventoryPart)
            // This is more complex, so we'll just check if the player has ANY kits if the resource check fails
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