#define DEV_BUILD

using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using static MonsieurMeh.Mods.TLD.BearSpearReloaded.Helpers;


namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    internal class Patches
    {

        [HarmonyPatch(typeof(vp_FPSPlayer), "HandleBearSpearInput", new Type[] { typeof(PlayerManager) })]
        internal class vp_FPSCameraPatches_HandleBearSpearInput
        {
            public static bool Prefix(vp_FPSPlayer __instance, PlayerManager pm)
            {
                if (GameManager.m_PlayerStruggle.m_Active && GameManager.m_PlayerStruggle.m_IsInSpearStruggle)
                {
                    return false;
                }

                //add sequence here for proximity minigame! or maybe require people to use LegendaryWolves and add the logic to wolf ai instead? that makes more sense...

                if (GameManager.m_SuppressWeaponAim)
                {
                    return false;
                }

                BearSpearItem spear = pm.m_ItemInHandsInternal.m_BearSpearItem;

                if (spear == null)
                {
                    return false;
                }

                if (InputManager.GetAltFirePressed(__instance))
                {
                    __instance.m_SpearZoomRequested = true;
                    __instance.m_CancelSpearZoomRequested = false;
                }


                if (InputManager.GetAltFireReleased(__instance))
                {
                    __instance.m_SpearZoomRequested = false;
                    __instance.m_CancelSpearZoomRequested = true;
                }


                if (spear.m_CurrentSpearState == BearSpearItem.SpearState.Raised && GameManager.m_PlayerMovement.m_SprintStamina <= 0.0001f)
                {
                    __instance.m_SpearZoomRequested = false;
                    __instance.m_CancelSpearZoomRequested = true;
                    return false;
                }

                if (GameManager.m_SuppressWeaponAim)
                {
                    return false;
                }


                if (__instance.m_SpearZoomRequested && !__instance.m_InZoom && !__instance.m_InteractReleaseRequiredBeforeSpearZoom && spear.m_CurrentSpearState == BearSpearItem.SpearState.None)
                {
                    //__instance.SetState("Zoom", true);
                    //__instance.FPSCamera.ToggleZoom(true);
                    //__instance.m_InZoom = true;
                    if (spear.m_CurrentSpearState != BearSpearItem.SpearState.Setting)
                    {
                        spear.m_PendingSpearState = BearSpearItem.SpearState.Setting;
                        spear.OnEnter_Setting();
                        if (spear.m_PendingSpearState == BearSpearItem.SpearState.Setting)
                        {
                            spear.m_CurrentSpearState = BearSpearItem.SpearState.Setting;
                        }
                    }
                    __instance.m_SpearZoomRequested = false;
                }

                if (__instance.m_CancelSpearZoomRequested)
                {
                    if (spear.m_CurrentSpearState == BearSpearItem.SpearState.None || spear.m_CurrentSpearState == BearSpearItem.SpearState.Raised)
                    {
                        __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                        __instance.m_CancelSpearZoomRequested = false;
                        //__instance.MaybeCancelZoomInternal();
                    }
                }

                return false;
            }
        }



        // First pass, still allows classic "setting" with left mouse but adds a "shoot spear" trigger when right + left. 
        // Decided I wanted to get rid of classic setting and do an action-button-sequence minigame on proximity while holding
        // and shift existing logic to "aiming" since "raise" still makes a lot of sense in the context of spear throwing, and i dont want to add a second phase to this mess

        /*
        [HarmonyPatch(typeof(vp_FPSPlayer), "HandleBearSpearInput", new Type[] { typeof(PlayerManager) })]
        internal class vp_FPSCameraPatches_HandleBearSpearInput
        {
            public static bool Prefix(vp_FPSPlayer __instance, PlayerManager pm)
            {
                if (GameManager.m_PlayerStruggle.m_Active && GameManager.m_PlayerStruggle.m_IsInSpearStruggle)
                {
                    return false;
                }

                if (GameManager.m_SuppressWeaponAim)
                {
                    return false;
                }

                BearSpearItem spear = pm.m_ItemInHandsInternal.m_BearSpearItem;

                if (spear == null)
                {
                    return false;
                }

                if (spear.m_CurrentSpearState == (BearSpearItem.SpearState)4)
                {
                    if (InputManager.GetFirePressed(__instance))
                    {
                        spear.m_CurrentSpearState = BearSpearItem.SpearState.None;
                        __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                        __instance.m_CancelSpearZoomRequested = false;
                        __instance.MaybeCancelZoomInternal();
                        ShootSpear(spear);
                        return false;
                    }
                    else if (InputManager.GetAltFireReleased(__instance))
                    {
                        spear.m_CurrentSpearState = BearSpearItem.SpearState.None; 
                        __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                        __instance.m_CancelSpearZoomRequested = false;
                        __instance.MaybeCancelZoomInternal();
                        return false;
                    }
                }


                if (spear.m_CurrentSpearState == BearSpearItem.SpearState.None && InputManager.GetAltFirePressed(__instance))
                {
                    spear.m_CurrentSpearState = (BearSpearItem.SpearState)4;
                    __instance.SetState("Zoom", true);
                    __instance.FPSCamera.ToggleZoom(true);
                    __instance.m_InZoom = true;
                    return false;
                }


                if (InputManager.GetFirePressed(__instance))
                {
                    __instance.m_SpearZoomRequested = true;
                    __instance.m_CancelSpearZoomRequested = false;
                }


                if (InputManager.GetFireReleased(__instance))
                {
                    __instance.m_SpearZoomRequested = false;
                    __instance.m_CancelSpearZoomRequested = true;
                }


                if (spear.m_CurrentSpearState == BearSpearItem.SpearState.Raised && GameManager.m_PlayerMovement.m_SprintStamina <= 0.0001f)
                {
                    __instance.m_SpearZoomRequested = false;
                    __instance.m_CancelSpearZoomRequested = true;
                    return false;
                }

                if (GameManager.m_SuppressWeaponAim)
                {
                    return false;
                }


                if (__instance.m_SpearZoomRequested && !__instance.m_InZoom && !__instance.m_InteractReleaseRequiredBeforeSpearZoom && spear.m_CurrentSpearState == BearSpearItem.SpearState.None)
                {
                    __instance.SetState("Zoom", true);
                    __instance.FPSCamera.ToggleZoom(true);
                    __instance.m_InZoom = true;
                    if (spear.m_CurrentSpearState != BearSpearItem.SpearState.Setting)
                    {
                        spear.m_PendingSpearState = BearSpearItem.SpearState.Setting;
                        spear.OnEnter_Setting();
                        if (spear.m_PendingSpearState == BearSpearItem.SpearState.Setting)
                        {
                            spear.m_CurrentSpearState = BearSpearItem.SpearState.Setting;
                        }
                    }
                    __instance.m_SpearZoomRequested = false;
                }

                if (__instance.m_CancelSpearZoomRequested)
                {
                    if (spear.m_CurrentSpearState == BearSpearItem.SpearState.None || spear.m_CurrentSpearState == BearSpearItem.SpearState.Raised)
                    {
                        __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                        __instance.m_CancelSpearZoomRequested = false;
                        __instance.MaybeCancelZoomInternal();
                    }
                }

                return false;
            }


            private static void ShootSpear(BearSpearItem spear)
            {

            }
        }
        */
    }
}
