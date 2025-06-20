using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static StatusEffect;

namespace AffectTech.Stats
{
    /// <summary>
    ///  Process of converting SEAcid to vanilla code-reliant
    /// </summary>
    internal class SEAcid2 : StatusEffectExtLegacy
    {
        public override StatusTypeDef StatType => StatusTypeDef.Acid;
        public override StackingTypes StackingType => StackingTypes.StackEffect;
        public override float TickInterval => 1.0f;
        public override TargetTeamTypes TargetTeamFlags => TargetTeamTypes.Enemies;
        public override ManDamage.DamageType TriggerDamageType => (ManDamage.DamageType)DamageTypesExt.Acid;
        public override State ConfigureNew(Visible visible, Visible sourceVisible)
        {
            AcidState AS = new AcidState(visible, sourceVisible, StatusType);
            Configure(AS);
            return AS;
        }
        public override void Configure(State existingEffect)
        {
        }

        public override void StackEffect(State existingEffect)
        {
            AcidState AS = existingEffect as AcidState;
            AS.impactValue += 
        }

        public override bool CanApplyEffectOnVisible(Visible visibleEffectee, Visible visibleEffector)
        {
            return base.CanApplyEffectOnVisible(visibleEffectee, visibleEffector);
        }
        protected class AcidState : StateExt
        {
            internal float impactValue = 0;
            public AcidState(Visible affecteeVisible, Visible affectorVisible, EffectTypes type) : base(affecteeVisible, affectorVisible, type)
            {

            }

            public override void OnTick()
            {
                StatusCondition.ignoreApplyEffect = true;
                //SC.redDelay = StatusCondition.RedPulseDelay;
                if (ManNetwork.IsHost)
                {
                    ManDamage.DamageInfo newDMG = StatusCondition.MakeAcid(null, null);
                    newDMG.ApplyDamageMultiplier(block.filledCells.Length * impactValue * 0.25f);
                    ManDamage.inst.DealDamage(newDMG, dmg);
                }
                else
                {
                    block.damage.MultiplayerFakeDamagePulse();
                }
                StatusCondition.ignoreApplyEffect = false;
            }
        }
    }
    internal class SEAcid : StatusEffectSelf
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "AcidWarning",
            AltUI.HighlightString("Acid") + " deals damage to " + AltUI.ObjectiveString("blocks") +
            " over time!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override StatusEffectSelf Instantiate()
        {
            return new SEAcid();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Acid;
        public override StatusTypeDef StatType => StatusTypeDef.Acid;
        public override bool GradualSpread => false;
        public override float FirstHitPercent => 1f;

        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            if (0.5f < emitValPercent)
            {
                return new Vector2(26.5f * t2d2H, 0.75f);
            }
            else
            {
                return new Vector2(27.5f * t2d2H, 0.25f);
            }
        }
        public override float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            if (info.Source is Explosion)
                lastDamageDelay = StatusCondition.RecoverDelay;
            return info.Damage * 0.5f;
        }
        public override void InitPostEvent()
        {
            ManStatusEffectsExt.acidUpdate.Subscribe(UpdateAcid);
            UpdateSpread();
        }
        public override void DeInit()
        {
            ManStatusEffectsExt.acidUpdate.Unsubscribe(UpdateAcid);
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type, 
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (type)
            {
                case DamageTypesExt.Acid:
                    if (sourceTank)
                    {
                        lastDamageDelay = StatusCondition.RecoverDelay;
                        AddToVal(damage);
                    }
                    break;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (!isRecovering)
                impactValue -= Time.deltaTime * AcidDecayPerSec * resistance;
            GetEmitVal(impactValue);
        }

        internal void UpdateAcid()
        {
            StatusCondition.ignoreApplyEffect = true;
            SC.redDelay = StatusCondition.RedPulseDelay;
            if (ManNetwork.IsHost)
            {
                ManDamage.DamageInfo newDMG = StatusCondition.MakeAcid(null, null);
                newDMG.ApplyDamageMultiplier(block.filledCells.Length * impactValue * 0.25f);
                ManDamage.inst.DealDamage(newDMG, dmg);
            }
            else
            {
                block.damage.MultiplayerFakeDamagePulse();
            }
            StatusCondition.ignoreApplyEffect = false;
        }
    }
}
