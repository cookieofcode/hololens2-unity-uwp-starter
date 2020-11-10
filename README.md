# HoloLens 2 Unity UWP Starter
This repository provides a template for creating a HoloLens 2 Unity application using Universal Windows Platform (UWP) capabilities. The project was built using the following versions: 
 - Unity Version: __Unity 2019.4.11f1__.
 - Mixed Reality Toolkit Version: __MRTK 2.4.0__.

## Overview
You can use this repository to get started with HoloLens 2 development with Unity and UWP. The Unity Editor has no Windows Runitme support, so in Unity project's scripts you can use the `ENABLE_WINMD_SUPPORT` #define directive (see [IL2CPP-WindowsRuntimeSupport](https://docs.unity3d.com/Manual/IL2CPP-WindowsRuntimeSupport.html)).

Some UWP features, such as the HoloLens 2 camera access, can not be used in the HoloLens 2 emulator. This introduces a long cycle until written code in Unity can be visualized, as it has to be deployed using IL2CPP on the HoloLens 2.

To ease development effort and provide faster feedback, this starter combines resources of a C# holographic DirectX project and Unity using MRTK in a shared resources folder. This enables fast UWP dependent development using the C# project of resources, that can be used in the Unity solution.

This starter demonstrates the Microsoft [Holographic face tracking sample](https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/HolographicFaceTracking). It shows how to acquire video frames from the camera and use the UWP FaceAnalysis API to determine if there are any faces in front of the HoloLens and a display a cube on top of the detected face.

## Holographic DirectX project
The C# project is created using the [Windows Mixed Reality App Templates](https://marketplace.visualstudio.com/items?itemName=WindowsMixedRealityteam.WindowsMixedRealityAppTemplatesVSIX) available in VisualStudio. The face tracking visualization is converted to C# from [Holographic face tracking sample](https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/HolographicFaceTracking).

## Unity project
The Unity project contains the [MixedRealityToolkit (MRTK)](https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/GettingStartedWithTheMRTK.html).

## Shared Resources
The shared resources contain code for both Unity and C# projects. The provided `createSymlink.bat` creates the symlink of the folder in the solutions. Before executing, ensure that the following folders exist:
- .\UWP\Shared (The Shared Resources folder)
- .\Unity\Assets\Scripts
- .\UWP\CSharp\

## Acknowledgment
The provided sample code origins from https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/HolographicFaceTracking and is licensed under the MIT license. The code has been converted from C++ to C#.