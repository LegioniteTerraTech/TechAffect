using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static StatusEffect;

namespace AffectTech
{
    public abstract class StatusEffectExt
    {
        public const float DiffuseLoss = 15;
        public const float DiffusePercent = 0.9f;

        public const float PryRecoveryPercentSec = 0.5f;
        public const float TempDisspationPercentSec = 0.1f;
        public const float UnJammingRateSec = 1250;
        public const float HackRecoveryPercentSec = 0.25f;
        public const float EMPGroundingRateSec = 250;
        public const float EMPGroundingRatePrecisePercentSec = 0.1f;
        public const float AcidDecayPerSec = 0.3f;
        public const float FilmShieldDrain = 0.025f;
        public const float OverclockDrain = 0.4f;
        public const float OverclockDrainNonConsumer = 0.05f;

        public const int MinimumVolRqdToHack = 7;

    }
    public abstract class StatusEffectExtLegacy : StatusEffectExternal
    {
        public abstract StatusTypeDef StatType { get; }
        public override EffectTypes StatusType => (EffectTypes)(Enum.GetValues(typeof(EffectTypes)).Length + (int)StatType);
        public abstract DamageTypesExt DmgType { get; }


    }
    public abstract class StatusEffectExternal : StatusEffectExt
    {
        public abstract EffectTypes StatusType { get; }
        public abstract StackingTypes StackingType { get; }
        public abstract float TickInterval { get; }
        public abstract TargetTeamTypes TargetTeamFlags { get; }
        public abstract ManDamage.DamageType TriggerDamageType { get; }
        public abstract bool GradualSpread { get; }
        /// <summary>
        /// Does it deal damage to the block it's on?
        /// </summary>
        public abstract bool IsHazardous { get; }
        public virtual bool IsHelpful(DamageTypesExt type) => DamageTypesExt.Beneficial == type;


        private StatusEffectExtInst inst;

        internal StatusEffectExtInst Init()
        {
            if (inst != null)
                return inst;
            inst = ScriptableObject.CreateInstance<StatusEffectExtInst>();
            inst.Init(StatusType, StackingType, TickInterval, TargetTeamFlags, this);
            return inst;
        }
        internal void DeInit()
        {
            if (inst == null)
                return;
            UnityEngine.Object.Destroy(inst);
            inst = null;
        }
        public abstract Vector2 GetColorer(float addVal3, float emitValPercent);

        public virtual State ConfigureNew(Visible visible, Visible sourceVisible)
        {
            return inst.ConfigureNewDefault(visible, sourceVisible);
        }
        public abstract void Configure(State existingEffect);

        public abstract void StackEffect(State existingEffect);

        public virtual bool CanApplyEffectOnVisible(Visible visibleEffectee, Visible visibleEffector)
        {
            return inst.CanApplyEffectOnVisibleDefault(visibleEffectee, visibleEffector);
        }


        protected class StateExt : State
        {
            protected Transform transform => Visible.transform;
            protected TankBlock block => Visible.block;
            protected Damageable dmg => Visible.damageable;
            public StateExt(Visible affecteeVisible, Visible affectorVisible, EffectTypes type) : base(affecteeVisible, affectorVisible, type)
            {

            }
        }

    }
    /// <summary>
    /// For defensive effects
    /// </summary>
    public abstract class StatusEffectSelf : StatusEffectExt
    {
        protected float t2d2H => StatusCondition.t2d2H;

        internal StatusEffect vInst;
        public StatusEffectSelf()
        {
            vInst = ScriptableObject.CreateInstance<StatusEffectExtInst>();
        }


        public virtual DamageTypesExt DmgType => DamageTypesExt.Standard;
        public virtual StatusTypeDef StatType => StatusTypeDef.NULL;
        public virtual bool GradualSpread => false;
        public virtual float FirstHitPercent => 1;
        /// <summary>
        /// Does it deal damage to the block it's on?
        /// </summary>
        public virtual bool IsHazardous => false;
        public virtual bool IsHelpful(DamageTypesExt type) => DamageTypesExt.Beneficial == type;
        protected virtual ExtUsageHint.UsageHint hintStatus => null;


        protected StatusCondition SC;
        internal StatusCondition status => SC;
        protected Transform transform => SC.transform;
        protected TankBlock block => SC.block;
        protected Damageable dmg => SC.damageable;
        internal float lastDamageDelay = 0;


        protected Func<float, float> GetEmitVal => SC.GetEmitVal;
        protected Action<float> MalfunctionUpdate => SC.MalfunctionUpdate;
        protected Action<float> SpeedUpdate => SC.SpeedUpdate;
        protected Action<bool> HaltUtils => SC.HaltUtils;


