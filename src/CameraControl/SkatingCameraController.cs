using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using IceSkates.src.Config;
using IceSkates.src.Physics;

namespace IceSkates.src.CameraControl
{
    /// <summary>
    /// skating camera controller - activates third-person shoulder cam when wearing skates
    /// adapted from TrueThirdPerson mod
    /// </summary>
    public class SkatingCameraController
    {
        private ICoreClientAPI? clientApi;
        private static bool cameraOverrideByPlayer = false;

        public static double CameraXPosition { get; set; } = 1.5;
        public static double CameraYPosition { get; set; } = 0.0;

        /// <summary>
        /// should the camera override be active?
        /// true when wearing skates in third-person (unless overridden)
        /// </summary>
        public static bool OverrideCamera { get; private set; } = false;

        /// <summary>
        /// camera bob controller for stride-synced camera effects
        /// </summary>
        private static CameraBobController? _cameraBobController;

        /// <summary>
        /// get current camera bob values for the Harmony patch
        /// </summary>
        public static (double vertical, double lateral, double roll) GetCameraBob()
        {
            return _cameraBobController?.GetBobValues() ?? (0, 0, 0);
        }

        public void Initialize(ICoreClientAPI api)
        {
            clientApi = api;
            _cameraBobController = new CameraBobController();

            // listen for camera mode changes to detect when player toggles with F5
            api.Event.RegisterGameTickListener(CheckSkatingStatus, 100); // Check every 100ms

            IceSkatesModSystem.Instance.Logger.Notification("[iceskates] Camera controller initialized");
        }

        private void CheckSkatingStatus(float dt)
        {
            if (clientApi?.World?.Player?.Entity == null) return;

            var config = IceSkatesModSystem.Instance.Config;
            var player = clientApi.World.Player.Entity as EntityAgent;

            // check if wearing skates and in third-person
            bool wearingSkates = SkatePhysicsHelper.IsWearingSkates(player);
            bool inThirdPerson = clientApi.World.Player.CameraMode == EnumCameraMode.ThirdPerson;

            // apply shoulder cam effect when wearing skates in third-person (unless overridden)
            if (wearingSkates && inThirdPerson && config.ForceThirdPersonCamera && !cameraOverrideByPlayer)
            {
                OverrideCamera = true;
            }
            else
            {
                OverrideCamera = false;
            }

            // disable vanilla head bobbing when wearing skates (if configured)
            if (player is EntityPlayer entityPlayer && config.DisableVanillaBobWhenSkating)
            {
                if (wearingSkates)
                {
                    // suppress vanilla bobbing completely
                    entityPlayer.HeadBobbingAmplitude = 0f;
                }
                else
                {
                    // restore vanilla bobbing (default is 1.0)
                    entityPlayer.HeadBobbingAmplitude = 1f;
                }
            }

            // update camera bob if wearing skates
            if (wearingSkates && _cameraBobController != null && player != null)
            {
                // calculate current speed in m/s
                double motionX = player.Pos.Motion.X;
                double motionZ = player.Pos.Motion.Z;
                double currentSpeed = Math.Sqrt(motionX * motionX + motionZ * motionZ) * 60.0; // motion is per-tick at 60Hz

                _cameraBobController.Update(player.EntityId, currentSpeed, dt / 1000.0, config);
            }
            else if (_cameraBobController != null)
            {
                _cameraBobController.Reset();
            }
        }

        /// <summary>
        /// allow player to override forced third-person (dev mode)
        /// </summary>
        public static void SetCameraOverride(bool overrideEnabled)
        {
            cameraOverrideByPlayer = overrideEnabled;
            IceSkatesModSystem.Instance.Logger.Notification(
                $"[iceskates] Camera override: {(overrideEnabled ? "ENABLED (player control)" : "DISABLED (auto third-person)")}"
            );
        }
    }

    /// <summary>
    /// harmony patches for camera position modification
    /// creates the Skyrim-style shoulder camera view
    /// based on TrueThirdPerson mod implementation
    /// </summary>
    [HarmonyPatchCategory("iceskates_camera")]
    public static class CameraMatrixPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), nameof(Camera.GetCameraMatrix))]
        public static void GetCameraMatrixStart(ref Camera __instance, Vec3d camEyePosIn, Vec3d worldPos,
            double yaw, double pitch, AABBIntersectionTest intersectionTester)
        {
            // get camera bob values (even if override not active, for first-person bob)
            var (bobVertical, bobLateral, bobRoll) = SkatingCameraController.GetCameraBob();

            // apply camera bob if there's any bob value
            bool hasBob = Math.Abs(bobVertical) > 0.001 || Math.Abs(bobLateral) > 0.001;

            if (hasBob)
            {
                // vertical bob
                camEyePosIn[1] += bobVertical;

                // lateral bob (perpendicular to facing direction)
                double sinYaw = Math.Sin(yaw);
                double cosYaw = Math.Cos(yaw);
                camEyePosIn[0] += bobLateral * cosYaw;
                camEyePosIn[2] += bobLateral * sinYaw;

                // note: Roll would require modifying the camera's rotation matrix
                // which is more complex and may not be easily accessible here
            }

            // return early if camera override is not active (shoulder cam)
            if (!SkatingCameraController.OverrideCamera) return;

            // normalize yaw to 0-2π range (using 6.28 as approximation of 2π)
            if (yaw < 0) yaw = 6.28 - (-1 * yaw);
            while (yaw < 0) yaw += 6.28;
            while (yaw >= 6.28) yaw -= 6.28;

            double cameraX = SkatingCameraController.CameraXPosition;
            double cameraY = SkatingCameraController.CameraYPosition;

            // get the percentage between the 2 numbers and desired number
            static double GetPercentage(double value, double minValue, double maxValue)
            {
                if (value <= minValue) return 0.0;
                else if (value >= maxValue) return 100.0;
                else return (value - minValue) / (maxValue - minValue) * 100.0;
            }

            // south to east
            if (yaw < 1.5)
            {
                var percentage = GetPercentage(yaw, 0.0, 1.5);
                camEyePosIn[0] -= cameraX * (1 - percentage / 100); // percentage 0 = max
                camEyePosIn[1] += cameraY;
                camEyePosIn[2] += cameraX * percentage / 100; // percentage 0 = min
            }
            // north to east
            else if (yaw >= 1.5 && yaw <= 3.15)
            {
                var percentage = GetPercentage(yaw, 1.5, 3.15);
                camEyePosIn[0] += cameraX * percentage / 100; // percentage 0 = min
                camEyePosIn[1] += cameraY;
                camEyePosIn[2] += cameraX * (1 - percentage / 100); // percentage 0 = max
            }
            // north to west
            else if (yaw > 3.15 && yaw <= 4.75)
            {
                var percentage = GetPercentage(yaw, 3.15, 4.75);
                camEyePosIn[0] += cameraX * (1 - percentage / 100); // percentage 0 = max
                camEyePosIn[1] += cameraY;
                camEyePosIn[2] -= cameraX * percentage / 100; // percentage 0 = min
            }
            // wouth to west
            else
            {
                var percentage = GetPercentage(yaw, 4.75, 6.28);
                camEyePosIn[0] -= cameraX * percentage / 100; // percentage 0 = min
                camEyePosIn[1] += cameraY;
                camEyePosIn[2] -= cameraX * (1 - percentage / 100); // percentage 0 = max
            }
        }
    }
}
