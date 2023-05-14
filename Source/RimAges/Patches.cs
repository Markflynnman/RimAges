using Verse;
using HarmonyLib;
using System;

namespace RimAges {
    //[HarmonyPatch(typeof(ResearchManager))]
    //[HarmonyPatch("FinishProject")]
    //internal class Patch_FinishProject_Patch {
    //    // Get ref of the completed research and pass to Postfix
    //    public static void Prefix(ref ResearchProjectDef proj, out ResearchProjectDef __state) {
    //        if (proj != null) { __state = proj; }
    //        else { __state = null; }
    //    }

    //    public static void Postfix(ResearchProjectDef __state) {
    //        var proj = __state;
    //        if (proj == null) {
    //            Log.Error($"{RimAges.modTag} Postfix __state is Null!");
    //        }

    //        // If completed research is in techAgeResearch (one of the "Age" researches) then reduce cost of research in corresponding age
    //        if (RimAges.techAgeResearch.Contains(proj)) {
    //            RimAges.UnlockAge(proj.defName, proj.techLevel);
    //        }
    //    }
    //}

    // No research patch
    [HarmonyPatch(typeof(ResearchUtility), "ApplyPlayerStartingResearch")]
    public class NewGamePatch {
        public static bool Prefix() {
            if (LoadedModManager.GetMod<RimAgesMod>().GetSettings<RimAgesSettings>().noResearch) {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ModSettings), "Write")]
    public class ModSettingsPatch {
        public static void Postfix() {
            Log.Warning($"{RimAges.modTag} - Settings saved at {DateTime.Now:hh:mm:ss tt}");
            RimAges.ApplyResearchCost();
            RimAges.ApplyEmptyResearch();
        }
    }
}
