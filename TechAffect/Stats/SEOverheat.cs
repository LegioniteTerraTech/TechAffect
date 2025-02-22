using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEOverheat : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "FireWarning",
            AltUI.HighlightString("Flamethrowers") + " make " + AltUI.EnemyString("Enemies") +
            " overheat, making them weaker to attacks!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;

        public override ExtStatusEffect Instantiate()
        {
            return new SEOverheat();
        }

        public override DamageTypesExt DmgType => DamageTypesExt.Fire;
        public override StatusType StatType => StatusType.Overheat;
        public override bool CanDefuse => true;
        public override float FirstHitPercent => 1.2f;

        public override bool IsHelpful(DamageTypesExt type)
        {
            return type == DamageTypesExt.Fire || base.IsHelpful(type);
        }

        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2((16.5f - addVal3) * t2d2H, emitValPercent);
        }

        public override float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            if (info.Source is Explosion)
                lastDamageDelay = StatusCondition.RecoverDelay;
            return info.Damage * 2;
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

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (!isRecovering)
                impactValue -= StatusCondition.PulseDelay * TempDisspationPercentSec * resistance;
            SetAllWorkingComponentsOff(false, impactValue);
            SC.allowModuleUpdate = true;
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusType inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (type)
            {
                case DamageTypesExt.Plasma:
                case DamageTypesExt.Energy:
                    AddToVal(damage * 2);
                    break;
                case DamageTypesExt.Fire:
                    AddToVal(damage * 2);
                    if (sourceTank)
                        lastDamageDelay = StatusCondition.RecoverDelay;
                    break;
                case DamageTypesExt.Cryo:
                    damageMulti *= SubFromVal(damage * 2);
                    return damageMulti == 0;
            }
            damageMulti *= 1 + (impactValue / (dmg.MaxHealth * 2));
            return true;
        }

    }
}
