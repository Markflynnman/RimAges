using RimWorld;
using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;
using static RimAges.RimAgesSettings;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics.Eventing.Reader;
using Verse.AI;

namespace RimAges {
    public class RimAgesSettings : ModSettings {
        public int tab;

        public bool noResearch;
        public bool emptyResearch;

        public int MedievalAgeCost;
        public int IndustrialAgeCost;
        public int SpacerAgeCost;
        public int UltraAgeCost;
        public int ArchotechAgeCost;
        public int MedievalCookingCost;
        public int MedievalDefensesCost;
        public int MedievalHygieneCost;
        public int MedievalResearchCost;
        public int TrainingTargetsCost;
        public int SpacerPlantsCost;
        public Dictionary<string, int> ResearchCostBackup = new Dictionary<string, int>();
        public Dictionary<string, List<string>> ResearchChanges;


        public static Vector2 leftScrollPos;
        public static Vector2 rightScrollPos;
        public static int currentPage = 0;
        public static string currentResearch = "Medieval Age";
        public static bool isDragging = false;
        public static (Def, List<ResearchProjectDef>, Vector2) dragging;
        public static Vector2 boxPos = new Vector2(200, 300);
        public static bool researchDropDownActive = false;
        public static bool filterDropDownActive = false;
        public static bool assignedDefsFilter = false;
        public static string searchFilter;

        public static Dictionary<Def, List<ResearchProjectDef>> leftDefDict = new Dictionary<Def, List<ResearchProjectDef>>();
        public static Dictionary<Def, List<ResearchProjectDef>> rightDefDict = new Dictionary<Def, List<ResearchProjectDef>>();
        public static bool rightDefDictInit = false;

        public override void ExposeData() {
            Scribe_Values.Look(ref noResearch, "noResearch", true, true);
            Scribe_Values.Look(ref emptyResearch, "emptyResearch", true, true);

            Scribe_Values.Look(ref MedievalAgeCost, "MedievalAgeCost", 1000, true);
            Scribe_Values.Look(ref IndustrialAgeCost, "IndustrialAgeCost", 2000, true);
            Scribe_Values.Look(ref SpacerAgeCost, "SpacerAgeCost", 3000, true);
            Scribe_Values.Look(ref UltraAgeCost, "UltraAgeCost", 4000, true);
            Scribe_Values.Look(ref ArchotechAgeCost, "ArchotechAgeCost", 5000, true);
            Scribe_Values.Look(ref MedievalCookingCost, "MedievalCookingCost", 1000, true);
            Scribe_Values.Look(ref MedievalDefensesCost, "MedievalDefensesCost", 1000, true);
            Scribe_Values.Look(ref MedievalHygieneCost, "MedievalHygieneCost", 1000, true);
            Scribe_Values.Look(ref MedievalResearchCost, "MedievalResearchCost", 1000, true);
            Scribe_Values.Look(ref TrainingTargetsCost, "TrainingTargetsCost", 1000, true);
            Scribe_Values.Look(ref SpacerPlantsCost, "SpacerPlantsCost", 1000, true);
            Scribe_Collections.Look(ref ResearchCostBackup, "ResearchCostBackup", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref ResearchChanges, "ResearchChanges", LookMode.Value, LookMode.Deep);

            base.ExposeData();
        }
    }

    public class RimAgesMod : Mod {
        RimAgesSettings settings;
        public static List<Def> allDefs = new List<Def>();

        public RimAgesMod(ModContentPack content) : base(content) {
            settings = GetSettings<RimAgesSettings>();
        }

        public override string SettingsCategory() {
            return "RimAges";
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            if (allDefs.NullOrEmpty()) {
                allDefs = GetUsableDefs();
            }

            Rect tabRect = inRect;
            tabRect.y -= 8f;
            tabRect.x += 94;
            inRect.y -= 8f;
            Widgets.DrawMenuSection(inRect);
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("Main", delegate {
                    settings.tab = 0;
                    settings.Write();
                    researchDropDownActive = false;
                    filterDropDownActive = false;
                }, settings.tab == 0),
                new TabRecord("Research Cost", delegate {
                    settings.tab = 1;
                    settings.Write();
                    researchDropDownActive = false;
                    filterDropDownActive = false;
                    Log.Message($"{RimAges.modTag} - Empty Reasearch: {settings.emptyResearch} - {DateTime.Now:hh:mm:ss tt}");
                }, settings.tab == 1),
                new TabRecord("Research Unlocks", delegate {
                    settings.tab = 2;
                    currentPage = 0;
                    settings.Write();
                    // Initialize def dicts
                    leftDefDict = UpdateDefDict(null);
                    rightDefDict = UpdateDefDict(currentResearch);
                    rightDefDictInit = true;
                }, settings.tab == 2),
                new TabRecord("Drag & Drop", delegate {
                    settings.tab = 3;
                    settings.Write();
                    researchDropDownActive = false;
                    filterDropDownActive = false;
                }, settings.tab == 3)
            };

            TabDrawer.DrawTabs(tabRect, tabs);
            if (settings.tab == 0) {
                DrawMain(inRect.ContractedBy(10f), settings);
            }
            if (settings.tab == 1) {
                DrawResearchCost(inRect.ContractedBy(10f), settings);
            }
            if (settings.tab == 2) {
                DrawResearchTab(inRect.ContractedBy(10f), settings);
            }
            if (settings.tab == 3) {
                DrawResearchSelection(inRect.ContractedBy(10f), settings);
            }
            base.DoSettingsWindowContents(inRect);
        }

