using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;
using System;
using Leap;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.WindowsMixedReality.Input
{
    /// <summary>
    /// A Leap Motion Hand Instance.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class LeapMotionHand : SimulatedHand
    {
        public override HandSimulationMode SimulationMode => HandSimulationMode.Articulated;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose lastPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose lastGripPose = MixedRealityPose.ZeroIdentity;

        private SimulatedHandData handData = new SimulatedHandData();

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

        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
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

            handData.Update(true, IsPinching, (joints) => { });
            UpdateInteractions(handData);
        }

        protected override void UpdateInteractions(SimulatedHandData handData)
        {
            lastPointerPose = currentPointerPose;
            lastGripPose = currentGripPose;

            Vector3 pointerPosition = jointPoses[TrackedHandJoint.Palm].Position;
            IsPositionAvailable = IsRotationAvailable = pointerPosition != Vector3.zero;

            if (IsPositionAvailable)
            {
                HandRay.Update(pointerPosition, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);

                Ray ray = HandRay.Ray;

                currentPointerPose.Position = ray.origin;
                currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);

                currentGripPose = jointPoses[TrackedHandJoint.Palm];
                currentIndexPose = jointPoses[TrackedHandJoint.IndexTip];
            }

            if (lastGripPose != currentGripPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentGripPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    CoreServices.InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentPointerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    CoreServices.InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentPointerRotation);
                }
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = currentPointerPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentPointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = currentGripPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentGripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                        Interactions[i].BoolData = handData.IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.TriggerPress:
                        Interactions[i].BoolData = handData.IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        Interactions[i].PoseData = currentIndexPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentIndexPose);
                        }
                        break;
                }
            }
        }

        public void UpdateHandMesh(HandMeshInfo handMeshInfo)
        {
            CoreServices.InputSystem?.RaiseHandMeshUpdated(InputSource, ControllerHandedness, handMeshInfo);
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

            if (!jointPoses.ContainsKey(joint))
            {
                jointPoses.Add(joint, pose);
            }
            else
            {
                jointPoses[joint] = pose;
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

            CoreServices.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);
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
