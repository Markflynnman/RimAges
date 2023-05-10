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
        public static void Postfix() {
            Log.Error($"{RimAges.modTag} Trigger");
            if (DefDatabase<ResearchProjectDef>.GetNamed("MedievalAge").IsFinished) {
                var researchDefs = DefDatabase<ResearchProjectDef>.AllDefsListForReading.ListFullCopy();
                foreach (var res in researchDefs) {
                    res.baseCost -= 100000;
                    res.ReapplyAllMods();
                }
            }
        }
    }
}
