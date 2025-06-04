using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace RimAges {
    public class RimAges_Utility {
        public static void Drag((Def, List<ResearchProjectDef>, Vector2) dragging) {
            Def def = dragging.Item1;
            List<ResearchProjectDef> researchProjectDef = dragging.Item2;
            Vector2 size = dragging.Item3;

            Rect dragRect = new Rect(0, 0, size.x, size.y);
            Vector2 mousePos = Input.mousePosition;
            
            dragRect.position = new Vector2(mousePos.x - (dragRect.width/2), (Screen.height + (mousePos.y * -1) - (dragRect.height/2)));
            Find.WindowStack.ImmediateWindow(15, dragRect, WindowLayer.Super, () => {
                RimAgesMod.DrawListItem(new Rect(0, 0, size.x, size.y), def, researchProjectDef);
            }, true);

        }

        public static bool CustomCheckbox(Listing_Standard listingStandard, string label, ref bool checkOn, string tooltip = null) {
            Rect checkbox = listingStandard.GetRect(24);
            Rect labelRect = new Rect(checkbox);
            labelRect.width -= 24;
            Rect checkRect = new Rect(labelRect.xMax, labelRect.y, 24, 24);

            Widgets.Label(labelRect, label);
            if (checkOn) {
                GUI.DrawTexture(checkRect, Widgets.CheckboxOnTex);
            }
            else {
                GUI.DrawTexture(checkRect, Widgets.CheckboxOffTex);
            }

            if (Mouse.IsOver(checkbox)) {
                Widgets.DrawHighlight(checkbox);
                if (!tooltip.NullOrEmpty()) {
                    TooltipHandler.TipRegion(checkbox, tooltip);
                }
                if (Event.current.type == EventType.MouseDown) {
                    if (checkbox.Contains(Event.current.mousePosition)) {
                        checkOn = !checkOn;
                        if (checkOn) {
                            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                            return true;
                        }
                        else {
                            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool CustomCheckboxButton(Listing_Standard listingStandard, string label, bool checkOn, string tooltip = null) {
            Rect checkbox = listingStandard.GetRect(32);
            Widgets.DrawWindowBackground(checkbox);
            Widgets.DrawLightHighlight(checkbox);
            Rect labelRect = new Rect(checkbox);
            labelRect.width -= 38;
            labelRect.x += 10;
            labelRect.y += 6;
            Rect checkRect = new Rect(labelRect.xMax, labelRect.y-2, 24, 24);

            Widgets.Label(labelRect, label);
            if (checkOn) {
                GUI.DrawTexture(checkRect, Widgets.CheckboxOnTex);
            }
            else {
                GUI.DrawTexture(checkRect, Widgets.CheckboxOffTex);
            }

            if (Mouse.IsOver(checkbox)) {
                Widgets.DrawHighlight(checkbox);
                if (!tooltip.NullOrEmpty()) {
                    TooltipHandler.TipRegion(checkbox, tooltip);
                }
                if (Event.current.type == EventType.MouseDown) {
                    if (checkbox.Contains(Event.current.mousePosition)) {
                        checkOn = !checkOn;
                        if (checkOn) {
                            SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                            return true;
                        }
                        else {
                            SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}