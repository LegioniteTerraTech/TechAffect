using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AffectTech
{
    public class SpecialDamageType : MonoBehaviour
    {
        /*
         * Place this module by anything with "m_DamageType" in it 
         *   to change it to a special damage type added by this mod
           "SpecialDamageType": { "OverrideDamageType" : "Cryo", },
         * 
         */

        private static readonly FieldInfo type = typeof(WeaponRound).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo typeB = typeof(BeamWeapon).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo typeE = typeof(Explosion).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo typeF = typeof(ModuleWeaponFlamethrower).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo typeD = typeof(ModuleDrill).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo typeM = typeof(ModuleMeleeWeapon).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);

        public DamageTypesExt OverrideDamageType = DamageTypesExt.Cryo;


        public void Start()
        {
            enabled = true;
        }
        public void OnPool()
        {
            Update();
            DebugAffectTech.Log("Booted SpecialDamageType on " + gameObject.name);
        }
        public void Update()
        {
            if (GetComponent<WeaponRound>())
            {
                type.SetValue(GetComponent<WeaponRound>(), (ManDamage.DamageType)OverrideDamageType);
            }
            if (GetComponent<BeamWeapon>())
            {
                typeB.SetValue(GetComponent<BeamWeapon>(), (ManDamage.DamageType)OverrideDamageType);
            }
            if (GetComponent<ModuleWeaponFlamethrower>())
            {
                typeF.SetValue(GetComponent<ModuleWeaponFlamethrower>(), (ManDamage.DamageType)OverrideDamageType);
            }
            if (GetComponent<ModuleDrill>())
            {
                typeD.SetValue(GetComponent<ModuleDrill>(), (ManDamage.DamageType)OverrideDamageType);
            }
            if (GetComponent<ModuleMeleeWeapon>())
            {
                typeM.SetValue(GetComponent<ModuleMeleeWeapon>(), (ManDamage.DamageType)OverrideDamageType);
            }
            if (GetComponent<ModuleWeaponTeslaCoil>())
            {
                GetComponent<ModuleWeaponTeslaCoil>().m_DamageType = (ManDamage.DamageType)OverrideDamageType;
            }
            enabled = false;
        }
        public void OverrideExplosion()
        {
            if (GetComponent<Explosion>())
            {
                typeE.SetValue(GetComponent<Explosion>(), (ManDamage.DamageType)OverrideDamageType);
            }
            enabled = false;
        }
    }
}
