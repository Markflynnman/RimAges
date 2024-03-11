using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine.EventSystems;
using UnityEngine;

//namespace RimAges {

//    [StaticConstructorOnStartup]
//    internal class RimAges_Utility {
//        public static Def[] allDefs;

//        static RimAges_Utility() {
//            AllDefsSetup();
//        }

//        static void AllDefsSetup() {
//            allDefs = allDefs.Concat(DefDatabase<ThingDef>.AllDefs.Where(x => (x.category == ThingCategory.Building || x.category == ThingCategory.Plant || x.category == ThingCategory.Item))).Distinct().ToArray();
//        }
//    }
//}




//public static void DrawTest(Rect contentRect, RimAgesSettings settings) {
//    Listing_Standard listingStandard = new Listing_Standard();

//    Rect resetRect = contentRect;
//    listingStandard.Begin(resetRect);
//    DrawResetButton(resetRect, listingStandard);
//    listingStandard.End();

//    // Left Side Scroll
//    Rect scrollRect = contentRect;
//    scrollRect.y += 40f;
//    scrollRect.yMax -= 102f;
//    scrollRect.xMax -= (scrollRect.width / 2) + 30;

//    Rect leftBackground = scrollRect.ContractedBy(-5f);
//    Widgets.DrawWindowBackground(leftBackground);

//    Rect listRect = new Rect(0f, 0f, scrollRect.width - 30f, (allDefs.Count()) * 22);

//    Widgets.BeginScrollView(scrollRect, ref scrollPos, listRect, true);
//    listingStandard.Begin(listRect);
//    int lineHeight = 22;
//    int cellPosition;
//    int lineNumber;
//    lineNumber = cellPosition = 0;

//    List<Def> removeDefs = new List<Def>();
//    for (int i = 0; i < allDefs.Count(); i++) {
//        if (allDefs[i].GetType().ToString() == "Verse.ThingDef") {
//            ThingDef def = DefDatabase<ThingDef>.GetNamed(allDefs[i].defName);
//            if (def.researchPrerequisites == null && def.plant == null) {
//                removeDefs.Add(allDefs[i]);
//            }
//            if (def.plant != null) {
//                if (def.plant.sowResearchPrerequisites == null) {
//                    removeDefs.Add(allDefs[i]);
//                }
//            }
//        }
//        else if (allDefs[i].GetType().ToString() == "Verse.TerrainDef") {
//            TerrainDef def = DefDatabase<TerrainDef>.GetNamed(allDefs[i].defName);
//            if (def.researchPrerequisites == null) {
//                removeDefs.Add(allDefs[i]);
//            }
//        }
//        else {
//            removeDefs.Add(allDefs[i]);
//        }
//    }
//    foreach (var def in removeDefs) {
//        allDefs.Remove(def);
//    }


//    for (int i = 0; i < allDefs.Count(); i++) { ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//        cellPosition += lineHeight;
//        ++lineNumber;
//        string text;
//        Rect rect = listingStandard.GetRect(lineHeight);
//        if (allDefs[i].GetType().ToString() == "Verse.ThingDef") {
//            ThingDef def = DefDatabase<ThingDef>.GetNamed(allDefs[i].defName);
//            if (def.researchPrerequisites != null) {
//                //if (def.researchPrerequisites.Contains(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge")) == false) {
//                //    def.researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
//                //}
//                if (def.researchPrerequisites.Count() > 0) {
//                    //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}");
//                    text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}";
//                }
//                else {
//                    //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}");
//                    text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}";
//                }
//            }
//            else if (def.plant != null) {
//                if (def.plant.sowResearchPrerequisites != null) {
//                    //if (def.plant.sowResearchPrerequisites.Contains(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge")) == false) {
//                    //    def.plant.sowResearchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
//                    //}
//                    if (def.plant.sowResearchPrerequisites.Count() > 0) {
//                        //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}::{def.plant.sowResearchPrerequisites.First().defName}");
//                        text = $"{def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}::{def.plant.sowResearchPrerequisites.First().defName}";
//                    }
//                    else {
//                        //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}");
//                        text = $"{def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}";
//                    }
//                }
//                else { text = $"{def.GetType()}::{def.defName}::null"; }
//            }
//            else { continue; }
//        }
//        else if (allDefs[i].GetType().ToString() == "Verse.TerrainDef") {
//            TerrainDef def = DefDatabase<TerrainDef>.GetNamed(allDefs[i].defName);
//            if (def.researchPrerequisites != null) {
//                //if (def.researchPrerequisites.Contains(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge")) == false) {
//                //    def.researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
//                //}
//                if (def.researchPrerequisites.Count() > 0) {
//                    //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}");
//                    text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}";
//                }
//                else {
//                    //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}");
//                    text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}";
//                }
//            }
//            else { continue; }
//        }
//        else { text = allDefs[i].GetType().ToString(); }

