using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    /// <summary>
    /// Different from cog jammer - stops weapons from firing instead
    /// </summary>
    internal class SEJamming : StatusEffectSelf
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "JammedWarning",
            AltUI.HighlightString("Weapon Jammers") + " make " + AltUI.EnemyString("Enemies") +
            " weapons stop firing.  Keep them quiet!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override StatusEffectSelf Instantiate()
        {
            return new SEJamming();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Jamming;
        public override StatusTypeDef StatType => StatusTypeDef.Jamming;
        public override bool GradualSpread => false;
        public override float FirstHitPercent => 1;


        public override bool CanAddNewTo(GameObject GO, out bool computing)
        {
            bool isWeapon = GO.GetComponent<ModuleWeapon>();
            bool isCab = GO.GetComponent<ModuleTechController>();
            bool antigrav = GO.GetComponent<ModuleAntiGravityEngine>();
            bool bubble = GO.GetComponent<ModuleShieldGenerator>();
            bool gyro = GO.GetComponent<ModuleGyro>();
            bool beam = GO.GetComponent<ModuleItemHolderBeam>();
            bool energy = GO.GetComponent<ModuleEnergy>();
            computing = isCab || bubble || antigrav || gyro || beam || (energy && !isWeapon);
            if (!isWeapon)
                return false;
            return true;
        }
        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            if (0.5f < emitValPercent)
            {
                return new Vector2(24.5f * t2d2H, 0.75f);
            }
            else
            {
                return new Vector2(23.5f * t2d2H, 0.25f);
            }
        }
        public override float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            lastDamageDelay = StatusCondition.RecoverDelay;
            return info.Damage * 64;
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

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusTypeDef inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (inflicted)
            {
                case StatusTypeDef.Jamming:
                    AddToVal(damage * 8);
                    break;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            impactValue -= StatusCondition.PulseDelay * UnJammingRateSec;
            bool frozen = 0.5f < GetEmitVal(impactValue);
            SC.allowModuleUpdate = !frozen;
            HaltUtils(frozen);
        }
    }
}
