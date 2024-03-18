using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimAges {
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
            RimAges.UpdateResearch();
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "UnlockedDefsGroupedByPrerequisites")]
    public class ResearchTabClearCache {
        public static void Postfix(ref List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> __result, ResearchProjectDef project) {
            if (RimAges.clearCacheList.Contains(project)) {
                Traverse.Create(project).Field("cachedUnlockedDefs").SetValue(null);

                Log.Warning($"{RimAges.modTag} - Cache cleared!");

                RimAges.clearCacheList.Remove(project);
            }

            __result = ResearchPrerequisitesUtility.UnlockedDefsGroupedByPrerequisites(project);
        }
    }
}
