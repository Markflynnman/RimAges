using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimAges;

namespace RimAges {
    [HarmonyPatch(typeof(ResearchManager))]
    [HarmonyPatch("FinishProject")]
    internal class Patch_FinishProject_Patch {
        // Get ref of the completed research and pass to Postfix
        public static void Prefix(ref ResearchProjectDef proj, out ResearchProjectDef __state) {
            if (proj != null) { __state = proj; }
            else { __state = null; }
        }

        public static void Postfix(ResearchProjectDef __state) {
            var proj = __state;
            if (proj == null) {
                Log.Error($"{RimAges.modTag} Postfix __state is Null!");
            }

            // If completed research is in techAgeResearch (one of the "Age" researches) then reduce cost of research in corresponding age
            if (RimAges.techAgeResearch.Contains(proj)) {
                RimAges.UnlockAge(proj.defName, proj.techLevel);
            }
        }
    }
}
