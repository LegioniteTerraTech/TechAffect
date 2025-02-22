using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static AchievementObject;

namespace AffectTech.Stats
{
    internal class SEOverclock : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "OverclockWarning",
            AltUI.HighlightString("Overclock") + " makes weak " + AltUI.ObjectiveString("Blocks") +
            " faster!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;

        public override ExtStatusEffect Instantiate()
        {
            return new SEOverclock();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Beneficial;
        public override StatusType StatType => StatusType.Overclock;
        public override bool CanDefuse => true;
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
            ManExtStatusEffects.spreadUpdate.Subscribe(UpdateSpreadPrewarm);
            UpdateSpread();
        }
        public override void DeInit()
        {
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpread);
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusType inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (inflicted)
            {
                case StatusType.Overclock:
                    AddToVal(damage);
                    break;
                case StatusType.Overheat:
                case StatusType.EMF:
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
