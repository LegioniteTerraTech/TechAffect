using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace AffectTech.Stats
{
    internal class SEEMP : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "EmpWarning",
            AltUI.HighlightString("EMPs") + " make " + AltUI.EnemyString("Enemies") +
            " flinch, making them stop and malfunction!", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override ExtStatusEffect Instantiate()
        {
            return new SEEMP();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.EMP;
        public override StatusType StatType => StatusType.EMF;
        public override bool CanDefuse => false;
        public override float FirstHitPercent => 0;

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
            if (!isWeapon && !isCab && !antigrav && !bubble && !gyro &&
                !beam && !energy && !GO.GetComponent<ModuleWheels>())
                return false;
            if (!computing && GO.GetComponent<Damageable>().MaxHealth < EMPGroundingRateSec)
                return false;
            return true;
        }
        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            if (0.5f < emitValPercent)
            {
                return new Vector2(21.5f * t2d2H, 0.75f);
            }
            else
            {
                return new Vector2(22.5f * t2d2H, 0.25f);
            }
        }
        public override void InitPostEvent()
        {
            ManExtStatusEffects.spreadUpdate.Subscribe(UpdateSpreadPrewarm);
            lastDamageDelay = StatusCondition.RecoverDelay;
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
            switch (type)
            {
                case DamageTypesExt.EMP:
                    AddToVal(damage * 6);
                    break;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            if (SC.precisionComponents)
                impactValue -= StatusCondition.PulseDelay * EMPGroundingRatePrecisePercentSec * resistance;
            else
                impactValue -= StatusCondition.PulseDelay * EMPGroundingRateSec;
            bool frozen = 0.5f < GetEmitVal(impactValue);
            SetAllWorkingComponentsOff(frozen, impactPercent, true);
            HaltUtils(frozen);
        }
        protected override void FastUpdate()
        {
            if (SC.GetComponent<ModuleTechController>())
            {
                if (SC.warningRepeat >= 3)
                {
                    ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, block.trans);
                    ManSFX.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, block.trans);
                    SC.warningRepeat = 0;
                }
                SC.warningRepeat += StatusCondition.PulseDelay;
            }
        }
    }
}