        protected void SetSpeed(float value) => SC.SetSpeed(value);
        protected void SetAllWorkingComponentsOff(bool turnOff, float impactPercent, bool forceFull = false)
        {
            SC.SetAllWorkingComponentsOff(turnOff, impactPercent, forceFull);
        }

        internal float impactValue = 0;
        internal float impactExcess = 0;
        internal float impactExcessProcessing = 0;
        internal float impactPercent => Mathf.Min(1, impactValue / (dmg.MaxHealth * 2f));

        public abstract StatusEffectSelf Instantiate();
        public virtual bool CanAddNewTo(GameObject GO, out bool computing)
        {
            bool isWeapon = GO.GetComponent<ModuleWeapon>();
            bool isCab = GO.GetComponent<ModuleTechController>();
            bool antigrav = GO.GetComponent<ModuleAntiGravityEngine>();
            bool bubble = GO.GetComponent<ModuleShieldGenerator>();
            bool gyro = GO.GetComponent<ModuleGyro>();
            bool beam = GO.GetComponent<ModuleItemHolderBeam>();
            bool energy = GO.GetComponent<ModuleEnergy>();
            computing = isCab || bubble || antigrav || gyro || beam || (energy && !isWeapon);
            return true; 
        }
        public abstract Vector2 GetColorer(float addVal3, float emitValPercent);


        public void Init(StatusCondition cond, float value, ref float damageMulti)
        {
            SC = cond;
            impactValue = value;
            impactExcess = 0;
            impactExcessProcessing = 0;
            damageMulti = FirstHitPercent;
            hintStatus.Show();
            InitPostEvent();
        }
        public void Init(StatusCondition cond, ManDamage.DamageInfo info, ref float damageMulti)
        {
            Init(cond, InitPreEvent(info, ref damageMulti), ref damageMulti);
        }
        public virtual float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            return info.Damage;
        }
        public virtual void InitPostEvent()
        {
        }
        public virtual void DeInit() { }


        protected bool StatusInflicted_Shield(float damage, StatusTypeDef stat, ref float damageMulti)
        {
            switch (stat)
            {
                case StatusTypeDef.NULL:
                    damageMulti *= SubFromVal(damage);
                    return damageMulti == 0;
                case StatusTypeDef.FilmShield:
                    AddToVal(damage);
                    break;
                case StatusTypeDef.EMF:
                    damageMulti *= SubFromVal(damage * 4);//(StatusType)((int)info.DamageType - 4));
                    if (SC.redDelay <= StatusCondition.RedPulseDelayImpatient)
                        SC.redDelay = StatusCondition.RedPulseDelayShort;
                    return damageMulti == 0;
                case StatusTypeDef.Overheat:
                    damageMulti *= SubFromVal(damage * 0.25f);
                    return damageMulti == 0;
                case StatusTypeDef.Freezing:
                    damageMulti *= SubFromVal(damage);
                    return damageMulti == 0;
                case StatusTypeDef.Jamming:
                    damageMulti *= SubFromVal(damage);
                    return damageMulti == 0;
                case StatusTypeDef.Hacked:
                    damageMulti *= SubFromVal(damage);
                    return damageMulti == 0;
                case StatusTypeDef.Acid:
                    damageMulti *= SubFromVal(damage);
                    return damageMulti == 0;
                default:
                    break;
            }
            return true;
        }

