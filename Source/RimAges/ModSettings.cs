using UnityEngine;
using Verse;
using Verse.Noise;

namespace RimAges {
    public class RimAgesSettings : ModSettings {
        public bool noResearch = true;
        public bool emptyResearch = true;

        public int MedievalAgeCost = 1000;
        public int IndustrialAgeCost = 2000;
        public int SpacerAgeCost = 3000;
        public int UltraAgeCost = 4000;
        public int ArchotechAgeCost = 5000;
        public int MedievalCookingCost = 1000;
        public int MedievalResearchCost = 1000;
        public int MedievalDefensesCost = 1000;
        public int MedievalHygieneCost = 1000;
        public int TrainingTargetsCost = 1000;
        public int SpacerPlantsCost = 1000;

        public override void ExposeData() {
            Scribe_Values.Look(ref noResearch, "noResearch");
            Scribe_Values.Look(ref emptyResearch, "emptyResearch");

            Scribe_Values.Look(ref MedievalAgeCost, "MedievalAgeCost");
            Scribe_Values.Look(ref IndustrialAgeCost, "IndustrialAgeCost");
            Scribe_Values.Look(ref SpacerAgeCost, "SpacerAgeCost");
            Scribe_Values.Look(ref UltraAgeCost, "UltraAgeCost");
            Scribe_Values.Look(ref ArchotechAgeCost, "ArchotechAgeCost");
            Scribe_Values.Look(ref MedievalCookingCost, "MedievalCookingCost");
            Scribe_Values.Look(ref MedievalResearchCost, "MedievalResearchCost");
            Scribe_Values.Look(ref MedievalDefensesCost, "MedievalDefensesCost");
            Scribe_Values.Look(ref MedievalHygieneCost, "MedievalHygieneCost");
            Scribe_Values.Look(ref TrainingTargetsCost, "TrainingTargetsCost");
            Scribe_Values.Look(ref SpacerPlantsCost, "SpacerPlantsCost");

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
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("No Starting Research", ref settings.noResearch, "Start the game with no research.");
            listingStandard.CheckboxLabeled("Enable Empty Research", ref settings.emptyResearch, "Enable research that has no unlocks.");

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

            listingStandard.Label($"Medieval Research Cost: {settings.MedievalResearchCost}", -1, "Cost of Medieval Research research");
            listingStandard.IntAdjuster(ref settings.MedievalResearchCost, 100, 100);

            listingStandard.Label($"Medieval Defenses Cost: {settings.MedievalDefensesCost}", -1, "Cost of Medieval Defenses research");
            listingStandard.IntAdjuster(ref settings.MedievalDefensesCost, 100, 100);

            listingStandard.Label($"Medieval Hygiene Cost: {settings.MedievalHygieneCost}", -1, "Cost of Medieval Hygiene research");
            listingStandard.IntAdjuster(ref settings.MedievalHygieneCost, 100, 100);

            listingStandard.Label($"Training Targets Cost: {settings.TrainingTargetsCost}", -1, "Cost of Training Targets research");
            listingStandard.IntAdjuster(ref settings.TrainingTargetsCost, 100, 100);

            listingStandard.Label($"Spacer Plants Cost: {settings.SpacerPlantsCost}", -1, "Cost of Spacer Plants research");
            listingStandard.IntAdjuster(ref settings.SpacerPlantsCost, 100, 100);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
