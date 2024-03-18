using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimAges {
    public class RimAges_Utility {
        public static void Drag((Def, ResearchProjectDef, Vector2) dragging) {
            Def def = dragging.Item1;
            ResearchProjectDef researchProjectDef = dragging.Item2;
            Vector2 size = dragging.Item3;

            Rect dragRect = new Rect(0, 0, size.x, size.y);
            Vector2 mousePos = Input.mousePosition;

            dragRect.position = new Vector2(mousePos.x - (dragRect.width / 2), (Screen.height + (mousePos.y * -1) - (dragRect.height / 2)));
            Find.WindowStack.ImmediateWindow(15, dragRect, WindowLayer.Super, () => {
                RimAgesMod.DrawListItem(new Rect(0, 0, size.x, size.y), def, researchProjectDef);
            }, true);

        }
    }
}