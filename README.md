# LeapMotionInputForMRTK
LeapMotionInputForMRTK simulates hand inputs for MRTK with Leap Motion.

## Demo video
[![Demo video](http://img.youtube.com/vi/kS4M85WSqDM/0.jpg)](http://www.youtube.com/watch?v=kS4M85WSqDM)


## Prerequisites
- Unity 2018.4.x
- HoloLens (1st gen)
- Leap Motion
- Leap Motion SDK (https://developer.leapmotion.com/get-started)

## Getting Started

### 1.A. Clone this repository
```
> git clone git@github.com:HoloLabInc/LeapMotionInputForMRTK.git --recursive
> cd LeapMotionInputForMRTK
> External\createSymlink.bat
```

Open LeapMotionInputForMRTK project with Unity 2018.4.x.

### 1.B. Import unitypackage
Create project with Unity 2018.4.x.

#### Import MRTK v2
Download and import MRTK v2 unitypackages.  
(https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/v2.3.0)

- Microsoft.MixedReality.Toolkit.Unity.Foundation.2.3.0.unitypackage
- Microsoft.MixedReality.Toolkit.Unity.Extensions.2.3.0.unitypackage
- Microsoft.MixedReality.Toolkit.Unity.Tools.2.3.0.unitypackage
- Microsoft.MixedReality.Toolkit.Unity.Examples.2.3.0.unitypackage

#### Import LeapMotionInputForMRTK
Download and import the latest LeapMotionInputForMRTK unitypackage.  
(https://github.com/HoloLabInc/LeapMotionInputForMRTK/releases)


### 2. Import Leap Motion Core Assets
Download "Unity Core Assets" and import to the unity project.  
(https://developer.leapmotion.com/unity/#5436356)


### 3.A. Open sample scene
Open `Assets/LeapMotionInputSimulation/Scenes/SampleScene.unity`.

### 3.B. Setup scenes
#### MRTK Configuration
Add Leap Motion Input Simulator to Input Data Providers of your MRTK configuration or use LeapMotionMixedRealityToolkitConfigurationProfile.

![image](https://user-images.githubusercontent.com/4415085/77847323-48c5ba00-71f7-11ea-8df8-2d5e66bb1545.png)

#### Add LeapXRServiceProvider
Select "Main Camera" in Hirerarchy and add LeapXRServiceProvider component.

![LeapXRServiceProvider](https://user-images.githubusercontent.com/4415085/58233883-2dd51e80-7d78-11e9-82a3-4a037223d1c9.png)

#### Enable Hand Mesh
If you want to enable Hand Mesh, attach HandModelManager script to "Main Camera".  
Edit the component like the following picture.  
LoPoly Rigged Hand prefabs are in `Assets\LeapMotion\Core\Prefabs\HandModelsNonHuman` folder.

![HandModelmanager](https://user-images.githubusercontent.com/4415085/58534145-65831100-8225-11e9-98fc-8772f3166f4c.png)

### 4. Switch platform
In the Build Settings window, switch platform to Universal Windows Platform.

### 5. Mount Leap Motion on HoloLens (1st gen)
Mount Leap Motion on HoloLens like the following picture.  
[3D Printable Models](https://github.com/HoloLabInc/3dPrintableModels) 

![MountedLeapMotion_small](https://user-images.githubusercontent.com/4415085/58304554-bebb0100-7e2f-11e9-8b74-7bef033bddc6.jpg)

Adjust the offset and tilt values.

![image](https://user-images.githubusercontent.com/4415085/58302558-bdd1a180-7e26-11e9-8282-82a57d88c052.png)

### 6. Play in Unity Editor
On your HoloLens, start the Holographic Remoting Player app.

In Unity, select "Window > XR > Holographic Emulation".  
Enter the IP address of your HoloLens and press Connect button.

![HolographicRemoting](https://user-images.githubusercontent.com/4415085/58303095-8284a200-7e29-11e9-8efe-dbe88019b629.png)

Press Play button in Unity Editor.

## Author
Furuta, Yusuke ([@tarukosu](https://twitter.com/tarukosu))

## License
MIT
