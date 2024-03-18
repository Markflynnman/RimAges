using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Verse;

namespace RimAges {
    [StaticConstructorOnStartup]
    public class RimAges {

        public static string modTag = "[RimAges]";
    public static List<ResearchProjectDef> techAgeResearch = new List<ResearchProjectDef>();
    public static List<ResearchProjectDef> excludeRequirement = new List<ResearchProjectDef>();
    public static Dictionary<Def, ResearchProjectDef> defaultDefs = new Dictionary<Def, ResearchProjectDef>();
    public static HashSet<ResearchProjectDef> clearCacheList = new HashSet<ResearchProjectDef>();

    public static bool emptyCache = false;

    static RimAges() {
            Harmony harmony = new Harmony("rimages.markflynnman.patch");
    Harmony.DEBUG = true;
            harmony.PatchAll();

            Log.Message($"{modTag}");

            InitTechAgeResearch();
    InitExcludeRequirement();

            if (defaultDefs.NullOrEmpty()) {
                foreach (Def def in RimAgesMod.GetUsableDefs()) {
                    var research = RimAgesMod.GetResearchProjectDef(def);
                    if (research.valid) {
                        defaultDefs.Add(def, research.researchDef);
                    }
                }
            }

            var tabs = DefDatabase<ResearchTabDef>.AllDefsListForReading.ListFullCopy();
tabs = ManageTabs(tabs);

var researchDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();

SortTabs(researchDefs, tabs);
OrganizeTabs(researchDefs);
ResearchPrerequisites(researchDefs, tabs);

// Add research prerequisite to thing def
DefDatabase<ThingDef>.GetNamed("SimpleResearchBench").researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("MedievalResearch"));

if (DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor").researchPrerequisites != null) {
    Log.Warning($"{modTag} - researchPrerequisites found");
    DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor").researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
}
else {
    Log.Warning($"{modTag} - researchPrerequisites null");
}

// Add research prerequisite to plant def
DefDatabase<ThingDef>.GetNamed("Plant_Devilstrand").plant.sowResearchPrerequisites.Clear();
//DefDatabase<ThingDef>.GetNamed("Plant_Devilstrand").plant.sowResearchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("SpacerPlants"));

ApplyEmptyResearch();
        }

