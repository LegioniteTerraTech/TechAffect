using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using AffectTech.Stats;
using Binding;
using UnityEngine.SceneManagement;

namespace AffectTech
{
    internal class ManExtStatusEffects : MonoBehaviour
    {
        private static readonly FieldInfo explodoType = typeof(Explosion).GetField("m_DamageType",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo explodoM = typeof(Projectile).GetField("m_Explosion",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        private float time = 0;
        private float timeFlash = 0;
        private float timeAcid = 0;
        private bool isBright = false;
        private float reloop = Mathf.PI * 2;
        private static List<Action> pendingSubs = new List<Action>();
        internal static HashSet<ExtStatusEffect> pendingSpread = new HashSet<ExtStatusEffect>();

        public static EventNoParams acidUpdate = new EventNoParams();
        public static EventNoParams spreadUpdate = new EventNoParams();

        internal static Dictionary<StatusType, ExtStatusEffect> effectsPrefabs =
            new Dictionary<StatusType, ExtStatusEffect>();

        internal static Dictionary<StatusType, Stack<ExtStatusEffect>> effectsPool =
            new Dictionary<StatusType, Stack<ExtStatusEffect>>();



        private void FirstInit()
        {
            if (!effectsPrefabs.Any())
            {
                RegisterEffect(new SEAcid());
                RegisterEffect(new SEEMP());
                RegisterEffect(new SEFilmShield());
                RegisterEffect(new SEFreezing());
                RegisterEffect(new SEHacked());
                RegisterEffect(new SEJamming());
                RegisterEffect(new SEOverheat());
                RegisterEffect(new SEPry());
                RegisterEffect(new SEOverclock());


                StatusCondition.InitNewStatus();
            }
        }
        internal static void FirstDelayedInit()
        {
            DebugAffectTech.Log("FirstDelayedInit");
            ModDeathExplosion(BlockTypes.GSOBattery_111, DamageTypesExt.Acid, 1f, 1f);
            DebugAffectTech.Log("AffectTech: GSOBattery_111 Acidified");
            ModDeathExplosion(BlockTypes.GCBattery_222, DamageTypesExt.Acid, 10f, 1.25f);
            DebugAffectTech.Log("AffectTech: GCBattery_222 Acidified");
            ModDeathExplosion(BlockTypes.VENBattery_111, DamageTypesExt.Acid, 1f, 0.75f);
            DebugAffectTech.Log("AffectTech: VENBattery_111 Acidified");
            ModDeathExplosion(BlockTypes.BF_Battery_112, DamageTypesExt.Acid, 6f, 2.5f);
            DebugAffectTech.Log("AffectTech: BF_Battery_112 Acidified");
            ModDeathExplosion(BlockTypes.HE_Battery_211, DamageTypesExt.Acid, 1f, 1.25f);
            DebugAffectTech.Log("AffectTech: HE_Battery_211 Acidified");

            BuffRRCabs();
            BuffSJ();
        }

        private static void ModDeathExplosion(BlockTypes typeToEdit, DamageTypesExt type, 
            float strengthMulti, float radMulti)
        {
            var prefab = ManSpawn.inst.GetBlockPrefab(typeToEdit);
            Explosion explodo = prefab?.GetComponent<ModuleDamage>()?.deathExplosion?.GetComponent<Explosion>();
            if (explodo != null)
            {
                if ((ManDamage.DamageType)explodoType.GetValue(explodo) !=
                    (ManDamage.DamageType)DamageTypesExt.Acid)
                {
                    explodoType.SetValue(explodo, (ManDamage.DamageType)type);
                    explodo.m_MaxDamageStrength *= strengthMulti;
                    explodo.m_EffectRadius *= radMulti;
                }
                DebugAffectTech.Log("FirstDelayedInit set " + typeToEdit.ToString());
            }
            else
                DebugAffectTech.Log("FAILED to set " + typeToEdit.ToString());
        }
        private static void BuffRRCabs()
        {
            AddModReg(BlockTypes.EXP_Cab_212, 3.5f, DoAddModReg);
            ModDeathExplosion(BlockTypes.EXP_Cab_212, DamageTypesExt.Acid, 1.5f, 2.5f);
            DebugAffectTech.Log("AffectTech: EXP_Cab_212 Acidified");
            AddModReg(BlockTypes.EXP_Cab_112, 1, DoAddModReg);
            ModDeathExplosion(BlockTypes.EXP_Cab_112, DamageTypesExt.Acid, 1.5f, 2.5f);
            DebugAffectTech.Log("AffectTech: EXP_Cab_112 Acidified");
        }
        private static void BuffSJ()
        {
            AddModReg(BlockTypes.SJ_Cab_122, 3.5f, DoAddModReg2);
            ModDeathExplosion(BlockTypes.SJ_Cannon_313, DamageTypesExt.Acid, 2f, 1.33f);
            DebugAffectTech.Log("AffectTech: SJ_Cab_122 Acidified & Clocked");
            AddModReg(BlockTypes.SJ_Cannon_313, 3.5f, DoAddModWeap);
            DebugAffectTech.Log("AffectTech: SJ_Cannon_313 Acidified");
            ModDeathExplosion(BlockTypes.SJ_MachineGun_112, DamageTypesExt.Fire, 1.25f, 1.33f);
            AddModReg(BlockTypes.SJ_MachineGun_112, 3.5f, DoAddModWeap2);
            DebugAffectTech.Log("AffectTech: SJ_MachineGun_112 Rusted");
            ModDeathExplosion(BlockTypes.SJ_FlameThrower_215, DamageTypesExt.Cryo, 1.5f, 1f);
            AddModReg(BlockTypes.SJ_FlameThrower_215, 3.5f, DoAddModWeap3);
            DebugAffectTech.Log("AffectTech: SJ_FlameThrower_215 Frozen");
        }
        private static void DoAddModReg(TankBlock item, float strength)
        {
            ModuleRegulator reg = item.gameObject.AddComponent<ModuleRegulator>();
            reg.ApplyerType = StatusType.FilmShield;
            reg.ApplyRate = 16f * strength;
            reg.DrainRate = 7.5f * strength;
            reg.enabled = true;
            reg.OnPool();
        }
        private static void DoAddModReg2(TankBlock item, float strength)
        {
            ModuleRegulator reg = item.gameObject.AddComponent<ModuleRegulator>();
            reg.ApplyerType = StatusType.Overclock;
            reg.ApplyRate = 16f * strength;
            reg.DrainRate = 0;
            reg.enabled = true;
            reg.OnPool();
        }

        private static void DoAddModWeap(TankBlock item, float strength)
        {
            FireData FD = item.gameObject.GetComponent<FireData>();
            WeaponRound WR = FD.m_BulletPrefab;
            if (!WR.GetComponent<SpecialDamageType>())
            {
                SpecialDamageType SDT = WR.gameObject.AddComponent<SpecialDamageType>();
                SDT.OverrideDamageType = DamageTypesExt.Acid;
                SDT.OnPool();
                Explosion Exp = ((Transform)explodoM.GetValue((Projectile)WR)).GetComponent<Explosion>();
                SDT = Exp.gameObject.AddComponent<SpecialDamageType>();
                SDT.OverrideDamageType = DamageTypesExt.Acid;
                SDT.OnPool();
            }
        }
        private static void DoAddModWeap2(TankBlock item, float strength)
        {
            FireData FD = item.gameObject.GetComponent<FireData>();
            WeaponRound WR = FD.m_BulletPrefab;
            SpecialDamageType SDT = WR.gameObject.AddComponent<SpecialDamageType>();
            SDT.OverrideDamageType = DamageTypesExt.Rust;
            SDT.OnPool();
        }
        private static void DoAddModWeap3(TankBlock item, float strength)
        {
            ParticleSystem PS = (ParticleSystem)StatusCondition.flameParticles.GetValue(item.GetComponent<ModuleWeaponFlamethrower>());
            var shape = PS.shape;
            shape.textureColorAffectsParticles = false;
            if (shape.spriteRenderer)
                shape.spriteRenderer.color = new Color(0.3f, 1, 1, 1);

            SpecialDamageType SDT = item.gameObject.AddComponent<SpecialDamageType>();
            SDT.OverrideDamageType = DamageTypesExt.Cryo;
            SDT.OnPool();
        }
        private static void AddModReg(BlockTypes BT, float strength, Action<TankBlock, float> act)
        {
            foreach (var item in FindObjectsOfType<TankBlock>())
            {
                try
                {
                    if ((BlockTypes)item.GetComponent<Visible>().ItemType == BT)
                    {
                        act.Invoke(item, strength);
                        DebugAffectTech.Log("FirstDelayedInit set " + item.name);
                    }
                }
                catch (Exception) { }
            }
            var prefab = ManSpawn.inst.GetBlockPrefab(BT);
            if (prefab && !prefab.GetComponent<ModuleRegulator>())
            {
                act.Invoke(prefab, strength);
                DebugAffectTech.Log("FirstDelayedInit[prefab] set " + prefab.name);
            }
        }



        internal void RegisterEffect(ExtStatusEffect effect)
        {
            if (effect.StatType == StatusType.NULL)
                throw new InvalidOperationException("ManStatus encountered " + effect.GetType() + " of StatusType " +
                    effect.StatType + " which is not a valid StatusType");
            if (effectsPrefabs.ContainsKey(effect.StatType))
                throw new InvalidOperationException("ManStatus encountered " + effect.GetType() + " of StatusType " + 
                    effect.StatType + " which already has StatType set up to a valid instance");
            effectsPrefabs.Add(effect.StatType, effect);
            effectsPool.Add(effect.StatType, new Stack<ExtStatusEffect>(8));
        }
        private static ExtStatusEffect GetPooledEffect(StatusType type)
        {
            if (effectsPool[type].Any())
            {
                return effectsPool[type].Pop();
            }
            else
                return (ExtStatusEffect)Activator.CreateInstance(effectsPrefabs[type].GetType());
        }
        internal static ExtStatusEffect CreateEffect(StatusCondition cond, StatusType type, float value, ref float damageMulti)
        {
            var effect = GetPooledEffect(type);
            effect.Init(cond, value, ref damageMulti);
            return effect;
        }
        internal static ExtStatusEffect CreateEffect(StatusCondition cond, StatusType type, ManDamage.DamageInfo info, ref float damageMulti)
        {
            var effect = GetPooledEffect(type);
            effect.Init(cond, info, ref damageMulti);
            return effect;
        }
        internal static void ReturnEffectToPool(ExtStatusEffect effect)
        {
            effect.DeInit();
            effect.impactValue = 0;
            effect.impactExcess = 0;
            effectsPool[effect.StatType].Push(effect);
        }

        internal static void PrepSpread(ExtStatusEffect effect)
        {
            if (!pendingSpread.Contains(effect))
            {
                pendingSpread.Add(effect);
            }
        }

        private void Update()
        {
            FirstInit();

            timeFlash = (timeFlash + (Time.deltaTime * 10)) % reloop;
            StatusCondition.t2d2.SetPixel(0, 24, new Color(1f, 1f, 1f, 0.7f + (0.25f * Mathf.Sin(timeFlash))));        // Scrambled Color
            if (time < Time.time)
            {
                isBright = !isBright;
                if (isBright)
                    StatusCondition.t2d2.SetPixel(0, 21, new Color(1f, 1f, 0.5f, 0.85f));   // EMP Color
                else
                    StatusCondition.t2d2.SetPixel(0, 21, new Color(1f, 1f, 0.5f, 0.55f));   // EMP Color
                if (!ManPauseGame.inst.IsPaused)
                {
                    spreadUpdate.Send();
                    DelayedSubscribe();
                    foreach (var item in pendingSpread)
                    {
                        item.DoUpdateSpread();
                    }
                    pendingSpread.Clear();
                }
                time = Time.time + 0.35f;
            }
            StatusCondition.t2d2.Apply();
            if (timeAcid < Time.time)
            {
                if (!ManPauseGame.inst.IsPaused)
                {
                    acidUpdate.Send();
                }
                timeAcid = Time.time + 1;
            }
        }
        public static void PrepForSub(Action eventCase)
        {
            pendingSubs.Add(eventCase);
        }
        // Event does not like it when we are subscribing during a call
        private void DelayedSubscribe()
        {
            foreach (Action sub in pendingSubs)
                spreadUpdate.Subscribe(sub);
            pendingSubs.Clear();
        }
    }

}
