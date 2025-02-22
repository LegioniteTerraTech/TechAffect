using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;
using static AchievementObject;

public class ModuleRegulator : AffectTech.ModuleRegulator { }

namespace AffectTech
{
    /// <summary>
    /// [WIP] Provides an effective defense against status effects, a minor extension to ModuleShieldGenerator.
    /// </summary>
    [RequireComponent(typeof(ModuleEnergy))]
    public class ModuleRegulator : ExtModule
    {
        private ModuleEnergy Energy;

        public StatusType ApplyerType = StatusType.FilmShield;
        public float ApplyRate = 16;
        public float PulseDelay = 1.25f;
        private float ActualApplyRate = 0;
        public float DrainRate = 0.25f;
        private float pulseTimer = 0;
        public Damageable damageable => block.visible.damageable;
        protected override void Pool()
        {
            Energy = gameObject.GetComponent<ModuleEnergy>();
            ActualApplyRate = ApplyRate * block.GetComponent<Damageable>().MaxHealth;
        }
        public override void OnAttach()
        {
            pulseTimer = PulseDelay;
            Energy.UpdateConsumeEvent.Subscribe(OnDrain);
        }
        public override void OnDetach()
        {
            Energy.UpdateConsumeEvent.Unsubscribe(OnDrain);
        }

        public void OnDrain()
        {
            if (pulseTimer <= 0)
            {
                if (GetCurrentEnergy() > 0)
                {
                    StatusCondition SC = damageable.GetComponent<StatusCondition>();
                    ExtStatusEffect statusEffect;
                    if (SC != null)
                    {
                        if (SC.effects.TryGetValue(ApplyerType, out statusEffect))
                        {
                            var reg = tonk.EnergyRegulator.Energy(TechEnergy.EnergyType.Electric);
                            float delta = DrainRate * PulseDelay;
                            reg.currentAmount = Mathf.Max(reg.currentAmount - delta, 0);
                            reg.spareCapacity = Mathf.Min(reg.spareCapacity - delta, reg.storageTotal);
                            //DebugAffectTech.Log("TechAffect - Maintaining effect " + ApplyerType);
                            statusEffect.AddToVal(ActualApplyRate * PulseDelay);
                            statusEffect.lastDamageDelay = StatusCondition.RecoverDelay;
                        }
                        else
                        {
                            //DebugAffectTech.Log("TechAffect - Adding effect(2) \"" + ApplyerType + "\" to block " + block.name + " for some reason");
                            SC.AddEffect(ActualApplyRate * PulseDelay, ApplyerType, false);
                        }
                    }
                    else
                    {
                        SC = StatusCondition.AddTo(damageable, ApplyerType);
                        if (!SC.effects.ContainsKey(ApplyerType))
                        {
                            //DebugAffectTech.Log("TechAffect - Adding effect \"" + ApplyerType + "\" to block " + block.name + " for some reason");
                            SC.AddEffect(ActualApplyRate * PulseDelay, ApplyerType, false);
                        }
                    }
                }
                pulseTimer = PulseDelay;
            }
            else
                pulseTimer -= Time.deltaTime;
        }

        private Tank tonk => block.tank;
        public float GetCurrentEnergy()
        {
            if (tonk != null)
            {
                var reg = tonk.EnergyRegulator.Energy(TechEnergy.EnergyType.Electric);
                return reg.storageTotal - reg.spareCapacity;
            }
            return 0;
        }
    }
}
