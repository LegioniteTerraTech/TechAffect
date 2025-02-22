using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;


namespace AffectTech
{
    internal class DamageTweak
    {
        internal float Multiplier = 1;

        internal DamageTypesExt Dealer = DamageTypesExt.Standard;
        internal ManDamage.DamageableType Taker = ManDamage.DamageableType.Standard;
        // Can add new DamageTypes but not DamageableTypes
        //   DamageableTypes are handled by their own respective "ModuleReinforced"
    }
    internal static class DamageTypesExtender
    {

        private static bool did = false;
        private static int AdditionalDamageTypes = Enum.GetNames(typeof(DamageTypesExt)).Length -
            Enum.GetNames(typeof(ManDamage.DamageType)).Length;
        private static FieldInfo damageChart = typeof(ManDamage).GetField("m_DamageMultiplierTable", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo damageVals = typeof(DamageMultiplierTable).GetField("m_DamageTypeMultiplierLookup", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool didTex = false;
        private static FieldInfo FIDT = typeof(SpriteFetcher).GetField("m_DamageTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo FIDAT = typeof(SpriteFetcher).GetField("m_DamageableTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static void AddNewTextures()
        {
            if (didTex)
                return;
            didTex = true;
            try
            {
                ResourcesHelper.TryGetModContainer("Affect Tech", out ModContainer MC);
                //ResourcesHelper.LookIntoModContents(MC);

                Sprite[] DTs = (Sprite[])FIDT.GetValue(ManUI.inst.m_SpriteFetcher);
                Sprite refSprite = DTs[1];
                Array.Resize(ref DTs, DTs.Length + AdditionalDamageTypes);
                Texture2D item = null;
                for (DamageTypesExt step = DamageTypesExt.Cryo; step <= DamageTypesExt.Acid; step++)
                {
                    string name = "DT_" + step.ToString();
                    item = ResourcesHelper.GetTextureFromModAssetBundle(MC, name);
                    if (item)
                        DTs[(int)step] = Sprite.Create(item, refSprite.rect, refSprite.pivot);
                    else
                        DebugAffectTech.Log("AffectTech: AddNewTextures - FAILED getting " + name);
                }
                FIDT.SetValue(ManUI.inst.m_SpriteFetcher, DTs);

                /*
                Sprite[] DATs = (Sprite[])FIDAT.GetValue(ManUI.inst.m_SpriteFetcher);
                refSprite = DATs[1];
                Array.Resize(ref DATs, DATs.Length + AdditionalDamageTypes);
                for (DamageTypesExt step = DamageTypesExt.Cryo; step <= DamageTypesExt.Acid; step++)
                {
                    Texture2D item = KickStartAffectTech.oInst.GetModObject<Texture2D>("DAT_" + step.ToString());
                    if (item)
                        DATs[(int)step] = Sprite.Create(item, refSprite.rect, refSprite.pivot);
                }
                FIDAT.SetValue(ManUI.inst.m_SpriteFetcher, DATs);
                */
            }
            catch (Exception e)
            {
                DebugAffectTech.Log("AffectTech: AddNewTextures - FAILED with " + e);
                foreach (var item in ResourcesHelper.IterateAllMods())
                {
                    DebugAffectTech.Log("- " + item.Key);
                }
            }
        }

        private static DamageMultiplierTable DMT;
        private static float[] defaultDmgs = null;
        internal static void RevertDamageTweaks()
        {
            if (!did)
                return;
            damageVals.SetValue(DMT, defaultDmgs);
            WikiPageDamageStats.OverrideDamageLookup(ManDamage.NumDamageTypes, ManDamage.NumDamageableTypes, defaultDmgs);
        }
        internal static void ApplyDamageTweaks()
        {
            if (did)
                return;
            DebugAffectTech.Log("AffectTech: ApplyDamageTweaks");
            try
            {
                did = true;
                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 1");
                DMT = (DamageMultiplierTable)damageChart.GetValue(
                    Singleton.Manager<ManDamage>.inst);
                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 2");
                if (defaultDmgs == null)
                    defaultDmgs = (float[])damageVals.GetValue(DMT);
                float[] arraydmge = defaultDmgs;
                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 3");
                
                ReformatMultipliers(ref arraydmge);
                WikiPageDamageStats.OverrideDamageLookup(ManDamage.NumDamageTypes + AdditionalDamageTypes,
                    ManDamage.NumDamageableTypes, defaultDmgs);
                List<DamageTweak> allTweaks = DMGTweaks;
                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 4");
                //allTweaks.AddRange(Tweaks.DMGTweaksVanilla);
                foreach (DamageTweak DT in allTweaks)
                {
                    arraydmge[(int)((int)DT.Dealer * ManDamage.NumDamageableTypes + DT.Taker)] = DT.Multiplier;
                }
                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 5");
                AddNewTextures();

                //DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - 6");
                damageVals.SetValue(DMT, arraydmge);
                DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - Success");
            }
            catch (Exception e)
            {
                DebugAffectTech.Log("AffectTech: ApplyDamageTweaks - FAILED with " + e);
            }
        }
        /// <summary>
        /// This just copies the array anyways
        /// </summary>
        /// <param name="arraydmge"></param>
        private static void ReformatMultipliers(ref float[] arraydmge)
        {
            if (AdditionalDamageTypes < 1)
                return;
            int count = arraydmge.Length;
            float[] arraydmge2 = new float[count + (ManDamage.NumDamageableTypes * AdditionalDamageTypes)];

            for (int step = 0; step < count; step++)
            {
                arraydmge2[step] = arraydmge[step];
            }
            int extendedSet = arraydmge2.Length;
            for (int step = count; step < extendedSet; step++)
            {
                arraydmge2[step] = 1;
            }
            arraydmge = arraydmge2;
        }



        internal static List<DamageTweak> DMGTweaksVanilla = new List<DamageTweak>()
        {
        };

        internal static List<DamageTweak> DMGTweaks = new List<DamageTweak>(){
            // Cryogenic
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Standard,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Armour,
            },
            new DamageTweak {
                Multiplier = 0.25f,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Rock,
            },
            new DamageTweak {
                Multiplier = 0.5f,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Rubber,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Shield,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Volatile,
            },
            new DamageTweak {
                Multiplier = 1.5f,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Wood,
            },
            new DamageTweak {
                Multiplier = 1.0f,
                Dealer = DamageTypesExt.Cryo,
                Taker = ManDamage.DamageableType.Compound,
            },
            // EMP
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Standard,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Armour,
            },
            new DamageTweak {
                Multiplier = 0.1f, // Strong resistance
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Rock,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Rubber,
            },
            new DamageTweak {
                Multiplier = 4,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Shield,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Volatile,
            },
            new DamageTweak {
                Multiplier = 0, // Can't zap wood
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Wood,
            },
            new DamageTweak {
                Multiplier = 1.0f,
                Dealer = DamageTypesExt.EMP,
                Taker = ManDamage.DamageableType.Compound,
            },
            // Scramble
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Standard,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Armour,
            },
            new DamageTweak {
                Multiplier = 0.1f, // Strong resistance
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Rock,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Rubber,
            },
            new DamageTweak {
                Multiplier = 4,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Shield,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Volatile,
            },
            new DamageTweak {
                Multiplier = 0, // Can't zap wood
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Wood,
            },
            new DamageTweak {
                Multiplier = 1.0f,
                Dealer = DamageTypesExt.Jamming,
                Taker = ManDamage.DamageableType.Compound,
            },
            // Acid
            new DamageTweak {
                Multiplier = 0.5f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Standard,
            },
            new DamageTweak {
                Multiplier = 0.5f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Armour,
            },
            new DamageTweak {
                Multiplier = 0.25f, // Strong resistance
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Rock,
            },
            new DamageTweak {
                Multiplier = 0.75f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Rubber,
            },
            new DamageTweak {
                Multiplier = 0.25f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Shield,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Volatile,
            },
            new DamageTweak {
                Multiplier = 1.25f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Wood,
            },
            new DamageTweak {
                Multiplier = 0.1f,
                Dealer = DamageTypesExt.Acid,
                Taker = ManDamage.DamageableType.Compound,
            },
            // Rust
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Standard,
            },
            new DamageTweak {
                Multiplier = 1.5f,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Armour,
            },
            new DamageTweak {
                Multiplier = 0.75f,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Rock,
            },
            new DamageTweak {
                Multiplier = 0.25f, // Strong resistance
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Rubber,
            },
            new DamageTweak {
                Multiplier = 1.5f,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Shield,
            },
            new DamageTweak {
                Multiplier = 1,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Volatile,
            },
            new DamageTweak {
                Multiplier = 0.75f,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Wood,
            },
            new DamageTweak {
                Multiplier = 0f,
                Dealer = DamageTypesExt.Rust,
                Taker = ManDamage.DamageableType.Compound,
            },
        };

    }
}
