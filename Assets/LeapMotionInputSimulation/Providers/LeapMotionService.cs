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
        private IMixedRealityController[] activeControllers = new IMixedRealityController[0];

        private LeapProvider leapProvider;

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

            if (leapProvider != null)
            {
                leapProvider.OnUpdateFrame += OnUpdateFrame;
            }
            else
            {
                Debug.LogError("Leap Provider not found");
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
            }

            // remove lost hands
            foreach (var handId in trackedHands.Keys.ToArray())
            {
                if (!frame.Hands.Any(h => h.Id == handId))
                {
                    RemoveHandDevice(handId);
                };
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
