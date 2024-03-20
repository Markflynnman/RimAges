using Verse;
using HarmonyLib;
using System;
using RimWorld;
using System.Collections.Generic;

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
        public static void Prefix() {
            RimAges.UpdateResearch();
            RimAgesMod.RimAgesResearchChanges.Save();
        }
        public static void Postfix() {
            Log.Warning($"{RimAges.modTag} - Settings saved at {DateTime.Now:hh:mm:ss tt}");
            RimAges.ApplyResearchCost();
            RimAges.ApplyEmptyResearch();
            RimAgesSettings.leftDefDict = RimAgesMod.UpdateDefDict(null);
            RimAgesSettings.rightDefDict = RimAgesMod.UpdateDefDict(RimAgesSettings.currentResearch);
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Research), "UnlockedDefsGroupedByPrerequisites")]
    public class ResearchTabClearCache {
        public static void Postfix(ref List<Pair<ResearchPrerequisitesUtility.UnlockedHeader, List<Def>>> __result, ResearchProjectDef project) {
            if (RimAges.clearCacheList.Contains(project)) {
                foreach (ResearchProjectDef research in RimAges.clearCacheList) {
                    Traverse.Create(research).Field("cachedUnlockedDefs").SetValue(null);
                }

                Log.Warning($"{RimAges.modTag} - Cache cleared!");

                RimAges.clearCacheList.Clear();
            }

            __result = ResearchPrerequisitesUtility.UnlockedDefsGroupedByPrerequisites(project);
        }
    }
}