        public static List<Def> GetUsableDefs() {
            List<Def> usableDefs = new List<Def>();
            List<string> blacklist = new List<string> { "Sandstone_Smooth", "Granite_Smooth", "Limestone_Smooth", "Slate_Smooth", "Marble_Smooth" };
            //!x.IsBlueprint && !x.IsFrame && !x.isUnfinishedThing && (x.category == ThingCategory.Item || x.category == ThingCategory.Building || x.category == ThingCategory.Plant || x.category == ThingCategory.Pawn)))
            usableDefs = usableDefs.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => !x.IsBlueprint && !x.isMechClusterThreat && x.BuildableByPlayer && (x.category == ThingCategory.Building || x.category == ThingCategory.Plant))) /////
                                   .Concat(DefDatabase<TerrainDef>.AllDefs.Where(x => x.IsFloor && !blacklist.Contains(x.defName)))
                                   //.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Item && x.recipeMaker != null))
                                   .Concat(DefDatabase<RecipeDef>.AllDefs)
                                   .Distinct().ToList(); // TODO: Filter out defs that are not useable by players to prevent "no researchPrerequisite" error logs... it causes massive lag
            //allDefs = allDefs.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Item && x.recipeMaker != null))
            //                     .Distinct().ToList();
            return usableDefs;
        }


        [TweakValue("RimAges", -1000f, 1000f)]
        public static float rectMin = -1000f;

        [TweakValue("RimAges", -100f, 100f)]
        public static float rectOff = 60f;

        [TweakValue("RimAges", -1000f, 1000f)]
        public static float buttonWidth = 120f;

        [TweakValue("RimAges", 0f, 100f)]
        public static float buttonHeight = 40f;

        [TweakValue("RimAges", -500f, 500f)]
        public static float spaceHeight = -500f;

        [TweakValue("RimAges", -100f, 100f)]
        public static float spaceOff = -50f;

        [TweakValue("RimAges", 0, 150)]
        public static int lines = 0;

        [TweakValue("RimAges", -150, 150)]
        public static float offset = 0;

        [TweakValue("RimAges", -250, 0)]
        public static float offsetY = 0;

        public static void DrawMain(Rect contentRect, RimAgesSettings settings) {
            Listing_Standard listingStandard = new Listing_Standard();

            Rect resetRect = contentRect;
            listingStandard.Begin(resetRect);
            DrawResetButton(resetRect, listingStandard);
            listingStandard.End();

            listingStandard.Begin(contentRect);
            listingStandard.CheckboxLabeled("No Starting Research", ref settings.noResearch, "Start the game with no research.");
            listingStandard.CheckboxLabeled("Enable Empty Research", ref settings.emptyResearch, "Enable research that has no unlocks.");

            listingStandard.End();
        }

        public static void DrawResearchCost(Rect contentRect, RimAgesSettings settings) {
            int normalInc = 100;
            int lockedInc = 0;

            Listing_Standard listingStandard = new Listing_Standard();

            Rect resetRect = contentRect;
            listingStandard.Begin(resetRect);
            DrawResetButton(resetRect, listingStandard);
            listingStandard.End();

            listingStandard.Begin(contentRect);

            listingStandard.Label($"Medieval Age Cost: {settings.MedievalAgeCost}", -1, "Cost of Medieval Age research.");
            listingStandard.IntAdjuster(ref settings.MedievalAgeCost, normalInc, 100);

            listingStandard.Label($"Industrial Age Cost: {settings.IndustrialAgeCost}", -1, "Cost of Industrial Age research");
            listingStandard.IntAdjuster(ref settings.IndustrialAgeCost, normalInc, 100);

            listingStandard.Label($"Spacer Age Cost: {settings.SpacerAgeCost}", -1, "Cost of Spacer Age research");
            listingStandard.IntAdjuster(ref settings.SpacerAgeCost, normalInc, 100);

            listingStandard.Label($"Ultra Age Cost: {settings.UltraAgeCost}", -1, "Cost of Ultra Age research");
            listingStandard.IntAdjuster(ref settings.UltraAgeCost, normalInc, 100);

            listingStandard.Label($"Archotech Age Cost: {settings.ArchotechAgeCost}", -1, $"Cost of Archotech Age research");
            listingStandard.IntAdjuster(ref settings.ArchotechAgeCost, normalInc, 100);

            listingStandard.Label($"Medieval Cooking Cost: {settings.MedievalCookingCost}", -1, "Cost of Medieval Cooking research");
            if (settings.MedievalCookingCost > 0) { listingStandard.IntAdjuster(ref settings.MedievalCookingCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.MedievalCookingCost, lockedInc, 0); }

            listingStandard.Label($"Medieval Defenses Cost: {settings.MedievalDefensesCost}", -1, "Cost of Medieval Defenses research");
            if (settings.MedievalDefensesCost > 0) { listingStandard.IntAdjuster(ref settings.MedievalDefensesCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.MedievalDefensesCost, lockedInc, 0); }

            listingStandard.Label($"Medieval Hygiene Cost: {settings.MedievalHygieneCost}", -1, "Cost of Medieval Hygiene research");
            if (settings.MedievalHygieneCost > 0) { listingStandard.IntAdjuster(ref settings.MedievalHygieneCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.MedievalHygieneCost, lockedInc, 0); }

            listingStandard.Label($"Medieval Research Cost: {settings.MedievalResearchCost}", -1, "Cost of Medieval Research research");
            if (settings.MedievalResearchCost > 0) { listingStandard.IntAdjuster(ref settings.MedievalResearchCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.MedievalResearchCost, lockedInc, 0); }

            listingStandard.Label($"Training Targets Cost: {settings.TrainingTargetsCost}", -1, "Cost of Training Targets research");
            if (settings.TrainingTargetsCost > 0) { listingStandard.IntAdjuster(ref settings.TrainingTargetsCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.TrainingTargetsCost, lockedInc, 0); }

            listingStandard.Label($"Spacer Plants Cost: {settings.SpacerPlantsCost}", -1, "Cost of Spacer Plants research");
            if (settings.SpacerPlantsCost > 0) { listingStandard.IntAdjuster(ref settings.SpacerPlantsCost, normalInc, 100); }
            else { listingStandard.IntAdjuster(ref settings.SpacerPlantsCost, lockedInc, 0); }

            listingStandard.End();
        }

        public static void DrawResearchTab(Rect contentRect, RimAgesSettings settings) {
            Listing_Standard listingStandard = new Listing_Standard();

            if (isDragging) { RimAges_Utility.Drag(dragging); }

            // Reset Button
            Rect resetRect = contentRect;
            listingStandard.Begin(resetRect);
            DrawResetButton(resetRect, listingStandard);
            listingStandard.End();

            // Current Research Button
            listingStandard.Begin(contentRect);

            Rect currentResearchRect = listingStandard.GetRect(buttonHeight); // Height of button
            currentResearchRect.xMin = (contentRect.width / 2) - (180 / 2);

            currentResearchRect = currentResearchRect.LeftPartPixels(180); // Width of button
            if (Widgets.ButtonText(currentResearchRect, currentResearch)) {
                if (!researchDropDownActive) { settings.Write(); researchDropDownActive = true; }
            }

            // Filter Button
            Rect filterRect = listingStandard.GetRect(buttonHeight); // Height of button
            filterRect = filterRect.LeftPartPixels(buttonWidth); // Width of button
            if (Widgets.ButtonText(filterRect, "Filters")) {
                if (!filterDropDownActive) { filterDropDownActive = true; }
            }

            // Search Bar
            Rect searchRect = filterRect;
            searchRect.height /= 2;
            searchRect.y += (buttonHeight / 2);
            searchRect.x += buttonWidth + 5;
            searchRect.width = buttonWidth + 75;
            searchFilter = Widgets.TextField(searchRect, searchFilter);

            // Filter Left Def Dict
            List<Def> leftDefList = leftDefDict.Keys.ToList();
            Dictionary<Def, List<ResearchProjectDef>> filteredLeftDefDict = FilterDefs(leftDefList);

            // Results
            Rect resultsRect = searchRect;
            resultsRect.width = 90;
            resultsRect.x = searchRect.xMax + 5;
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(resultsRect, $"Results: {filteredLeftDefDict.Count}");
            Text.Anchor = anchor;

            // Scroll Rects
            Rect scrollRect = listingStandard.GetRect(contentRect.height - (buttonHeight * 3) - 20);
            scrollRect.y += 10;
            Rect leftScrollRect = scrollRect;
            leftScrollRect.width = (leftScrollRect.width / 2) - 5;
            Widgets.DrawWindowBackground(leftScrollRect);
            Rect leftScrollPosRect = leftScrollRect.ContractedBy(10);

            Rect rightScrollRect = scrollRect;
            rightScrollRect.width = (rightScrollRect.width / 2) - 5;
            rightScrollRect.x = (scrollRect.width / 2) + 5;
            Widgets.DrawWindowBackground(rightScrollRect);
            Rect rightScrollPosRect = rightScrollRect.ContractedBy(10);

            // Add/Remove defs from dicts
            Vector2 mousePos = Event.current.mousePosition;
            if ((mousePos.x > leftScrollRect.xMin && mousePos.x < leftScrollRect.xMax) && (mousePos.y > leftScrollRect.yMin && mousePos.y < leftScrollRect.yMax)) { // Add to left side
                if (isDragging) { // Draw highlight
                    // TODO Make highlight draw over all scroll items
                    Rect leftHighlight = leftScrollRect;
                    Widgets.DrawHighlight(leftHighlight);
                }
                if (Input.GetMouseButtonUp(0) && isDragging) {
                    isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) {
                        if (!leftDefDict.ContainsKey(dragging.Item1)) {
                            leftDefDict.Add(dragging.Item1, dragging.Item2);
                        }
                        if (rightDefDict.ContainsKey(dragging.Item1)) {
                            rightDefDict.Remove(dragging.Item1);
                        }
                    }
                }
            }
            if ((mousePos.x > rightScrollRect.xMin && mousePos.x < rightScrollRect.xMax) && (mousePos.y > rightScrollRect.yMin && mousePos.y < rightScrollRect.yMax)) { // Add to right side
                if (isDragging) { // Draw highlight
                    // TODO Make highlight draw over all scroll items
                    Rect rightHighlight = rightScrollRect;
                    Widgets.DrawHighlight(rightHighlight);
                }
                if (Input.GetMouseButtonUp(0) && isDragging) {
                    isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) {
                        if (!rightDefDict.ContainsKey(dragging.Item1)) {
                            rightDefDict.Add(dragging.Item1, dragging.Item2);
                            if (leftDefDict.ContainsKey(dragging.Item1)) {
                                leftDefDict.Remove(dragging.Item1);
                            }
                        }
                    }
                }
            }

            // Reset dragging
            if (Input.GetMouseButtonUp(0) && isDragging) { isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) { dragging = (null, null, new Vector2(0, 0)); } }

            // Left Scroll
            int lineHeight = 50;
            Rect leftDefListRect = new Rect(0, 0, leftScrollPosRect.width - 30, filteredLeftDefDict.Count * lineHeight);

            DefScroll leftDefScroll = new DefScroll(leftDefListRect, leftScrollPosRect, listingStandard, lineHeight);
            leftDefScroll.DrawDefScroll(ref leftScrollPos, filteredLeftDefDict);

            // Right Scroll
            Rect rightDefListRect = new Rect(0, 0, rightScrollPosRect.width - 30, rightDefDict.Count * lineHeight);

            DefScroll rightDefScroll = new DefScroll(rightDefListRect, rightScrollPosRect, listingStandard, lineHeight);
            rightDefScroll.DrawDefScroll(ref rightScrollPos, rightDefDict);

            listingStandard.End();

            // Current Research Drop Down
            if (researchDropDownActive) {
                float researchDropDownSizeX = 400;
                float researchDropDownSizeY = 360;
                Rect researchDropDown = new Rect((Screen.width / 2) - (researchDropDownSizeX / 2), ((Screen.height / 2) - (researchDropDownSizeY / 2)) - 65, researchDropDownSizeX, researchDropDownSizeY);
                Find.WindowStack.ImmediateWindow(12, researchDropDown, WindowLayer.Super, () => {
                    DrawResearchDropDown(researchDropDown, listingStandard);
                }, true, true, 1, () => { researchDropDownActive = false; });
            }

            // Filter Drop Down
            if (filterDropDownActive) {
                float filterDropDownSizeX = 400;
                float filterDropDownSizeY = 360;
                Rect filterDropDown = new Rect((Screen.width / 2) - (filterDropDownSizeX / 2), ((Screen.height / 2) - (filterDropDownSizeY / 2)) - 65, filterDropDownSizeX, filterDropDownSizeY);
                Find.WindowStack.ImmediateWindow(11, filterDropDown, WindowLayer.Super, () => {
                    DrawFilterDropDown(filterDropDown, listingStandard);
                }, true, true, 1, () => { filterDropDownActive = false; });
            }
        }

        public static Dictionary<Def, List<ResearchProjectDef>> UpdateDefDict(string researchName) { // Make/Update defDict for left & right scroll lists
            Dictionary<Def, List<ResearchProjectDef>> defDict = new Dictionary<Def, List<ResearchProjectDef>>();
            if (researchName == null) {
                List<Def> defList = allDefs;
                for (int i = 0; i < defList.Count; i++) {
                    Def currentDef = defList[i];
                    Def keyDef = null;
                    List<ResearchProjectDef> valueDef;
                    bool valid;
                    (valueDef, valid) = GetResearchProjectDef(currentDef);
                    if (valid) { // True if current def has a researchPrerequisite(s) tag
                        keyDef = currentDef;
                    }
                    if (keyDef != null) {
                        defDict.Add(keyDef, valueDef);
                    }
                }
            }
            else {
                List<Def> defList = GetUsableDefs();
                researchName = currentResearch.Replace(" ", "");
                foreach (Def def in defList) {
                    var research = GetResearchProjectDef(def);
                    if ((!research.researchDef.NullOrEmpty()) && research.valid == true) {
                        if (research.researchDef.Contains(DefDatabase<ResearchProjectDef>.GetNamed($"{researchName}"))) {
                            defDict.Add(def, research.researchDef);
                            if (isDragging) {
                                if (dragging != (null, null, new Vector2(0, 0))) {
                                    if (!(defDict.ContainsKey(dragging.Item1))) {
                                        defDict.Add(dragging.Item1, dragging.Item2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return defDict;
        }

        public static (List<ResearchProjectDef> researchDef, bool valid) GetResearchProjectDef(Def def) {
            switch ($"{def.GetType()}") {
#pragma warning disable 0168
                case "Verse.TerrainDef":
                    try {
                        List<ResearchProjectDef> researchList = DefDatabase<TerrainDef>.GetNamed(def.defName).researchPrerequisites;
                        if (researchList.Count != 0) {
                            return (researchList.Distinct().ToList(), true);
                        }
                    }
                    catch (Exception e) {
                        //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                        return (null, false);
                    }
                    break;
                case "Verse.ThingDef":
                    if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Building && DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker == null) {
                        try {
                            List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(def.defName).researchPrerequisites;
                            if (researchList.Count != 0) {
                                return (researchList.Distinct().ToList(), true);
                            }
                        }
                        catch (Exception e) {
                            //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                            return (null, false);
                        }
                    }
                    else if (DefDatabase<ThingDef>.GetNamed(def.defName).category == ThingCategory.Plant) {
                        try {
                            List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(def.defName).plant.sowResearchPrerequisites;
                            if (researchList.Count != 0) {
                                return (researchList.Distinct().ToList(), true);
                            }
                        }
                        catch (Exception e) {
                            //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                            return (null, false);
                        }
                    }
                    else if (DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker != null) {
                        if (DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite != null) {
                            return (new List<ResearchProjectDef> { DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite }, true);
                        }
                    }
                    break;
                case "Verse.RecipeDef":
                    if (DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite != null) {
                        return (new List<ResearchProjectDef> { DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite }, true);
                    }
                    break;
#pragma warning restore 0168
            }
            return (null, true);
        }

        public static Dictionary<Def, List<ResearchProjectDef>> FilterDefs(List<Def> defList) {
            Dictionary<Def, List<ResearchProjectDef>> defDict = new Dictionary<Def, List<ResearchProjectDef>>();
            for (int i = 0; i < defList.Count; i++) {
                Def currentDef = defList[i];
                Def keyDef = null;
                List<ResearchProjectDef> valueDef = new List<ResearchProjectDef>();
                if (assignedDefsFilter) { // any filters == true
                    if (assignedDefsFilter) {
#pragma warning disable 0168
                        switch ($"{currentDef.GetType()}") {
                            case "Verse.TerrainDef":
                                try {
                                    if (DefDatabase<TerrainDef>.GetNamed(currentDef.defName).researchPrerequisites.Count == 0) { keyDef = currentDef; }
                                }
                                catch (Exception e) {
                                    Log.Warning($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                }
                                break;
                            case "Verse.ThingDef":
                                if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).category == ThingCategory.Building) {
                                    try {
                                        if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).researchPrerequisites.Count == 0) { keyDef = currentDef; }
                                    }
                                    catch (Exception e) {
                                        Log.Warning($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                    }
                                }
                                else if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).category == ThingCategory.Plant) {
                                    try {
                                        if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).plant.sowResearchPrerequisites.Count == 0) { keyDef = currentDef; }
                                    }
                                    catch (Exception e) {
                                        Log.Warning($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                    }
                                }
                                else if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).category == ThingCategory.Item && DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker != null) {
                                    try {
                                        if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite == null) { keyDef = currentDef; }
                                    }
                                    catch (Exception e) {
                                        Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                    }
                                }
                                break;
                            case "Verse.RecipeDef":
                                try {
                                    if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite == null) { keyDef = currentDef; }
                                }
                                catch (Exception e) {
                                    Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                }
                                break;
#pragma warning restore 0168
                        }
                    }
                }
                else { // If no filters are enabled
#pragma warning disable 0168
                    switch ($"{currentDef.GetType()}") {
                        case "Verse.TerrainDef":
                            try {
                                List<ResearchProjectDef> researchList = DefDatabase<TerrainDef>.GetNamed(currentDef.defName).researchPrerequisites;
                                keyDef = currentDef;
                                if (researchList.Count != 0) {
                                    valueDef = researchList.Distinct().ToList();
                                }
                            }
                            catch (Exception e) {
                                //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                            }
                            break;
                        case "Verse.ThingDef":
                            if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).category == ThingCategory.Building && DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker == null) {
                                try {
                                    List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(currentDef.defName).researchPrerequisites;
                                    keyDef = currentDef;
                                    if (researchList.Count != 0) {
                                        valueDef = researchList.Distinct().ToList();
                                    }
                                }
                                catch (Exception e) {
                                    //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                                }
                            }
                            else if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).category == ThingCategory.Plant) {
                                try {
                                    List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(currentDef.defName).plant.sowResearchPrerequisites;
                                    keyDef = currentDef;
                                    if (researchList.Count != 0) {
                                        valueDef = researchList.Distinct().ToList();
                                    }
                                }
                                catch (Exception e) {
                                    //Log.Error($"[RimAges] ERROR: {currentDef.defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.\n{e}");
                                }
                            }
                            else if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker != null) {
                                keyDef = currentDef;
                                if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite != null) {
                                    valueDef.Add(DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite);
                                }
                            }
                            break;
                        case "Verse.RecipeDef":
                            keyDef = currentDef;
                            try {
                                if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite != null) {
                                    valueDef.Add(DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite);
                                }
                            }
                            catch (Exception e) {
                                Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                            }
                            break;
