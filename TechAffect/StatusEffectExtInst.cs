using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffectTech
{
    public class StatusEffectExtInst : StatusEffect
    {
        private StatusEffectExternal effect;
        internal void Init(EffectTypes type, StackingTypes stackingType, float tickInterval, 
            TargetTeamTypes targetTeamFlags, StatusEffectExternal inst)
        {
            m_EffectType = type;
            m_StackingType = stackingType;
            m_TickInterval = tickInterval;
            m_TargetTeamFlags = targetTeamFlags;
            this.effect = inst;
        }
        protected override State ConfigureNew(Visible visible, Visible sourceVisible)
        {
            return effect.ConfigureNew(visible, sourceVisible);
            //return base.ConfigureNew(visible, sourceVisible);
        }
        internal State ConfigureNewDefault(Visible visible, Visible sourceVisible)
        {
            return base.ConfigureNew(visible, sourceVisible);
        }
        protected override void Configure(State existingEffect)
        {
            effect.Configure(existingEffect);
            base.Configure(existingEffect);
        }

        protected override void StackEffect(State existingEffect)
        {
            effect.StackEffect(existingEffect);
        }

        public override bool CanApplyEffectOnVisible(Visible visibleEffectee, Visible visibleEffector)
        {
            return effect.CanApplyEffectOnVisible(visibleEffectee, visibleEffector);
        }
        internal bool CanApplyEffectOnVisibleDefault(Visible visibleEffectee, Visible visibleEffector)
        {
            return base.CanApplyEffectOnVisible(visibleEffectee, visibleEffector);
        }
    }
}