        static void InitTechAgeResearch() {
    List<string> AgeDef = new List<string> { "MedievalAge", "IndustrialAge", "SpacerAge", "UltraAge", "ArchotechAge" };
    foreach (var age in AgeDef) {
        techAgeResearch.Add(DefDatabase<ResearchProjectDef>.GetNamed(age));
    }
}

static void InitExcludeRequirement() {
    var researchDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();
    foreach (var res in researchDefs) {
        if (res.techprintCount == 0 && res.RequiredStudiedThingCount == 0) { continue; }
        excludeRequirement.Add(res);
    }

    //Log.Warning($"{modTag} Excluded Research:");
    //foreach (var res in excludeRequirement) {
    //    Log.Warning($"{modTag} {res}");
    //}
    //Log.Warning($"{modTag} Total: {excludeRequirement.Count}");
}

static List<ResearchTabDef> ManageTabs(List<ResearchTabDef> tabs) {
    // Reassign "Main" tab to "Neolithic Age"
    foreach (var tab in tabs) {
        if (tab.defName == "Main") {
            tab.defName = "NeolithicAge";
            tab.label = "Neolithic Age";
        }
        //Log.Message($"{modTag} {tab.defName}");
    }

    // Removes unused tabs
    //-- Condense -- Ex. https://ludeon.com/forums/index.php?topic=50275.0
    List<ResearchTabDef> tabsToRemove = new List<ResearchTabDef>();
    foreach (var tab in tabs) {
        if (tab.defName != "NeolithicAge" && tab.defName != "MedievalAge" && tab.defName != "IndustrialAge" && tab.defName != "SpacerAge" && tab.defName != "UltraAge" && tab.defName != "ArchotechAge") {
            //Log.Warning($"{modTag} Tab to remove: {tab.defName}");
            tabsToRemove.Add(tab);
            tab.label = "";
        }
    }
    foreach (var tab in tabsToRemove) {
        tabs.Remove(tab);
    }

    DefDatabase<ResearchTabDef>.Clear();
    foreach (var t in tabs) {
        DefDatabase<ResearchTabDef>.Add(t);
    }
    //-- End condense --

    return tabs;
}

// Assign ResearchProjectDefs to correct tabs
static void SortTabs(List<ResearchProjectDef> researchDefs, List<ResearchTabDef> tabs) {
    foreach (var def in researchDefs) {
        if (def.defName == "MedievalAge" || def.defName == "IndustrialAge" || def.defName == "SpacerAge" || def.defName == "UltraAge" || def.defName == "ArchotechAge") { continue; }
        def.tab = DefDatabase<ResearchTabDef>.GetNamed($"{def.techLevel}Age");
    }
}

// Clean up research nodes
static void OrganizeTabs(List<ResearchProjectDef> researchDefs) {
    var researchByTab = researchDefs.GroupBy(res => res.tab, res => res);
    List<ResearchProjectDef> resList = new List<ResearchProjectDef>(); // DEBUG ONLY

    foreach (var tab in researchByTab) {
        var defList = tab.ToList();

        // Group ResearchProjectDefs by the mod that added them and group vanilla/dlc ResearchProjectDefs under "Vanilla"
        var researchByMod = defList.GroupBy(res => (res.modContentPack.ToString() == "Ludeon.RimWorld" ||
            res.modContentPack.ToString() == "Ludeon.RimWorld.Royalty" ||
            res.modContentPack.ToString() == "Ludeon.RimWorld.Biotech" ||
            res.modContentPack.ToString() == "Ludeon.RimWorld.Ideology") ? "Vanilla" : res.modContentPack.ToString(), res => res);

        Dictionary<string, Dictionary<string, float>> resX = new Dictionary<string, Dictionary<string, float>>();

        // Shift Vanilla Research to start of tab
        //Log.Error($"{modTag} {tab.Key}");
        foreach (var mod in researchByMod) {
            if (mod.Key != "Vanilla") { continue; }
            List<float> intiPosX = new List<float>();
            foreach (var def in mod) {
                //Log.Message($"{modTag} {def} X: {def.researchViewX}");
                intiPosX.Add(def.researchViewX);
            }

            List<float> posX = new List<float>();
            foreach (var def in mod) {
                def.researchViewX -= intiPosX.Min();
                //Log.Message($"{modTag} {def} X: {def.researchViewX}");
                posX.Add(def.researchViewX);
            }
            resX.Add(mod.Key, new Dictionary<string, float> { { "Min", posX.Min() }, { "Max", posX.Max() } });
            //Log.Warning($"{modTag} {mod.Key} - Min: {resX[mod.Key]["Min"]} Max: {resX[mod.Key]["Max"]}");
        }

        // Shift Modded Research
        var lastMod = "Vanilla";
        foreach (var mod in researchByMod) {
            if (mod.Key == "Vanilla") { continue; }
            List<float> intiPosX = new List<float>();
            foreach (var def in mod) {
                //Log.Message($"{modTag} {def} X: {def.researchViewX}");
                intiPosX.Add(def.researchViewX);
            }

            List<float> posX = new List<float>();
            foreach (var def in mod) {
                def.researchViewX = (def.researchViewX - intiPosX.Min() + (resX[lastMod]["Max"] + 1));
                //Log.Message($"{modTag} {def} X: {def.researchViewX}");
                posX.Add(def.researchViewX);
            }

            resX.Add(mod.Key, new Dictionary<string, float> { { "Min", posX.Min() }, { "Max", posX.Max() } });
            //Log.Warning($"{modTag} {mod.Key} - Min: {resX[mod.Key]["Min"]} Max: {resX[mod.Key]["Max"]}");
            lastMod = mod.Key;
        }

        // Add modified ResearchProjectDef back to main list
        foreach (var mod in researchByMod) {
            foreach (var def in mod) {
                foreach (var def2 in researchDefs) {
                    if (def.defName != def2.defName) { continue; }
                    //Log.Error($"{modTag} OLD: {def2.researchViewX} NEW: {def.researchViewX}");
                    def2.researchViewX = def.researchViewX;
                }
                resList.Add(def);
            }
        }
    }

    //foreach (var res in resList) { Log.Warning($"{modTag} Mod: {res.modContentPack} Name: {res.defName} X: {res.researchViewX}"); }
    ResearchProjectDef.GenerateNonOverlappingCoordinates(); // REQUIRED TO UPDATE RESEARCH POSITION
}

// Set other research as prerequisite of custom research
static void ResearchPrerequisites(List<ResearchProjectDef> researchDefs, List<ResearchTabDef> tabs) {
    foreach (var res in researchDefs) {
        // Add current research to "Age" research prerequisites
        if (res.techLevel != TechLevel.Archotech && res.techprintCount == 0 && res.RequiredStudiedThingCount == 0) {
            // Get index of current research techLevel in TechLevel enum, add 1 to get the next techLevel and add "Age" to the end to get correct research
            var ageRes = DefDatabase<ResearchProjectDef>.GetNamed($"{(TechLevel)(byte)Enum.Parse(typeof(TechLevel), res.techLevel.ToString()) + 1}Age");
            if (ageRes != res) {
                ageRes.prerequisites.Add(res);
            }
        }

        // Add "Age" research to current research prerequisites if tech level is not neolithic and is not an "Age" research
        if (res.techLevel != TechLevel.Neolithic && techAgeResearch.Contains(res) == false) {
            res.prerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed($"{res.techLevel}Age"));
        }
    }
}

