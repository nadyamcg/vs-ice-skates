using System;
using System.Collections.Concurrent;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace IceSkates.src.Physics
{
    /// <summary>
    /// harmony patch for ice skating physics
    /// intercepts EntityBehaviorControlledPhysics.MotionAndCollision to modify movement behavior
    /// adapted from Sprint Momentum mod with full config integration
    /// </summary>
    [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), "MotionAndCollision")]
    public static class IceSkatingPhysicsPatch
    {
        // track previous velocity per entity for momentum calculations
        private static readonly ConcurrentDictionary<long, Vec2d> PreviousVelocity = new();

        // track ramp-up ticks for acceleration curves
        private static readonly ConcurrentDictionary<long, double> RampTicks = new();

        // track surface state per entity (for optimized checking)
        private static readonly ConcurrentDictionary<long, SurfaceState> SurfaceStates = new();

        // track last surface check time
        private static readonly ConcurrentDictionary<long, double> LastSurfaceCheck = new();

        private const double MinVectorLength = 1E-06;
        private const double MinInputLength = 1E-06;
        private const double ComparisonEpsilon = 1E-07;

        /// <summary>
        /// stores cached surface state for an entity
        /// </summary>
        private struct SurfaceState
        {
            public bool IsOnIce;
            public bool IsWearingSkates;
            public double LastCheckTime;
        }

        /// <summary>
        /// prefix patch - capture previous velocity before vanilla physics
        /// </summary>
        [HarmonyPrefix]
        private static void Prefix(
            EntityBehaviorControlledPhysics __instance,
            EntityPos pos,
            EntityControls controls,
            float dt)
        {
            if (__instance.Entity is not EntityAgent entity)
                return;

            // store current velocity as "previous" for next frame
            var entityId = entity.EntityId;
            PreviousVelocity[entityId] = new Vec2d(pos.Motion.X, pos.Motion.Z);
        }

        /// <summary>
        /// postfix patch - apply ice skating physics after vanilla physics
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(
            EntityBehaviorControlledPhysics __instance,
            EntityPos pos,
            EntityControls controls,
            float dt)
        {
            if (__instance.Entity is not EntityAgent entity)
                return;

            var entityId = entity.EntityId;

            // get previous velocity
            if (!PreviousVelocity.TryGetValue(entityId, out var previousVel))
                return;

            // reset state if not on ground
            if (!entity.OnGround)
            {
                PreviousVelocity.TryRemove(entityId, out _);
                RampTicks.TryRemove(entityId, out _);
                SurfaceStates.TryRemove(entityId, out _);
                LastSurfaceCheck.TryRemove(entityId, out _);
                StridePhysicsSystem.ClearState(entityId);
                return;
            }

            // get or update surface state
            var surfaceState = GetSurfaceState(entity, entityId);

            // if not on ice or not wearing skates, skip physics modification
            // (unless wearing skates off-ice with penalty enabled)
            var config = IceSkatesModSystem.Instance.Config;

            bool shouldApplyPhysics = surfaceState.IsOnIce && surfaceState.IsWearingSkates;
            bool shouldApplyPenalty = !surfaceState.IsOnIce && surfaceState.IsWearingSkates && config.EnableOffIcePenalty;

            if (!shouldApplyPhysics && !shouldApplyPenalty)
            {
                PreviousVelocity.TryRemove(entityId, out _);
                return;
            }

            // current velocity (after vanilla physics)
            var currentVel = new Vec2d(pos.Motion.X, pos.Motion.Z);
            double vanillaSpeed = Math.Sqrt(currentVel.X * currentVel.X + currentVel.Y * currentVel.Y);

            // input direction
            var inputDir = new Vec2d(controls.WalkVector.X, controls.WalkVector.Z);
            double inputMagnitude = Math.Sqrt(inputDir.X * inputDir.X + inputDir.Y * inputDir.Y);
            bool hasInput = inputMagnitude > MinInputLength;

            // get surface physics parameters
            var taus = SkatePhysicsHelper.GetSurfaceTaus(surfaceState.IsOnIce, surfaceState.IsWearingSkates, config);

            // calculate lateral movement ratio (for hockey-style skating)
            double lateralRatio = 0.0;
            if (hasInput)
            {
                lateralRatio = SkatePhysicsHelper.CalculateLateralRatio(inputDir, inputMagnitude, entity.Pos.Yaw);
            }

            // check if ramping up (accelerating)
            bool rampUp = false;
            if (hasInput)
            {
                rampUp = SkatePhysicsHelper.IsRampingUp(previousVel, currentVel, inputDir, inputMagnitude);
            }

            // pick base tau value (use sprint-specific coast tau when no input)
            double baseTau;
            if (!hasInput)
            {
                // use sprint-specific coast tau for better gliding
                baseTau = controls.Sprint ? config.IceTauNoInputSprint : config.IceTauNoInputWalk;
            }
            else
            {
                baseTau = SkatePhysicsHelper.PickBaseTau(taus, controls.Sprint, hasInput, rampUp);
            }
            double finalTau = baseTau;

            // apply lateral movement penalty to tau
            if (lateralRatio > config.LateralMovementThreshold)
            {
                // blend between base tau and lateral tau based on lateral ratio
                finalTau = baseTau + lateralRatio * (taus.Lateral - baseTau);
            }

            // calculate turn resistance
            double turnFactor = 0.0;
            double prevVelMagnitude = Math.Sqrt(previousVel.X * previousVel.X + previousVel.Y * previousVel.Y);

            if (hasInput && prevVelMagnitude > MinVectorLength)
            {
                turnFactor = SkatePhysicsHelper.CalculateTurnFactor(previousVel, inputDir, inputMagnitude, prevVelMagnitude);

                // apply turn strength
                double turnTau = baseTau * taus.TurnStrength;
                finalTau = Math.Max(finalTau, baseTau + turnFactor * (turnTau - baseTau));
            }


            // calculate friction factor using exponential decay
            // this is the key formula from Sprint Momentum
            double frictionFactor = 1.0 - Math.Exp(-dt * 30.0 / Math.Max(MinVectorLength, finalTau));

            // apply ramp-up smoothing for acceleration
            double smoothedFriction = frictionFactor;
            if (hasInput && rampUp)
            {
                RampTicks.TryGetValue(entityId, out double rampTime);
                rampTime += dt * 60.0;

                double rampProgress = 1.0 - Math.Exp(-rampTime / Math.Max(MinVectorLength, baseTau));
                double rampSmoothing = rampProgress * rampProgress;

                smoothedFriction = frictionFactor * rampSmoothing;
                RampTicks[entityId] = rampTime;
            }
            else
            {
                RampTicks.TryRemove(entityId, out _);
            }

            // apply momentum physics using interpolation
            pos.Motion.X = previousVel.X + (currentVel.X - previousVel.X) * smoothedFriction;
            pos.Motion.Z = previousVel.Y + (currentVel.Y - previousVel.Y) * smoothedFriction;

            // === STRIDE SYSTEM (VISUAL ONLY) ===
            // update stride cycle for wobble and camera bob effects
            bool hasForwardInput = controls.Forward && !controls.Backward;
            if (shouldApplyPhysics && config.EnableStrideSystem)
            {
                StridePhysicsSystem.UpdateStride(
                    entityId,
                    hasForwardInput,
                    controls.Sprint,
                    vanillaSpeed * 60.0, // convert to m/s (motion is per-tick at 60Hz)
                    dt,
                    config
                );
            }

            // === LATERAL WOBBLE ===
            // apply stride-synced lateral wobble (side-to-side sway during skating)
            if (shouldApplyPhysics && config.EnableStrideSystem && config.EnableLateralWobble && hasForwardInput)
            {
                double wobbleOffset = StridePhysicsSystem.GetLateralOffset(entityId, config);

                if (Math.Abs(wobbleOffset) > MinVectorLength)
                {
                    // convert wobble to world-space perpendicular to entity facing direction
                    // yaw is in radians, perpendicular is yaw + 90 degrees
                    double perpYaw = entity.Pos.Yaw + Math.PI / 2.0;
                    double sinPerp = Math.Sin(perpYaw);
                    double cosPerp = Math.Cos(perpYaw);

                    // apply wobble as a velocity offset (scaled by dt for smooth motion)
                    // note: wobbleOffset is already scaled by speed in GetLateralOffset
                    pos.Motion.X += wobbleOffset * sinPerp * dt * 2.0;
                    pos.Motion.Z += wobbleOffset * cosPerp * dt * 2.0;
                }
            }

            // apply turn speed penalty (direct speed reduction for sharp turns)
            if (turnFactor > 0.0)
            {
                SkatePhysicsHelper.ApplyTurnSpeedPenalty(pos, turnFactor, config);
            }

            // apply speed cap (clamps to maximum, doesn't boost)
            SkatePhysicsHelper.ApplySpeedMultiplier(
                pos,
                surfaceState.IsOnIce,
                surfaceState.IsWearingSkates,
                controls.Sprint,
                config,
                lateralRatio
            );

            // debug logging
            if (config.DebugMode && config.LogPhysicsCalculations)
            {
                var logger = IceSkatesModSystem.Instance.Logger;
                logger.Debug($"[IceSkate Physics] Entity {entityId}:");
                logger.Debug($"  OnIce: {surfaceState.IsOnIce}, Skates: {surfaceState.IsWearingSkates}, Sprint: {controls.Sprint}");
                logger.Debug($"  PrevVel: ({previousVel.X:F4}, {previousVel.Y:F4}), VanillaSpeed: {vanillaSpeed:F4}");
                logger.Debug($"  CurrVel: ({currentVel.X:F4}, {currentVel.Y:F4})");
                logger.Debug($"  FinalVel: ({pos.Motion.X:F4}, {pos.Motion.Z:F4})");
                logger.Debug($"  Tau: {finalTau:F2}, Friction: {smoothedFriction:F4}");
                logger.Debug($"  Turn: {turnFactor:F4}, Lateral: {lateralRatio:F4}");
            }

            // clean up previous velocity from dictionary after use
            PreviousVelocity.TryRemove(entityId, out _);
        }

        /// <summary>
        /// get or update cached surface state for entity
        /// </summary>
        private static SurfaceState GetSurfaceState(EntityAgent entity, long entityId)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var currentTime = entity.World.ElapsedMilliseconds / 1000.0;

            // check if we have a cached state
            if (SurfaceStates.TryGetValue(entityId, out var state))
            {
                // check if cache is still valid
                if (currentTime - state.LastCheckTime < config.SurfaceCheckInterval)
                {
                    return state;
                }
            }

            // update surface state
            var newState = new SurfaceState
            {
                IsOnIce = SkatePhysicsHelper.IsOnIce(entity, config.SurfaceCheckYOffset),
                IsWearingSkates = SkatePhysicsHelper.IsWearingSkates(entity),
                LastCheckTime = currentTime
            };

            SurfaceStates[entityId] = newState;
            return newState;
        }

        /// <summary>
        /// clear all cached state (called when mod unloads or config reloads)
        /// </summary>
        public static void ClearCache()
        {
            PreviousVelocity.Clear();
            RampTicks.Clear();
            SurfaceStates.Clear();
            LastSurfaceCheck.Clear();
            StridePhysicsSystem.ClearAll();
        }
    }
}
