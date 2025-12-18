using System;
using System.Collections.Concurrent;
using IceSkates.src.Config;

namespace IceSkates.src.Physics
{
    /// <summary>
    /// discrete phases within a single stride cycle
    /// </summary>
    public enum StridePhase
    {
        /// <summary>push phase - maximum acceleration, leg pushing off</summary>
        Push,

        /// <summary>glide phase - minimal input, momentum carries forward</summary>
        Glide,

        /// <summary>transition phase - switching legs</summary>
        Transition
    }

    /// <summary>
    /// tracks the current stride cycle state for an entity
    /// </summary>
    public struct StrideState
    {
        /// <summary>current phase in the stride cycle (0.0 to 1.0)</summary>
        public double CyclePhase;

        /// <summary>which leg is currently pushing (true = left, false = right)</summary>
        public bool IsLeftLeg;

        /// <summary>time since last stride transition</summary>
        public double TimeSinceTransition;

        /// <summary>current stride cycle duration (modified by speed/sprint)</summary>
        public double CurrentStrideDuration;

        /// <summary>is player in active skating motion (vs coasting)</summary>
        public bool IsActivelyStriding;

        /// <summary>current speed for stride scaling</summary>
        public double CurrentSpeed;
    }

    /// <summary>
    /// tracks stride cycle state for visual effects (wobble, camera bob).
    /// no longer affects physics - purely cosmetic.
    /// </summary>
    public static class StridePhysicsSystem
    {
        // entity stride state storage
        private static readonly ConcurrentDictionary<long, StrideState> StrideStates = new();

        // phase distribution within stride (should sum to 1.0)
        private const double PushPhaseRatio = 0.30;   // 30% of stride is push
        private const double GlidePhaseRatio = 0.60;  // 60% of stride is glide
        private const double TransitionRatio = 0.10;  // 10% is leg switch transition

        // phase boundaries
        private const double PushEnd = PushPhaseRatio;
        private const double GlideEnd = PushPhaseRatio + GlidePhaseRatio;
        // transition goes from GlideEnd to 1.0

        /// <summary>
        /// update stride cycle state for visual effects only.
        /// does not affect physics - purely for wobble and camera bob.
        /// </summary>
        /// <param name="entityId">Entity ID for state tracking</param>
        /// <param name="hasForwardInput">Is player holding forward</param>
        /// <param name="isSprinting">Sprint key held</param>
        /// <param name="currentSpeed">Current horizontal speed (m/s)</param>
        /// <param name="dt">Delta time in seconds</param>
        /// <param name="config">Config reference</param>
        public static void UpdateStride(
            long entityId,
            bool hasForwardInput,
            bool isSprinting,
            double currentSpeed,
            double dt,
            IceSkatesConfig config)
        {
            if (!config.EnableStrideSystem)
                return;

            // get or create state
            var state = StrideStates.GetOrAdd(entityId, _ => CreateInitialState(config));

            // determine stride duration based on sprint state
            state.CurrentStrideDuration = isSprinting
                ? config.SprintStrideDuration
                : config.BaseStrideDuration;

            state.CurrentSpeed = currentSpeed;

            // update stride cycle if actively striding
            if (hasForwardInput && currentSpeed > 0.01)
            {
                state.IsActivelyStriding = true;

                // advance cycle phase
                double phaseAdvance = dt / state.CurrentStrideDuration;
                state.CyclePhase += phaseAdvance;

                // handle cycle wrap
                if (state.CyclePhase >= 1.0)
                {
                    state.CyclePhase -= 1.0;
                    state.IsLeftLeg = !state.IsLeftLeg; // Switch legs
                    state.TimeSinceTransition = 0;
                }
                else
                {
                    state.TimeSinceTransition += dt;
                }
            }
            else
            {
                state.IsActivelyStriding = false;
            }

            // store updated state
            StrideStates[entityId] = state;
        }

        /// <summary>
        /// get current stride phase and progress within that phase
        /// </summary>
        public static (StridePhase phase, double phaseProgress) GetStridePhase(long entityId)
        {
            if (!StrideStates.TryGetValue(entityId, out var state))
                return (StridePhase.Glide, 0.0);

            double cyclePhase = state.CyclePhase;

            if (cyclePhase < PushEnd)
            {
                // In push phase
                return (StridePhase.Push, cyclePhase / PushPhaseRatio);
            }
            else if (cyclePhase < GlideEnd)
            {
                // In glide phase
                return (StridePhase.Glide, (cyclePhase - PushEnd) / GlidePhaseRatio);
            }
            else
            {
                // In transition phase
                return (StridePhase.Transition, (cyclePhase - GlideEnd) / TransitionRatio);
            }
        }

        /// <summary>
        /// get lateral wobble offset for the current stride phase.
        /// returns signed offset perpendicular to movement direction.
        /// </summary>
        public static double GetLateralOffset(long entityId, IceSkatesConfig config)
        {
            if (!config.EnableLateralWobble)
                return 0.0;

            if (!StrideStates.TryGetValue(entityId, out var state))
                return 0.0;

            if (!state.IsActivelyStriding)
                return 0.0;

            // sinusoidal wobble based on cycle phase
            // left leg push = wobble right, right leg push = wobble left
            double wobbleSign = state.IsLeftLeg ? 1.0 : -1.0;
            double wobbleWave = Math.Sin(state.CyclePhase * Math.PI * 2);

            // scale wobble by speed (more wobble at higher speeds, up to max)
            double speedScale = Math.Min(1.0, state.CurrentSpeed / 6.0);

            return wobbleSign * wobbleWave * config.MaxLateralWobble * speedScale;
        }


        /// <summary>
        /// get full stride state for debugging/HUD display
        /// </summary>
        public static StrideState? GetStrideState(long entityId)
        {
            return StrideStates.TryGetValue(entityId, out var state) ? state : null;
        }

        /// <summary>
        /// clear stride state for an entity
        /// </summary>
        public static void ClearState(long entityId)
        {
            StrideStates.TryRemove(entityId, out _);
        }

        /// <summary>
        /// clear all stride states (called on mod unload)
        /// </summary>
        public static void ClearAll()
        {
            StrideStates.Clear();
        }

        // ========================================
        // PRIVATE HELPERS
        // ========================================

        private static StrideState CreateInitialState(IceSkatesConfig config)
        {
            return new StrideState
            {
                CyclePhase = 0.0,
                IsLeftLeg = true,
                TimeSinceTransition = 0,
                CurrentStrideDuration = config.BaseStrideDuration,
                IsActivelyStriding = false,
                CurrentSpeed = 0.0
            };
        }

    }
}
