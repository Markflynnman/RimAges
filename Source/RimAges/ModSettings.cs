using RimWorld;
using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;

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

        public override void ExposeData() {
            Scribe_Values.Look(ref noResearch, "noResearch", true);
            Scribe_Values.Look(ref emptyResearch, "emptyResearch", true);

            Scribe_Values.Look(ref MedievalAgeCost, "MedievalAgeCost", 1000);
            Scribe_Values.Look(ref IndustrialAgeCost, "IndustrialAgeCost", 2000);
            Scribe_Values.Look(ref SpacerAgeCost, "SpacerAgeCost", 3000);
            Scribe_Values.Look(ref UltraAgeCost, "UltraAgeCost", 4000);
            Scribe_Values.Look(ref ArchotechAgeCost, "ArchotechAgeCost", 5000);
            Scribe_Values.Look(ref MedievalCookingCost, "MedievalCookingCost", 1000);
            Scribe_Values.Look(ref MedievalDefensesCost, "MedievalDefensesCost", 1000);
            Scribe_Values.Look(ref MedievalHygieneCost, "MedievalHygieneCost", 1000);
            Scribe_Values.Look(ref MedievalResearchCost, "MedievalResearchCost", 1000);
            Scribe_Values.Look(ref TrainingTargetsCost, "TrainingTargetsCost", 1000);
            Scribe_Values.Look(ref SpacerPlantsCost, "SpacerPlantsCost", 1000);
            Scribe_Collections.Look(ref ResearchCostBackup, "ResearchCostBackup", LookMode.Value, LookMode.Value);

            base.ExposeData();
        }
    }

    public class RimAgesMod : Mod {
        RimAgesSettings settings;

        public RimAgesMod(ModContentPack content) : base(content) {
            settings = GetSettings<RimAgesSettings>();
        }

        public override string SettingsCategory() {
            return "RimAges";
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Rect tabRect = inRect;
            tabRect.y -= 8f;
            tabRect.x += 100;
            inRect.y -= 8f;
            Widgets.DrawMenuSection(inRect);
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("Main", delegate
                {
                    settings.tab = 0;
                    settings.Write();
                }, settings.tab == 0),
                new TabRecord("Research".Translate(), delegate
                {
                    settings.tab = 1;
                    settings.Write();
                    Log.Message($"{RimAges.modTag} - Empty Reasearch: {settings.emptyResearch} - {DateTime.Now:hh:mm:ss tt}");
                }, settings.tab == 1)
            };

            TabDrawer.DrawTabs(tabRect, tabs);
            if (settings.tab == 0) {
                DrawMain(inRect.ContractedBy(10f), settings);
            }
            if (settings.tab == 1) {
                DrawResearch(inRect.ContractedBy(10f), settings);
            }
            base.DoSettingsWindowContents(inRect);
        }

        [TweakValue("rectMin", -1000f, 1000f)]
        public static float rectMin = -1000f;

        [TweakValue("rectOff", -100f, 100f)]
        public static float rectOff = 60f;

        [TweakValue("rectLeft", -1000f, 1000f)]
        public static float rectLeft = 120f;

        [TweakValue("buttonHeight", 0f, 100f)]
        public static float buttonHeight = 40f;

        [TweakValue("spaceHeight", -500f, 500f)]
        public static float spaceHeight = -500f;

        [TweakValue("spaceOff", -100f, 100f)]
        public static float spaceOff = -50f;

        public static void DrawMain(Rect contentRect, RimAgesSettings settings) {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(contentRect);
            listingStandard.CheckboxLabeled("No Starting Research", ref settings.noResearch, "Start the game with no research.");
            listingStandard.CheckboxLabeled("Enable Empty Research", ref settings.emptyResearch, "Enable research that has no unlocks.");

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

            rect = rect.LeftPartPixels(rectLeft); // Width of button
            if (Widgets.ButtonText(rect, "Reset")) {
                Log.Warning($"{RimAges.modTag} Pressed!");
                RimAgesDefaults.RimAgesSettingsReset();
            }
            listingStandard.End();
        }

        public static void DrawResearch(Rect contentRect, RimAgesSettings settings) {
            int normalInc = 100;
            int lockedInc = 0;

            Listing_Standard listingStandard = new Listing_Standard();
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
            rect = rect.LeftPartPixels(rectLeft); // Width of button
            if (Widgets.ButtonText(rect, "Reset")) {
                Log.Warning($"{RimAges.modTag} Pressed!");
                RimAgesDefaults.RimAgesSettingsReset();
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
                settings.noResearch = noResearch;
                settings.emptyResearch = emptyResearch;

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
            }
        }
        public static class RimAgesBackup {
            public static void SaveBackup() {
                RimAgesSettings settings = LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>();

                if (settings.ResearchCostBackup == null) { InitResearchCostBackup(); }

                if (settings.MedievalCookingCost > 0) { settings.ResearchCostBackup["MedievalCookingCost"] = settings.MedievalCookingCost; }
                if (settings.MedievalDefensesCost > 0) { settings.ResearchCostBackup["MedievalDefensesCost"] = settings.MedievalDefensesCost; }
                if (settings.MedievalHygieneCost > 0) { settings.ResearchCostBackup["MedievalHygieneCost"] = settings.MedievalHygieneCost; }
                if (settings.MedievalResearchCost > 0) { settings.ResearchCostBackup["MedievalResearchCost"] = settings.MedievalResearchCost; }
                if (settings.TrainingTargetsCost > 0) { settings.ResearchCostBackup["TrainingTargetsCost"] = settings.TrainingTargetsCost; }
                if (settings.SpacerPlantsCost > 0) { settings.ResearchCostBackup["SpacerPlantsCost"] = settings.SpacerPlantsCost; }
            }

            public static void InitResearchCostBackup() {
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
    }
}
