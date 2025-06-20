using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static AchievementObject;

namespace AffectTech.Stats
{
    internal class SEOverclock : StatusEffectSelf
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "OverclockWarning",
            AltUI.HighlightString("Overclock") + " makes weak " + AltUI.ObjectiveString("Blocks") +
            " faster!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;

        public override StatusEffectSelf Instantiate()
        {
            return new SEOverclock();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Beneficial;
        public override StatusTypeDef StatType => StatusTypeDef.Overclock;
        public override bool GradualSpread => true;
        public override float FirstHitPercent => 0;

        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2((32.5f + addVal3) * t2d2H, 0.375f);
        }
        public override float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            if (info.Source is Explosion)
               lastDamageDelay = StatusCondition.RecoverDelay;
            return info.Damage * 4;
        }
        public override void InitPostEvent()
        {
            ManStatusEffectsExt.spreadUpdate.Subscribe(UpdateSpreadPrewarm);
            UpdateSpread();
        }
        public override void DeInit()
        {
            ManStatusEffectsExt.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManStatusEffectsExt.spreadUpdate.Unsubscribe(UpdateSpread);
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (inflicted)
            {
                case StatusTypeDef.Overclock:
                    AddToVal(damage);
                    break;
                case StatusTypeDef.Overheat:
                case StatusTypeDef.EMF:
                    damageMulti *= SubFromVal(damage * 2);
                    return damageMulti == 0;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (!isRecovering)
            {
                if (SC.precisionComponents)
                    impactValue -= StatusCondition.PulseDelay * OverclockDrain * resistance;
                else
                    impactValue -= StatusCondition.PulseDelay * OverclockDrainNonConsumer * resistance;
            }
            float percent = GetEmitVal(impactValue) * 1f;
            SetSpeed(percent + 1);
            SpeedUpdate(percent);
        }

    }
}
