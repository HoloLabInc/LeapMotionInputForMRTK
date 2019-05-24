using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;
using System;
using Leap;
using UnityEngine;
using System.Linq;

namespace Microsoft.MixedReality.Toolkit.WindowsMixedReality.Input
{
    /// <summary>
    /// A Leap Motion Hand Instance.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class LeapMotionHand : WindowsMixedRealityController, IMixedRealityHand
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="controllerHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public LeapMotionHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, controllerHandedness, inputSource, interactions) { }

        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(2, "Select", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(3, "Grab", AxisType.SingleAxis, DeviceInputType.TriggerPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger, MixedRealityInputAction.None)
        };

        #region IMixedRealityHand Implementation

        /// <inheritdoc/>
        public bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose)
        {
            return unityJointPoses.TryGetValue(joint, out pose);
        }

        #endregion IMixedRealityHand Implementation

        public override bool IsInPointingPose
        {
            get
            {
                bool valid = true;
                var palmNormal = GetPalmNormal();
                if (CursorBeamBackwardTolerance >= 0)
                {
                    Vector3 cameraBackward = -CameraCache.Main.transform.forward;
                    if (Vector3.Dot(palmNormal.normalized, cameraBackward) > CursorBeamBackwardTolerance)
                    {
                        valid = false;
                    }
                }
                if (valid && CursorBeamUpTolerance >= 0)
                {
                    if (Vector3.Dot(palmNormal, Vector3.up) > CursorBeamUpTolerance)
                    {
                        valid = false;
                    }
                }
                return valid;
            }
        }

        private readonly Dictionary<TrackedHandJoint, MixedRealityPose> unityJointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private readonly HandRay handRay = new HandRay();

        private readonly float CursorBeamBackwardTolerance = 0.5f;
        private readonly float CursorBeamUpTolerance = 0.8f;

        private readonly float StartPinchingDistance = 0.04f;
        private readonly float StopPinchingDistance = 0.05f;


        private bool isPinching = false;
        protected bool IsPinching
        {
            get
            {
                var gotThumb = TryGetJoint(TrackedHandJoint.ThumbTip, out var thumbTipPose);
                var gotIndex = TryGetJoint(TrackedHandJoint.IndexTip, out var indexTipPose);

                if (gotThumb && gotIndex)
                {
                    var distance = Vector3.Distance(thumbTipPose.Position, indexTipPose.Position);

                    if (isPinching && distance > StopPinchingDistance)
                    {
                        isPinching = false;
                    }
                    else if(!isPinching && distance < StartPinchingDistance)
                    {
                        isPinching = true;
                    }
                }
                else
                {
                    isPinching = false;
                }

                return isPinching;
            }
        }

        #region Update data functions

        public void UpdateController(Hand hand)
        {
            if (!Enabled) { return; }

            UpdateControllerData(hand);

            if (Interactions == null)
            {
                Debug.LogError($"No interaction configuration for Windows Mixed Reality Articulated Hand {ControllerHandedness}");
                Enabled = false;
            }

            UpdateCurrentPoses();

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.None:
                        break;
                    case DeviceInputType.SpatialPointer:
                        UpdatePointerData(hand, Interactions[i]);
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.TriggerPress:
                        UpdateTriggerData(hand, Interactions[i]);
                        break;
                    case DeviceInputType.SpatialGrip:
                         UpdateGripData(hand, Interactions[i]);
                        break;
                    case DeviceInputType.IndexFinger:
                        UpdateIndexFingerData(hand, Interactions[i]);
                        break;
                    default:
                        Debug.LogError($"Input [{Interactions[i].InputType}] is not handled for this controller [WindowsMixedRealityArticulatedHand]");
                        Enabled = false;
                        break;
                }
            }
        }
        
        private void UpdateTriggerData(Hand hand, MixedRealityInteractionMapping interactionMapping)
        {
            interactionMapping.BoolData = IsPinching;

            if (interactionMapping.Changed)
            {
                if (interactionMapping.BoolData)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
                else
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                }
            }
        }

        private void UpdateGripData(Hand hand, MixedRealityInteractionMapping interactionMapping)
        {
            interactionMapping.PoseData = currentGripPose;

            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentGripPose);
            }
        }

        protected void UpdatePointerData(Hand hand, MixedRealityInteractionMapping interactionMapping)
        {
            interactionMapping.PoseData = currentPointerPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentPointerPose);
            }
        }

        private void UpdateIndexFingerData(Hand hand, MixedRealityInteractionMapping interactionMapping)
        {
            interactionMapping.PoseData = currentIndexPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                MixedRealityToolkit.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentIndexPose);
            }
        }

        protected void UpdateCurrentPoses()
        {
            var gotIndex = TryGetJoint(TrackedHandJoint.IndexTip, out var indexTipPose);
            var gotPalm = TryGetJoint(TrackedHandJoint.Palm, out var palmPose);

            if (!gotIndex || !gotPalm)
            {
                return;
            }

            // update index pose
            currentIndexPose.Position = indexTipPose.Position;
            currentIndexPose.Rotation = indexTipPose.Rotation;

            // update grip pose
            currentGripPose.Position = palmPose.Position;
            currentGripPose.Rotation = palmPose.Rotation;
            MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentGripPose);

            // update pointer pose;
            var pointerOrigin = palmPose.Position;
            handRay.Update(pointerOrigin, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);
            Ray ray = handRay.Ray;

            currentPointerPose.Position = ray.origin;
            currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);
        }

        protected Vector3 GetPalmNormal()
        {
            if (TryGetJoint(TrackedHandJoint.Palm, out MixedRealityPose pose))
            {
                return -pose.Up;
            }
            return Vector3.zero;
        }

        protected TrackedHandJoint ConvertBoneToTrackedHandJoint(Finger.FingerType fingerType, Bone.BoneType boneType)
        {
            switch (fingerType)
            {
                case Finger.FingerType.TYPE_THUMB:
                    switch (boneType)
                    {
                        case Bone.BoneType.TYPE_DISTAL:
                            return TrackedHandJoint.ThumbDistalJoint;
                        case Bone.BoneType.TYPE_INTERMEDIATE:
                            return TrackedHandJoint.ThumbProximalJoint;
                        case Bone.BoneType.TYPE_PROXIMAL:
                            return TrackedHandJoint.ThumbMetacarpalJoint;
                        default:
                            return TrackedHandJoint.None;
                    }
                case Finger.FingerType.TYPE_INDEX:
                    switch (boneType)
                    {
                        case Bone.BoneType.TYPE_DISTAL:
                            return TrackedHandJoint.IndexDistalJoint;
                        case Bone.BoneType.TYPE_INTERMEDIATE:
                            return TrackedHandJoint.IndexMiddleJoint;
                        case Bone.BoneType.TYPE_PROXIMAL:
                            return TrackedHandJoint.IndexKnuckle;
                        case Bone.BoneType.TYPE_METACARPAL:
                            return TrackedHandJoint.IndexMetacarpal;
                        default:
                            return TrackedHandJoint.None;
                    }
                case Finger.FingerType.TYPE_MIDDLE:
                    switch (boneType)
                    {
                        case Bone.BoneType.TYPE_DISTAL:
                            return TrackedHandJoint.MiddleDistalJoint;
                        case Bone.BoneType.TYPE_INTERMEDIATE:
                            return TrackedHandJoint.MiddleMiddleJoint;
                        case Bone.BoneType.TYPE_PROXIMAL:
                            return TrackedHandJoint.MiddleKnuckle;
                        case Bone.BoneType.TYPE_METACARPAL:
                            return TrackedHandJoint.MiddleMetacarpal;
                        default:
                            return TrackedHandJoint.None;
                    }

                case Finger.FingerType.TYPE_RING:
                    switch (boneType)
                    {
                        case Bone.BoneType.TYPE_DISTAL:
                            return TrackedHandJoint.RingDistalJoint;
                        case Bone.BoneType.TYPE_INTERMEDIATE:
                            return TrackedHandJoint.RingMiddleJoint;
                        case Bone.BoneType.TYPE_PROXIMAL:
                            return TrackedHandJoint.RingKnuckle;
                        case Bone.BoneType.TYPE_METACARPAL:
                            return TrackedHandJoint.RingMetacarpal;
                        default:
                            return TrackedHandJoint.None;
                    }
                case Finger.FingerType.TYPE_PINKY:
                    switch (boneType)
                    {
                        case Bone.BoneType.TYPE_DISTAL:
                            return TrackedHandJoint.PinkyDistalJoint;
                        case Bone.BoneType.TYPE_INTERMEDIATE:
                            return TrackedHandJoint.PinkyMiddleJoint;
                        case Bone.BoneType.TYPE_PROXIMAL:
                            return TrackedHandJoint.PinkyKnuckle;
                        case Bone.BoneType.TYPE_METACARPAL:
                            return TrackedHandJoint.PinkyMetacarpal;
                        default:
                            return TrackedHandJoint.None;
                    }
                default:
                    return TrackedHandJoint.None;
            }
        }
        
        protected TrackedHandJoint ConvertFingerToTrackedHandJointTip(Finger.FingerType fingerType)
        {
            switch (fingerType)
            {
                case Finger.FingerType.TYPE_THUMB:
                    return TrackedHandJoint.ThumbTip;
                case Finger.FingerType.TYPE_INDEX:
                    return TrackedHandJoint.IndexTip;
                case Finger.FingerType.TYPE_MIDDLE:
                    return TrackedHandJoint.MiddleTip;
                case Finger.FingerType.TYPE_RING:
                    return TrackedHandJoint.RingTip;
                case Finger.FingerType.TYPE_PINKY:
                    return TrackedHandJoint.PinkyTip;
                default:
                    return TrackedHandJoint.None;
            }
        }

        protected void UpdateJointPose(Finger.FingerType fingerType, Bone bone)
        {
            TrackedHandJoint joint = ConvertBoneToTrackedHandJoint(fingerType, bone.Type);
            if(joint == TrackedHandJoint.None)
            {
                return;
            }

            var rotation = bone.Rotation.ToQuaternion();
            var position = bone.PrevJoint.ToVector3();
            UpdateJointPose(joint, position, rotation);

            // Update finger tip
            if (bone.Type == Bone.BoneType.TYPE_DISTAL)
            {
                joint = ConvertFingerToTrackedHandJointTip(fingerType);
                if (joint == TrackedHandJoint.None)
                {
                    return;
                }
                position = bone.NextJoint.ToVector3();
                UpdateJointPose(joint, position, rotation);
            }
        }

        protected void UpdateJointPose(TrackedHandJoint joint, Vector3 position, Quaternion rotation)
        {
            var pose = new MixedRealityPose(position, rotation);

            if (!unityJointPoses.ContainsKey(joint))
            {
                unityJointPoses.Add(joint, pose);
            }
            else
            {
                unityJointPoses[joint] = pose;
            }
        }

        protected void UpdateControllerData(Hand hand)
        {
            var fingers = hand.Fingers;
            foreach(var finger in fingers)
            {
                foreach(var bone in finger.bones)
                {
                    UpdateJointPose(finger.Type, bone);
                }
            }

            var palmRotation = Quaternion.LookRotation(hand.Direction.ToVector3(), -hand.PalmNormal.ToVector3());

            UpdateJointPose(TrackedHandJoint.Palm, hand.PalmPosition.ToVector3(), palmRotation);
            UpdateJointPose(TrackedHandJoint.Wrist, hand.WristPosition.ToVector3(), palmRotation);

            MixedRealityToolkit.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, unityJointPoses);
        }

        #endregion Update data functions
    }

    public static class LeapMotionExtension
    {
        public static Quaternion ToQuaternion(this LeapQuaternion q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        public static Vector3 ToVector3(this Vector v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }
}
