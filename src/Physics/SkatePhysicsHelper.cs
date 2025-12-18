using System;
using System.Linq;
using IceSkates.src.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace IceSkates.src.Physics
{
    /// <summary>
    /// helper methods for ice skating physics calculations
    /// </summary>
    public static class SkatePhysicsHelper
    {
        /// <summary>
        /// check if an entity is wearing ice skates
        /// based on vanilla EntityPlayer walkSpeed system and clothescategory attributes
        /// </summary>
        public static bool IsWearingSkates(EntityAgent entity)
        {
            if (entity is not EntityPlayer player)
                return false;

            // get character inventory
            var inv = player.Player?.InventoryManager?.GetOwnInventory("character");
            if (inv == null)
                return false;

            // character inventory has slots for different clothing categories
            // iterate and check clothescategory="foot" rather than hardcode slot indices
            // this is robust to inventory layout changes
            int skateCount = 0;

            foreach (var slot in inv)
            {
                if (slot?.Itemstack?.Item == null) continue;

                // check if this is a foot clothing item
                var clothesCategory = slot.Itemstack.Item.Attributes?["clothescategory"]?.AsString();
                if (clothesCategory != "foot") continue;

                // check if item code contains "skate"
                // this will match "iceskates-basic", "iceskates-reinforced", etc.
                var itemCode = slot.Itemstack.Collectible.Code?.Path ?? "";
                if (itemCode.Contains("skate"))
                {
                    skateCount++;
                }
            }

            // for now, return true if we find any skates
            // TODO: Respect IceSkatesConfig.RequireBothSkates setting
            // (would need to count foot slots with skates vs total foot slots)

            // TODO: we may want to move from clothing slot to armor slot
            return skateCount > 0;
        }

        /// <summary>
        /// check if entity is standing on ice surface
        /// </summary>
        public static bool IsOnIce(Entity entity, double yOffset = 0.05)
        {
            if (entity.World == null)
                return false;

            try
            {
                var blockAccessor = entity.World.BlockAccessor;
                var pos = entity.Pos;

                // check block beneath player's feet
                var blockPos = new BlockPos(
                    (int)pos.X,
                    (int)(pos.InternalY - yOffset),
                    (int)pos.Z,
                    pos.Dimension
                );

                var block = blockAccessor.GetBlock(blockPos);
                if (block == null)
                    return false;

                // check block material
                var material = block.GetBlockMaterial(blockAccessor, blockPos, null);

                // ice is EnumBlockMaterial.Ice
                return material == EnumBlockMaterial.Ice;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// get surface tau values based on config and current state
        /// </summary>
        public static SurfaceTaus GetSurfaceTaus(bool isOnIce, bool isWearingSkates, IceSkatesConfig config)
        {
            if (isOnIce && isWearingSkates)
            {
                // on ice with skates
                return new SurfaceTaus(
                    config.IceTauWalkUp,
                    config.IceTauWalkDown,
                    config.IceTauSprintUp,
                    config.IceTauSprintDown,
                    config.IceTauNoInput,
                    config.IceTauTurnStrength,
                    config.IceTauLateral
                );
            }
            else if (!isOnIce && isWearingSkates && config.EnableOffIcePenalty)
            {
                // off ice with skates,severe penalty
                return new SurfaceTaus(
                    config.OffIceFrictionTau,
                    config.OffIceFrictionTau * 0.8,
                    config.OffIceFrictionTau * 1.2,
                    config.OffIceFrictionTau,
                    config.OffIceFrictionTau * 0.5,
                    config.OffIceFrictionTau * 2.0,
                    config.OffIceFrictionTau * 3.0
                );
            }
            else
            {
                // normal ground physics (vanilla behavior)
                // return default values that won't affect vanilla physics
                return new SurfaceTaus(2.5, 2.0, 5.0, 2.0, 2.0, 1.5, 2.5);
            }
        }

        /// <summary>
        /// pick appropriate tau value based on player state
        /// </summary>
        public static double PickBaseTau(
            SurfaceTaus taus,
            bool isSprinting,
            bool hasInput,
            bool rampUp)
        {
            if (!hasInput)
                return taus.NoInputDown;

            return isSprinting
                ? rampUp ? taus.SprintUp : taus.SprintDown
                : rampUp ? taus.WalkUp : taus.WalkDown;
        }

        /// <summary>
        /// calculate if player is ramping up speed (accelerating in direction of movement)
        /// </summary>
        public static bool IsRampingUp(
            Vec2d previousVelocity,
            Vec2d currentVelocity,
            Vec2d inputDirection,
            double inputMagnitude)
        {
            if (inputMagnitude <= 1E-06)
                return false;

            // normalize input direction
            double normX = inputDirection.X / inputMagnitude;
            double normY = inputDirection.Y / inputMagnitude;

            // dot product of previous velocity with input direction
            double prevDot = previousVelocity.X * normX + previousVelocity.Y * normY;

            // dot product of current velocity with input direction
            double currentDot = currentVelocity.X * normX + currentVelocity.Y * normY;

            // ramping up if current velocity in direction of input is greater than previous
            return currentDot > prevDot + 1E-07;
        }

        /// <summary>
        /// calculate turn factor based on angle between previous velocity and input direction
        /// returns a value from 0 (no turn) to 1 (180° turn)
        /// </summary>
        public static double CalculateTurnFactor(
            Vec2d previousVelocity,
            Vec2d inputDirection,
            double inputMagnitude,
            double prevVelMagnitude)
        {
            if (inputMagnitude <= 1E-06 || prevVelMagnitude <= 1E-06)
                return 0.0;

            // normalize vectors
            double prevNormX = previousVelocity.X / prevVelMagnitude;
            double prevNormY = previousVelocity.Y / prevVelMagnitude;
            double inputNormX = inputDirection.X / inputMagnitude;
            double inputNormY = inputDirection.Y / inputMagnitude;

            // dot product gives cosine of angle between vectors
            double dotProduct = prevNormX * inputNormX + prevNormY * inputNormY;

            // convert dot product to turn factor (0 = same direction, 1 = opposite)
            // dotProduct ranges from -1 (180°) to 1 (0°)
            double turnFactor = (1.0 - dotProduct) / 2.0;

            return turnFactor;
        }

        /// <summary>
        /// apply direct speed penalty for sharp turns
        /// </summary>
        public static void ApplyTurnSpeedPenalty(
            EntityPos pos,
            double turnFactor,
            IceSkatesConfig config)
        {
            // convert turn factor to angle in degrees
            double turnAngle = turnFactor * 180.0;

            if (turnAngle > config.TurnAngleThreshold)
            {
                // calculate speed penalty based on turn severity
                double penaltyFactor = (turnAngle - config.TurnAngleThreshold) / (180.0 - config.TurnAngleThreshold);
                double speedPenalty = 1.0 - (penaltyFactor * config.TurnSpeedLossFactor);

                pos.Motion.X *= speedPenalty;
                pos.Motion.Z *= speedPenalty;
            }
        }

        /// <summary>
        /// calculate lateral movement ratio (0 = pure forward, 1 = pure sideways)
        /// </summary>
        public static double CalculateLateralRatio(
            Vec2d inputDirection,
            double inputMagnitude,
            double entityYaw)
        {
            if (inputMagnitude <= 1E-06)
                return 0.0;

            // normalize input direction
            double normX = inputDirection.X / inputMagnitude;
            double normY = inputDirection.Y / inputMagnitude;

            // entity's forward direction
            double forwardX = Math.Sin(-entityYaw);
            double forwardZ = Math.Cos(-entityYaw);

            // dot product with forward direction
            double forwardComponent = Math.Abs(normX * forwardX + normY * forwardZ);

            // lateral ratio is inverse of forward component
            return 1.0 - forwardComponent;
        }

        // absolute maximum speed cap to prevent runaway physics (motion per tick)
        // at 60 ticks/sec, 0.25 motion = ~15 m/s actual speed
        private const double AbsoluteMaxMotion = 0.25;

        // vanilla baseline speeds (motion per tick, derived from GlobalConstants)
        // BaseMoveSpeed = 1.5, SprintSpeedMultiplier = 2.0, groundDragFactor = 0.3
        // Walk: ~0.057 motion/tick (~3.4 m/s)
        // Sprint: ~0.113 motion/tick (~6.8 m/s)
        private const double VanillaWalkMotion = 0.057;
        private const double VanillaSprintMotion = 0.113;

        /// <summary>
        /// apply speed cap when on ice with skates.
        /// </summary>
        public static void ApplySpeedMultiplier(
            EntityPos pos,
            bool isOnIce,
            bool isWearingSkates,
            bool isSprinting,
            IceSkatesConfig config,
            double lateralRatio = 0.0)
        {
            double currentSpeed = Math.Sqrt(pos.Motion.X * pos.Motion.X + pos.Motion.Z * pos.Motion.Z);

            if (!isOnIce || !isWearingSkates)
            {
                // apply off-ice penalty if wearing skates
                if (isWearingSkates && config.EnableOffIcePenalty)
                {
                    // severely limit speed when walking on non-ice with skates
                    double maxSpeed = 0.02;

                    if (currentSpeed > maxSpeed)
                    {
                        double factor = maxSpeed / currentSpeed;
                        pos.Motion.X *= factor;
                        pos.Motion.Z *= factor;
                    }
                }
                return;
            }

            // on ice with skates - calculate max allowed speed
            // use vanilla baseline speeds, then apply configured multiplier

            // choose baseline and multiplier based on sprint state
            double baselineMotion = isSprinting ? VanillaSprintMotion : VanillaWalkMotion;
            double speedMultiplier = isSprinting
                ? config.SprintSkatingSpeedMultiplier
                : config.WalkSkatingSpeedMultiplier;

            // calculate max allowed motion
            double maxSkateMotion = baselineMotion * speedMultiplier;

            // apply lateral movement penalty to max speed
            if (lateralRatio > config.LateralMovementThreshold)
            {
                double lateralPenalty = lateralRatio * (1.0 - config.LateralSpeedMultiplier);
                maxSkateMotion *= (1.0 - lateralPenalty);
            }

            // enforce absolute maximum to prevent physics explosion
            maxSkateMotion = Math.Min(maxSkateMotion, AbsoluteMaxMotion);

            // clamp speed if exceeding maximum
            if (currentSpeed > maxSkateMotion)
            {
                double factor = maxSkateMotion / currentSpeed;
                pos.Motion.X *= factor;
                pos.Motion.Z *= factor;
            }
        }
    }
}
