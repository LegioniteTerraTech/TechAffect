using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEAcid : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "AcidWarning",
            AltUI.HighlightString("Acid") + " deals damage to " + AltUI.ObjectiveString("blocks") +
            " over time!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override ExtStatusEffect Instantiate()
        {
            return new SEAcid();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Acid;
        public override StatusType StatType => StatusType.Acid;
        public override bool CanDefuse => false;
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
            ManExtStatusEffects.acidUpdate.Subscribe(UpdateAcid);
            UpdateSpread();
        }
        public override void DeInit()
        {
            ManExtStatusEffects.acidUpdate.Unsubscribe(UpdateAcid);
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type, 
            StatusType inflicted, Tank sourceTank, ref float damageMulti)
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
