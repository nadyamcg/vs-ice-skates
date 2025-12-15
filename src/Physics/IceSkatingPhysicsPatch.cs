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
    /// Harmony patch for ice skating physics
    /// Intercepts EntityBehaviorControlledPhysics.MotionAndCollision to modify movement behavior
    /// Adapted from Sprint Momentum mod with full config integration
    /// </summary>
    [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), "MotionAndCollision")]
    public static class IceSkatingPhysicsPatch
    {
        // Track previous velocity per entity for momentum calculations
        private static readonly ConcurrentDictionary<long, Vec2d> PreviousVelocity = new();

        // Track ramp-up ticks for acceleration curves
        private static readonly ConcurrentDictionary<long, double> RampTicks = new();

        // Track surface state per entity (for optimized checking)
        private static readonly ConcurrentDictionary<long, SurfaceState> SurfaceStates = new();

        // Track last surface check time
        private static readonly ConcurrentDictionary<long, double> LastSurfaceCheck = new();

        private const double MinVectorLength = 1E-06;
        private const double MinInputLength = 1E-06;
        private const double ComparisonEpsilon = 1E-07;

        /// <summary>
        /// Stores cached surface state for an entity
        /// </summary>
        private struct SurfaceState
        {
            public bool IsOnIce;
            public bool IsWearingSkates;
            public double LastCheckTime;
        }

        /// <summary>
        /// Prefix patch - capture previous velocity before vanilla physics
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

            // Store current velocity as "previous" for next frame
            var entityId = entity.EntityId;
            PreviousVelocity[entityId] = new Vec2d(pos.Motion.X, pos.Motion.Z);
        }

        /// <summary>
        /// Postfix patch - apply ice skating physics after vanilla physics
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

            // Get previous velocity
            if (!PreviousVelocity.TryGetValue(entityId, out var previousVel))
                return;

            // Reset state if not on ground
            if (!entity.OnGround)
            {
                PreviousVelocity.TryRemove(entityId, out _);
                RampTicks.TryRemove(entityId, out _);
                SurfaceStates.TryRemove(entityId, out _);
                LastSurfaceCheck.TryRemove(entityId, out _);
                return;
            }

            // Get or update surface state
            var surfaceState = GetSurfaceState(entity, entityId);

            // If not on ice or not wearing skates, skip physics modification
            // (unless wearing skates off-ice with penalty enabled)
            var config = IceSkatesModSystem.Instance.Config;

            bool shouldApplyPhysics = surfaceState.IsOnIce && surfaceState.IsWearingSkates;
            bool shouldApplyPenalty = !surfaceState.IsOnIce && surfaceState.IsWearingSkates && config.EnableOffIcePenalty;

            if (!shouldApplyPhysics && !shouldApplyPenalty)
            {
                PreviousVelocity.TryRemove(entityId, out _);
                return;
            }

            // Current velocity (after vanilla physics)
            var currentVel = new Vec2d(pos.Motion.X, pos.Motion.Z);

            // Input direction
            var inputDir = new Vec2d(controls.WalkVector.X, controls.WalkVector.Z);
            double inputMagnitude = Math.Sqrt(inputDir.X * inputDir.X + inputDir.Y * inputDir.Y);
            bool hasInput = inputMagnitude > MinInputLength;

            // Get surface physics parameters
            var taus = SkatePhysicsHelper.GetSurfaceTaus(surfaceState.IsOnIce, surfaceState.IsWearingSkates, config);

            // Check if ramping up (accelerating)
            bool rampUp = false;
            if (hasInput)
            {
                rampUp = SkatePhysicsHelper.IsRampingUp(previousVel, currentVel, inputDir, inputMagnitude);
            }

            // Pick base tau value
            double baseTau = SkatePhysicsHelper.PickBaseTau(taus, controls.Sprint, hasInput, rampUp);
            double finalTau = baseTau;

            // Calculate turn resistance
            double turnFactor = 0.0;
            double prevVelMagnitude = Math.Sqrt(previousVel.X * previousVel.X + previousVel.Y * previousVel.Y);

            if (hasInput && prevVelMagnitude > MinVectorLength)
            {
                turnFactor = SkatePhysicsHelper.CalculateTurnFactor(previousVel, inputDir, inputMagnitude, prevVelMagnitude);

                // Apply turn strength
                double turnTau = baseTau * taus.TurnStrength;
                finalTau = baseTau + turnFactor * (turnTau - baseTau);
            }

            // Calculate friction factor using exponential decay
            // This is the key formula from Sprint Momentum
            double frictionFactor = 1.0 - Math.Exp(-dt * 30.0 / Math.Max(MinVectorLength, finalTau));

            // Apply ramp-up smoothing for acceleration
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

            // Apply momentum physics using interpolation
            pos.Motion.X = previousVel.X + (currentVel.X - previousVel.X) * smoothedFriction;
            pos.Motion.Z = previousVel.Y + (currentVel.Y - previousVel.Y) * smoothedFriction;

            // Apply speed cap/multiplier
            SkatePhysicsHelper.ApplySpeedMultiplier(pos, surfaceState.IsOnIce, surfaceState.IsWearingSkates, config);

            // Debug logging
            if (config.DebugMode && config.LogPhysicsCalculations)
            {
                var logger = IceSkatesModSystem.Instance.Logger;
                logger.Debug($"[IceSkate Physics] Entity {entityId}:");
                logger.Debug($"  OnIce: {surfaceState.IsOnIce}, Skates: {surfaceState.IsWearingSkates}");
                logger.Debug($"  PrevVel: ({previousVel.X:F4}, {previousVel.Y:F4})");
                logger.Debug($"  CurrVel: ({currentVel.X:F4}, {currentVel.Y:F4})");
                logger.Debug($"  FinalVel: ({pos.Motion.X:F4}, {pos.Motion.Z:F4})");
                logger.Debug($"  Tau: {finalTau:F2}, Friction: {smoothedFriction:F4}, Turn: {turnFactor:F4}");
            }

            // Clean up previous velocity from dictionary after use
            PreviousVelocity.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Get or update cached surface state for entity
        /// </summary>
        private static SurfaceState GetSurfaceState(EntityAgent entity, long entityId)
        {
            var config = IceSkatesModSystem.Instance.Config;
            var currentTime = entity.World.ElapsedMilliseconds / 1000.0;

            // Check if we have a cached state
            if (SurfaceStates.TryGetValue(entityId, out var state))
            {
                // Check if cache is still valid
                if (currentTime - state.LastCheckTime < config.SurfaceCheckInterval)
                {
                    return state;
                }
            }

            // Update surface state
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
        /// Clear all cached state (called when mod unloads or config reloads)
        /// </summary>
        public static void ClearCache()
        {
            PreviousVelocity.Clear();
            RampTicks.Clear();
            SurfaceStates.Clear();
            LastSurfaceCheck.Clear();
        }
    }
}