#pragma warning restore 0168
                    }
                }
                if (keyDef != null) {
                    defDict.Add(keyDef, valueDef);
                }
            }
            if (searchFilter != null) { defDict = searchDefs(defDict); }
            return defDict;
        }

        // Return searched list of defs
        public static Dictionary<Def, List<ResearchProjectDef>> searchDefs(Dictionary<Def, List<ResearchProjectDef>> defDict) {
            // Check if searched term is in def.label def.defName or defs research
            Dictionary<Def, List<ResearchProjectDef>> searchedDefs = new Dictionary<Def, List<ResearchProjectDef>>();
            string search = searchFilter.ToLower();
            string[] searchWords = search.Split(',');
            List<string> keywords = new List<string> { "#name", "#def", "#type", "#research", "#mod" };

            if (searchWords.Length > 1) { // Multiple Terms
                List<string> foundKeywords = new List<string>();
                foreach (string word in searchWords) { // Check for keywords and add them to a list
                    string foundKeyword = null;
                    foreach (string keyword in keywords) {
                        if (word.Contains(keyword)) { foundKeyword = keyword; }
                    }
                    foundKeywords.Add(foundKeyword);
                }
                for (int i = 0; i < searchWords.Length; i++) { // check each searched term
                    search = searchWords[i];
                    search = search.Trim();

                    string modifier = null;
                    if (search.Length > 0) { // Remove keywords
                        if (search[0] == '#') {
                            string[] split = search.Split(' ');
                            if (split.Length > 1) {
                                search = "";
                                for (int n = 0; n < split.Length; n++) {
                                    if (n != 0) {
                                        search += split[n];
                                    }
                                }
                            }
                        }
                        if (search[0] == '!') { modifier = "!"; search = search.Replace("!", ""); }
                    }
                    search = search.Replace(" ", "");

                    if (i > 0) {
                        defDict.Clear();
                        foreach (var item in searchedDefs) { defDict.Add(item.Key, item.Value); }
                        searchedDefs.Clear();
                    }
                    if (search == null) { continue; }

                    foreach (var item in defDict) {
                        string label = item.Key.label.ToLower().Replace(" ", "");
                        string defName = item.Key.defName.ToLower().Replace(" ", "");
                        string defType = item.Key.GetType().ToString().ToLower().Replace("verse.", "");
                        string modTag = item.Key.modContentPack.ToStringSafe().ToLower().Replace(" ", "");
                        List<string> researchNames = new List<string>();
                        if (!item.Value.NullOrEmpty()) {
                            foreach (ResearchProjectDef research in item.Value) {
                                researchNames.Add(research.defName.ToLower().Replace(" ", ""));
                            }
                        }
                        else { researchNames.Add("nullnonenotfound"); } // allows terms: null / none / not found
                        if (foundKeywords[i] != null) {
                            switch (foundKeywords[i]) {
                                case "#name":
                                    if (modifier == "!") { if (!(label.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (label.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#def":
                                    if (modifier == "!") { if (!(defName.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (defName.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#type":
                                    if (modifier == "!") { if (!(defType.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (defType.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#research":
                                    if (modifier == "!") {
                                        bool found = false;
                                        foreach (string research in researchNames) {
                                            if ((research.Contains(search))) { found = true; break; }
                                        }
                                        if (!found) { searchedDefs.Add(item.Key, item.Value); }
                                    }
                                    else {
                                        if (researchNames.Contains(search)) {
                                            searchedDefs.Add(item.Key, item.Value);
                                        }
                                    }
                                    break;
                                case "#mod":
                                    if (modifier == "!") { if (!(modTag.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (modTag.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                            }
                        }
                        else if (modifier == "!") {
                            bool found = false;
                            foreach (string research in researchNames) {
                                if ((label.Contains(search) || defName.Contains(search) || defType.Contains(search) || research.Contains(search) || modTag.Contains(search))) { found = true; break; }
                            }
                            if (!found) { searchedDefs.Add(item.Key, item.Value); }
                        }
                        else if (label.Contains(search) || defName.Contains(search) || defType.Contains(search) || modTag.Contains(search)) { searchedDefs.Add(item.Key, item.Value); }
                        else {
                            foreach (string research in researchNames) {
                                if (research.Contains(search)) { searchedDefs.Add(item.Key, item.Value); }
                            }
                        }
                    }
                }
            }
            else { // Single term
                search = search.Trim();
                string foundKeyword = null;
                string modifier = null;
                if (search.Length > 0) {
                    foreach (string keyword in keywords) {
                        if (search.Contains(keyword)) { foundKeyword = keyword; }
                    }
                    if (search[0] == '#') {
                        string[] split = search.Split(' ');
                        if (split.Length > 1) {
                            search = "";
                            for (int i = 0; i < split.Length; i++) {
                                if (i != 0) {
                                    search += split[i];
                                }
                            }
                        }
                    }
                    if (search[0] == '!') { modifier = "!"; search = search.Replace("!", ""); }
                }
                search = search.Replace(" ", "");
                foreach (var item in defDict) {
                    string label = item.Key.label.ToLower().Replace(" ", "");
                    string defName = item.Key.defName.ToLower().Replace(" ", "");
                    string defType = item.Key.GetType().ToString().ToLower().Replace("verse.", "");
                    string modTag = item.Key.modContentPack.ToStringSafe().ToLower().Replace(" ", "");
                    List<string> researchNames = new List<string>();
                    if (!item.Value.NullOrEmpty()) {
                        foreach (ResearchProjectDef research in item.Value) {
                            researchNames.Add(research.defName.ToLower().Replace(" ", ""));
                        }
                    }
                    else { researchNames.Add("nullnonenotfound"); } // allows terms: null / none / not found
                    if (foundKeyword != null) {
                        switch (foundKeyword) {
                            case "#name":
                                if (modifier == "!") { if (!(label.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (label.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#def":
                                if (modifier == "!") { if (!(defName.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (defName.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#type":
                                if (modifier == "!") { if (!(defType.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (defType.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#research":
                                if (modifier == "!") {
                                    bool found = false;
                                    foreach (string research in researchNames) {
                                        if ((research.Contains(search))) { found = true; break; }
                                    }
                                    if (!found) { searchedDefs.Add(item.Key, item.Value); }
                                }
                                else {
                                    if (researchNames.Contains(search)) {
                                        searchedDefs.Add(item.Key, item.Value);
                                    }
                                }
                                break;
                            case "#mod":
                                if (modifier == "!") { if (!(modTag.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (modTag.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                        }
                    }
                    else if (modifier == "!") {
                        bool found = false;
                        foreach (string research in researchNames) {
                            if ((label.Contains(search) || defName.Contains(search) || defType.Contains(search) || research.Contains(search) || modTag.Contains(search))) { found = true; break; }
                        }
                        if (!found) { searchedDefs.Add(item.Key, item.Value); }
                    }
                    else if (label.Contains(search) || defName.Contains(search) || defType.Contains(search) || modTag.Contains(search)) { searchedDefs.Add(item.Key, item.Value); }
                    else {
                        foreach (string research in researchNames) {
                            if (research.Contains(search)) { searchedDefs.Add(item.Key, item.Value); }
                        }
                    }
                }
            }
            return searchedDefs;
        }

        // Makes a scrollable list of defs
        public class DefScroll {
            public Dictionary<Def, List<ResearchProjectDef>> defDict;
            public Rect defListRect;
            public Rect scrollPosRect;
            public Listing_Standard listingStandard;
            public int lineHeight;


            public DefScroll(Rect _defListRect, Rect _scrollPosRect, Listing_Standard _listingStandard, int _lineHeight) {
                defListRect = _defListRect;
                scrollPosRect = _scrollPosRect;
                listingStandard = _listingStandard;
                lineHeight = _lineHeight;
            }

            public void DrawDefScroll(ref Vector2 _scrollPos, Dictionary<Def, List<ResearchProjectDef>> _defDict) {
                defDict = _defDict;
                Widgets.BeginScrollView(scrollPosRect, ref _scrollPos, defListRect, true);
                listingStandard.Begin(defListRect);
                int cellPosition;
                int lineNumber;
                lineNumber = cellPosition = 0;

                foreach (var item in defDict) {
                    cellPosition += lineHeight;
                    lineNumber++;

                    Rect rect = listingStandard.GetRect(lineHeight);
                    Widgets.DrawWindowBackground(rect);
                    if (lineNumber % 2 != 1) { Widgets.DrawLightHighlight(rect); }
                    Def currentDef = item.Key;
                    List<ResearchProjectDef> researchDef = item.Value;
                    DrawListItem(rect, currentDef, researchDef);

                    if (Mouse.IsOver(rect)) {
                        if (isDragging == false) { Widgets.DrawLightHighlight(rect); }
                        if (Input.GetMouseButtonDown(0) && isDragging == false) { dragging = (currentDef, researchDef, rect.size); isDragging = true; }
                    }

                }
                listingStandard.End();
                Widgets.EndScrollView();
            }
        }

        public static void DrawListItem(Rect rect, Def currentDef, List<ResearchProjectDef> researchDef) {
            Rect labelItemRect = rect.ContractedBy(5);
            labelItemRect.height = 22;
            Widgets.Label(labelItemRect, $"{currentDef.label.CapitalizeFirst()}");

            string mod = currentDef.modContentPack.ToStringSafe();

            Text.Font = GameFont.Tiny;
            Rect defItemRect = labelItemRect;
            defItemRect.y += 25;
            Widgets.Label(defItemRect, $"[{mod}]");


            Rect researchItemRect = labelItemRect;
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperRight;
            if (!researchDef.NullOrEmpty()) {
                if (researchDef.Count > 1) {
                    Widgets.Label(researchItemRect, $"{researchDef[0]}+{researchDef.Count - 1}");
                }
                else {
                    Widgets.Label(researchItemRect, $"{researchDef[0]}");
                }
            }
            else if ($"{currentDef.GetType()}" == "Verse.ThingDef") {
                if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker != null || DefDatabase<ThingDef>.GetNamed(currentDef.defName).researchPrerequisites != null) {
                    Widgets.Label(researchItemRect, $"None");
                }
                else {
                    Widgets.Label(researchItemRect, $"Not Found");
                }
            }
            else {
                Widgets.Label(researchItemRect, $"Not Found");
            }

            Rect typeItemRect = defItemRect;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(typeItemRect, $"{currentDef.GetType().ToString().Replace("Verse.", "")}");
            Text.Anchor = anchor;
            Text.Font = GameFont.Small;
        }

        public static void DrawResearchDropDown(Rect dropDown, Listing_Standard listingStandard) {
            Rect dropRect = dropDown.ContractedBy(10f);
            dropRect.position = new Vector2(10, 10);

            Rect leftRect = dropRect;
            leftRect.xMax -= (leftRect.width / 2) + 10;

            Rect rightRect = dropRect;
            rightRect.xMin += (rightRect.width / 2) + 10;

            listingStandard.Begin(leftRect);
            if (listingStandard.RadioButton("Medieval Age", currentResearch == "Medieval Age")) {
                currentResearch = "Medieval Age";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Industrial Age", currentResearch == "Industrial Age")) {
                currentResearch = "Industrial Age";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Spacer Age", currentResearch == "Spacer Age")) {
                currentResearch = "Spacer Age";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Ultra Age", currentResearch == "Ultra Age")) {
                currentResearch = "Ultra Age";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Archotech Age", currentResearch == "Archotech Age")) {
                currentResearch = "Archotech Age";
                researchDropDownActive = false;
            }
            listingStandard.End();

            listingStandard.Begin(rightRect);
            if (listingStandard.RadioButton("Medieval Cooking", currentResearch == "Medieval Cooking")) {
                currentResearch = "Medieval Cooking";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Medieval Defenses", currentResearch == "Medieval Defenses")) {
                currentResearch = "Medieval Defenses";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Medieval Hygiene", currentResearch == "Medieval Hygiene")) {
                currentResearch = "Medieval Hygiene";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Medieval Research", currentResearch == "Medieval Research")) {
                currentResearch = "Medieval Research";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Training Targets", currentResearch == "Training Targets")) {
                currentResearch = "Training Targets";
                researchDropDownActive = false;
            }
            if (listingStandard.RadioButton("Spacer Plants", currentResearch == "Spacer Plants")) {
                currentResearch = "Spacer Plants";
                researchDropDownActive = false;
            }
            listingStandard.End();

            leftDefDict = UpdateDefDict(null);
            rightDefDict = UpdateDefDict(currentResearch);
        }

        public static void DrawFilterDropDown(Rect dropDown, Listing_Standard listingStandard) {
            Rect dropRect = dropDown.ContractedBy(10f);
            dropRect.position = new Vector2(10, 10);

            listingStandard.Begin(dropRect);
            listingStandard.CheckboxLabeled("Exclude Assigned Defs", ref assignedDefsFilter);
            listingStandard.End();
        }

        public static void DrawResearchFilters(Rect contentRect, RimAgesSettings settings) {
            Listing_Standard listingStandard = new Listing_Standard();

            Rect backRect = contentRect;
            listingStandard.Begin(backRect);
            Rect space = listingStandard.GetRect(backRect.height - buttonHeight);
            Widgets.Label(space, "");

            Rect rect = listingStandard.GetRect(buttonHeight); // Height of button
            rect.xMin = (backRect.width / 2) - (buttonWidth / 2);

            rect = rect.LeftPartPixels(buttonWidth); // Width of button
            if (Widgets.ButtonText(rect, "Back")) {
                currentPage = 0;
            }
            listingStandard.End();

            bool test = false;
            listingStandard.Begin(contentRect);
            listingStandard.Label("Filters");
            listingStandard.CheckboxLabeled("Include Assigned Defs", ref test, "Test");
            listingStandard.GapLine();
            listingStandard.CheckboxLabeled("Placeholder", ref test, "Test");
            listingStandard.GapLine();
            listingStandard.End();
        }

        public static void DrawResetButton(Rect contentRect, Listing_Standard listingStandard) {
            if (spaceHeight == -500f) {
                Rect space = listingStandard.GetRect(contentRect.height - buttonHeight);
                Widgets.Label(space, "");
            }
            else {
                Rect space = listingStandard.GetRect(contentRect.height + spaceOff);
                Widgets.Label(space, "");
            }

            Rect rect = listingStandard.GetRect(buttonHeight); // Height of button
            if (rectMin == -1000f) {
                rect.xMin = (contentRect.width / 2) - rectOff;
            }
            else {
                rect.xMin = rectMin;
            }

            rect = rect.LeftPartPixels(buttonWidth); // Width of button
            if (Widgets.ButtonText(rect, "Reset")) {
                Log.Warning($"{RimAges.modTag} Pressed!");
                TaggedString taggedStringConfirm = "Reset";
                TaggedString taggedStringCancel = "Cancel";
                Find.WindowStack.Add(new Dialog_MessageBox("Are you sure you want to reset your settings to default?\n(This only resets settings on the current page.)", taggedStringConfirm, ResetSettings, taggedStringCancel, null, null, true)); // Replace with better wording?
            }
            void ResetSettings() {
                RimAgesDefaults.RimAgesSettingsReset();
                lines = 0;
            }
        }

        public static void DrawResearchSelection(Rect contentRect, RimAgesSettings settings) {  // OLD
            Listing_Standard listingStandard = new Listing_Standard();

            Rect backRect = contentRect;
            listingStandard.Begin(backRect);
            Rect space = listingStandard.GetRect(backRect.height - buttonHeight);
            Widgets.Label(space, "");

            Rect rect = listingStandard.GetRect(buttonHeight); // Height of button
            rect.xMin = (backRect.width / 2) - (buttonWidth / 2);

            rect = rect.LeftPartPixels(buttonWidth); // Width of button
            if (Widgets.ButtonText(rect, "Back")) {
            }
            listingStandard.End();

            (Vector2 pos, Vector2 size) bounds = (new Vector2(10, 250), new Vector2(contentRect.width, 300));
            Rect boundsBox = new Rect(bounds.pos, bounds.size);
            Widgets.DrawWindowBackground(boundsBox);

            Rect dragRect = new Rect(boxPos.x + 5, boxPos.y + 5, 120, 40);
            if (Input.GetMouseButtonDown(0)) {
                Vector2 pos = Event.current.mousePosition;
                if ((pos.x >= boxPos.x && pos.x <= (boxPos.x + buttonWidth)) && (pos.y >= boxPos.y && pos.y <= (boxPos.y + buttonHeight))) {
                    isDragging = true;
                }
            }
            if (Input.GetMouseButtonUp(0)) {
                Vector2 pos = Event.current.mousePosition;
                if (isDragging) {
                    if (!((pos.x >= bounds.pos.x + (buttonWidth / 2) && pos.x <= (bounds.pos.x + bounds.size.x) - (buttonWidth / 2) - 6) && (pos.y >= bounds.pos.y + (buttonHeight / 2) && pos.y <= (bounds.pos.y + bounds.size.y) - (buttonHeight / 2)))) {
                        float x = pos.x - (buttonWidth / 2);
                        float y = pos.y - (buttonHeight / 2);
                        if (pos.x <= bounds.pos.x + (buttonWidth / 2)) {
                            x = bounds.pos.x - 4;
                        }
                        if (pos.x >= (bounds.pos.x + bounds.size.x) - (buttonWidth / 2) - 6) {
                            x = ((bounds.pos.x + bounds.size.x) - buttonWidth) - 6;
                        }
                        if (pos.y <= bounds.pos.y + (buttonHeight / 2)) {
                            y = bounds.pos.y - 4;
                        }
                        if (pos.y >= (bounds.pos.y + bounds.size.y) - (buttonHeight / 2)) {
                            y = ((bounds.pos.y + bounds.size.y) - 36); //Button height is incorrect (Current size: 30)
                        }
                        boxPos = new Vector2(x, y);
                    }
                }
                isDragging = false;
            }
            if (isDragging) {
                Vector2 pos = Event.current.mousePosition;
                boxPos = pos - new Vector2(buttonWidth / 2, buttonHeight / 2);
            }

            listingStandard.Begin(dragRect);
            if (listingStandard.ButtonText("Drag Me!")) {
                if (!isDragging) {
                    Log.Message($"RimAges: Dragging = {isDragging}");
                }
                else {
                    Log.Message($"RimAges: Dragging = {isDragging}");
                }
            }
            listingStandard.End();
        }

        public class RimAgesDefaults {
            private static readonly bool noResearch = true;
            private static readonly bool emptyResearch = true;

            private static readonly int MedievalAgeCost = 1000;
            private static readonly int IndustrialAgeCost = 2000;
            private static readonly int SpacerAgeCost = 3000;
            private static readonly int UltraAgeCost = 4000;
            private static readonly int ArchotechAgeCost = 5000;
            private static readonly int MedievalCookingCost = 1000;
            private static readonly int MedievalDefensesCost = 1000;
            private static readonly int MedievalHygieneCost = 1000;
            private static readonly int MedievalResearchCost = 1000;
            private static readonly int TrainingTargetsCost = 1000;
            private static readonly int SpacerPlantsCost = 1000;

            public static void RimAgesSettingsReset() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                switch (settings.tab) {
                    case 0:
                        settings.noResearch = noResearch;
                        settings.emptyResearch = emptyResearch;
                        break;
                    case 1:
                        settings.MedievalAgeCost = MedievalAgeCost;
                        settings.IndustrialAgeCost = IndustrialAgeCost;
                        settings.SpacerAgeCost = SpacerAgeCost;
                        settings.UltraAgeCost = UltraAgeCost;
                        settings.ArchotechAgeCost = ArchotechAgeCost;
                        settings.MedievalCookingCost = MedievalCookingCost;
                        settings.MedievalDefensesCost = MedievalDefensesCost;
                        settings.MedievalHygieneCost = MedievalHygieneCost;
                        settings.MedievalResearchCost = MedievalResearchCost;
                        settings.TrainingTargetsCost = TrainingTargetsCost;
                        settings.SpacerPlantsCost = SpacerPlantsCost;
                        break;
                    case 2:
                        RimAgesResearchChanges.Reset();
                        //foreach (var def in GetUsableDefs()) {
                        //    List<ResearchProjectDef> researchList = GetResearchProjectDef(def).researchDef;
                        //    if (!researchList.NullOrEmpty()) {
                        //        foreach (ResearchProjectDef researchDef in researchList) { RimAges.clearCacheList.Add(researchDef); }
                        //    }
                        //    RimAges.RemoveResearch(def);

                        //    List<ResearchProjectDef> research;
                        //    if (RimAges.defaultDefs.TryGetValue(def, out research)) {
                        //        if (!research.NullOrEmpty()) {
                        //            RimAges.AddResearch(def, research);
                        //        }
                        //    }
                        //}
                        leftDefDict = UpdateDefDict(null);
                        rightDefDict = UpdateDefDict(currentResearch);
                        settings.Write();
                        break;
                }
            }
        }
        public static class RimAgesBackup {
            public static void Save() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();

                if (settings.ResearchCostBackup == null) { Init(); }

                if (settings.MedievalCookingCost > 0) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; }
                if (settings.MedievalDefensesCost > 0) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; }
                if (settings.MedievalHygieneCost > 0) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; }
                if (settings.MedievalResearchCost > 0) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; }
                if (settings.TrainingTargetsCost > 0) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; }
                if (settings.SpacerPlantsCost > 0) { settings.ResearchCostBackup["SpacerPlantsCost"] = settings.SpacerPlantsCost; }
            }

            public static void Init() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                settings.ResearchCostBackup = new Dictionary<string, int>()
                {
                    {"MedievalCookingCost", 1000},
                    {"MedievalDefensesCost", 1000},
                    {"MedievalHygieneCost", 1000},
                    {"MedievalResearchCost", 1000},
                    {"TrainingTargetsCost", 1000},
                    {"SpacerPlantsCost", 1000}
                };
            }
        }

        public static class RimAgesResearchChanges {
            public static void Save() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                ResearchProjectDef currentResearchDef = DefDatabase<ResearchProjectDef>.GetNamed(currentResearch.Replace(" ", ""));
                List<string> items;

                if (settings.ResearchChanges == null) { Init(settings); settings.Write(); }

                List<Def> defList = GetUsableDefs().Where(x => GetResearchProjectDef(x).researchDef != null).Where(x => GetResearchProjectDef(x).researchDef.Contains(currentResearchDef)).ToList();
                List<string> defNameList = new List<string>();
                foreach (Def def in defList) { defNameList.Add(def.defName); }

                foreach (Def def in defList) {
                    if (settings.ResearchChanges.TryGetValue(currentResearchDef.defName, out items)) {
                        if (!items.Contains(def.defName)) {
                            items.Add(def.defName);
                        }
                    }
                }
                if (settings.ResearchChanges.TryGetValue(currentResearchDef.defName, out items)) {
                    List<string> removeList = new List<string>();
                    foreach (string defName in items) {
                        if (!defNameList.Contains(defName)) {
                            removeList.Add(defName);
                        }
                    }
                    foreach (string remove in removeList) {
                        items.Remove(remove);
                    }
                }
            }

            public static void Init(RimAgesSettings settings) {
                List<ResearchProjectDef> customResearch = DefDatabase<ResearchProjectDef>.AllDefs.Where(x => x.modContentPack.ToStringSafe().ToLower().Replace(" ", "") == "markflynnman.rimages").ToList();
                settings.ResearchChanges = new Dictionary<string, List<string>>();
                foreach (var def in customResearch) {
                    settings.ResearchChanges.Add(def.defName, new List<string> { "null" });
                }
            }

            public static void Reset() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                RimAges.clearCacheList = DefDatabase<ResearchProjectDef>.AllDefs.ToHashSet();
                settings.ResearchChanges.Clear();
                Init(settings);
                //settings.Write();

                foreach (var def in GetUsableDefs()) {
                    RimAges.RemoveResearch(def);

                    List<ResearchProjectDef> research;
                    if (RimAges.defaultDefs.TryGetValue(def, out research)) {
                        if (!research.NullOrEmpty()) {
                            RimAges.AddResearch(def, research);
                        }
                    }
                }
                //settings.Write();
            }

            public static void Remove(KeyValuePair<Def, List<ResearchProjectDef>> defDict) {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                foreach (ResearchProjectDef researchDef in defDict.Value) {
                    if (settings.ResearchChanges.TryGetValue(researchDef.defName, out List<string> items)) {
                        items.Remove(defDict.Key.defName);
                    }
                }
            }
        }
    }
}
