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

                BearSpearItem spear = pm.m_ItemInHandsInternal.m_BearSpearItem;

                if (spear == null)
                {
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
        }
    }
}
