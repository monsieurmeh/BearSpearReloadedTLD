#define DEV_BUILD

using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using static Il2Cpp.BearSpearItem;
using static MonsieurMeh.Mods.TLD.BearSpearReloaded.Helpers;


namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    internal class Patches
    {
        [HarmonyPatch(typeof(BaseAi), "OnSpearHit", new Type[] { typeof(bool), typeof(Il2CppSystem.Action)})]
        internal class BaseAiPatches_OnSpearHit
        {
            public static bool Prefix(bool isFatal, Il2CppSystem.Action onStruggleEnd, BaseAi __instance)
            {
                __instance.m_SpearStruggleEndAction = onStruggleEnd;
                __instance.m_WasHitBySpear = true;
                __instance.m_WasHitBySpearFatal = isFatal;
                PlayerAnimation newPlayerAnimation = GameManager.m_NewPlayerAnimation;
                if (newPlayerAnimation == null)
                {
                    LogError("GameManager returned null new PlayerAnimation during BaseAi.OnSpearHit!");
                    return false;
                }
                newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount++;
                newPlayerAnimation.UpdateHipAndShoulderOffsetLayerWeigth(0.0f);
                __instance.SetAiMode((AiMode)0xe);
                GameManager.m_PlayerStruggle?.BeginSpearStruggle(__instance.gameObject, isFatal);
                __instance.m_PlayedAttackStartAnimation = false;
                return false;
            }
        }
        

        [HarmonyPatch(typeof(BearSpearItem), "GetBleedOutMinutes", new Type[] { typeof(BaseAi) })]
        internal class BearSpearItemPatches_GetBleedOutMinutes
        {
            public static bool Prefix(BaseAi bai, BearSpearItem __instance, ref float __result)
            {
                __result = __instance.m_BleedOutMinutes;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "IsDamageFatal", new Type[] { typeof(BaseAi), typeof(float) })]
        internal class BearSpearItemPatches_IsDamageFatal
        {
            public static bool Prefix(BaseAi bai, float damageDealt, BearSpearItem __instance, ref bool __result)
            {
                __result = false;
                if (bai == null)
                {
                    LogError($"Null ai on BearSpearItem.IsDamageFatal!");
                    return false;
                }
                if (bai.m_CurrentMode == AiMode.Dead)
                {
                    __result = true;
                    return false;
                }
                if (bai.m_AiType == AiType.Predator)
                {
                    AuroraManager auroraManager = GameManager.m_AuroraManager;
                    if (auroraManager == null)
                    {
                        LogError("Null AuroraManager in BearSpearItem.IsDamageFatal!");
                        __result = false;
                        return false;
                    }
                    damageDealt *= auroraManager.AuroraIsActive() ? auroraManager.m_DamageToPredatorsScale : 1.0f;
                }
                __result = bai.m_CurrentHP - damageDealt <= 0.0001f;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnAiCollidedWithSpear", new Type[] { typeof(BaseAi), typeof(Vector3), typeof(LocalizedDamage) })]
        internal class BearSpearItemPatches_OnAiCollidedWithSpear
        {
            public static bool Prefix(BaseAi bai, Vector3 spearForward, LocalizedDamage localizedDamage, BearSpearItem __instance)
            {
                if (bai == null)
                {
                    LogError("Null ai on BearSpearItem.OnAiCollidedWithSpear!");
                    return false;
                }
                if (localizedDamage == null)
                {
                    LogError("Null LocalizedDamage on BearSpearItem.OnAiCollidedWithSpear!");
                    return false;
                }
                Transform playerTransform = GameManager.GetPlayerTransform();
                if (playerTransform == null)
                {
                    LogError("Null player transform in BearSpearItem.OnAiCollidedWithSpear!");
                    return false;
                }
                Transform hitTransform = localizedDamage?.transform;
                if (hitTransform == null)
                {
                    LogError("Null transform on localized damage in BearSpearItem.OnAiCollidedWithSpear!");
                    return false;
                }
                if (__instance.GetAngleBetweenSpearAndBearHeadings(bai, spearForward) > __instance.m_AngleForDamageDegrees)
                {
                    return false;
                }
                if (__instance.m_CurrentSpearState != SpearState.None)
                {
                    __instance.m_PendingSpearState = SpearState.None;
                    __instance.OnEnter_None();
                    if (__instance.m_PendingSpearState == SpearState.None)
                    {
                        __instance.m_CurrentSpearState = SpearState.None;
                    }
                }
                __instance.m_HitAi = bai;
                __instance.m_LocalizedDamage = localizedDamage;
                __instance.m_HitPosition = hitTransform.position;
                __instance.m_HitSourcePosition = playerTransform.position;
                __instance.m_HitDamage = __instance.m_DamageDealt;
                __instance.m_HitBleedOutMinutes = __instance.m_BleedOutMinutes;
                bai.OnSpearHit(__instance.IsDamageFatal(bai, __instance.m_DamageDealt), default);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "UpdateCollision", new Type[] { typeof(BaseAi) })]
        internal class BearSpearItemPatches_UpdateCollision
        {
            public static bool Prefix(BaseAi bai, BearSpearItem __instance)
            {
                if (__instance.m_CurrentSpearState != SpearState.Raised)
                {
                    return false;
                }
                if (bai== null)
                {
                    LogError("Null BaseAi on BearSpearItem.UpdateCollision!");
                    return false;
                }
                if (!bai.CanBeHitBySpear())
                {
                    return false;
                }

                Transform playerTransform = GameManager.GetPlayerTransform();
                if (playerTransform == null)
                {
                    LogError("Null player transform on BearSpearItem.UpdateCollision!!");
                    return false;
                }

                Vector3 playerPos = playerTransform.position;
                Vector3 playerForward = playerTransform.forward;
                Transform aiTransform = bai?.m_CachedTransform;
                if (aiTransform == null)

                {
                    LogError("Null ai transform on BearSpearItem.UpdateCollision!");
                    return false;
                }

                Vector3 aiPos = aiTransform.position;
                float maxDistance = __instance.m_DistanceForDamageMeters;
                if ((playerPos - aiPos).sqrMagnitude > maxDistance * maxDistance)
                {
                    return false;
                }

                BearSpearHead spearHeadNode = __instance.m_BearSpearHead;
                if (spearHeadNode == null)
                {
                    LogError("Null BearSpearHead on BearSpearItem.UpdateCollision!");
                    return false;
                }

                Transform spearHeadTransform = spearHeadNode.transform;
                if (spearHeadTransform == null)
                {
                    LogError("Null BearSpearHead transform on BearSpearItem.UpdateCollision!");
                    return false;
                }

                Vector3 spearLocalToPlayer = playerTransform.TransformPoint(spearHeadTransform.position);
                LocalizedDamage closest = null; 
                float closestDistSq = float.PositiveInfinity;
                foreach (LocalizedDamage damagePoint in bai.GetComponentsInChildren<LocalizedDamage>())
                {
                    if (damagePoint == null)
                    {
                        continue;
                    }

                    Transform damageTransform = damagePoint.transform;
                    if (damageTransform == null)
                    {
                        continue;
                    }

                    Vector3 damagePos = damageTransform.position;

                    float distSq = (spearLocalToPlayer - damagePos).sqrMagnitude;
                    if (distSq < closestDistSq)
                    {
                        closest = damagePoint;
                        closestDistSq = distSq;
                    }
                }
                if (closest != null)
                {
                    Vector3 impactDirection = playerForward;
                    __instance.OnAiCollidedWithSpear(bai, impactDirection, closest);
                }
                return false;
            }

        }


        [HarmonyPatch(typeof(BearSpearItem), "SetCurrentState", new Type[] { typeof(SpearState) })]
        internal class BearSpearItemPatches_SetCurrentState
        {
            public static bool Prefix(SpearState state)
            {
                Log($"begin void BearSpearItem.SetCurrentState({state})");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.SetCurrentState()");
            }
        }



        [HarmonyPatch(typeof(BearSpearItem), "OnStruggleHitEnd")]
        internal class BearSpearItemPatches_OnStruggleHitEnd
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnStruggleHitEnd()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnStruggleHitEnd()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "Break")]
        internal class BearSpearItemPatches_Break
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.Break()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.Break()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "DistanceToNearestSpearable")]
        internal class BearSpearItemPatches_DistanceToNearestSpearable
        {
            public static bool Prefix()
            {
                Log($"begin float BearSpearItem.DistanceToNearestSpearable()");
                return true;
            }

            public static void Postfix(float __result)
            {
                Log($"end float BearSpearItem.DistanceToNearestSpearable() => {__result}");
            }
        }



        [HarmonyPatch(typeof(BearSpearItem), "Update_SpearRaised")]
        internal class BearSpearItemPatches_Update_SpearRaised
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (__instance.m_GearItem == null)
                {
                    LogError("Null gear item on bear spear!");
                    return false;
                }
                if (__instance.m_GearItem.m_WornOut || !__instance.IsEquipped())
                {
                    __instance.CancelAction();
                }
                GameManager.m_PlayerMovement?.AddSprintStamina(-__instance.m_StaminaCost);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "Update")]
        internal class BearSpearItemPatches_Update
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (!GameManager.m_IsPaused)
                {
                    if (GameManager.m_PlayerStruggle.InStruggle() && __instance.m_CurrentSpearState != SpearState.None)
                    {
                        __instance.CancelAction();
                    }
                    __instance.m_ActionElapsedSeconds += Time.deltaTime;

                    switch (__instance.m_CurrentSpearState)
                    {
                        case SpearState.Setting:
                            if (__instance.m_ActionElapsedSeconds > __instance.m_SetSpearDurationSeconds)
                            {
                                __instance.m_PendingSpearState = SpearState.Raised;
                                __instance.OnEnter_Raised();
                                if (__instance.m_PendingSpearState == SpearState.Raised)
                                {
                                    __instance.m_CurrentSpearState = SpearState.Raised;
                                }
                            }
                            break;
                        case SpearState.Raised:
                            __instance.Update_SpearRaised();
                            break;
                        case SpearState.Recovering:
                            if (__instance.m_ActionElapsedSeconds > __instance.m_RecoverSpearDurationSeconds)
                            {
                                __instance.m_PendingSpearState = SpearState.None;
                                __instance.OnEnter_None();
                                if (__instance.m_PendingSpearState == SpearState.None)
                                {
                                    __instance.m_CurrentSpearState = SpearState.None;
                                }
                            }
                            break;
                    }

                }
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "LateUpdate")]
        internal class BearSpearItemPatches_LateUpdate
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.LateUpdate()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.LateUpdate()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "IsRaised")]
        internal class BearSpearItemPatches_IsRaised
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.IsRaised()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.IsRaised() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "IsRaisedOrRaisingOrLowering")]
        internal class BearSpearItemPatches_IsRaisedOrRaisingOrLowering
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.IsRaisedOrRaisingOrLowering()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.IsRaisedOrRaisingOrLowering() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "GetActionElapsedInSeconds")]
        internal class BearSpearItemPatches_GetActionElapsedInSeconds
        {
            public static bool Prefix()
            {
                Log($"begin float BearSpearItem.GetActionElapsedInSeconds()");
                return true;
            }

            public static void Postfix(float __result)
            {
                Log($"end float BearSpearItem.GetActionElapsedInSeconds() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CanStartZoom")]
        internal class BearSpearItemPatches_CanStartZoom
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.CanStartZoom()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.CanStartZoom() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CheckStaminaForRaising")]
        internal class BearSpearItemPatches_CheckStaminaForRaising
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.CheckStaminaForRaising()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.CheckStaminaForRaising() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CanEndZoom")]
        internal class BearSpearItemPatches_CanEndZoom
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.CanEndZoom()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.CanEndZoom() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "ZoomStart")]
        internal class BearSpearItemPatches_ZoomStart
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.ZoomStart()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.ZoomStart()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "GetCurrentSpearState")]
        internal class BearSpearItemPatches_GetCurrentSpearState
        {
            public static bool Prefix()
            {
                Log($"begin SpearState BearSpearItem.GetCurrentSpearState()");
                return true;
            }

            public static void Postfix(SpearState __result)
            {
                Log($"end SpearState BearSpearItem.GetCurrentSpearState() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "RestoreYawPitchLimits")]
        internal class BearSpearItemPatches_RestoreYawPitchLimits
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.RestoreYawPitchLimits()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.RestoreYawPitchLimits()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "ZoomEnd")]
        internal class BearSpearItemPatches_ZoomEnd
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.ZoomEnd()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.ZoomEnd()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CancelAction")]
        internal class BearSpearItemPatches_CancelAction
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.CancelAction()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.CancelAction()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "HitAction")]
        internal class BearSpearItemPatches_HitAction
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.HitAction()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.HitAction()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "GetStateDebugString")]
        internal class BearSpearItemPatches_GetStateDebugString
        {
            public static bool Prefix()
            {
                Log($"begin string BearSpearItem.GetStateDebugString()");
                return true;
            }

            public static void Postfix(string __result)
            {
                Log($"end string BearSpearItem.GetStateDebugString() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnDestroy")]
        internal class BearSpearItemPatches_OnDestroy
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnDestroy()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnDestroy()");
            }
        }

        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_None")]
        internal class BearSpearItemPatches_OnEnter_None
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnEnter_None()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnEnter_None()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Setting")]
        internal class BearSpearItemPatches_OnEnter_Setting
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnEnter_Setting()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnEnter_Setting()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Raised")]
        internal class BearSpearItemPatches_OnEnter_Raised
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnEnter_Raised()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnEnter_Raised()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Recovering")]
        internal class BearSpearItemPatches_OnEnter_Recovering
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnEnter_Recovering()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnEnter_Recovering()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "MaybeDisableStaminaRecharge")]
        internal class BearSpearItemPatches_MaybeDisableStaminaRecharge
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.MaybeDisableStaminaRecharge()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.MaybeDisableStaminaRecharge()");
            }
        }





        [HarmonyPatch(typeof(BearSpearItem), "MaybeEnableStaminaRecharge")]
        internal class BearSpearItemPatches_MaybeEnableStaminaRecharge
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.MaybeEnableStaminaRecharge()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.MaybeEnableStaminaRecharge()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "IsEquipped")]
        internal class BearSpearItemPatches_IsEquipped
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.IsEquipped()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.IsEquipped() => {__result}");
            }
        }


        /*
         * console really doesnt like these ones...


        [HarmonyPatch(typeof(BearSpearItem), "OnExit_None")]
        internal class BearSpearItemPatches_OnExit_None
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnExit_None()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnExit_None()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnExit_Setting")]
        internal class BearSpearItemPatches_OnExit_Setting
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnExit_Setting()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnExit_Setting()");
            }
        }

        [HarmonyPatch(typeof(BearSpearItem), "OnExit_Raised")]
        internal class BearSpearItemPatches_OnExit_Raised
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnExit_Raised()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnExit_Raised()");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnExit_Recovering")]
        internal class BearSpearItemPatches_OnExit_Recovering
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnExit_Recovering()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnExit_Recovering()");
            }
        }
        */


        /*
        // super loud!
        [HarmonyPatch(typeof(BearSpearItem), "IsSpearHitLethal")]
        internal class BearSpearItemPatches_IsSpearHitLethal
        {
            public static bool Prefix()
            {
                Log($"begin bool BearSpearItem.IsSpearHitLethal()");
                return true;
            }

            public static void Postfix(bool __result)
            {
                Log($"end bool BearSpearItem.IsSpearHitLethal() => {__result}");
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnRaised")]
        internal class BearSpearItemPatches_OnRaised
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnRaised()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnRaised()");
            }
        }




        [HarmonyPatch(typeof(BearSpearItem), "OnRaisedCancelled")]
        internal class BearSpearItemPatches_OnRaisedCancelled
        {
            public static bool Prefix()
            {
                Log($"begin void BearSpearItem.OnRaisedCancelled()");
                return true;
            }

            public static void Postfix()
            {
                Log($"end void BearSpearItem.OnRaisedCancelled()");
            }
        }
        */




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


        // Roughly functioning translation of original logic


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
        */
    }
}
