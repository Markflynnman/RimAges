using UnityEngine;
using Verse;

namespace RimAges {
    public class RimAgesSettings : ModSettings {
        public bool noResearch = true;

        public override void ExposeData() {
            Scribe_Values.Look(ref noResearch, "noResearch");
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
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
