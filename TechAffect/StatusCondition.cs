using AffectTech.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static AchievementObject;
using static LocalisationEnums;

namespace AffectTech
{
    /// <summary>
    /// This is not applied consistantly between clients.  What the client sees on their own Tech is what they experience
    /// </summary>
    public class StatusCondition : MonoBehaviour
    {   // A Block CAN have more than two statuses at once

        internal static ManExtStatusEffects man;
        internal static bool ignoreApplyEffect = false;

        internal const byte spreadLastFrameDelay = 3;
        private const float RedDelay = 0.5f;
        internal const float RedPulseDelay = 1f;
        internal const float RedPulseDelayShort = 0.75f;
        internal const float RedPulseDelayImpatient = 0.25f;
        public const float RecoverDelay = 2.5f;
        private const float DamageVisualSwitch = 1.25f;

        private const float AcidBaseDamage = 1.2f;

        public const float FilmShieldDrain = 0.025f;

        private static MaterialPropertyBlock MPB;
        private static int matProp;
        private static MethodInfo MPBReset;
        public static Texture2D t2d2;
        public static float t2d2H = 1;

        private static readonly FieldInfo colorsForMore = typeof(ManTechMaterialSwap).GetField("m_BlockDamageCLUT", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo colorsForMoreH = typeof(ManTechMaterialSwap).GetField("m_BlockDamageScaleV", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        private static readonly FieldInfo renderRef = typeof(MaterialSwapper).GetField("m_Renderers",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo skinUVGet = typeof(MaterialSwapper).GetField("m_SkinValueUV", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);


        // Local Properties
        internal TankBlock block;
        public Damageable damageable => block.visible.damageable;
        internal MaterialSwapper swap;
        internal List<Renderer> renders;

        private Vector2 skinVector2 = Vector2.zero;

        private float renderSwitchDelay = 0;
        private byte renderSwitcher = 0;
        internal float redDelay = 0;
        internal float warningRepeat = 0;
        internal float originalFragility = 0;
        internal bool disabledComponents = false;
        internal bool precisionComponents = false;
        internal byte spreadLastFrame = 0;
        public bool allowModuleUpdate = true;

        internal Dictionary<StatusType, ExtStatusEffect> effects = new Dictionary<StatusType, ExtStatusEffect>();


        public bool MachineInterference()
        {
            return effects.ContainsKey(StatusType.EMF) || effects.ContainsKey(StatusType.Freezing);
        }
        public float GetOpPercent()
        {
            if (effects.TryGetValue(StatusType.EMF, out var effect) && effect.impactPercent > 0.5f)
                return 0;
            else if (effects.TryGetValue(StatusType.Freezing, out effect))
                return 1 - (effect.impactPercent / 1.25f);
            return 1;
        }

        public static bool IsSameTeam(Damageable dmg, ManDamage.DamageInfo info)
        {
            Tank attackTank = info.SourceTank;
            if (attackTank)
            {
                Tank defTank = dmg?.Block?.tank;
                if (defTank)
                {
                    if (defTank.visible.ID == attackTank.visible.ID || defTank.IsFriendly(attackTank.Team))
                        return true;
                }
            }
            return false;
        }
        public static ManDamage.DamageInfo MakeAcid(Component source, Tank sourceTank)
        {
            return new ManDamage.DamageInfo(AcidBaseDamage, (ManDamage.DamageType)DamageTypesExt.Acid, source, sourceTank);
        }

        public static bool inited = false;
        public static void InitNewStatus()
        {
            if (!inited)
            {
                man = Instantiate(new GameObject("ManStatus"), null, true).AddComponent<ManExtStatusEffects>();
                inited = true;
                DebugAffectTech.Log("StatusCondition: InitNewStatus");
            }
            matProp = (int)typeof(MaterialSwapper).GetField("s_matPropCoreFourId", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            MPBReset = typeof(MaterialSwapper).GetMethod("InitStatic", BindingFlags.NonPublic | BindingFlags.Static);
            MPB = (MaterialPropertyBlock)typeof(MaterialSwapper).GetField("s_matPropBlock", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            t2d2 = new Texture2D(1, 64);
            t2d2.SetPixel(0, 0, new Color(1f, 1f, 1f, 0f));
            t2d2.SetPixel(0, 1, Globals.inst.moduleDamageParams.DamageColour);
            t2d2.SetPixel(0, 2, Globals.inst.moduleDamageParams.HealColour);
            t2d2.SetPixel(0, 3, new Color(1f, 1f, 1f, 0f));
            t2d2.SetPixel(0, 4, Globals.inst.moduleDamageParams.ScavengeColour);
            t2d2.SetPixel(0, 5, Globals.inst.moduleDamageParams.OutOfShieldColour);
            t2d2.SetPixel(0, 6, Globals.inst.moduleDamageParams.CloggedColour);
            // reserve like up to 13 pixels for Payload

            t2d2.SetPixel(0, 13, new Color(1f, 0.5f, 0f, 0.95f));       // Overheat
            t2d2.SetPixel(0, 14, new Color(1f, 0.5f, 0f, 0.75f));       // 
            t2d2.SetPixel(0, 15, new Color(1f, 0.5f, 0f, 0.55f));       // 
            t2d2.SetPixel(0, 16, new Color(1f, 0.5f, 0f, 0.35f));       // 
            t2d2.SetPixel(0, 17, new Color(0.4f, 0.95f, 0.9f, 0.35f));  // Freeze
            t2d2.SetPixel(0, 18, new Color(0.4f, 0.95f, 0.9f, 0.55f));  //
            t2d2.SetPixel(0, 19, new Color(0.4f, 0.95f, 0.9f, 0.75f));  //
            t2d2.SetPixel(0, 20, new Color(0.4f, 0.95f, 0.9f, 0.95f));  //
            t2d2.SetPixel(0, 21, new Color(1f, 1f, 0.5f, 0.85f));       // EMP
            t2d2.SetPixel(0, 22, new Color(1f, 1f, 0.5f, 0.45f));       // 
            t2d2.SetPixel(0, 23, new Color(1f, 1f, 1f, 0.25f));         // Scrambled
            t2d2.SetPixel(0, 24, new Color(1f, 1f, 1f, 0.65f));         // 
            t2d2.SetPixel(0, 25, new Color(0f, 0.4f, 0f, 0.99f));       // Hacked
            t2d2.SetPixel(0, 26, new Color(0.4f, 0.01f, 1f, 1f));       // Acid
            t2d2.SetPixel(0, 27, new Color(0.4f, 0.01f, 1f, 0.75f));    // 
            t2d2.SetPixel(0, 28, new Color(0.1f, 0.3f, 1f, 0.25f));     // Shield
            t2d2.SetPixel(0, 29, new Color(0.1f, 0.3f, 1f, 0.35f));     //
            t2d2.SetPixel(0, 30, new Color(0.1f, 0.3f, 1f, 0.45f));     //
            t2d2.SetPixel(0, 31, new Color(0.1f, 0.3f, 1f, 0.55f));     //
            t2d2.SetPixel(0, 32, new Color(1f, 0.31f, 1f, 0.25f));      // Overclock
            t2d2.SetPixel(0, 33, new Color(1f, 0.31f, 1f, 0.40f));      //
            t2d2.SetPixel(0, 34, new Color(1f, 0.31f, 1f, 0.55f));      //
            t2d2.SetPixel(0, 35, new Color(1f, 0.31f, 1f, 0.70f));      //
            t2d2.Apply();
            colorsForMore.SetValue(ManTechMaterialSwap.inst, t2d2);
            Shader.SetGlobalTexture("_DamageCLUT", t2d2);
            t2d2H = 1f / (float)t2d2.height;
            colorsForMoreH.SetValue(ManTechMaterialSwap.inst, t2d2H);
        }
        public static void TryApplyEffect(Damageable damageable, ManDamage.DamageInfo info, 
            StatusType inflicted, bool doDamage, ref float damageMulti, float affectMulti = 1)
        {
            if (ignoreApplyEffect)
                return;
            StatusCondition SC = damageable.Block?.GetComponent<StatusCondition>();
            if ((bool)SC)
            {
                if (SC.RemainsAfterOtherStatuses(info, inflicted, ref damageMulti))
                {
                    SC.AddEffect(info.Damage * affectMulti, inflicted, doDamage, ref damageMulti);
                    SC.CancelInvoke("Remove");
                    SC.renderSwitchDelay = DamageVisualSwitch;
                    SC.renderSwitcher = (byte)SC.effects.Keys.ToList().IndexOf(inflicted);
                }
                return;
            }
            else
            {
                SC = AddTo(damageable, inflicted);
                if (!SC)
                {
                    DebugAffectTech.LogError("AffectTech: Tried to add a StatusCondition of type " +
                         inflicted.ToString() + " to block " +  
                         (damageable.name.NullOrEmpty() ? "<NULL>" : damageable.name) + ", but failed!");
                    return;
                }

                SC.AddEffect(info.Damage * affectMulti, inflicted, doDamage, ref damageMulti);
                SC.renderSwitchDelay = DamageVisualSwitch;
                SC.renderSwitcher = (byte)SC.effects.Keys.ToList().IndexOf(inflicted);
            }
        }
        public static StatusCondition AddTo(Damageable damageable, StatusType inflicted)
        {
            GameObject GO = damageable.Block?.gameObject;
            if (!(bool)GO)
                return null;
            var MS = GO.GetComponent<MaterialSwapper>();
            if (!(bool)MS)
                return null;
            if (!CanAddNewTo(GO, inflicted, out bool precision))
                return null;
            StatusCondition SC = GO.AddComponent<StatusCondition>();
            damageable.Block.AttachedEvent.Subscribe(SC.OnDelta);
            damageable.Block.DetachedEvent.Subscribe(SC.OnDelta);
            SC.precisionComponents = precision;
            SC.block = GO.GetComponent<TankBlock>();
            SC.swap = GO.GetComponent<MaterialSwapper>();
            SC.renders = (List<Renderer>)renderRef.GetValue(SC.swap);
            SC.originalFragility = GO.GetComponent<ModuleDamage>().m_DamageDetachFragility;
            damageable.Block.visible.RecycledEvent.Subscribe(SC.RemoveImmedeate);
            ModuleWeaponFlamethrower MWF = GO.GetComponent<ModuleWeaponFlamethrower>();
            if (MWF)
            {
                SC.flameLengthStart = (float)flameDist.GetValue(MWF);
                SC.flameSpeedStart = ((ParticleSystem)flameParticles.GetValue(MWF)).main.startSpeedMultiplier;
            }
            return SC;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="damageable"></param>
        /// <param name="info"></param>
        /// <param name="inflicted"></param>
        /// <returns>True if effect can be added</returns>
        private bool RemainsAfterOtherStatuses(ManDamage.DamageInfo info, StatusType inflicted, ref float damageMulti)
        {
            foreach (var item in effects)
            {
                item.Value.StatusInflicted_Internal(info.Damage, (DamageTypesExt)info.DamageType,
                    inflicted, info.SourceTank, ref damageMulti);
                if (damageMulti <= 0)
                    return false;
            }
            return true;
        }
        public static bool CanAddNewTo(GameObject GO, StatusType inflicted, out bool computing)
        {
            var value = ManExtStatusEffects.effectsPrefabs[inflicted];
            return value.CanAddNewTo(GO, out computing);
        }

        public static StatusCondition InitFilmShield(Damageable damageable, float value)
        {
            float temp = 1;
            StatusCondition SC = damageable.GetComponent<StatusCondition>();
            if ((bool)SC)
            {
                SC.CancelInvoke("Remove");
                SC.AddEffect(value, StatusType.FilmShield, false, ref temp);
                return SC;
            }
            SC = AddTo(damageable, StatusType.FilmShield);
            if (!SC)
                return null;

            SC.AddEffect(value, StatusType.FilmShield, false, ref temp);
            return SC;
        }

        // On receiving damage
        public static float OnDamageStatus(Damageable dmg, ManDamage.DamageInfo info)
        {
            if (!dmg)
                return 0;
            if (dmg.Invulnerable)
                return 0;
            StatusCondition SC = dmg.Block?.GetComponent<StatusCondition>();
            DamageTypesExt DTE = (DamageTypesExt)info.DamageType;
            bool harmful = true;
            if (SC)
            {
                if (IsSameTeam(dmg, info))
                {
                    foreach (var item in SC.effects)
                    {
                        if (item.Value.IsHelpful(DTE))
                        {
                            harmful = false;
                            break;
                        }
                    }
                    if (!harmful)
                        return 1;
                }
                /*
                switch (info.DamageType)
                {
                    case (ManDamage.DamageType)DamageTypesExt.Cryo:
                    case (ManDamage.DamageType)DamageTypesExt.Jamming:
                    case (ManDamage.DamageType)DamageTypesExt.Hack:
                        return 0;
                    case (ManDamage.DamageType)DamageTypesExt.EMP:
                        if (dmg.DamageableType == ManDamage.DamageableType.Shield)
                        {
                            if (SC.redDelay <= RedPulseDelayImpatient)
                                SC.redDelay = RedPulseDelayShort;
                            return 1;
                        }
                        return 0;
                    case (ManDamage.DamageType)DamageTypesExt.Acid:
                        return 0.1f;
                    default:
                        if (SC.redDelay <= RedPulseDelayImpatient)
                            SC.redDelay = RedPulseDelayShort;
                        break;
                }
                */
            }
            float damageMulti = 1;
            switch ((DamageTypesExt)info.DamageType)
            {
                case DamageTypesExt.Impact:
                    //TryApplyEffect(dmg, info, StatusType.Pry, ref damageMulti, 32);
                    break;
                case DamageTypesExt.Cutting:
                    //TryApplyEffect(dmg, info, StatusType.Pry, ref damageMulti, 6);
                    break;
                case DamageTypesExt.Energy:
                    TryApplyEffect(dmg, info, StatusType.Overheat, harmful, ref damageMulti, 6.4f);
                    break;
                case DamageTypesExt.Plasma:
                    TryApplyEffect(dmg, info, StatusType.Overheat, harmful, ref damageMulti, 16f);
                    break;
                case DamageTypesExt.Fire:
                    TryApplyEffect(dmg, info, StatusType.Overheat, harmful, ref damageMulti, 36f);
                    break;
                case DamageTypesExt.Bullet:
                    //InitAndOrAdd(dmg, info, StatusType.Freezing, ref damageMulti, 1);
                    //damageMulti = 1;
                    break;
                case DamageTypesExt.Electric:
                    TryApplyEffect(dmg, info, StatusType.EMF, harmful, ref damageMulti, 12f);
                    break;
                case DamageTypesExt.Standard:
                    //InitAndOrAdd(dmg, info, StatusType.Hacked, ref damageMulti);
                    //damageMulti = 1;
                    break;
                case DamageTypesExt.Cryo:
                    TryApplyEffect(dmg, info, StatusType.Freezing, harmful, ref damageMulti, 64);
                    break;
                case DamageTypesExt.EMP:
                    TryApplyEffect(dmg, info, StatusType.EMF, harmful, ref damageMulti);
                    break;
                case DamageTypesExt.Jamming:
                    TryApplyEffect(dmg, info, StatusType.Jamming, harmful, ref damageMulti);
                    break;
                case DamageTypesExt.Hack:
                    TryApplyEffect(dmg, info, StatusType.Hacked, harmful, ref damageMulti);
                    break;
                case DamageTypesExt.Acid:
                    TryApplyEffect(dmg, info, StatusType.Acid, harmful, ref damageMulti);
                    break;
                case DamageTypesExt.Rust:
                    TryApplyEffect(dmg, info, StatusType.Jamming, harmful, ref damageMulti);
                    break;
            }
            return damageMulti;
        }

        public void AddEffect(float addVal, StatusType type, bool redFlash)
        {
            float damageMulti = 0;
            AddEffect(addVal, type, redFlash, ref damageMulti);
        }
        public void AddEffect(float addVal, StatusType type, bool redFlash, ref float damageMulti)
        {
            if (!effects.TryGetValue(type, out ExtStatusEffect effect))
            {
                effect = ManExtStatusEffects.CreateEffect(this, type, addVal, ref damageMulti);
                effects.Add(type, effect);
            }
            if (redFlash && redDelay <= RedPulseDelayImpatient)
                redDelay = RedPulseDelayShort;
            effect.AddToVal(addVal);
        }
        

        internal void DoSpreadStatus(float value, StatusType inflicted)
        {
            float multi = 1;
            ExtStatusEffect ESE = ManExtStatusEffects.effectsPrefabs[inflicted];
            for (int step = 0; step < effects.Count;)
            {
                var item = effects.ElementAt(step);
                if (item.Value.StatusInflicted_Internal(value, 
                    ESE.DmgType, inflicted, null, ref multi))
                {
                    value *= multi;
                    step++;
                }
                else
                {
                    effects.Remove(item.Key);
                }
                if (value == 0)
                    return;
            }
            value *= multi;
            multi = 1;
            AddEffect(value, inflicted, false, ref multi);
        }


        private void OnDelta()
        {
            swap.enabled = false;
            if (effects.Any())
                UpdateRenders(effects.FirstOrDefault().Value);
        }
        public void RemoveImmedeate(Visible vis)
        {
            foreach (var effect in effects)
            {
                ManExtStatusEffects.ReturnEffectToPool(effect.Value);
            }
            effects.Clear();
            ResetRenders();
            ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, transform);
            swap.enabled = true;
            GetComponent<ModuleDamage>().m_DamageDetachFragility = originalFragility;
            swap.SwapMaterialTime(false);
            swap.SwapMaterialDamage(true);
            SetAllWorkingComponentsOff(false, 0);
            RemoveFinal();
        }
        internal void OnRemove(ExtStatusEffect effect)
        {
            effects.Remove(effect.StatType);
            ManExtStatusEffects.ReturnEffectToPool(effect);

            if (!effects.Any())
            {
                RemovePre();
            }
        }
        public void RemovePre()
        {
            ResetRenders();
            ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, transform);
            swap.enabled = true;
            GetComponent<ModuleDamage>().m_DamageDetachFragility = originalFragility;
            swap.SwapMaterialTime(false);
            swap.SwapMaterialDamage(true);
            SetAllWorkingComponentsOff(false, 0);
            Invoke("Remove", 0.001f);
        }
        public void RemoveFinal()
        {
            if (effects.Count == 0)
            {
                //DebugAffectTech.Log("AffectTech: StatusCondition - Remove");
                damageable.Block.DetachedEvent.Unsubscribe(OnDelta);
                damageable.Block.AttachedEvent.Unsubscribe(OnDelta);
                swap.enabled = true;
                swap.SwapMaterialDamage(false);
                swap.SwapMaterialTime(ManTimeOfDay.inst.NightTime);
                if (damageable.Health < block.damage.maxHealth * Globals.inst.moduleDamageParams.lowHealthFlashThreshold)
                    block.StartMaterialPulse(ManTechMaterialSwap.MaterialTypes.Damage, 
                        ManTechMaterialSwap.MaterialColour.Damage);
                block.visible.RecycledEvent.Unsubscribe(RemoveImmedeate);
                Destroy(this);
            }
        }

        public float GetEmitVal(float impactValue)
        {
            return Mathf.Min(1, impactValue / (damageable.MaxHealth * 2f));
        }
        private float UpdateRenders(ExtStatusEffect effect)
        {
            //DebugAffectTech.Log("AffectTech: StatusCondition - UpdateRenders");

            swap.enabled = false;
            float emitValPercent = Mathf.Min(1, effect.impactValue / (damageable.MaxHealth * 2f));
            //MaterialPropertyBlock MPB = propInitcache;
            float addVal3 = Mathf.Round(3 * emitValPercent);
            //Color caseC = MPB.GetColor("_EmissionColor");
            Vector2 visVec;
            if (redDelay > RedDelay)
            {
                visVec = new Vector2(1.5f * t2d2H, emitValPercent);
            }
            else
            {
                if (redDelay <= 0 && damageable.Health < damageable.MaxHealth * Globals.inst.moduleDamageParams.lowHealthFlashThreshold)
                {
                    visVec = new Vector2(1.5f * t2d2H, emitValPercent);
                    redDelay = RedPulseDelay;
                }
                else
                    visVec = effect.GetColorer(addVal3, emitValPercent);
            }
            if (visVec != skinVector2)
            {
                skinVector2 = visVec;
                Vector4 vec4 = MPB.GetVector(matProp);
                vec4.x = visVec.x;
                vec4.y = visVec.y;
                int skinIndex = block.GetSkinIndex();
                vec4.z = skinIndex / 8f;
                vec4.w = skinIndex % 8f;
                MPB.SetVector(matProp, vec4);
                foreach (Renderer ren in renders)
                {
                    try
                    {
                        ren.SetPropertyBlock(MPB);
                    }
                    catch { }
                }
            }
            return emitValPercent;
        }
        public void ResetRenders()
        {
            float defaultEmitScale = 1;
            Vector2 visVec = new Vector2(0.5f * t2d2H, defaultEmitScale);
            if (visVec != skinVector2)
            {
                skinVector2 = visVec;
                MPBReset.Invoke(null, new object[0]);
                int skinIndex = block.GetSkinIndex();
                MPB.SetVector(matProp, new Vector4(
                    visVec.x, visVec.y, skinIndex / 8f, skinIndex % 8f));
                foreach (Renderer ren in renders)
                {
                    try
                    {
                        ren.SetPropertyBlock(MPB);
                    }
                    catch { }
                }
            }
        }


        private static readonly FieldInfo dirtyGrav = typeof(ModuleAntiGravityEngine).GetField("m_GravityTargetsDirty", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo switchGrav = typeof(ModuleAntiGravityEngine).GetField("m_SwitchedOn", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo flameDist = typeof(ModuleWeaponFlamethrower).GetField("m_MaxFlameLength", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo flameParticles = typeof(ModuleWeaponFlamethrower).GetField("m_Particles", BindingFlags.NonPublic | BindingFlags.Instance);

        private float SpeedAll = 1;
        private float flameLengthStart = 0;
        private float flameSpeedStart = 0;
        public void SetSpeed(float value)
        {
            if (SpeedAll != value)
            {
                SpeedAll = value;
                var anchor = GetComponent<ModuleAnchor>();
                float invertValue = 1 - value;
                foreach (var item in block.gameObject.GetComponentsInChildren<Animator>(true))
                {
                    item.speed = Mathf.Max(0.1f, value);
                }
                if (block.Anchor)
                    block.Anchor.SetCloggedTorque(anchor.MaxTorque * invertValue);
                if (block.weapon)
                {
                    ModuleWeaponGun MWG = GetComponent<ModuleWeaponGun>();
                    if (MWG)
                    {
                        float recoilD = Mathf.Max(0.1f, MWG.m_ShotCooldown / value);
                        foreach (var item in block.gameObject.GetComponentsInChildren<CannonBarrel>(true))
                        {
                            item.CapRecoilDuration(recoilD);
                        }
                    }
                    ModuleWeaponFlamethrower MWF = GetComponent<ModuleWeaponFlamethrower>();
                    if (MWF)
                    {
                        flameDist.SetValue(MWF, flameLengthStart * value);
                        var mainer = ((ParticleSystem)flameParticles.GetValue(MWF)).main;
                        mainer.startSpeedMultiplier = flameSpeedStart * value;
                    }
                    if (value > 1)
                    {
                        float speedFalloff = block.weapon.m_RotateSpeed;
                        if (speedFalloff > 45)
                            block.weapon.SetRotationSpeedCoefficient(value);
                        else if (speedFalloff > 20)
                            block.weapon.SetRotationSpeedCoefficient(value * 2);
                        else if (speedFalloff > 10)
                            block.weapon.SetRotationSpeedCoefficient(value * 3);
                        else
                            block.weapon.SetRotationSpeedCoefficient(value * 4);
                    }
                    else
                        block.weapon.SetRotationSpeedCoefficient(value);
                }
                if (block.wheelsModule)
                {
                    ModuleWheels MW = block.wheelsModule;
                    block.wheelsModule.TrySetWheelClogged(0, 0.15f * invertValue, 0, 0.15f * invertValue);
                }
            }
        }
        public void SetAllWorkingComponentsOff(bool turnOff, float impactPercent, bool forceFull = false)
        {
            if (turnOff)
            {
                if (forceFull)
                    GetComponent<ModuleDamage>().m_DamageDetachFragility = 0;
                else
                    GetComponent<ModuleDamage>().m_DamageDetachFragility = (impactPercent / 2) + originalFragility;
            }
            if (disabledComponents == turnOff)
                return;
            disabledComponents = turnOff;
            if (!block.tank && turnOff)
            {
                SetAllWorkingComponentsOff(false, 0);
                return;
            }
            float Strength;
            if (forceFull)
                Strength = 1;
            else
                Strength = impactPercent / 2;
            if (turnOff)
            {
                if (forceFull)
                {
                    var aGrav = GetComponent<ModuleAntiGravityEngine>();
                    if (aGrav)
                    {
                        aGrav.enabled = false;
                        if (aGrav.SwitchedOn())
                        {
                            switchGrav.SetValue(aGrav, false);
                            dirtyGrav.SetValue(aGrav, true);
                        }
                    }
                    if (GetComponent<ModuleWing>())
                        GetComponent<ModuleWing>().enabled = false;
                }
                SetSpeed(Strength);
            }
            else
            {   // Reset everything
                GetComponent<ModuleDamage>().m_DamageDetachFragility = originalFragility;

                if (GetComponent<ModuleWing>())
                    GetComponent<ModuleWing>().enabled = true;

                if (GetComponent<ModuleAntiGravityEngine>())
                    GetComponent<ModuleAntiGravityEngine>().enabled = true;

                SetSpeed(0);
                //BlockTweak.ApplyToBlockLocal(block);
            }
        }

        public void SetComponentsOverclock(float impactPercent)
        {
            SetSpeed(Mathf.RoundToInt(impactPercent));
        }

        private static readonly FieldInfo MWGCooldown = typeof(ModuleWeaponGun).GetField("m_ShotTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo MWTCooldown = typeof(ModuleWeaponTeslaCoil).GetField("m_ChargeTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo MWGBarrel = typeof(CannonBarrel).GetField("recoiling", BindingFlags.NonPublic | BindingFlags.Instance);

        internal void SpeedUpdate(float impactPercent)
        {
            ModuleWeaponGun MWG = GetComponent<ModuleWeaponGun>();
            float SpedVal = impactPercent * Time.deltaTime;
            if (MWG)
            {
                MWGCooldown.SetValue(MWG, (float)MWGCooldown.GetValue(MWG) - SpedVal);
            }
            ModuleWeaponTeslaCoil MWT = GetComponent<ModuleWeaponTeslaCoil>();
            if (MWT)
                MWTCooldown.SetValue(MWT, (float)MWTCooldown.GetValue(MWT) - SpedVal);
        }
        internal void MalfunctionUpdate(float impactPercent)
        {
            allowModuleUpdate = UnityEngine.Random.Range(0, 100) >= impactPercent * 40;
            if (!allowModuleUpdate)
            {
                ModuleWeaponGun MWG = GetComponent<ModuleWeaponGun>();
                if (MWG)
                    MWGCooldown.SetValue(MWG, (float)MWGCooldown.GetValue(MWG) + PulseDelay);
                ModuleWeaponTeslaCoil MWT = GetComponent<ModuleWeaponTeslaCoil>();
                if (MWT)
                    MWTCooldown.SetValue(MWT, (float)MWTCooldown.GetValue(MWT) + PulseDelay);
            }
        }
        internal void HaltUtils(bool Freeze)
        {
            allowModuleUpdate = !Freeze;
            if (Freeze)
            {
                ModuleWeaponGun MWG = GetComponent<ModuleWeaponGun>();
                if (MWG)
                    MWGCooldown.SetValue(MWG, (float)MWGCooldown.GetValue(MWG) + PulseDelay);
                ModuleWeaponTeslaCoil MWT = GetComponent<ModuleWeaponTeslaCoil>();
                if (MWT)
                    MWTCooldown.SetValue(MWT, (float)MWTCooldown.GetValue(MWT) + PulseDelay);
            }
        }

        public const float PulseDelay = 0.25f;
        private float pulseTimer = 0;
        internal void Update()
        { //obujecots sounds like a neat word
            float resistance = damageable.MaxHealth * 2;
            if (effects.Count == 0)
            {
            }
            else if (effects.Count == 1)
            {
                renderSwitcher = 0;
                UpdateRenders(effects.FirstOrDefault().Value);
            }
            else
            { 
                if (renderSwitchDelay <= 0)
                {
                    renderSwitcher++;
                    renderSwitchDelay = DamageVisualSwitch;
                }
                if (renderSwitcher >= effects.Count)
                    renderSwitcher = 0;
                UpdateRenders(effects.ElementAt(renderSwitcher).Value);
                renderSwitchDelay -= Time.deltaTime;
            }

            if (redDelay > 0)
                redDelay -= Time.deltaTime;
            if (pulseTimer < Time.time)
            {
                pulseTimer = Time.time + PulseDelay;
                for (int step = 0; step < effects.Count;)
                {
                    var item = effects.Values.ElementAt(step);
                    bool isRecovering;
                    if (item.lastDamageDelay > 0)
                    {
                        item.lastDamageDelay -= PulseDelay;
                        isRecovering = true;
                    }
                    else
                        isRecovering = false;
                    if (item.MainStatusUpdate(resistance, isRecovering))
                    {
                        step++;
                    }
                }
            }
            foreach (var item in effects.Values)
            {
                item.FastStatusUpdate();
            }
        }
    }
}