public static void ApplyResearchCost() {
    RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
    DefDatabase<ResearchProjectDef>.GetNamed("MedievalAge").baseCost = (settings.MedievalAgeCost);
    DefDatabase<ResearchProjectDef>.GetNamed("IndustrialAge").baseCost = (settings.IndustrialAgeCost);
    DefDatabase<ResearchProjectDef>.GetNamed("SpacerAge").baseCost = (settings.SpacerAgeCost);
    DefDatabase<ResearchProjectDef>.GetNamed("UltraAge").baseCost = (settings.UltraAgeCost);
    DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge").baseCost = (settings.ArchotechAgeCost);
    DefDatabase<ResearchProjectDef>.GetNamed("MedievalCooking").baseCost = (settings.MedievalCookingCost);
    DefDatabase<ResearchProjectDef>.GetNamed("MedievalDefenses").baseCost = (settings.MedievalDefensesCost);
    DefDatabase<ResearchProjectDef>.GetNamed("MedievalHygiene").baseCost = (settings.MedievalHygieneCost);
    DefDatabase<ResearchProjectDef>.GetNamed("MedievalResearch").baseCost = (settings.MedievalResearchCost);
    DefDatabase<ResearchProjectDef>.GetNamed("TrainingTargets").baseCost = (settings.TrainingTargetsCost);
    DefDatabase<ResearchProjectDef>.GetNamed("SpacerPlants").baseCost = (settings.SpacerPlantsCost);
}

