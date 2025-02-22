using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Nuterra.NativeOptions;
using ModHelper;
using HarmonyLib;

namespace AffectTech
{
    public class KickStartAffectTech : ModBase
    {

        internal static KickStartAffectTech oInst;

        bool isInit = false;
        bool firstInit = false;
        public override bool HasEarlyInit()
        {
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            if (oInst == null)
            {
                oInst = this;
                if (!KickStart.hasPatched)
                {
                    try
                    {
                        KickStart.harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                        KickStart.hasPatched = true;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Error in AffectTech: ", e);
                    }
                }
                StatusEffectsWiki.InitWiki();
            }
        }
        public override void Init()
        {
            if (isInit)
                return;
            if (oInst == null)
                oInst = this;
            try
            {
                KickStart.Enable();
            }
            catch (Exception e)
            {
                throw new Exception("Error in AffectTech: ", e);
            }
            if (!firstInit)
            {
                StatusCondition.InitNewStatus();
                firstInit = true;
            }
            else { }
            isInit = true;
        }
        public override void DeInit()
        {
            if (!isInit)
                return;
            KickStart.Disable();
            isInit = false;
        }

    }

    /// <summary>
    /// Please reach out to LegioniteTerraTech if you would like to request changes.
    /// </summary>
    public class KickStart
    {
        //Let hooks happen i guess
        public const string name = "Affect Tech";

#if STEAM
        public static bool EnableThis = false;
#else
        public static bool EnableThis = true;
#endif

        internal static bool RandomAdditionsAvail = false;
        internal static bool ActiveDefensesAvail = false;
        internal static bool WeaponAimModAvail = false;
        internal static bool TACAIModAvail = false;
        internal static bool FusionBlockAvail = false;
        internal static bool isBlockInjectorPresent = false;

        internal static bool hasPatched = false;

        internal static Harmony harmonyInstance = new Harmony("legioniteterratech.tweaktech");
        private static MethodInfo patchedECase = null;
        public static void Enable()
        {
            EnableThis = true;
            RandomAdditionsAvail = LookForMod("RandomAdditions");
            WeaponAimModAvail = LookForMod("WeaponAimMod");
            TACAIModAvail = LookForMod("TAC_AI");
            FusionBlockAvail = LookForMod("Fusion Block");
            isBlockInjectorPresent = LookForMod("BlockInjector");
            if (!hasPatched)
            {
                int error = 0;
                try
                {
                    harmonyInstance.PatchAll();
                    error++;
                    hasPatched = true;

                    var inner = AccessTools.Inner(typeof(MaterialSwapper),
                        "BlockMatConfigProperties").GetMethod("SetDamageColour");
                    error++;
                    var other = new HarmonyMethod(typeof(KickStart).GetMethod("PrefixAnnoyance"));
                    error++;

                    patchedECase = harmonyInstance.Patch(inner, other);
                    error++;
                }
                catch (Exception e)
                {
                    DebugAffectTech.Log("AffectTech Failed - " + error);
                    ManUI.inst.ShowErrorPopup("AffectTech: Please install Harmony (2.1.0) on the Steam Workshop to use the mod TweakTech, it is a functional dependancy.");
                    DebugAffectTech.Log("AffectTech Failed - " + e);
                }
            }

            try
            {
                //KickStartInitOptions.TryInit();
            }
            catch (Exception e) { DebugAffectTech.LogError("AffectTech: Failed on Options&Config " + e); }
            
            DamageTypesExtender.ApplyDamageTweaks();

            DebugAffectTech.Log("AffectTech: Init");

        }
        public static bool PrefixAnnoyance(MonoBehaviour __instance)
        {
            var SC = __instance.GetComponent<StatusCondition>();
            if (SC)
                return false;
            return true;
        }

        public static void Disable()
        {
            EnableThis = false;
            DamageTypesExtender.RevertDamageTweaks();
            if (hasPatched)
            {
                var inner = AccessTools.Inner(typeof(MaterialSwapper),
                    "BlockMatConfigProperties").GetMethod("SetDamageColour");
                harmonyInstance.Unpatch(inner, HarmonyPatchType.Prefix, "legioniteterratech.tweaktech");
                harmonyInstance.UnpatchAll("legioniteterratech.tweaktech");
                hasPatched = false;
            }


            DebugAffectTech.Log("AffectTech: DeInit");

        }

        public static bool LookForMod(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static class KickStartInitOptions
    {
        const string ModName = "Affect Tech";

        public static OptionToggle enabledMod;
        //public static OptionRange multiHP;
        public static OptionToggle blockHP150;
        public static OptionToggle maxRestrict;
        public static OptionRange reduceProjectiles;
        public static OptionRange reduceProjectilesThreshold;

        public static void TryInit()
        {
            ModConfig thisModConfig = new ModConfig();
            thisModConfig.BindConfig<KickStart>(null, "EnableThis");

            string Tweakables = ModName;
            NativeOptionsMod.onOptionsSaved.AddListener(() => { thisModConfig.WriteConfigJsonFile(); });
        }
    }
}
