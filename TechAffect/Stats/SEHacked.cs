using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static HarmonyLib.Code;

namespace AffectTech.Stats
{
    internal class SEHacked : ExtStatusEffect
    {
        internal static ExtUsageHint.UsageHint hint = new ExtUsageHint.UsageHint(KickStart.name, "HACKWarning",
            AltUI.HighlightString("Hacking beams ") + " make " + AltUI.EnemyString("Enemy") +
            " Techs be transferred to your control!  Doesn't work with players, hmmm...", 3.5f, true);
        protected override ExtUsageHint.UsageHint hintStatus => hint;
        public override ExtStatusEffect Instantiate()
        {
            return new SEHacked();
        }
        public override DamageTypesExt DmgType => DamageTypesExt.Hack;
        public override StatusType StatType => StatusType.Hacked;
        public override bool CanDefuse => false;


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
            if (!isWeapon && !isCab && !bubble && !gyro &&
                !beam && !GO.GetComponent<ModuleRadar>())
                return false;
            if (isWeapon && !isCab && GO.GetComponent<TankBlock>().filledCells.Length <= MinimumVolRqdToHack)
                return false;
            return true;
        }
        public override Vector2 GetColorer(float addVal3, float emitValPercent)
        {
            return new Vector2(25.5f * t2d2H, 0.5f);
        }
        public override float InitPreEvent(ManDamage.DamageInfo info, ref float damageMulti)
        {
            StatusInflicted(info.Damage, (DamageTypesExt)info.DamageType, StatType, 
                info.SourceTank, ref damageMulti);
            return damageMulti;
        }
        public override void InitPostEvent()
        {
        }
        public override void DeInit()
        {
        }

        public override bool StatusInflicted(float damage, DamageTypesExt type,
            StatusType inflicted, Tank sourceTank, ref float damageMulti)
        {
            switch (type)
            {
                case DamageTypesExt.Hack:
                    if (sourceTank)
                    {
                        if (sourceTank.Team == block.LastTechTeam)
                        {
                            damageMulti *= SubFromVal(damage);
                            return damageMulti == 0;
                        }
                        else
                        {
                            BleepCommand();
                            AddToVal(damage * 0.5f, sourceTank.Team);
                            lastDamageDelay = StatusCondition.RecoverDelay;
                        }
                    }
                    else
                        AddToVal(damage * 0.5f);
                    break;
            }
            return true;
        }

        protected override void UpdateDecay(float resistance, bool isRecovering)
        {
            SetAllWorkingComponentsOff(false, impactValue);
            SC.allowModuleUpdate = true;
            if (0.95f < GetEmitVal(impactValue))
                impactValue -= StatusCondition.PulseDelay * HackRecoveryPercentSec * resistance;
            else
            {
                if (!isRecovering)
                    impactValue -= StatusCondition.PulseDelay * HackRecoveryPercentSec * resistance;
            }
        }
        protected override void FastUpdate()
        {
            bool Cracked = 0.95f < GetEmitVal(impactValue);
            if (0.95f >= GetEmitVal(impactValue))
            {
                if (SC.GetComponent<ModuleTechController>())
                {
                    if (SC.warningRepeat >= 3)
                    {
                        ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, block.trans);
                        ManSFX.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming, block.trans);
                        SC.warningRepeat = 0;
                    }
                    SC.warningRepeat += Time.deltaTime;
                }
            }
        }


        private static FieldInfo clank = typeof(ManOnScreenMessages).GetField("m_NewLineSfxEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FMODEvent soundSteal2 = (FMODEvent)clank.GetValue(Singleton.Manager<ManOnScreenMessages>.inst);
        private static FMODEventInstance fInst;
        private static float bleeperTimer = 0;
        private const float bleepTime = 0.28f;
        public void BleepCommand()
        {
            if (Singleton.Manager<ManPauseGame>.inst.IsPaused)
            {   // Pause the text noise
                try
                {
                    if (fInst.IsInited)
                        fInst.StopAndRelease();
                }
                catch { }
                return;
            }
            if (Time.time > bleeperTimer)
            {
                soundSteal2.PlayOneShot();
                bleeperTimer = Time.time + bleepTime;
            }
            //Debug_SMissions.Log("Is playing noise " + fInst.CheckPlaybackState(FMOD.Studio.PLAYBACK_STATE.PLAYING));
        }

    }
}
