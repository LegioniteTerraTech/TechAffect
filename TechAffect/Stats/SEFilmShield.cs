using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEFilmShield : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "ShieldWarn",
            AltUI.HighlightString("Film Shields") + " protect " + AltUI.ObjectiveString("Blocks") +
            " from status effects and attacks.", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override ExtStatusEffect Instantiate()
        {
            return new SEFilmShield();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Beneficial;
        public override StatusType StatType => StatusType.FilmShield;
        public override bool CanDefuse => true;
        public override float FirstHitPercent => 0;
        public override bool IsHelpful(DamageTypesExt type)
        {
            return type == DamageTypesExt.Cryo || base.IsHelpful(type);
        }

        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2((28.5f + addVal3) * t2d2H, emitValPercent);
        }
        public override void InitPostEvent()
        {
            ManExtStatusEffects.spreadUpdate.Subscribe(UpdateSpreadPrewarm);
        }
        public override void DeInit()
        {
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpreadPrewarm);
            ManExtStatusEffects.spreadUpdate.Unsubscribe(UpdateSpread);
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            impactValue -= StatusCondition.PulseDelay * FilmShieldDrain * resistance;
            GetEmitVal(impactValue);
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusType inflicted, Tank sourceTank, ref float damageMulti) =>
            StatusInflicted_Shield(damage, inflicted, ref damageMulti);

    }
}
