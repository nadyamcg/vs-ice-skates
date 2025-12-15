using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace IceSkates.src.Physics
{
    /// <summary>
    /// Helper methods for ice skating physics calculations
    /// </summary>
    public static class SkatePhysicsHelper
    {
        /// <summary>
        /// Check if an entity is wearing ice skates
        /// </summary>
        public static bool IsWearingSkates(EntityAgent entity)
        {
            if (entity is not EntityPlayer player)
                return false;

            // Access player's inventory (use Player property to get IPlayer interface)
            var inv = player.Player?.InventoryManager?.GetOwnInventory("character");
            if (inv == null)
                return false;

            // Check for boots slot (slot index for feet equipment)
            // Inventory slots: 0-14 are character slots, slot 9 is typically feet
            var footSlot = inv.FirstOrDefault(slot => slot != null && slot.Itemstack?.Collectible.Code != null);

            if (footSlot?.Itemstack == null)
                return false;

            // Check if the item code contains "skate"
            // This will match any item with "skate" in the code (e.g., "boneskate", "iceskate")
            var itemCode = footSlot.Itemstack.Collectible.Code?.Path ?? "";
            return itemCode.Contains("skate");
        }

        /// <summary>
        /// Check if entity is standing on ice surface
        /// </summary>
        public static bool IsOnIce(Entity entity, double yOffset = 0.05)
        {
            if (entity.World == null)
                return false;

            try
            {
                var blockAccessor = entity.World.BlockAccessor;
                var pos = entity.Pos;

                // Check block beneath player's feet
                var blockPos = new BlockPos(
                    (int)pos.X,
                    (int)(pos.InternalY - yOffset),
                    (int)pos.Z,
                    pos.Dimension
                );

                var block = blockAccessor.GetBlock(blockPos);
                if (block == null)
                    return false;

                // Check block material
                var material = block.GetBlockMaterial(blockAccessor, blockPos, null);

                // Ice is EnumBlockMaterial.Ice
                return material == EnumBlockMaterial.Ice;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get surface tau values based on config and current state
        /// </summary>
        public static SurfaceTaus GetSurfaceTaus(bool isOnIce, bool isWearingSkates, IceSkatesConfig config)
        {
            if (isOnIce && isWearingSkates)
            {
                // On ice with skates - use configured ice physics
                return new SurfaceTaus(
                    config.IceTauWalkUp,
                    config.IceTauWalkDown,
                    config.IceTauSprintUp,
                    config.IceTauSprintDown,
                    config.IceTauNoInput,
                    config.IceTauTurnStrength
                );
            }
            else if (!isOnIce && isWearingSkates && config.EnableOffIcePenalty)
            {
                // Off ice with skates - severe penalty ("bear scenario")
                return new SurfaceTaus(
                    config.OffIceFrictionTau,
                    config.OffIceFrictionTau * 0.8,
                    config.OffIceFrictionTau * 1.2,
                    config.OffIceFrictionTau,
                    config.OffIceFrictionTau * 0.5,
                    config.OffIceFrictionTau * 2.0
                );
            }
            else
            {
                // Normal ground physics (vanilla behavior)
                // Return default values that won't affect vanilla physics
                return new SurfaceTaus(2.5, 2.0, 5.0, 2.0, 2.0, 1.5);
            }
        }

        /// <summary>
        /// Pick appropriate tau value based on player state
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
        /// Calculate if player is ramping up speed (accelerating in direction of movement)
        /// </summary>
        public static bool IsRampingUp(
            Vec2d previousVelocity,
            Vec2d currentVelocity,
            Vec2d inputDirection,
            double inputMagnitude)
        {
            if (inputMagnitude <= 1E-06)
                return false;

            // Normalize input direction
            double normX = inputDirection.X / inputMagnitude;
            double normY = inputDirection.Y / inputMagnitude;

            // Dot product of previous velocity with input direction
            double prevDot = previousVelocity.X * normX + previousVelocity.Y * normY;

            // Dot product of current velocity with input direction
            double currentDot = currentVelocity.X * normX + currentVelocity.Y * normY;

            // Ramping up if current velocity in direction of input is greater than previous
            return currentDot > prevDot + 1E-07;
        }

        /// <summary>
        /// Calculate turn factor based on angle between previous velocity and input direction
        /// </summary>
        public static double CalculateTurnFactor(
            Vec2d previousVelocity,
            Vec2d inputDirection,
            double inputMagnitude,
            double prevVelMagnitude)
        {
            if (inputMagnitude <= 1E-06 || prevVelMagnitude <= 1E-06)
                return 0.0;

            // Normalize vectors
            double prevNormX = previousVelocity.X / prevVelMagnitude;
            double prevNormY = previousVelocity.Y / prevVelMagnitude;
            double inputNormX = inputDirection.X / inputMagnitude;
            double inputNormY = inputDirection.Y / inputMagnitude;

            // Dot product gives cosine of angle between vectors
            double dotProduct = prevNormX * inputNormX + prevNormY * inputNormY;

            // If moving backwards relative to input direction, return full turn factor
            if (dotProduct < 0.0)
                return -dotProduct;

            return 0.0;
        }

        /// <summary>
        /// Apply speed multiplier from config when on ice with skates
        /// </summary>
        public static void ApplySpeedMultiplier(
            EntityPos pos,
            bool isOnIce,
            bool isWearingSkates,
            IceSkatesConfig config)
        {
            if (!isOnIce || !isWearingSkates)
            {
                // Apply off-ice penalty if wearing skates
                if (isWearingSkates && config.EnableOffIcePenalty)
                {
                    // Severely limit speed
                    double maxSpeed = 0.05; // Very slow
                    double speed = Math.Sqrt(pos.Motion.X * pos.Motion.X + pos.Motion.Z * pos.Motion.Z);

                    if (speed > maxSpeed)
                    {
                        double factor = maxSpeed / speed;
                        pos.Motion.X *= factor;
                        pos.Motion.Z *= factor;
                    }
                }
                return;
            }

            // On ice with skates - allow faster skating
            // The speed multiplier is relative to base walk speed
            // This is a cap, not a boost - physics naturally accelerate
            // Just prevent exceeding configured maximum

            double baseWalkSpeed = 0.05; // Approximate base walk speed
            double maxSkateSpeed = baseWalkSpeed * config.MaxSkatingSpeedMultiplier;

            double currentSpeed = Math.Sqrt(pos.Motion.X * pos.Motion.X + pos.Motion.Z * pos.Motion.Z);

            if (currentSpeed > maxSkateSpeed)
            {
                double factor = maxSkateSpeed / currentSpeed;
                pos.Motion.X *= factor;
                pos.Motion.Z *= factor;
            }
        }
    }
}
