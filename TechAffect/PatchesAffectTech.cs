using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AffectTech
{
    internal class Patches
    {
        // Major Patches
        [HarmonyPatch(typeof(ManSpawn))]
        [HarmonyPatch("OnDLCLoadComplete")]//
        private class AdjustBlocks
        {
            private static void Postfix(ManSpawn __instance)
            {
                try
                {
                    StatusCondition.InitNewStatus();
                    ManExtStatusEffects.FirstDelayedInit();
                }
                catch (Exception e)
                {
                    DebugAffectTech.LogError("AffectTech: Failed to init CRITICAL - " + e);
                }
            }
        }


        [HarmonyPatch(typeof(MaterialSwapper))]
        [HarmonyPatch("StartMaterialPulse")]//
        private class FixStrobeError
        {
            private static bool Prefix(MaterialSwapper __instance)
            {
                var SC = __instance.GetComponent<StatusCondition>();
                if (SC)
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(StringLookup))]
        [HarmonyPatch("GetDamageTypeName")]//
        private class AddDamageTypesText
        {
            private static bool Prefix(ref ManDamage.DamageType damageType, ref string __result)
            {
                switch ((DamageTypesExt)damageType)
                {
                    case DamageTypesExt.Cryo:
                        __result = "Cryogenic";
                        return false;
                    case DamageTypesExt.EMP:
                        __result = "E.M.P.";
                        return false;
                    case DamageTypesExt.Jamming:
                        __result = "Weapon Jam";
                        return false;
                    case DamageTypesExt.Hack:
                        __result = "Hacking";
                        return false;
                    case DamageTypesExt.Acid:
                        __result = "Corrosive";
                        return false;
                    case DamageTypesExt.Rust:
                        __result = "Rust";
                        return false;
                    case DamageTypesExt.Beneficial:
                        __result = "Helper";
                        return false;
                    default:
                        break;
                }
                return true;
            }
        }



        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnSpawn")]//
        private class FixRedError2
        {
            private static void Postfix(TankBlock __instance)
            {
                var MS = __instance.GetComponent<MaterialSwapper>();
                if (MS)
                {
                    MS.SwapMaterialDamage(false);
                    //MS.ResetMaterialToDefault();
                } 
            }
        }

        /*
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnAttach")]//
        private class FixFailInCalls
        {
            private static void Postfix(TankBlock __instance)
            {
                ReAimer.CreateOrUpdateForBlock(__instance);
            }
        }*/

        [HarmonyPatch(typeof(Explosion))]
        [HarmonyPatch("Explode")]//
        private class UpdateExplosionASAP
        {
            private static void Prefix(Explosion __instance)
            {
                var SDT = __instance.GetComponent<SpecialDamageType>();
                if (SDT)
                {
                    SDT.OverrideExplosion();
                }
            }
        }


        [HarmonyPatch(typeof(Damageable))]
        [HarmonyPatch("Damage")]//
        private class AddNewDamageTypesHandling
        {
            private static void Prefix(Damageable __instance, ref ManDamage.DamageInfo info, ref bool actuallyDealDamage)
            {
                if (actuallyDealDamage)
                {
                    info.ApplyDamageMultiplier(StatusCondition.OnDamageStatus(__instance, info));
                }
            }
            /*
            private static void Postfix(Damageable __instance, ref ManDamage.DamageInfo info, ref bool actuallyDealDamage)
            {
                if (actuallyDealDamage)
                {
                    StatusCondition.RunStatusPost(__instance, info);
                }
            }*/
        }

        // ------------------------------------------------------
        //                      Game-Changers
        // ------------------------------------------------------
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("InitNew")]//
        private class ResetBlock
        {
            private static void Prefix(TankBlock __instance)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    Status.RemovePre();
                }
            }
        }
        [HarmonyPatch(typeof(ModuleWeapon))]
        [HarmonyPatch("Process")]//
        private class MakeModuleWeaponAffectable
        {
            private static bool Prefix(ModuleWeapon __instance)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    if (!Status.allowModuleUpdate)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ModuleWheels))]
        [HarmonyPatch("ControlInput")]//
        private class MakeModuleWheelsAffectable
        {
            private static bool Prefix(ModuleWheels __instance, ref TankControl.ControlState drive)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    TankControl.ControlState driveR;
                    TankControl.State state;
                    float drivePower;
                    if (Status.MachineInterference())
                    {
                        float MaxSped = __instance.m_WheelParams.radius * __instance.m_TorqueParams.torqueCurveMaxRpm * Mathf.PI;
                        float currentSpeed;
                        if (__instance.block.tank?.rbody)
                            currentSpeed = __instance.block.tank.rbody.velocity.magnitude;
                        else
                            currentSpeed = 0;
                        if (MaxSped < 1)
                            MaxSped = 4;
                        float maximalOut = Mathf.Clamp((MaxSped * (Status.GetOpPercent() + 0.2f) - currentSpeed), -1, 1) / MaxSped;

                        if (currentSpeed > 4)
                            drivePower = Mathf.Min(1, maximalOut);
                        else
                            drivePower = 1;
                        state = new TankControl.State();
                        state.m_InputMovement = drive.InputMovement * drivePower;
                        state.m_InputRotation = drive.InputRotation * drivePower;
                        driveR = new TankControl.ControlState();
                        driveR.m_State = state;
                        drive = driveR;
                    }
                }
                return true;
            }

        }

        [HarmonyPatch(typeof(ModuleBooster))]
        [HarmonyPatch("DriveControlInput")]//
        private class MakeBoostersAffectable
        {
            private static bool Prefix(ModuleBooster __instance, ref TankControl.ControlState driveData)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    TankControl.ControlState driveR;
                    TankControl.State state;
                    float drivePower;
                    if (Status.MachineInterference())
                    {
                        drivePower = Status.GetOpPercent();
                        state = new TankControl.State();
                        state.m_BoostJets = driveData.BoostJets ? Status.allowModuleUpdate : false;
                        state.m_BoostProps = driveData.BoostProps ? Status.allowModuleUpdate : false;
                        state.m_InputMovement = driveData.InputMovement * drivePower;
                        state.m_InputRotation = driveData.InputRotation * drivePower;
                        driveR = new TankControl.ControlState();
                        driveR.m_State = state;
                        driveData = driveR;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ModuleLinearMotionEngine))]
        [HarmonyPatch("OnDriveControl")]//
        private class MakeLMEAffectable
        {
            private static bool Prefix(ModuleLinearMotionEngine __instance, ref TankControl.ControlState driveData)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    TankControl.ControlState driveR;
                    TankControl.State state;
                    float drivePower;
                    if (Status.MachineInterference())
                    {
                        drivePower = Status.GetOpPercent();
                        state = new TankControl.State();
                        state.m_BoostJets = driveData.BoostJets ? Status.allowModuleUpdate : false;
                        state.m_BoostProps = driveData.BoostProps ? Status.allowModuleUpdate : false;
                        state.m_InputMovement = driveData.InputMovement * drivePower;
                        state.m_InputRotation = driveData.InputRotation * drivePower;
                        driveR = new TankControl.ControlState();
                        driveR.m_State = state;
                        driveData = driveR;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ModuleHover))]
        [HarmonyPatch("DriveControlInput")]//
        private class MakeHoversAffectable
        {
            private static bool Prefix(ModuleHover __instance, ref TankControl.ControlState controlState)
            {
                var Status = __instance.GetComponent<StatusCondition>();
                if (Status)
                {
                    TankControl.ControlState driveR;
                    TankControl.State state;
                    float drivePower;
                    if (Status.MachineInterference())
                    {
                        drivePower = Status.GetOpPercent();
                        state = new TankControl.State();
                        state.m_InputMovement = controlState.InputMovement * drivePower;
                        state.m_InputRotation = controlState.InputRotation * drivePower;
                        driveR = new TankControl.ControlState();
                        driveR.m_State = state;
                        controlState = driveR;
                    }
                }
                return true;
            }
        }
    }
}
