//#define DEV_BUILD

using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Security.Util;
using UnityEngine;
using static Il2Cpp.BearSpearItem;
using static MonsieurMeh.Mods.TLD.BearSpearReloaded.Helpers;


namespace MonsieurMeh.Mods.TLD.BearSpearReloaded
{
    internal class Patches
    {
        [HarmonyPatch(typeof(BaseAi), "OnSpearHit", new Type[] { typeof(bool), typeof(Il2CppSystem.Action) })]
        internal class BaseAiPatches_OnSpearHit
        {
            public static bool Prefix(bool isFatal, Il2CppSystem.Action onSpearStruggleEnd, BaseAi __instance)
            {
                __instance.m_SpearStruggleEndAction = onSpearStruggleEnd;
                __instance.m_WasHitBySpear = true;
                __instance.m_WasHitBySpearFatal = isFatal;
                __instance.SetAiMode(isFatal ? AiMode.Dead : AiMode.Flee);
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
                __instance.SetCurrentState(SpearState.Recovering);
                __instance.m_HitAi = bai;
                __instance.m_LocalizedDamage = localizedDamage;
                __instance.m_HitPosition = hitTransform.position;
                __instance.m_HitSourcePosition = playerTransform.position;
                __instance.m_HitDamage = __instance.m_DamageDealt;
                __instance.m_HitBleedOutMinutes = __instance.m_BleedOutMinutes;
                Log($"MOTHERFUCKING BOOM!");
                bai.OnSpearHit(__instance.IsDamageFatal(bai, __instance.m_DamageDealt), bai.m_SpearStruggleEndAction);
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
                if (bai == null)
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
                Log($"comparing ((playerPos-aiPos).sqrMagnituce ({(playerPos - aiPos).sqrMagnitude} > maxDistance * maxDistance ({maxDistance * maxDistance}: {(playerPos - aiPos).sqrMagnitude > maxDistance * maxDistance}");
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
            public static bool Prefix(SpearState state, BearSpearItem __instance)
            {
                if (__instance.m_PendingSpearState != state)
                {
                    __instance.m_PendingSpearState = state;
                }
                switch (state)
                {
                    case SpearState.None:
                        __instance.OnEnter_None();
                        break;
                    case SpearState.Setting:
                        __instance.OnEnter_Setting();
                        break;
                    case SpearState.Raised:
                        __instance.OnEnter_Raised();
                        break;
                    case SpearState.Recovering:
                        __instance.OnEnter_Recovering();
                        break;
                    case (SpearState)4:
                        break;

                }
                if (__instance.m_PendingSpearState == state)
                {
                    __instance.m_CurrentSpearState = state;
                }
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnStruggleHitEnd")]
        internal class BearSpearItemPatches_OnStruggleHitEnd
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (__instance.m_HitAi == null || __instance.m_HitAi.gameObject == null)
                {
                    LogError("Null HitAi or HitAi.gameObject on BearSpearItem.OnStruggleHitEnd!");
                    return false;
                }
                if (__instance.m_GearItem == null)
                {
                    LogError("Null gear item on BearSpearItem.OnStruggleHitEnd!");
                    return false;
                }
                PlayerStruggle playerStruggle = GameManager.m_PlayerStruggle;
                if (playerStruggle == null)
                {
                    LogError("Null playerStruggle on bearSpearItem.OnStruggleHitEnd!");
                    return false;
                }
                if (playerStruggle.m_BearSpearStruggleOutcome == BearSpearStruggleOutcome.Failed)
                {
                    __instance.m_HitDamage = 0.0f;
                    __instance.m_HitBleedOutMinutes = 0.0f;
                }
                if (__instance.m_LocalizedDamage != null)
                {
                    __instance.m_HitAi.SetupDamageForAnim(__instance.m_HitPosition, __instance.m_HitSourcePosition, __instance.m_LocalizedDamage);
                }
                __instance.m_HitAi.ApplyDamage(__instance.m_HitDamage, __instance.m_HitBleedOutMinutes, DamageSource.Player, string.Empty); //string literal for this call was blank, not sure whats up there. might be a default to blank on c# side?
                __instance.m_GearItem.DegradeOnUse();
                if (__instance.m_GearItem.m_WornOut)
                {
                    __instance.Break();
                }
                return false;
            }


        }