        internal bool StatusInflicted_Internal(float damage, DamageTypesExt type,
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti)
        {
            if (inflicted == StatusTypeDef.FilmShield && 
                !StatusInflicted_Shield(damage, inflicted, ref damageMulti))
                return false;
            return StatusInflicted(damage, type,
            inflicted, sourceTank, ref damageMulti);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="inflicted"></param>
        /// <param name="val"></param>
        /// <returns>True if this effect should be kept intact</returns>
        public abstract bool StatusInflicted(float damage, DamageTypesExt type, 
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti);

        internal bool MainStatusUpdate(float resistance, bool isRecovering)
        {
            UpdateDecay(resistance, isRecovering);
            if (impactValue <= 0)
            {
                impactValue = 0;
                OnRemove();
                return false;
            }
            return true;
        }
        internal void FastStatusUpdate() => 
            FastUpdate();
        protected abstract void UpdateDecay(float resistance, bool isRecovering);
        protected virtual void FastUpdate()
        { 
        }


        // Nearby Spreading
        private static List<StatusCondition> cacheSpread = new List<StatusCondition>(16);
        private void DiffuseNearby(float Diffusive)
        {
            if (block.NumConnectedAPs != 0)
            {
                if (SC == null)
                    throw new NullReferenceException("SC is NULL, tf");
                SC.spreadLastFrame = StatusCondition.spreadLastFrameDelay;
                float outcome = (Diffusive * DiffusePercent / block.NumConnectedAPs) - DiffuseLoss;
                if (outcome > 0)
                {
                    foreach (TankBlock tb in block.ConnectedBlocksByAP)
                    {
                        if (tb.IsNotNull())
                        {
                            if (CanSpreadStatus(tb, out StatusCondition SC2))
                                cacheSpread.Add(SC2);
                        }
                    }
                    int blockTotal = cacheSpread.Count;
                    if (blockTotal == 0)
                        return;
                    cacheSpread.Shuffle();
                    foreach (StatusCondition SC3 in cacheSpread)
                    {
                        SC3.DoSpreadStatus(outcome, StatType);
                    }
                    cacheSpread.Clear();
                }
            }
        }
        public bool CanSpreadStatus(TankBlock TB, out StatusCondition SC2)
        {
            SC2 = TB.GetComponent<StatusCondition>();
            if ((bool)SC2)
            {
                if (SC2.spreadLastFrame == 0)
                {
                    SC2.CancelInvoke("Remove");
                    return true;
                }
                return false;
            }
            SC2 = StatusCondition.AddTo(TB.visible.damageable, StatType);
            return true;
        }
        public void UpdateSpreadPrewarm()
        {
            ManStatusEffectsExt.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManStatusEffectsExt.spreadUpdate.Subscribe(UpdateSpread);
        }
        public void UpdateSpread()
        {
            if (SC.spreadLastFrame > 0)
                SC.spreadLastFrame--;
            else if (impactExcess > 0)
            {
                ManStatusEffectsExt.PrepSpread(this);
                impactExcessProcessing = impactExcess;
                impactExcess = 0;
            }
        }
        public void DoUpdateSpread()
        {
            DiffuseNearby(impactExcessProcessing);
            impactExcessProcessing = 0;
        }


        internal void AddToVal(float addVal)
        {
            float dHealth = dmg.MaxHealth * 2;
            float change = impactValue + Mathf.Abs(addVal);
            if (change < dHealth)
                impactValue = change;
            else
            {
                impactExcess = change - dHealth;
                impactValue = dHealth;
            }
            //DebugAffectTech.Log("AffectTech: StatusCondition - Value " + Status + " is now " + impactValue);
        }
        internal void AddToVal(float addVal, int team)
        {
            float dHealth = dmg.MaxHealth * 2;
            float change = impactValue + Mathf.Abs(addVal);
            if (change < dHealth)
            {
                impactValue = change;
            }
            else
            {
                impactExcess = change - dHealth;
                impactValue = dHealth;
                bool isValid = !ManNetwork.IsNetworked || ManNetwork.IsHost;
                if (block?.tank)
                {
                    if (block.tank.IsPopulation && block.tank.blockman.IterateBlockComponents<ModuleTechController>().Count() == 1)
                    {
                        if (isValid)
                            block.tank.SetTeam(team);
                        DebugAffectTech.Log("AffectTech: StatusCondition - HACKED");
                        OnRemove();
                        return;
                    }
                    else
                    {
                        if (isValid)
                        {
                            ManLooseBlocks.inst.HostDetachBlock(block, false, true);
                            block.visible.SetLockTimout(Visible.LockTimerTypes.Interactible, 1 / HackRecoveryPercentSec);
                        }
                        ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, transform);
                    }
                }
            }
            //DebugAffectTech.Log("AffectTech: StatusCondition - Value " + Status + " is now " + impactValue);
        }
        internal void AddToValDetach(float addVal)
        {
            float dHealth = dmg.MaxHealth * 2;
            float change = impactValue + Mathf.Abs(addVal);
            if (change < dHealth)
            {
                impactValue = change;
            }
            else
            {
                impactExcess = change - dHealth;
                impactValue = dHealth;
                if (block?.tank)
                {
                    if (!ManNetwork.IsNetworked || ManNetwork.IsHost)
                    {
                        ManLooseBlocks.inst.HostDetachBlock(block, false, true);
                        block.visible.SetLockTimout(Visible.LockTimerTypes.Interactible, 1 / PryRecoveryPercentSec);
                    }
                }
            }
            //DebugAffectTech.Log("AffectTech: StatusCondition - Value " + Status + " is now " + impactValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subVal"></param>
        /// <returns>Percent of impact remaining</returns>
        internal float SubFromVal(float subVal)
        {
            impactExcess = 0;
            float change = impactValue - subVal;
            if (change >= 0)
                impactValue = change;
            else
            {
                impactValue = 0;
                return (-change) / subVal;
            }
            return 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subVal"></param>
        /// <returns>Percent of impact remaining</returns>
        internal float SubFromValRemove(float subVal)
        {
            impactExcess = 0;
            float change = impactValue - subVal;
            if (change >= 0)
                impactValue = change;
            else
            {
                impactValue = 0;
                OnRemove();
                return (-change) / subVal;
            }
            return 0;
        }

        protected void OnRemove()
        {
            SC.OnRemove(this);
        }
    }
}
