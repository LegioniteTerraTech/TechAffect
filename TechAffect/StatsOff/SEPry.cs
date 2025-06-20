using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEPry : StatusEffectSelf
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "PryWarning",
            AltUI.HighlightString("Impacts") + " make " + AltUI.ObjectiveString("Blocks") +
            " weaker, making them easier to detach!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override StatusEffectSelf Instantiate()
        {
            return new SEPry();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Impact;
        public override StatusTypeDef StatType => StatusTypeDef.Pry;
        public override bool GradualSpread => false;


        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2(4.5f * t2d2H, emitValPercent);
        }
        public override void InitPostEvent()
        {
            ManStatusEffectsExt.spreadUpdate.Subscribe(UpdateSpread);
        }
        public override void DeInit()
        {
            ManStatusEffectsExt.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManStatusEffectsExt.spreadUpdate.Unsubscribe(UpdateSpread);
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (!isRecovering)
                impactValue -= Time.deltaTime * PryRecoveryPercentSec * resistance;
            GetEmitVal(impactValue);
            SetAllWorkingComponentsOff(false, impactValue);
            SC.allowModuleUpdate = true;
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti)
        {
            if (type == DamageTypesExt.Cutting)
            {
                if (sourceTank)
                    AddToValDetach(damage);
            }
            return true;
        }
    }
}