        [HarmonyPatch(typeof(BearSpearItem), "Break")]
        internal class BearSpearItemPatches_Break
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (__instance.m_BrokenSpearPrefab == null)
                {
                    LogError("Null broken spear prefab on BearSpearItem.Break!");
                    return false;
                }
                GameObject newBrokenSpearPrefab = GameObject.Instantiate(__instance.m_BrokenSpearPrefab);
                GearItem newBrokenSpearGearItem = newBrokenSpearPrefab.GetComponent<GearItem>();
                newBrokenSpearPrefab.name = __instance.m_BrokenSpearPrefab.name;
                if (!newBrokenSpearGearItem.m_InPlayerInventory)
                {
                    newBrokenSpearGearItem.transform.position = __instance.transform.position;
                    newBrokenSpearGearItem.transform.rotation = __instance.transform.rotation;
                }
                else
                {
                    GameManager.m_Inventory.AddGear(newBrokenSpearGearItem, false);
                }
                GameManager.m_Inventory.DestroyGear(__instance.gameObject);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "Update_SpearRaised")]
        internal class BearSpearItemPatches_Update_SpearRaised
        {
            public static bool Prefix()
            {
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "Update")]
        internal class BearSpearItemPatches_Update
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CanStartZoom")]
        internal class BearSpearItemPatches_CanStartZoom
        {
            public static bool Prefix(BearSpearItem __instance, ref bool __result)
            {
                __result = __instance.m_CurrentSpearState == SpearState.None;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CheckStaminaForRaising")]
        internal class BearSpearItemPatches_CheckStaminaForRaising
        {
            public static bool Prefix(ref bool __result)
            {
                __result = GameManager.m_PlayerMovement.m_SprintStamina > 0.0001f;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CanEndZoom")]
        internal class BearSpearItemPatches_CanEndZoom
        {
            public static bool Prefix(BearSpearItem __instance, ref bool __result)
            {
                __result = __instance.m_CurrentSpearState == SpearState.Raised;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "ZoomStart")]
        internal class BearSpearItemPatches_ZoomStart
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                __instance.SetCurrentState(SpearState.Setting);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "RestoreYawPitchLimits")]
        internal class BearSpearItemPatches_RestoreYawPitchLimits
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                vp_FPSCamera camera = GameManager.m_vpFPSCamera;
                if (camera == null)
                {
                    LogError("Nul vp_FPSCamera on BearSpearitem.RestoreYawPitchLimits!");
                    return false;
                }
                camera.RotationPitchLimit = __instance.m_StartPitchLimit;
                camera.RotationYawLimit = __instance.m_StartYawLimit;
                camera.UnlockRotationLimit();
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "ZoomEnd")]
        internal class BearSpearItemPatches_ZoomEnd
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                __instance.SetCurrentState(SpearState.Recovering);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "CancelAction")]
        internal class BearSpearItemPatches_CancelAction
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                __instance.SetCurrentState(SpearState.None);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "HitAction")]
        internal class BearSpearItemPatches_HitAction
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                __instance.SetCurrentState(SpearState.None);
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_None")]
        internal class BearSpearItemPatches_OnEnter_None
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                Log("OnEnter_None");
                PlayerAnimation newPlayerAnimation = GameManager.m_NewPlayerAnimation;
                PlayerManager playerManager = GameManager.m_PlayerManager;
                vp_FPSCamera camera = GameManager.m_vpFPSCamera;
                if (__instance.m_HasDisabledHipAndShoulderOffsetLayer)
                {
                    newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount--;
                    if (newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount > 0)
                    {
                        newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount = 0;
                    }
                    newPlayerAnimation.UpdateHipAndShoulderOffsetLayerWeigth(newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount == 0 ? 1.0f : 0.0f);
                    __instance.m_HasDisabledHipAndShoulderOffsetLayer = false;
                }
                newPlayerAnimation.SetParameterRecoverSpearMultiplier(10000.0f);
                if (playerManager.GetControlMode() == PlayerControlMode.BearSpear)
                {
                    playerManager.SetControlMode(__instance.m_ControlModeToRestore);
                }
                camera.RotationPitchLimit = __instance.m_StartPitchLimit;
                camera.RotationYawLimit = __instance.m_StartYawLimit;
                camera.UnlockRotationLimit();
                __instance.MaybeEnableStaminaRecharge();
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Setting")]
        internal class BearSpearItemPatches_OnEnter_Setting
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                //Log("OnEnter_Setting");
                PlayerAnimation newPlayerAnimation = GameManager.m_NewPlayerAnimation;
                PlayerManager playerManager = GameManager.m_PlayerManager;
                vp_FPSCamera camera = GameManager.m_vpFPSCamera;
                __instance.m_ControlModeToRestore = playerManager.GetControlMode();
                __instance.m_StartPitchLimit = camera.RotationPitchLimit;
                __instance.m_StartYawLimit = camera.RotationYawLimit;
                camera.RotationPitchLimit = __instance.m_PitchLimitDegrees;
                camera.SetYawLimit(__instance.gameObject.transform.rotation, __instance.m_YawLimitDegrees);
                camera.m_LockRotationLimit = true;
                camera.m_LockedRotationPitchLimit = camera.RotationPitchLimit;
                camera.m_LockedRotationYawLimit = camera.RotationYawLimit;
                playerManager.SetControlMode(PlayerControlMode.BearSpear);
                float animSpeedMult = (__instance.m_SetSpearDurationSeconds > 0.01f) ? __instance.m_AnimationSetBaseDuration / __instance.m_SetSpearDurationSeconds : 10000f;
                newPlayerAnimation.SetParameterSetSpearMultiplier(animSpeedMult);
                newPlayerAnimation.m_HipAndShoulderOffsetLayerDisableCount++;
                newPlayerAnimation.UpdateHipAndShoulderOffsetLayerWeigth(0.0f);
                __instance.m_HasDisabledHipAndShoulderOffsetLayer = true;
                __instance.m_HasTriggeredGenericAim = true;
                newPlayerAnimation.Trigger_Generic_Aim(new Action(__instance.OnRaised));
                __instance.m_ActionElapsedSeconds = 0.0f;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Raised")]
        internal class BearSpearItemPatches_OnEnter_Raised
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                //Log("OnEnter_Raised");
                vp_FPSCamera camera = GameManager.m_vpFPSCamera;
                __instance.m_ActionElapsedSeconds = 0.0f;
                Transform playerTransform = GameManager.GetPlayerTransform();
                float minDistance = float.PositiveInfinity;
                BaseAi closestAi = null;
                foreach (BaseAi ai in BaseAiManager.m_BaseAis)
                {
                    if (ai == null) continue;
                    if (!ai.CanBeHitBySpear()) continue;
                    if (ai.m_CachedTransform == null) continue;
                    float dist = Vector3.Distance(playerTransform.position, ai.m_CachedTransform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestAi = ai;
                    }
                }
                if (closestAi != null)
                {
                    BearSpearReloadedManager.Instance.NearestAi = closestAi;
                }

                if (camera.m_CurrentWeapon == null)
                {
                    LogError("FPS Camera has null weapon!");
                    return false;
                }
                if (camera.m_CurrentWeapon.m_FirstPersonWeaponShoulder == null)
                {
                    LogError("FPS Camera current shoulder weapon is null!");
                    return false;
                }
                __instance.m_BearSpearHead = camera.m_CurrentWeapon.m_FirstPersonWeaponShoulder.GetComponentInChildren<BearSpearHead>();
                __instance.MaybeDisableStaminaRecharge();
                return false;
            }

        }


        [HarmonyPatch(typeof(BearSpearItem), "OnEnter_Recovering")]
        internal class BearSpearItemPatches_OnEnter_Recovering
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                BearSpearReloadedManager.Instance.NearestAi = null;
                //Log("OnEnter_Recovering");
                PlayerAnimation playerAnimation = GameManager.m_NewPlayerAnimation;
                playerAnimation.SetParameterSetSpearMultiplier(__instance.m_RecoverSpearDurationSeconds > 0.01f ? __instance.m_AnimationRecoverBaseDuration / __instance.m_RecoverSpearDurationSeconds : 10000f);
                if (__instance.m_HasTriggeredGenericAim)
                {
                    playerAnimation.Trigger_Generic_Aim_Cancel(new Action(__instance.OnRaisedCancelled), false);
                }
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "MaybeDisableStaminaRecharge")]
        internal class BearSpearItemPatches_MaybeDisableStaminaRecharge
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (__instance.m_StaminaCost > 0.0001f)
                {
                    return false;
                }
                GameManager.m_PlayerMovement.m_StaminaRechargeDisabled = false;
                return false;
            }
        }


        [HarmonyPatch(typeof(BearSpearItem), "MaybeEnableStaminaRecharge")]
        internal class BearSpearItemPatches_MaybeEnableStaminaRecharge
        {
            public static bool Prefix(BearSpearItem __instance)
            {
                if (__instance.m_StaminaCost > 0.0001f)
                {
                    return false;
                }
                GameManager.m_PlayerMovement.m_StaminaRechargeDisabled = true;
                return false;
            }
        }


        // First pass, still allows classic "setting" with left mouse but adds a "shoot spear" trigger when right + left. 
        // Decided I wanted to get rid of classic setting and do an action-button-sequence minigame on proximity while holding
        // and shift existing logic to "aiming" since "raise" still makes a lot of sense in the context of spear throwing, and i dont want to add a second phase to this mess


        [HarmonyPatch(typeof(vp_FPSPlayer), "HandleBearSpearInput", new Type[] { typeof(PlayerManager) })]
        internal class vp_FPSPlayerPatches_HandleBearSpearInput
        {
            public static bool Prefix(vp_FPSPlayer __instance, PlayerManager pm)
            {
                if (GameManager.m_IsPaused)
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
                if (GameManager.m_PlayerStruggle.m_Active && GameManager.m_PlayerStruggle.m_IsInSpearStruggle)
                {
                    spear.CancelAction();
                    return false;
                }
                spear.m_ActionElapsedSeconds += Time.deltaTime;
                switch (spear.m_CurrentSpearState)
                {
                    case SpearState.None:
                        if (InputManager.GetFirePressed(__instance))
                        {
                            //Log("Fire pressed!");
                            __instance.m_SpearZoomRequested = true;
                            __instance.m_CancelSpearZoomRequested = false;
                        }
                        else if (InputManager.GetAltFirePressed(__instance))
                        {
                            //Log("AltFire pressed in none state, prepping for throw!");
                            spear.m_CurrentSpearState = (SpearState)4;
                            __instance.SetState("Zoom", true);
                            __instance.FPSCamera.ToggleZoom(true);
                            __instance.m_InZoom = true;
                            return false;
                        }
                        break;
                    case SpearState.Setting:
                        if (InputManager.GetFireReleased(__instance))
                        {
                            //Log("Fire released!");
                            __instance.m_SpearZoomRequested = false;
                            __instance.m_CancelSpearZoomRequested = true;
                        }
                        if (spear.m_ActionElapsedSeconds > spear.m_SetSpearDurationSeconds)
                        {
                            spear.SetCurrentState(SpearState.Raised);
                        }
                        break;
                    case SpearState.Raised:
                        if (spear.m_GearItem == null)
                        {
                            LogError("Null gear item on bear spear!");
                            return false;
                        }
                        if (spear.m_GearItem.m_WornOut || !spear.IsEquipped())
                        {
                            spear.CancelAction();
                        }
                        GameManager.m_PlayerMovement?.AddSprintStamina(-spear.m_StaminaCost * Time.deltaTime);
                        if (GameManager.m_PlayerMovement.m_SprintStamina <= 0.0001f)
                        {
                            //Log("Out of stamina while spear raised!");
                            __instance.m_SpearZoomRequested = false;
                            __instance.m_CancelSpearZoomRequested = true;
                        }
                        if (InputManager.GetFireReleased(__instance))
                        {
                            //Log("Fire released!");
                            __instance.m_SpearZoomRequested = false;
                            __instance.m_CancelSpearZoomRequested = true;
                        }
                        if (BearSpearReloadedManager.Instance.NearestAi != null)
                        {
                            spear.UpdateCollision(BearSpearReloadedManager.Instance.NearestAi);
                        }
                        break;
                    case SpearState.Recovering:
                        if (spear.m_ActionElapsedSeconds > spear.m_RecoverSpearDurationSeconds)
                        {
                            spear.SetCurrentState(SpearState.None);
                        }
                        break;
                    case (SpearState)4:
                        if (InputManager.GetFirePressed(__instance))
                        {
                            //Log("AltFire pressed in throw state, throwing!");
                            __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                            __instance.m_CancelSpearZoomRequested = false;
                            __instance.SetState("Zoom", false);
                            __instance.FPSCamera.ToggleZoom(false);
                            __instance.ReenableWeaponStatesIn(0.5f);
                            __instance.m_InZoom = false;
                            ShootSpear(spear);
                            spear.SetCurrentState(SpearState.Recovering);
                            return false;
                        }
                        else if (InputManager.GetAltFireReleased(__instance))
                        {
                            //Log("AltFire released in throw state, returning!");
                            __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                            __instance.m_CancelSpearZoomRequested = false;
                            __instance.SetState("Zoom", false);
                            __instance.FPSCamera.ToggleZoom(false);
                            __instance.ReenableWeaponStatesIn(0.5f);
                            __instance.m_InZoom = false;
                            spear.SetCurrentState(SpearState.Recovering);
                            return false;
                        }
                        break;
                }
                if (__instance.m_SpearZoomRequested && !__instance.m_InZoom && !__instance.m_InteractReleaseRequiredBeforeSpearZoom && spear.m_CurrentSpearState == SpearState.None)
                {
                    //Log("transitioning to spear set!");
                    __instance.SetState("Zoom", true);
                    __instance.FPSCamera.ToggleZoom(true);
                    __instance.m_InZoom = true;
                    __instance.m_SpearZoomRequested = false;
                    spear.SetCurrentState(SpearState.Setting);
                }
                if (__instance.m_CancelSpearZoomRequested)
                {
                    //Log("cancelling spear  zoom!");
                    if (spear.m_CurrentSpearState == SpearState.None || spear.m_CurrentSpearState == SpearState.Raised)
                    {
                        //Log($"Correct state {spear.m_CurrentSpearState}, actually cancelling...");
                        __instance.m_InteractReleaseRequiredBeforeSpearZoom = false;
                        __instance.m_CancelSpearZoomRequested = false;
                        __instance.SetState("Zoom", false);
                        __instance.FPSCamera.ToggleZoom(false);
                        __instance.ReenableWeaponStatesIn(0.5f);
                        __instance.m_InZoom = false;
                        spear.SetCurrentState(SpearState.Recovering);
                    }
                }
                return false;
            }

            private static void ShootSpear(BearSpearItem spear)
            {
                Log($"pewpew!");
            }
        }


        [HarmonyPatch(typeof(vp_FPSPlayer), "MaybeCancelZoomInternal")]
        internal class vp_FPSPlayerPatches_MaybeCancelZoomInternal()
        {

            public static bool Prefix(vp_FPSPlayer __instance)
            {
                if (!__instance.m_InZoom)
                {
                    //Log($"vp_FPSPlayer is not inzoom, cancelling...");
                    return false;
                }

                __instance.SetState("Zoom", false);
                __instance.FPSCamera.ToggleZoom(false);
                __instance.ReenableWeaponStatesIn(0.5f);
                GunItem gunItem = __instance.FPSCamera.CurrentWeapon?.m_GunItem ?? null;
                if (gunItem != null)
                {
                    gunItem.ZoomEnd();
                    if (gunItem.m_Clip._size >= 1)
                    {
                        GameAudioManager.PlaySound(gunItem.m_UncockAudio, GameManager.GetPlayerObject());
                    }
                }
                PlayerManager playerManager = GameManager.m_PlayerManager;
                if (playerManager == null)
                {
                    LogError("Null PlayerManager at vp_FPSPlayer.MaybeCancelZoomInternal!");
                    return false;
                }
                PlayerAnimation playerAnimation = GameManager.m_NewPlayerAnimation;
                if (playerManager == null)
                {
                    LogError("Null playerAnimation at vp_FPSPlayer.MaybeCancelZoomInternal!");
                    return false;
                }
                GearItem itemInHands = playerManager.m_ItemInHandsInternal;
                if (itemInHands?.m_StoneItem != null)
                {
                    //Log($"StoneItem found!");
                    playerAnimation.Trigger_Generic_Aim_Cancel(new Action(itemInHands.m_StoneItem.OnAimingCancelled), false);
                    GameManager.m_Freezing.MaybeCancelPlayerFreezingTeethChatter();
                }
                return false;
            }
        }


        //LET NOTHING STOP THE MIGHTY SPEAR
        [HarmonyPatch(typeof(BaseAi), "CanBeHitBySpear")]
        internal class BaseAiPatches_CanBeHitBySpear
        {
            public static bool Prefix(BaseAi __instance, ref bool __result)
            {
                __result = true;
                return false;
            }
        }
    }
}