//        //if (def.researchPrerequisites != null) {
//        //    if (def.researchPrerequisites.Contains(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge")) == false) {
//        //        def.researchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
//        //    }
//        //    if (def.researchPrerequisites.Count() > 0) {
//        //        //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}");
//        //        text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}::{def.researchPrerequisites.First().defName}";
//        //    }
//        //    else {
//        //        //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}");
//        //        text = $"{def.GetType()}::{def.defName}::{def.researchPrerequisites.Count}";
//        //    }
//        //}
//        //else if (def.plant != null) {
//        //    if (def.plant.sowResearchPrerequisites != null) {
//        //        if (def.plant.sowResearchPrerequisites.Contains(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge")) == false) {
//        //            def.plant.sowResearchPrerequisites.Add(DefDatabase<ResearchProjectDef>.GetNamed("ArchotechAge"));
//        //        }
//        //        if (def.plant.sowResearchPrerequisites.Count() > 0) {
//        //            //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}::{def.plant.sowResearchPrerequisites.First().defName}");
//        //            text = $"{def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}::{def.plant.sowResearchPrerequisites.First().defName}";
//        //        }
//        //        else {
//        //            //Log.Warning($"{RimAges.modTag} - {def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}");
//        //            text = $"{def.GetType()}::{def.defName}::{def.plant.sowResearchPrerequisites.Count}";
//        //        }
//        //    }
//        //    else { text = $"{def.GetType()}::{def.defName}::null"; }
//        //}
//        //else { text = $"{def.GetType()}::{def.defName}::null"; }
//        Widgets.Label(rect, text);
//        if (lineNumber % 2 != 1) Widgets.DrawLightHighlight(rect);
//    }
//    listingStandard.End();
//    Widgets.EndScrollView();

//    // Right Side Scroll
//    Rect scrollEnabledRect = contentRect;
//    scrollEnabledRect.y += 40f;
//    scrollEnabledRect.yMax -= 102f;
//    scrollEnabledRect.xMin += (scrollEnabledRect.width / 2) + 30;

//    Rect rightBackground = scrollEnabledRect.ContractedBy(-5f);
//    Widgets.DrawWindowBackground(rightBackground);

//    Rect listEnabledRect = new Rect(0f, 0f, scrollEnabledRect.width - 30f, (lines) * 22);

//    Widgets.BeginScrollView(scrollEnabledRect, ref scrollPos2, listEnabledRect, true);
//    listingStandard.Begin(listEnabledRect);
//    int lineHeight2 = 22;
//    int cellPosition2;
//    int lineNumber2;
//    lineNumber2 = cellPosition2 = 0;
//    for (int i = 0; i < lines; i++) {
//        cellPosition2 += lineHeight2;
//        ++lineNumber2;

//        Rect rect = listingStandard.GetRect(lineHeight2);
//        Widgets.Label(rect, "-");
//        if (lineNumber2 % 2 != 1) Widgets.DrawLightHighlight(rect);
//    }
//    listingStandard.End();
//    Widgets.EndScrollView();

//    // Transfer Buttons
//    Rect transferButtons = contentRect;
//    transferButtons.yMin += (contentRect.height / 2) - 35;
//    transferButtons.yMax -= (contentRect.height / 2) - 50;
//    transferButtons.xMin += (contentRect.width / 2) - 25;
//    transferButtons.xMax -= (contentRect.width / 2) - 25;

//    //Rect testBackground = transferButtons;
//    //Widgets.DrawWindowBackground(testBackground);

//    listingStandard.Begin(transferButtons);
//    Rect addRect = listingStandard.GetRect(30f); // Height of button
//    Rect space = listingStandard.GetRect(15f);
//    Rect removeRect = listingStandard.GetRect(30f); // Height of button
//    //transferRect.xMin = (transferButtons.width / 2);

//    //transferRect = transferRect.LeftPartPixels(rectLeft); // Width of button
//    if (Widgets.ButtonText(addRect, "+")) {
//        lines += 1;
//    }

//    Widgets.Label(space, "");

//    if (Widgets.ButtonText(removeRect, "-")) {
//        lines -= 1;
//    }
//    listingStandard.End();
//}