public static void ApplyEmptyResearch() {
    RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
    RimAgesMod.RimAgesBackup.SaveBackup();
    if (settings.emptyResearch == false) {
        var res = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();
        foreach (var def in res) {
            if (def.modContentPack.ToString() == "markflynnman.rimages" && techAgeResearch.Contains(def) == false) {
                Log.Warning($"{modTag} - {def.defName}: {def.UnlockedDefs.Count}");
                if (def.UnlockedDefs.Count == 0) {
                    switch (def.defName) {
                        case "MedievalCooking":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalCooking").baseCost = (settings.MedievalCookingCost);
                            settings.MedievalCookingCost = 0;
                            break;
                        case "MedievalDefenses":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalDefenses").baseCost = (settings.MedievalDefensesCost);
                            settings.MedievalDefensesCost = 0;
                            break;
                        case "MedievalHygiene":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalHygiene").baseCost = (settings.MedievalHygieneCost);
                            settings.MedievalHygieneCost = 0;
                            break;
                        case "MedievalResearch":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalResearch").baseCost = (settings.MedievalResearchCost);
                            settings.MedievalResearchCost = 0;
                            break;
                        case "TrainingTargets":
                            DefDatabase<ResearchProjectDef>.GetNamed("TrainingTargets").baseCost = (settings.TrainingTargetsCost);
                            settings.TrainingTargetsCost = 0;
                            break;
                        case "SpacerPlants":
                            DefDatabase<ResearchProjectDef>.GetNamed("SpacerPlants").baseCost = (settings.SpacerPlantsCost);
                            settings.SpacerPlantsCost = 0;
                            break;
                    }
                }
                else {
                    switch (def.defName) {
                        case "MedievalCooking":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalCooking").baseCost = (settings.MedievalCookingCost);
                            settings.MedievalCookingCost = settings.ResearchCostBackup["MedievalCookingCost"];
                            break;
                        case "MedievalDefenses":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalDefenses").baseCost = (settings.MedievalDefensesCost);
                            settings.MedievalDefensesCost = settings.ResearchCostBackup["MedievalDefensesCost"];
                            break;
                        case "MedievalHygiene":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalHygiene").baseCost = (settings.MedievalHygieneCost);
                            settings.MedievalHygieneCost = settings.ResearchCostBackup["MedievalHygieneCost"];
                            break;
                        case "MedievalResearch":
                            DefDatabase<ResearchProjectDef>.GetNamed("MedievalResearch").baseCost = (settings.MedievalResearchCost);
                            settings.MedievalResearchCost = settings.ResearchCostBackup["MedievalResearchCost"];
                            break;
                        case "TrainingTargets":
                            DefDatabase<ResearchProjectDef>.GetNamed("TrainingTargets").baseCost = (settings.TrainingTargetsCost);
                            settings.TrainingTargetsCost = settings.ResearchCostBackup["TrainingTargetsCost"];
                            break;
                        case "SpacerPlants":
                            DefDatabase<ResearchProjectDef>.GetNamed("SpacerPlants").baseCost = (settings.SpacerPlantsCost);
                            settings.SpacerPlantsCost = settings.ResearchCostBackup["SpacerPlantsCost"];
                            break;
                    }
                }
            }
        }
    }
    else {
        var res = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();
        foreach (var def in res) {
            if (def.modContentPack.ToString() == "markflynnman.rimages" && techAgeResearch.Contains(def) == false) {
                switch (def.defName) {
                    case "MedievalCooking":
                        DefDatabase<ResearchProjectDef>.GetNamed("MedievalCooking").baseCost = (settings.MedievalCookingCost);
                        settings.MedievalCookingCost = settings.ResearchCostBackup["MedievalCookingCost"];
                        break;
                    case "MedievalDefenses":
                        DefDatabase<ResearchProjectDef>.GetNamed("MedievalDefenses").baseCost = (settings.MedievalDefensesCost);
                        settings.MedievalDefensesCost = settings.ResearchCostBackup["MedievalDefensesCost"];
                        break;
                    case "MedievalHygiene":
                        DefDatabase<ResearchProjectDef>.GetNamed("MedievalHygiene").baseCost = (settings.MedievalHygieneCost);
                        settings.MedievalHygieneCost = settings.ResearchCostBackup["MedievalHygieneCost"];
                        break;
                    case "MedievalResearch":
                        DefDatabase<ResearchProjectDef>.GetNamed("MedievalResearch").baseCost = (settings.MedievalResearchCost);
                        settings.MedievalResearchCost = settings.ResearchCostBackup["MedievalResearchCost"];
                        break;
                    case "TrainingTargets":
                        DefDatabase<ResearchProjectDef>.GetNamed("TrainingTargets").baseCost = (settings.TrainingTargetsCost);
                        settings.TrainingTargetsCost = settings.ResearchCostBackup["TrainingTargetsCost"];
                        break;
                    case "SpacerPlants":
                        DefDatabase<ResearchProjectDef>.GetNamed("SpacerPlants").baseCost = (settings.SpacerPlantsCost);
                        settings.SpacerPlantsCost = settings.ResearchCostBackup["SpacerPlantsCost"];
                        break;
                }
            }
        }
    }
}

