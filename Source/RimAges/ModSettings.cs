using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;
using static RimAges.RimAgesSettings;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse.Sound;

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
        public List<string> EmptyResearchOverride = new List<string>();

        public static int leftDefPageNum = 1;
        public static int rightDefPageNum = 1;
        public static int currentPage = 0;
        public static int scrollAmount;
        public static string currentResearch = "Medieval Age";
        public static bool isDragging = false;
        public static (Def, List<ResearchProjectDef>, Vector2) dragging;
        public static Vector2 boxPos = new Vector2(200, 300);
        public static bool researchDropDownActive = false;
        public static bool filterDropDownActive = false;
        public static bool assignedDefsFilter = false;
        public static bool costInputWindowActive = false;
        public static int costInput;
        public static string costInputBuffer;
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
            Scribe_Collections.Look(ref ResearchChanges, "ResearchChanges", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref EmptyResearchOverride, "EmptyResearchOverride", LookMode.Value);

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
                new TabRecord("Research Options", delegate {
                    settings.tab = 1;
                    settings.Write();
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
                DrawResearchOptions(inRect.ContractedBy(10f), settings);
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
            usableDefs = usableDefs.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => !x.IsBlueprint && !x.isMechClusterThreat && x.BuildableByPlayer && x.category == ThingCategory.Building)) // Building
                                   .Concat(DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Plant)) // Plants
                                   .Concat(DefDatabase<TerrainDef>.AllDefs.Where(x => x.IsFloor && !x.defName.Contains("Carpet") && !blacklist.Contains(x.defName))) // Floors
                                   .Concat(DefDatabase<RecipeDef>.AllDefs) // Recipes
                                   .Distinct().ToList(); // TODO: Filter out defs that are not useable by players to prevent "no researchPrerequisite" error logs... it causes massive lag
            //.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Item && x.recipeMaker != null))
            //allDefs = allDefs.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => x.category == ThingCategory.Item && x.recipeMaker != null))
            //                     .Distinct().ToList();

            List<Def> carpetList = new List<Def>(); // Only get one of each carpet type
            foreach (Def carpet in DefDatabase<TerrainDef>.AllDefs.Where(x => x.IsFloor && x.defName.Contains("Carpet"))) {
                bool inList = false;
                foreach (Def addedCarpet in carpetList) {
                    if (addedCarpet.label.Split('(')[0] == carpet.label.Split('(')[0]) { inList = true; break; }
                }
                if (!inList) { carpetList.Add(carpet); }
            }
            usableDefs = usableDefs.Concat(carpetList).Distinct().ToList();

            return usableDefs;
        }

        public static int GetCurrentResearchCost() {
            RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();

            switch (currentResearch) {
                case "Medieval Age":
                    return settings.MedievalAgeCost;
                case "Industrial Age":
                    return settings.IndustrialAgeCost;
                case "Spacer Age":
                    return settings.SpacerAgeCost;
                case "Ultra Age":
                    return settings.UltraAgeCost;
                case "Archotech Age":
                    return settings.ArchotechAgeCost;
                case "Medieval Cooking":
                    return settings.MedievalCookingCost;
                case "Medieval Defenses":
                    return settings.MedievalDefensesCost;
                case "Medieval Hygiene":
                    return settings.MedievalHygieneCost;
                case "Medieval Research":
                    return settings.MedievalResearchCost;
                case "Training Targets":
                    return settings.TrainingTargetsCost;
                case "Spacer Plants":
                    return settings.SpacerAgeCost;
                default:
                    return -1;
            }
        }

        public static void SetCurrentResarchCost(int cost) {
            RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();

            if (settings.ResearchCostBackup.Keys.Contains($"{currentResearch.Replace(" ", "")}Cost")) {
                if (!settings.emptyResearch) {
                    if (!settings.EmptyResearchOverride.NullOrEmpty()) {
                        if (!settings.EmptyResearchOverride.Contains(currentResearch.Replace(" ", ""))) {
                            return;
                        }
                    }
                    else {
                        return;
                    }
                }
            }

            switch (currentResearch) {
                case "Medieval Age":
                    settings.MedievalAgeCost = cost;
                    break;
                case "Industrial Age":
                    settings.IndustrialAgeCost = cost;
                    break;
                case "Spacer Age":
                    settings.SpacerAgeCost = cost;
                    break;
                case "Ultra Age":
                    settings.UltraAgeCost = cost;
                    break;
                case "Archotech Age":
                    settings.ArchotechAgeCost = cost;
                    break;
                case "Medieval Cooking":
                    settings.MedievalCookingCost = cost;
                    break;
                case "Medieval Defenses":
                    settings.MedievalDefensesCost = cost;
                    break;
                case "Medieval Hygiene":
                    settings.MedievalHygieneCost = cost;
                    break;
                case "Medieval Research":
                    settings.MedievalResearchCost = cost;
                    break;
                case "Training Targets":
                    settings.TrainingTargetsCost = cost;
                    break;
                case "Spacer Plants":
                    settings.SpacerAgeCost = cost;
                    break;
            }
            RimAges.ApplyEmptyResearch();
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
            if (RimAges_Utility.CustomCheckbox(listingStandard, "No Starting Research", ref settings.noResearch, "Start the game with no research.")) {
                RimAges.noResearch = !RimAges.noResearch;
                RimAges.ApplyEmptyResearch();
            }
            if (RimAges_Utility.CustomCheckbox(listingStandard, "Enable Empty Research", ref settings.emptyResearch, "Enable research that has no unlocks.")) {
                settings.MedievalCookingCost = settings.ResearchCostBackup["MedievalCookingCost"];
                settings.MedievalDefensesCost = settings.ResearchCostBackup["MedievalDefensesCost"];
                settings.MedievalHygieneCost = settings.ResearchCostBackup["MedievalHygieneCost"];
                settings.MedievalResearchCost = settings.ResearchCostBackup["MedievalResearchCost"];
                settings.TrainingTargetsCost = settings.ResearchCostBackup["TrainingTargetsCost"];
                settings.SpacerPlantsCost = settings.ResearchCostBackup["SpacerPlantsCost"];
                RimAges.emptyResearch = !RimAges.emptyResearch;
                RimAges.ApplyEmptyResearch();
            }
            listingStandard.End();
        }

        public static void DrawResearchOptions(Rect contentRect, RimAgesSettings settings) {
            Listing_Standard listingStandard = new Listing_Standard();

            // Reset Button
            Rect resetRect = contentRect;
            listingStandard.Begin(resetRect);
            DrawResetButton(resetRect, listingStandard);
            listingStandard.End();

            listingStandard.Begin(contentRect);

            // Current Research Button
            Rect currentResearchRect = listingStandard.GetRect(buttonHeight); // Height of button
            currentResearchRect.xMin = (contentRect.width / 2) - (180 / 2);

            currentResearchRect = currentResearchRect.LeftPartPixels(180); // Width of button
            if (Widgets.ButtonText(currentResearchRect, currentResearch)) {
                if (!researchDropDownActive) { settings.Write(); researchDropDownActive = true; }
            }

            // Current Research Drop Down
            if (researchDropDownActive) { DrawResearchDropDown(listingStandard); }

            // Buffer
            listingStandard.GetRect(60);

            // Background
            Rect background = contentRect;
            background.height -= (buttonHeight * 4) - 20;
            background.y += 48;
            background.x -= 10;
            Widgets.DrawWindowBackground(background);

            // Research Cost Setting
            Rect researchCostRect = listingStandard.GetRect(buttonHeight);

            Rect researchCostLabel = researchCostRect;
            researchCostLabel.width = 180;
            researchCostLabel.x += (researchCostRect.width / 2) - researchCostLabel.width / 2;

            Rect outerCostLabelBG = researchCostLabel;
            outerCostLabelBG.width += 2;
            outerCostLabelBG.x -= 1;
            Color bg = new Color(21f/255f, 25f/255f, 29f/255f);
            Widgets.DrawBoxSolidWithOutline(outerCostLabelBG, bg, Color.black);
            Rect innerCostLabelBG = researchCostLabel;
            innerCostLabelBG.height -= 2;
            innerCostLabelBG.y += 1;
            Widgets.DrawWindowBackground(innerCostLabelBG);
            Widgets.DrawLightHighlight(outerCostLabelBG);

            // Input Window
            if (Mouse.IsOver(researchCostLabel) && costInputWindowActive == false) {
                Widgets.DrawHighlight(outerCostLabelBG);
                if (Input.GetMouseButtonUp(0)) {
                    settings.Write();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    costInputWindowActive = true;
                    costInput = GetCurrentResearchCost();
                    costInputBuffer = GetCurrentResearchCost().ToString();
                    Log.Message($"current: {GetCurrentResearchCost()} | costInput: {costInput} | costInputBuffer: {costInputBuffer}");
                }
            }

            if (costInputWindowActive) {
                DrawCostInputWindow();
            }

            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(researchCostLabel, $"Research Cost: {GetCurrentResearchCost()}");
            Text.Anchor = anchor;

            // + / - Buttons
            Rect decreaseRect10 = researchCostLabel;
            decreaseRect10.width = 50;
            decreaseRect10.x -= decreaseRect10.width;
            Rect decreaseRect100 = decreaseRect10;
            decreaseRect100.x -= decreaseRect10.width;
            Rect decreaseRect1000 = decreaseRect100;
            decreaseRect1000.x -= decreaseRect100.width;

            Rect increaseRect10 = researchCostLabel;
            increaseRect10.width = 50;
            increaseRect10.x += researchCostLabel.width;
            Rect increaseRect100 = increaseRect10;
            increaseRect100.x += increaseRect10.width;
            Rect increaseRect1000 = increaseRect100;
            increaseRect1000.x += increaseRect100.width;

            int currentCost = GetCurrentResearchCost();

            if (currentCost > 0) {
                if (Widgets.ButtonText(decreaseRect10, "-10")) {
                    if (currentCost - 10 <= 0) {
                        SetCurrentResarchCost(0);
                    }
                    else {
                        SetCurrentResarchCost(currentCost - 10);
                    }
                    Log.Message("-10"); 
                }
                if (Widgets.ButtonText(decreaseRect100, "-100")) {
                    if (currentCost - 100 <= 0) {
                        SetCurrentResarchCost(0);
                    }
                    else {
                        SetCurrentResarchCost(currentCost - 100);
                    }
                    Log.Message("-100"); 
                }
                if (Widgets.ButtonText(decreaseRect1000, "-1000")) {
                    if (currentCost - 1000 <= 0) {
                        SetCurrentResarchCost(0);
                    }
                    else {
                        SetCurrentResarchCost(currentCost - 1000);
                    }
                    Log.Message("-1000"); 
                }
            }
            
            int maxCost = 1_000_000;
            if (currentCost < maxCost) {
                if (Widgets.ButtonText(increaseRect10, "+10")) {
                    if (currentCost + 10 >= maxCost) {
                        SetCurrentResarchCost(maxCost);
                    }
                    else {
                        SetCurrentResarchCost(currentCost + 10);
                    }
                    Log.Message("+10");
                }
                if (Widgets.ButtonText(increaseRect100, "+100")) {
                    if (currentCost + 100 >= maxCost) {
                        SetCurrentResarchCost(maxCost);
                    }
                    else {
                        SetCurrentResarchCost(currentCost + 100);
                    }
                    Log.Message("+100");
                }
                if (Widgets.ButtonText(increaseRect1000, "+1000")) {
                    if (currentCost + 1000 >= maxCost) {
                        SetCurrentResarchCost(maxCost);
                    }
                    else {
                        SetCurrentResarchCost(currentCost + 1000);
                    }
                    Log.Message("+1000");
                }
            }

            // Buffer
            listingStandard.GetRect(9);

            // Override Empty Research
            bool emptyOverride;
            if (!settings.EmptyResearchOverride.NullOrEmpty()) {
                emptyOverride = settings.EmptyResearchOverride.Contains(currentResearch.Replace(" ", ""));
            }
            else {
                emptyOverride = false;
            }

            if (!settings.emptyResearch && settings.ResearchCostBackup.Keys.Contains($"{currentResearch.Replace(" ", "")}Cost")) {
                Rect overrideRect = listingStandard.GetRect(buttonHeight);
                overrideRect.width -= 20;
                overrideRect.x += 10;

                Listing_Standard overrideList = new Listing_Standard();
                overrideList.Begin(overrideRect);
                if (RimAges_Utility.CustomCheckboxButton(overrideList, "Override Empty Research Setting", emptyOverride, "Override the empty research option to allow this research to have a cost.")) {
                    if (!emptyOverride) {
                        if (settings.EmptyResearchOverride.NullOrEmpty()) {
                            settings.EmptyResearchOverride = new List<string> { currentResearch.Replace(" ", "") };
                        }
                        else {
                            settings.EmptyResearchOverride.Add(currentResearch.Replace(" ", ""));
                        }
                        // Set current cost to backup to avoid instantly setting it to 0
                        SetCurrentResarchCost(settings.ResearchCostBackup[$"{currentResearch.Replace(" ", "")}Cost"]);
                    }
                    else {
                        settings.EmptyResearchOverride.Remove(currentResearch.Replace(" ", ""));
                    }
                    RimAges.ApplyEmptyResearch();
                }
                overrideList.End();
            }

            listingStandard.End();
        }

        public static void DrawResearchTab(Rect contentRect, RimAgesSettings settings) {
            Vector2 currentMousePos = Event.current.mousePosition;
            TextAnchor anchor = Text.Anchor;
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
            currentResearchRect.xMin = (contentRect.width / 2) - (180/2);
            
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

            // Search Modifier Help Window
            if (Mouse.IsOver(searchRect)) {
                float searchHelpSizeX = 325;
                float searchHelpSizeY = 150;
                Vector2 mousePos = Input.mousePosition;
                Rect searchHelpRect = new Rect(mousePos.x - searchHelpSizeX, Screen.height + (mousePos.y * -1), searchHelpSizeX, searchHelpSizeY);
                Find.WindowStack.ImmediateWindow(13, searchHelpRect, WindowLayer.Super, () => {
                    Rect dropRect = searchHelpRect.ContractedBy(5f);
                    dropRect.position = new Vector2(10, 10);

                    anchor = Text.Anchor;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(dropRect, $"Search Modifiers:\nUse ',' to sepperate search terms.\nUse '!' to invert a search term.\n#name/#n - Defs Label / Readable Name\n#def/#d - Def Name\n#type/#t - Def Type (Thing, Terrain, etc.)\n#research/#r - Research a def currently has.\n#mod/#m - Mod that adds the def.");
                    Text.Anchor = anchor;
                });
            }

            // Filter Left Def Dict
            List<Def> leftDefList = leftDefDict.Keys.ToList();
            Dictionary<Def, List<ResearchProjectDef>> filteredLeftDefDict = FilterDefs(leftDefList);

            // Results
            Rect resultsRect = searchRect;
            resultsRect.width = 90;
            resultsRect.x = searchRect.xMax + 5;
            anchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(resultsRect, $"Results: {filteredLeftDefDict.Count}");
            Text.Anchor = anchor;

            // Def List Rects
            Rect defListsRect = listingStandard.GetRect(contentRect.height - (buttonHeight * 3) - 20);
            defListsRect.y += 10;
            Rect leftListBgRect = defListsRect;
            leftListBgRect.width = (leftListBgRect.width / 2) - 5;
            Widgets.DrawWindowBackground(leftListBgRect);
            Rect leftListPosRect = leftListBgRect.ContractedBy(10);

            Rect rightListBgRect = defListsRect;
            rightListBgRect.width = (rightListBgRect.width / 2) - 5;
            rightListBgRect.x = (defListsRect.width / 2) + 5;
            Widgets.DrawWindowBackground(rightListBgRect);
            Rect rightListPosRect = rightListBgRect.ContractedBy(10);

            // Add/Remove defs from dicts (dragging)
            if ((currentMousePos.x > leftListBgRect.xMin && currentMousePos.x < leftListBgRect.xMax) && (currentMousePos.y > leftListBgRect.yMin && currentMousePos.y < leftListBgRect.yMax)) { // Add to left side
                if (isDragging) { // Draw highlight
                    // TODO Make highlight draw over all scroll items
                    Rect leftHighlight = leftListBgRect;
                    Widgets.DrawHighlight(leftHighlight);
                }
                if (Input.GetMouseButtonUp(0) && isDragging) { isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) {
                        if (!leftDefDict.ContainsKey(dragging.Item1)) {
                            leftDefDict.Add(dragging.Item1, dragging.Item2);
                            RimAges.UpdateResearch();
                            RimAges.ApplyEmptyResearch();
                            RimAges.ApplyResearchCost();
                            RimAgesMod.RimAgesResearchChanges.Save();
                        }
                        if (rightDefDict.ContainsKey(dragging.Item1)) {
                            rightDefDict.Remove(dragging.Item1);
                        }
                    } 
                }
            }
            if ((currentMousePos.x > rightListBgRect.xMin && currentMousePos.x < rightListBgRect.xMax) && (currentMousePos.y > rightListBgRect.yMin && currentMousePos.y < rightListBgRect.yMax)) { // Add to right side
                if (isDragging) { // Draw highlight
                    // TODO Make highlight draw over all scroll items
                    Rect rightHighlight = rightListBgRect;
                    Widgets.DrawHighlight(rightHighlight);
                }
                if (Input.GetMouseButtonUp(0) && isDragging) { isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) {
                        if (!rightDefDict.ContainsKey(dragging.Item1)) {
                            rightDefDict.Add(dragging.Item1, dragging.Item2);
                            RimAges.UpdateResearch();
                            RimAges.ApplyEmptyResearch();
                            RimAges.ApplyResearchCost();
                            RimAgesMod.RimAgesResearchChanges.Save();
                            if (leftDefDict.ContainsKey(dragging.Item1)) {
                                leftDefDict.Remove(dragging.Item1);
                            }
                        }
                    } 
                }
            }

            // Reset dragging
            if (Input.GetMouseButtonUp(0) && isDragging) { isDragging = false; if (dragging != (null, null, new Vector2(0, 0))) { dragging = (null, null, new Vector2(0, 0)); } }

            // Left Def List
            int lineHeight = 60;
            Rect leftDefListRect = new Rect(0, 0, leftListPosRect.width - 30, Math.Min(filteredLeftDefDict.Count, 10) * lineHeight); // which ever is smaller 9? or def count

            DefPages leftDefPages = new DefPages(leftDefListRect, leftListPosRect, listingStandard, lineHeight);
            leftDefPages.DrawDefPage(ref leftDefPageNum, ref scrollAmount, filteredLeftDefDict);

            // Right Def List
            Rect rightDefListRect = new Rect(0, 0, rightListPosRect.width - 30, Math.Min(rightDefDict.Count, 10) * lineHeight);

            DefPages rightDefPages = new DefPages(rightDefListRect, rightListPosRect, listingStandard, lineHeight);
            rightDefPages.DrawDefPage(ref rightDefPageNum, ref scrollAmount, rightDefDict);

            listingStandard.End();

            // Current Research Drop Down
            if (researchDropDownActive) { DrawResearchDropDown(listingStandard); }

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
                        try {
                            List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisites;
                            if (researchList.Count != 0) {
                                return (researchList.Distinct().ToList(), true);
                            }
                        }
                        catch {
                            if (DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite != null) {
                                return (new List<ResearchProjectDef> { DefDatabase<ThingDef>.GetNamed(def.defName).recipeMaker.researchPrerequisite }, true);
                            }
                        }
                    }
                    break;
                case "Verse.RecipeDef":
                    try {
                        List<ResearchProjectDef> researchList = DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisites;
                        if (researchList.Count != 0) {
                            return (researchList.Distinct().ToList(), true);
                        }
                    }
                    catch (Exception) {
                        if (DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite != null) {
                            return (new List<ResearchProjectDef> { DefDatabase<RecipeDef>.GetNamed(def.defName).researchPrerequisite }, true);
                        }
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
                                        if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisites.Count == 0) { keyDef = currentDef; }
                                    }
                                    catch {
                                        try {
                                            if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite == null) { keyDef = currentDef; }
                                        }
                                        catch (Exception e) {
                                            Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                        }
                                    }
                                }
                                break;
                            case "Verse.RecipeDef":
                                try {
                                    if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisites.Count == 0) { keyDef = currentDef; }
                                }
                                catch {
                                    try {
                                        if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite == null) { keyDef = currentDef; }
                                    }
                                    catch (Exception e) {
                                        Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                    }
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
                                try {
                                    List<ResearchProjectDef> researchList = DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisites;
                                    keyDef = currentDef;
                                    if (researchList.Count != 0) {
                                        valueDef = researchList.Distinct().ToList();
                                    }
                                }
                                catch {
                                    keyDef = currentDef;
                                    if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite != null) {
                                        valueDef.Add(DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker.researchPrerequisite);
                                    }
                                }
                            }
                            break;
                        case "Verse.RecipeDef":
                            try {
                                List<ResearchProjectDef> researchList = DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisites;
                                keyDef = currentDef;
                                if (researchList.Count != 0) {
                                    valueDef = researchList.Distinct().ToList();
                                }
                            }
                            catch {
                                keyDef = currentDef;
                                try {
                                    if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite != null) {
                                        valueDef.Add(DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisite);
                                    }
                                }
                                catch (Exception e) {
                                    Log.Warning($"[RimAges] ERROR: {defList[i].defName} does not have a researchPrerequisites tag. You can safely continue but it will not be in the def list.");
                                }
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
            List<string> keywords = new List<string> { "#name", "#n", "#def", "#d", "#type", "#t", "#research", "#r", "#mod", "#m" };

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
                                case "#name": case "#n":
                                    if (modifier == "!") { if (!(label.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (label.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#def": case "#d":
                                    if (modifier == "!") { if (!(defName.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (defName.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#type": case "#t":
                                    if (modifier == "!") { if (!(defType.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                    else { if (defType.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                    break;
                                case "#research": case "#r":
                                    if (modifier == "!") {
                                        bool found = false;
                                        foreach (string research in researchNames) {
                                            if ((research.Contains(search))) { found = true; break; }
                                        }
                                        if (!found) { searchedDefs.Add(item.Key, item.Value); }
                                    }
                                    else {
                                        foreach (string research in researchNames) {
                                            if (research.Contains(search)) { searchedDefs[item.Key] = item.Value; break; }
                                        }
                                    }
                                    break;
                                case "#mod": case "#m":
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
                                if (research.Contains(search)) { searchedDefs[item.Key] = item.Value; }
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
                    //if (item.Key.defName == "ExtractOvum") { Log.Error($"{item.Value.Count}"); }
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
                            case "#name": case "#n":
                                if (modifier == "!") { if (!(label.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (label.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#def": case "#d":
                                if (modifier == "!") { if (!(defName.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (defName.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#type": case "#t":
                                if (modifier == "!") { if (!(defType.Contains(search))) { searchedDefs.Add(item.Key, item.Value); } }
                                else { if (defType.Contains(search)) { searchedDefs.Add(item.Key, item.Value); } }
                                break;
                            case "#research": case "#r":
                                if (modifier == "!") {
                                    bool found = false;
                                    foreach (string research in researchNames) {
                                        if (research.Contains(search)) { found = true; break; }
                                    }
                                    if (!found) { searchedDefs.Add(item.Key, item.Value); }
                                }
                                else {
                                    foreach (string research in researchNames) {
                                        if (research.Contains(search)) { searchedDefs[item.Key] = item.Value; break; }
                                    }
                                }
                                break;
                            case "#mod": case "#m":
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
                        foreach(string research in researchNames) {
                            if (research.Contains(search)) { searchedDefs[item.Key] = item.Value; }
                        }
                    }
                }
            }
            return searchedDefs;
        }

        // Makes a page based list of defs
        public class DefPages {
            public Dictionary<Def, List<ResearchProjectDef>> defDict;
            public Rect defListRect;
            public Rect listPosRect;
            public Listing_Standard listingStandard;
            public int lineHeight;

            public DefPages(Rect _defListRect, Rect _listPosRect, Listing_Standard _listingStandard, int _lineHeight) {
                defListRect = _defListRect;
                listPosRect = _listPosRect;
                listingStandard = _listingStandard;
                lineHeight = _lineHeight;
            }

            public void DrawDefPage(ref int _defPageNum, ref int _scrollAmount, Dictionary<Def, List<ResearchProjectDef>> _defDict) {
                defDict = _defDict;
                listingStandard.Begin(listPosRect);
                int cellPosition;
                int lineNumber;
                int defPerPage = 6;
                int numOfPages;
                int firstItem;
                lineNumber = cellPosition = 0;

                numOfPages = (int)Math.Ceiling((double)_defDict.Count / defPerPage);
                if (numOfPages < 1) { numOfPages = 1; }

                if (_defPageNum > numOfPages) { _defPageNum = numOfPages; }
                if (_defPageNum < 1) { _defPageNum = 1; }
                firstItem = (_defPageNum - 1) * defPerPage;

                for (int item = firstItem; item < firstItem + defPerPage; item++) {
                    if (defDict.Count - 1 < item) { Log.Message("break"); break; }

                    cellPosition += lineHeight;
                    lineNumber++;

                    Rect rect = listingStandard.GetRect(lineHeight);
                    Widgets.DrawWindowBackground(rect);
                    if (lineNumber % 2 != 1) { Widgets.DrawLightHighlight(rect); }
                    Def currentDef = defDict.ElementAt(item).Key;
                    List<ResearchProjectDef> researchDef = defDict.ElementAt(item).Value;
                    DrawListItem(rect, currentDef, researchDef);

                    if (Mouse.IsOver(rect)) {
                        if (isDragging == false) { Widgets.DrawLightHighlight(rect); }
                        if (Input.GetMouseButtonDown(0) && isDragging == false) { dragging = (currentDef, researchDef, rect.size); isDragging = true; }
                    }

                }
                listingStandard.End();

                // Page Nav Bar
                Rect pageNavBar = listPosRect;
                pageNavBar.height = (lineHeight / 2) + 5;
                pageNavBar.y += listPosRect.height - pageNavBar.height;

                // Page Number
                Rect pageNumberText = pageNavBar;
                pageNumberText.width = 60;
                pageNumberText.x += (listPosRect.width / 2) - pageNumberText.width / 2;
                TextAnchor anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(pageNumberText, $"{_defPageNum}/{numOfPages}");
                Text.Anchor = anchor;

                // Page Nav Buttons
                if (_defPageNum > 1) {
                    Rect navLeftButton = pageNumberText;
                    navLeftButton.width -= 20;
                    navLeftButton.x -= navLeftButton.width + 5;
                    if (Widgets.ButtonText(navLeftButton, "<")) {
                        Log.Message($"{RimAges.modTag} - Nav Left");
                        _defPageNum -= 1;
                    }

                    Rect navLeftSkipButton = navLeftButton;
                    navLeftSkipButton.x -= navLeftSkipButton.width + 5;
                    if (Widgets.ButtonText(navLeftSkipButton, "<<")) {
                        Log.Message($"{RimAges.modTag} - Nav Skip Left");
                        _defPageNum = 0;
                    }
                }

                if (_defPageNum < numOfPages) {
                    Rect navRightButton = pageNumberText;
                    navRightButton.width -= 20;
                    navRightButton.x += navRightButton.width + 25;
                    if (Widgets.ButtonText(navRightButton, ">")) {
                        Log.Message($"{RimAges.modTag} - Nav Right");
                        _defPageNum += 1;
                    }

                    Rect navRightSkipButton = navRightButton;
                    navRightSkipButton.x += navRightSkipButton.width + 5;
                    if (Widgets.ButtonText(navRightSkipButton, ">>")) {
                        Log.Message($"{RimAges.modTag} - Nav Skip Right");
                        _defPageNum = numOfPages;
                    }
                }

                // Scroll Logic
                if (Mouse.IsOver(listPosRect)) {
                    // Scorll down
                    if (Input.mouseScrollDelta.y < 0) {
                        _scrollAmount += 1;
                        Log.Message(_scrollAmount);
                    }

                    // Scroll up
                    if (Input.mouseScrollDelta.y > 0) {
                        _scrollAmount -= 1;
                        Log.Message(_scrollAmount);
                    }

                    if (Math.Abs(_scrollAmount) >= 3) {
                        int pages = (int)Math.Floor((double)_scrollAmount / 3);
                        if ((_defPageNum + pages) < 1) { _defPageNum = 1; }
                        else if ((_defPageNum + pages) > numOfPages) { _defPageNum = numOfPages; }
                        else { _defPageNum += pages; }
                        _scrollAmount = 0;
                        Log.Message(pages);
                    }

                    if (Input.mouseScrollDelta.y == 0 && _scrollAmount != 0) {
                        _scrollAmount = 0;
                        Log.Message("Reset Scroll Amount");
                    }
                }

                if (_defPageNum < 1) { _defPageNum = 1; }
                if (_defPageNum > numOfPages) { _defPageNum = numOfPages; }
            }
        }

        public static void DrawListItem(Rect rect, Def currentDef, List<ResearchProjectDef> researchDef) {
            Rect labelItemRect = rect.ContractedBy(5);
            labelItemRect.height = 22;
            if (currentDef.defName.Contains("Carpet")) { // Stops every color carpet being listed
                Widgets.Label(labelItemRect, $"{currentDef.label.Split('(')[0].CapitalizeFirst().Trim()}");
            }
            else {
                Widgets.Label(labelItemRect, $"{currentDef.label.CapitalizeFirst()}");
            }

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
                if (DefDatabase<ThingDef>.GetNamed(currentDef.defName).recipeMaker != null || DefDatabase<ThingDef>.GetNamed(currentDef.defName).researchPrerequisites.NullOrEmpty()) {
                    Widgets.Label(researchItemRect, $"None");
                }
                else {
                    Widgets.Label(researchItemRect, $"Not Found");
                }
            }
            else if ($"{currentDef.GetType()}" == "Verse.RecipeDef") {
                if (DefDatabase<RecipeDef>.GetNamed(currentDef.defName).researchPrerequisites.NullOrEmpty()) {
                    Widgets.Label(researchItemRect, $"None");
                }
                else {
                    Widgets.Label(researchItemRect, $"Not Found");
                }
            }
            else if ($"{currentDef.GetType()}" == "Verse.TerrainDef") {
                if (DefDatabase<TerrainDef>.GetNamed(currentDef.defName).researchPrerequisites.NullOrEmpty()) {
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

        public static void DrawResearchDropDown(Listing_Standard listingStandard) {
            float researchDropDownSizeX = 400;
            float researchDropDownSizeY = 360;
            Rect researchDropDown = new Rect((Screen.width / 2) - (researchDropDownSizeX / 2), ((Screen.height / 2) - (researchDropDownSizeY / 2)) - 65, researchDropDownSizeX, researchDropDownSizeY);
            Find.WindowStack.ImmediateWindow(12, researchDropDown, WindowLayer.Super, () => {
                Rect dropRect = researchDropDown.ContractedBy(10f);
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

                if (!researchDropDownActive) {
                    RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                    if (settings.tab == 2) {
                        leftDefDict = UpdateDefDict(null);
                        rightDefDict = UpdateDefDict(currentResearch);
                    }
                }
            }, true, true, 1, () => { researchDropDownActive = false; });
        }

        public static void DrawFilterDropDown(Rect dropDown, Listing_Standard listingStandard) {
            Rect dropRect = dropDown.ContractedBy(10f);
            dropRect.position = new Vector2(10, 10);

            listingStandard.Begin(dropRect);
            listingStandard.CheckboxLabeled("Exclude Assigned Defs", ref assignedDefsFilter);
            listingStandard.End();
        }

        public static void DrawCostInputWindow() {
            float costInputWindowSizeX = 150;
            float costInputWindowSizeY = 40;
            Rect costInputWindow = new Rect((Screen.width / 2) - (costInputWindowSizeX / 2), (Screen.height / 2) - 195, costInputWindowSizeX, costInputWindowSizeY);
            Find.WindowStack.ImmediateWindow(12, costInputWindow, WindowLayer.Super, () => {
                Rect dropRect = costInputWindow.ContractedBy(10f);
                dropRect.position = new Vector2(10, 10);

                Widgets.TextFieldNumeric(dropRect, ref costInput, ref costInputBuffer);
            }, true, true, 1, () => { 
                costInputWindowActive = false;
                if (costInput < 0) {
                    SetCurrentResarchCost(0);
                }
                else if (costInput > 1_000_000) {
                    SetCurrentResarchCost(1_000_000);
                }
                else {
                    SetCurrentResarchCost(costInput);
                }
            });
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

            Rect dragRect = new Rect(boxPos.x+5, boxPos.y+5, 120, 40);
            if (Input.GetMouseButtonDown(0)) {
                Vector2 pos = Event.current.mousePosition;
                if ((pos.x >= boxPos.x && pos.x <= (boxPos.x + buttonWidth))&&(pos.y >= boxPos.y && pos.y <= (boxPos.y + buttonHeight))) {
                    isDragging = true;
                }
            }
            if (Input.GetMouseButtonUp(0)) {
                Vector2 pos = Event.current.mousePosition;
                if (isDragging) {
                    if (!((pos.x >= bounds.pos.x + (buttonWidth / 2) && pos.x <= (bounds.pos.x + bounds.size.x) - (buttonWidth / 2) - 6) && (pos.y >= bounds.pos.y + (buttonHeight / 2) && pos.y <= (bounds.pos.y + bounds.size.y) - (buttonHeight / 2)))) {
                        float x = pos.x - (buttonWidth/2);
                        float y = pos.y - (buttonHeight/2);
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

                if (settings.ResearchCostBackup.NullOrEmpty()) { Init(); }

                if (settings.emptyResearch) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.MedievalCookingCost > 0) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; } }
                else if (settings.EmptyResearchOverride.Contains("MedievalCooking")) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; }
                else if (settings.MedievalCookingCost > 0) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; }

                if (settings.emptyResearch) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.MedievalDefensesCost > 0) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; } }
                else if (settings.EmptyResearchOverride.Contains("MedievalDefenses")) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; }
                else if (settings.MedievalDefensesCost > 0) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; }

                if (settings.emptyResearch) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.MedievalHygieneCost > 0) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; } }
                else if (settings.EmptyResearchOverride.Contains("MedievalHygiene")) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; }
                if (settings.MedievalHygieneCost > 0) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; }

                if (settings.emptyResearch) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.MedievalResearchCost > 0) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; } }
                else if (settings.EmptyResearchOverride.Contains("MedievalResearch")) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; }
                if (settings.MedievalResearchCost > 0) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; }

                if (settings.emptyResearch) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.TrainingTargetsCost > 0) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; } }
                else if (settings.EmptyResearchOverride.Contains("TrainingTargets")) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; }
                if (settings.TrainingTargetsCost > 0) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; }

                if (settings.emptyResearch) { settings.ResearchCostBackup["SpacerPlantsCost"] = settings.SpacerPlantsCost; }
                else if (settings.EmptyResearchOverride.NullOrEmpty()) { if (settings.SpacerPlantsCost > 0) { settings.ResearchCostBackup["SpacerPlantsCost"] = settings.SpacerPlantsCost; } }
                else if (settings.EmptyResearchOverride.Contains("SpacerPlants")) { settings.ResearchCostBackup["SpacerPlantsCost"] = settings.SpacerPlantsCost; }
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
                    settings.ResearchChanges.Add(def.defName, new List<string>()); 
                }
            }

            public static void Reset() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();
                RimAges.clearCacheList = DefDatabase<ResearchProjectDef>.AllDefs.ToHashSet();
                settings.ResearchChanges.Clear();
                Init(settings);

                foreach (var def in GetUsableDefs()) {
                    RimAges.RemoveResearch(def);

                    List<ResearchProjectDef> research;
                    if (RimAges.defaultDefs.TryGetValue(def, out research)) {
                        if (!research.NullOrEmpty()) {
                            RimAges.AddResearch(def, research);
                        }
                    }
                }
                RimAges.ApplyEmptyResearch();
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
