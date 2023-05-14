using RimWorld;
using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;

namespace RimAges {
    public class RimAgesSettings : ModSettings {
        public int tab;

        public bool noResearch;
        public bool emptyResearch; // Not Functional 

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

        public override void ExposeData() {
            Scribe_Values.Look(ref noResearch, "noResearch", true);
            Scribe_Values.Look(ref emptyResearch, "emptyResearch", true); // Not Functional 

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
            listingStandard.CheckboxLabeled("Enable Empty Research", ref settings.emptyResearch, "Enable research that has no unlocks."); // Not Functional 

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
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(contentRect);

            listingStandard.Label($"Medieval Age Cost: {settings.MedievalAgeCost}", -1, "Cost of Medieval Age research.");
            listingStandard.IntAdjuster(ref settings.MedievalAgeCost, 100, 100);

            listingStandard.Label($"Industrial Age Cost: {settings.IndustrialAgeCost}", -1, "Cost of Industrial Age research");
            listingStandard.IntAdjuster(ref settings.IndustrialAgeCost, 100, 100);

            listingStandard.Label($"Spacer Age Cost: {settings.SpacerAgeCost}", -1, "Cost of Spacer Age research");
            listingStandard.IntAdjuster(ref settings.SpacerAgeCost, 100, 100);

            listingStandard.Label($"Ultra Age Cost: {settings.UltraAgeCost}", -1, "Cost of Ultra Age research");
            listingStandard.IntAdjuster(ref settings.UltraAgeCost, 100, 100);

            listingStandard.Label($"Archotech Age Cost: {settings.ArchotechAgeCost}", -1, $"Cost of Archotech Age research");
            listingStandard.IntAdjuster(ref settings.ArchotechAgeCost, 100, 100);

            listingStandard.Label($"Medieval Cooking Cost: {settings.MedievalCookingCost}", -1, "Cost of Medieval Cooking research");
            listingStandard.IntAdjuster(ref settings.MedievalCookingCost, 100, 100);

            listingStandard.Label($"Medieval Defenses Cost: {settings.MedievalDefensesCost}", -1, "Cost of Medieval Defenses research");
            listingStandard.IntAdjuster(ref settings.MedievalDefensesCost, 100, 100);

            listingStandard.Label($"Medieval Hygiene Cost: {settings.MedievalHygieneCost}", -1, "Cost of Medieval Hygiene research");
            listingStandard.IntAdjuster(ref settings.MedievalHygieneCost, 100, 100);

            listingStandard.Label($"Medieval Research Cost: {settings.MedievalResearchCost}", -1, "Cost of Medieval Research research");
            listingStandard.IntAdjuster(ref settings.MedievalResearchCost, 100, 100);

            listingStandard.Label($"Training Targets Cost: {settings.TrainingTargetsCost}", -1, "Cost of Training Targets research");
            listingStandard.IntAdjuster(ref settings.TrainingTargetsCost, 100, 100);

            listingStandard.Label($"Spacer Plants Cost: {settings.SpacerPlantsCost}", -1, "Cost of Spacer Plants research");
            listingStandard.IntAdjuster(ref settings.SpacerPlantsCost, 100, 100);

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

            //rect.xMax = rectMax;
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
    }
}
