using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEFreezing : StatusEffectSelf
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "IceWarning",
            AltUI.HighlightString("Cryogenics") + " make " + AltUI.EnemyString("Enemies") +
            " freeze over, making them slower to react!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override StatusEffectSelf Instantiate()
        {
            return new SEFreezing();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Cryo;
        public override StatusTypeDef StatType => StatusTypeDef.Freezing;
        public override bool GradualSpread => true;
        public override float FirstHitPercent => 0.1f;

        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2((17.5f + addVal3) * t2d2H, 0.175f);
        }
        public override bool IsHelpful(DamageTypesExt type)
        {
            return type == DamageTypesExt.Fire || base.IsHelpful(type);
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
                case StatusTypeDef.Overheat:
                    damageMulti *= SubFromVal(damage * 2);
                    return damageMulti == 0;
                case StatusTypeDef.Freezing:
                    AddToVal(damage * 2);
                    if (sourceTank)
                        lastDamageDelay = StatusCondition.RecoverDelay;
                    break;
                default:
                    break;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (!isRecovering)
                impactValue -= StatusCondition.PulseDelay * TempDisspationPercentSec * resistance;
            float percent = GetEmitVal(impactValue);
            SetAllWorkingComponentsOff(true, percent);
            MalfunctionUpdate(percent);
        }
    }
}