public static void UpdateResearch() {
    RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
    ResearchProjectDef currentResearch = DefDatabase<ResearchProjectDef>.GetNamed(RimAgesSettings.currentResearch.Replace(" ", ""));
    Dictionary<Def, ResearchProjectDef> currentDefDict = RimAgesMod.UpdateDefDict(RimAgesSettings.currentResearch);
    if (RimAgesSettings.rightDefDictInit == false) { return; }
    foreach (var def in RimAgesSettings.rightDefDict) {
        if (!currentDefDict.ContainsKey(def.Key)) { // If def is not in currentResearch unlocks
            RemoveResearch(def.Key);
            AddResearch(def.Key, currentResearch);
            clearCacheList.Add(currentResearch);
        }
    }
    foreach (var def in currentDefDict) {
        if (!RimAgesSettings.rightDefDict.ContainsKey(def.Key)) {
            RemoveResearch(def.Key);
            clearCacheList.Add(currentResearch);
        }
    }

    //if (DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor").researchPrerequisites != null) {
    //    Log.Warning($"{modTag} - researchPrerequisites found");
    //    DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor").researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
    //}
}

public static void RemoveResearch(Def def) {
    switch ($"{def.GetType()}") {
        case "Verse.TerrainDef":
            try {
                DefDatabase<TerrainDef>.GetNamed(def.defName).researchPrerequisites.Clear();

            }
            catch (Exception) {
                //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
            }
            break;
        case "Verse.ThingDef":
            if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Building && DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker == null) {
                try {
                    DefDatabase<ThingDef>.GetNamed(def.defName).researchPrerequisites.Clear();
                }
                catch (Exception) {
                    //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                }
            }
            else if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Plant) {
                try {
                    DefDatabase<ThingDef>.GetNamed(def.defName).plant.sowResearchPrerequisites.Clear();
                }
                catch (Exception) {
                    //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                }
            }
            else if (DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker != null) {
                DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite = null;
            }
            break;
        case "Verse.RecipeDef":
            DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite = null;
            break;
    }
}

public static void AddResearch(Def def, ResearchProjectDef researchDef) {
    switch ($"{def.GetType()}") {
        case "Verse.TerrainDef":
            try {
                DefDatabase<TerrainDef>.GetNamed(def.defName).researchPrerequisites.Add(researchDef);

            }
            catch (Exception e) {
                Log.Error($"[RimAges] ERROR: {def.defName} - Failed to add research.\n{e}");
            }
            break;
        case "Verse.ThingDef":
            if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Building && DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker == null) {
                try {
                    DefDatabase<ThingDef>.GetNamed(def.defName).researchPrerequisites.Add(researchDef);
                }
                catch (Exception e) {
                    Log.Error($"[RimAges] ERROR: {def.defName} - Failed to add research.\n{e}");
                }
            }
            else if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Plant) {
                try {
                    DefDatabase<ThingDef>.GetNamed(def.defName).plant.sowResearchPrerequisites.Add(researchDef);
                }
                catch (Exception e) {
                    Log.Error($"[RimAges] ERROR: {def.defName} - Failed to add research.\n{e}");
                }
            }
            else if (DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker != null) {
                DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite = researchDef;
            }
            break;
        case "Verse.RecipeDef":
            DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite = researchDef;
            break;
    }
}

        // Currently not used
        //public static void UnlockAge(string ageName, TechLevel ageLevel) {
        //}
    }
}