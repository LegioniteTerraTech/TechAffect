using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TerraTechETCUtil;
using System.Reflection;

namespace AffectTech
{
    internal class StatusEffectsWiki
    {
        private static string name => KickStart.name;
        private static Sprite fireSprite;

        internal static void InitWiki()
        {
            try
            {
                ManIngameWiki.InsureWiki(name);
                fireSprite = ManUI.inst.GetDamageTypeIcon(ManDamage.DamageType.Fire);
                InitMechanics();
            }
            catch (Exception e)
            {
                throw new Exception("Error in AffectTech wiki: ", e);
            }
        }
        internal static void InitMechanics()
        {
            ManIngameWiki.WikiPageGroup Grouper = new ManIngameWiki.WikiPageGroup(name,
               "Mechanics", fireSprite);
            new WikiPageInfo(name, "Status Effects", fireSprite,
                PageStatusTypes, Grouper);
        }
        internal static void PageStatusTypes()
        {
            GUILayout.BeginHorizontal();
            AltUI.Sprite(fireSprite, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(AltUI.TextfieldBlackHuge);
            GUILayout.Label("Weapons can now apply " + AltUI.UIObjectiveMarkerText +
                "effects" + AltUI.UIEndColor + ", which can vary the outcome of a fight." +
                "\nBlocks can be set on fire, freeze them, be stunned, and more!\n" +
                AltUI.UIObjectiveMarkerText + "Melee" + AltUI.UIEndColor +
                ", now powerful, has the undisputable ability to dislodge blocks, even those at 0 fragility for that matter.\n\n" +
                AltUI.UIEnemyText + "Enemies" + AltUI.UIEndColor + " also can do the same back to you as well!", AltUI.LabelWhite);

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

        }

        internal static LoadingHintsExt.LoadingHint loadHint1 = new LoadingHintsExt.LoadingHint(KickStart.name, "STATUS EFFECT HINT",
            "Flamethrowers will " + AltUI.HighlightString("Overheat") + " the " + AltUI.EnemyString("enemy") + ".\n" +
            "This will boost the amount of damage you do to them!");
        internal static LoadingHintsExt.LoadingHint loadHint2 = new LoadingHintsExt.LoadingHint(KickStart.name, "STATUS EFFECT HINT",
            "Cryo weapons will " + AltUI.HighlightString("Freeze") + " the " + AltUI.EnemyString("enemy") + ".\n" +
            "This will make them easier to catch up to.");
        internal static LoadingHintsExt.LoadingHint loadHint3 = new LoadingHintsExt.LoadingHint(KickStart.name, "STATUS EFFECT HINT",
            "EMP weapons will " + AltUI.HighlightString("Stun") + " the " + AltUI.EnemyString("enemy") + ".\n" +
            "This helps slow weapons hit.");
        internal static LoadingHintsExt.LoadingHint loadHint4 = new LoadingHintsExt.LoadingHint(KickStart.name, "STATUS EFFECT HINT",
            "Impact weapons will " + AltUI.HighlightString("Loosen") + " the " + AltUI.EnemyString("enemy's") + ".\n" +
            " blocks.  This will help greatly against enemies with heavy repair systems.");

    }
}
