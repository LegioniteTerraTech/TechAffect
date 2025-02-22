using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEPry : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "PryWarning",
            AltUI.HighlightString("Impacts") + " make " + AltUI.ObjectiveString("Blocks") +
            " weaker, making them easier to detach!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override ExtStatusEffect Instantiate()
        {
            return new SEPry();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Impact;
        public override StatusType StatType => StatusType.Pry;
        public override bool CanDefuse => false;


        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2(4.5f * t2d2H, emitValPercent);
        }
        public override void InitPostEvent()
        {
            ManExtStatusEffects.spreadUpdate.Subscribe(UpdateSpread);
        }
        public override void DeInit()
        {
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpread);
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
            StatusType inflicted, Tank sourceTank, ref float damageMulti)
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
