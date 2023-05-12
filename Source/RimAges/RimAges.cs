﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Unix.Native;
using RimWorld;
using RimWorld.BaseGen;
using Steamworks;
using Verse;
using Verse.AI;
using Verse.Noise;
using HarmonyLib;

namespace RimAges {
    [StaticConstructorOnStartup]
    public class RimAges : ResearchProjectDef {

        public static string modTag = "[RimAges]";
        enum ResearchTabs {
            Neolithic,
            Medieval,
            Industrial,
            Spacer,
            Ultra,
            Archotech
        }
        public static List<ResearchProjectDef> techAgeResearch = new List<ResearchProjectDef>();
        public static List<ResearchProjectDef> excludeRequirement = new List<ResearchProjectDef>();
        public static ResearchTabDef medievalTab = new ResearchTabDef();

        static RimAges() {
            Harmony harmony = new Harmony("rimages.markflynnman.patch");
            Harmony.DEBUG = true;
            harmony.PatchAll();

            Log.Message($"{modTag}");

            InitTechAgeResearch();
            InitExcludeRequirement();

            var tabs = DefDatabase<ResearchTabDef>.AllDefsListForReading.ListFullCopy();
            tabs = ManageTabs(tabs);

            var researchDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();

            SortTabs(researchDefs, tabs);
            OrganizeTabs(researchDefs);

            ResearchPrerequisites(researchDefs, tabs);

            //var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading.ListFullCopy();
            //def.plant.sowResearchPrerequisites - Plant Research Prerequisites
            //def.researchPrerequisites - Building Research Prerequisites

            //var recipeDefs = DefDatabase<RecipeDef>.AllDefsListForReading.ListFullCopy();
            //def.researchPrerequisites - Recipe Research Prerequisites
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

            Log.Warning($"{modTag} Excluded Research:");
            foreach (var res in excludeRequirement) {
                Log.Warning($"{modTag} {res}");
            }
            Log.Warning($"{modTag} Total: {excludeRequirement.Count}");
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

        static void SortTabs(List<ResearchProjectDef> researchDefs, List<ResearchTabDef> tabs) {
            foreach (var def in researchDefs) {
                // Assign ResearchProjectDefs to correct tabs
                if (def.defName == "MedievalAge" || def.defName == "IndustrialAge" || def.defName == "SpacerAge" || def.defName == "UltraAge" || def.defName == "ArchotechAge") { continue; }
                def.tab = tabs[(int)(ResearchTabs)Enum.Parse(typeof(ResearchTabs), def.techLevel.ToString())];
            }
        }

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
            foreach(var res in researchDefs) {
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

        // Currently not used
        //public static void UnlockAge(string ageName, TechLevel ageLevel) {
        //}
    }
}