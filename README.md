# LeapMotionInputForMRTK
LeapMotionInputForMRTK simulates hand inputs for MRTK with Leap Motion.

## Demo video
[![Demo video](http://img.youtube.com/vi/jJw2ULaQsXM/0.jpg)](http://www.youtube.com/watch?v=jJw2ULaQsXM)


# Prerequisites
- Unity 2018.3.x
- HoloLens (1st gen)
- Leap Motion
- Leap Motion SDK (https://developer.leapmotion.com/get-started)

# Getting Started

## 1.A. Clone this repository
```
> git clone git@github.com:HoloLabInc/LeapMotionInputForMRTK.git --recursive
> cd LeapMotionInputForMRTK
> External\createSymlink.bat
```

Open LeapMotionInputForMRTK project with Unity 2018.3.x.

## 1.B. Import unitypackage
Create project with Unity 2018.3.x.

### Import MRTK v2
Download and import MRTK v2 unitypackages.  
(https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/v2.0.0-RC1-Refresh)

- Microsoft.MixedReality.Toolkit.Unity.Examples-v2.0.0-RC1-Refresh.unitypackage
- Microsoft.MixedReality.Toolkit.Unity.Foundation-v2.0.0-RC1-Refresh.unitypackage

### Import LeapMotionInputForMRTK
Download and import LeapMotionInputForMRTK unitypackage.  
(https://github.com/HoloLabInc/LeapMotionInputForMRTK/releases/tag/v0.1)


## 2. Import Leap Motion Core Assets
Download "Unity Core Assets" and import to the unity project.  
(https://developer.leapmotion.com/unity/#5436356)


## 3.A. Open sample scene
Open Assets/LeapMotionInputSimulation/Scenes/SampleScene.

## 3.B. Create new scene
When creating new scene, MRTK settings window pops up.  
Drag and drop Assets\LeapMotionInputSimulation\LeapMotionMixedRealityToolkitConfigurationProfile.asset to the window and press "Yes".

![SceneSetting](https://user-images.githubusercontent.com/4415085/58233879-2ada2e00-7d78-11e9-81e7-09c0e68ac23a.png)

After the scene is loaded, select "Main Camera" in Hirerarchy and add "Leap XR Service Provider" component.

![LeapXRServiceProvider](https://user-images.githubusercontent.com/4415085/58233883-2dd51e80-7d78-11e9-82a3-4a037223d1c9.png)

## 4. Switch platform
In the Build Settings window, switch platform to Universal Windows Platform.

## 5. Mount Leap Motion on HoloLens (1st gen)
Mount Leap Motion on HoloLens like the following picture.

![MountedLeapMotion_small](https://user-images.githubusercontent.com/4415085/58304554-bebb0100-7e2f-11e9-8b74-7bef033bddc6.jpg)

Adjust the offset and tilt values.

![image](https://user-images.githubusercontent.com/4415085/58302558-bdd1a180-7e26-11e9-8282-82a57d88c052.png)

## 6. Play in Unity Editor
On your HoloLens, start the Holographic Remoting Player app.

In Unity, select "Window > XR > Holographic Emulation".  
Enter the IP address of your HoloLens and press Connect button.

![HolographicRemoting](https://user-images.githubusercontent.com/4415085/58303095-8284a200-7e29-11e9-8efe-dbe88019b629.png)

Press Play button in Unity Editor.

# License
MIT
