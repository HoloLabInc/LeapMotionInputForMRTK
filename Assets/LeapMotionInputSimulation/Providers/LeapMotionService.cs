using Leap;
using Leap.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.WindowsMixedReality.Input
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.WindowsEditor,
        "Leap Motion Input Simulator")]
    public class LeapMotionService : BaseInputDeviceManager
    {
        public LeapMotionService(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            MixedRealityInputSystemProfile inputSystemProfile,
            Transform playspace,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(registrar, inputSystem, inputSystemProfile, playspace, name, priority, profile) { }

        private Dictionary<int, LeapMotionHand> trackedHands = new Dictionary<int, LeapMotionHand>();
        private Dictionary<int, SkinnedMeshRenderer> handMeshRenderers = new Dictionary<int, SkinnedMeshRenderer>();
        private IMixedRealityController[] activeControllers = new IMixedRealityController[0];

        private LeapProvider leapProvider;
        private HandModelManager handModelManager;

        public override IMixedRealityController[] GetActiveControllers()
        {
            return activeControllers;
        }

        /// <inheritdoc />
        public override void Enable()
        {
            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }

            if (leapProvider == null)
            {
                Debug.LogError("Leap Provider not found");
                return;
            }

            leapProvider.OnUpdateFrame += OnUpdateFrame;

            if(handModelManager == null)
            {
                handModelManager = leapProvider.GetComponentInChildren<HandModelManager>();
            }

            if (handModelManager == null)
            {
                Debug.LogWarning("Hand Model Manager not found");
                return;
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            leapProvider.OnUpdateFrame -= OnUpdateFrame;
            RemoveAllHandDevices();
        }

        private void OnUpdateFrame(Frame frame)
        {
            foreach (var hand in frame.Hands)
            {
                var controller = GetOrAddHand(hand);
                controller.UpdateController(hand);

                if (handModelManager)
                {
                    SkinnedMeshRenderer skinnedMeshRenderer;
                    if (handMeshRenderers.ContainsKey(hand.Id))
                    {
                        skinnedMeshRenderer = handMeshRenderers[hand.Id];
                    }
                    else
                    {
                        var handModel = handModelManager.GetHandModel<HandModelBase>(hand.Id);
                        skinnedMeshRenderer = handModel?.GetComponentInChildren<SkinnedMeshRenderer>();

                        if (skinnedMeshRenderer != null)
                        {
                            // hide original mesh
                            skinnedMeshRenderer.materials = new Material[0];
                            handMeshRenderers.Add(hand.Id, skinnedMeshRenderer);
                        }
                    }

                    // update hand mesh
                    var mesh = new Mesh();
                    skinnedMeshRenderer?.BakeMesh(mesh);
                    var handMeshInfo = MeshToHandMeshInfo(mesh);

                    if (handMeshInfo != null)
                    {
                        controller.UpdateHandMesh(handMeshInfo);
                    }
                }
            }

            // remove lost hands
            foreach (var handId in trackedHands.Keys.ToArray())
            {
                if (!frame.Hands.Any(h => h.Id == handId))
                {
                    RemoveHandDevice(handId);
                };

                handMeshRenderers.Remove(handId);
            }
        }

        private LeapMotionHand GetOrAddHand(Hand hand)
        {
            var handId = hand.Id;
            var handedness = GetHandedness(hand);

            if (trackedHands.ContainsKey(handId))
            {
                var existingHand = trackedHands[handId];
                if(existingHand.ControllerHandedness == handedness)
                {
                    return existingHand;
                }
                else
                {
                    RemoveHandDevice(handId);
                }
            }

            // Add new hand
            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;
            var inputSource = inputSystem?.RequestNewGenericInputSource($"Leap Motion Hand {handId}", pointers, inputSourceType);

            var controller = new LeapMotionHand(TrackingState.Tracked, handedness, inputSource);
            controller.SetupConfiguration(typeof(LeapMotionHand), inputSourceType);

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            inputSystem?.RaiseSourceDetected(inputSource, controller);

            trackedHands.Add(handId, controller);
            UpdateActiveControllers();

            return controller;
        }

        private void RemoveHandDevice(int handId)
        {
            if (trackedHands.ContainsKey(handId))
            {
                var controller = trackedHands[handId];

                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);

                trackedHands.Remove(handId);
                UpdateActiveControllers();
            }
        }

        private void RemoveAllHandDevices()
        {
            foreach (var controller in trackedHands.Values)
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }
            trackedHands.Clear();
            UpdateActiveControllers();
        }

        private HandMeshInfo MeshToHandMeshInfo(Mesh mesh)
        {
            if(mesh == null || mesh.vertexCount == 0)
            {
                return null;
            }

            HandMeshInfo handMeshInfo = new HandMeshInfo
            {
                vertices = mesh.vertices,
                normals = mesh.normals,
                triangles = mesh.triangles,
                uvs = mesh.uv,
                position = Vector3.zero,
                rotation = Quaternion.identity
            };

            return handMeshInfo;
        }

        private void UpdateActiveControllers()
        {
            activeControllers = trackedHands.Values.ToArray<IMixedRealityController>();
        }

        private Handedness GetHandedness(Hand hand)
        {
            if (hand.IsLeft)
            {
                return Handedness.Left;
            }
            else if (hand.IsRight)
            {
                return Handedness.Right;
            }
            else
            {
                return Handedness.Other;
            }
        }
    }
}
