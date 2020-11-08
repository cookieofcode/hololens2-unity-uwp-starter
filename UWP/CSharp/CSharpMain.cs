﻿//
// Comment out this preprocessor definition to disable all of the
// sample content.
//
// To remove the content after disabling it:
//     * Remove the unused code from this file.
//     * Delete the Content folder provided with this template.
//
#define DRAW_SAMPLE_CONTENT
#define ENABLE_WINMD_SUPPORT

using System;
using Windows.Gaming.Input;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;

using CSharp.Common;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using System.Collections.Generic;
using System.Threading;
using Debug = System.Diagnostics.Debug;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices.Core;
#if DRAW_SAMPLE_CONTENT
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using CSharp.Content;
using CSharp.Shared;
using SDKTemplate;

#endif

namespace CSharp
{
    /// <summary>
    /// Updates, renders, and presents holographic content using Direct3D.
    /// </summary>
    internal class CSharpMain : IDisposable
    {

#if DRAW_SAMPLE_CONTENT
        // Renders a colorful holographic cube that's 20 centimeters wide. This sample content
        // is used to demonstrate world-locked rendering.
        private SpinningCubeRenderer spinningCubeRenderer;

        private SpatialInputHandler spatialInputHandler;

        private VideoFrameProcessor videoFrameProcessor;
#endif

        // Cached reference to device resources.
        private DeviceResources deviceResources;

        // Render loop timer.
        private StepTimer timer = new StepTimer();

        // Represents the holographic space around the user.
        HolographicSpace holographicSpace;

        // SpatialLocator that is attached to the default HolographicDisplay.
        SpatialLocator spatialLocator;

        // A stationary reference frame based on spatialLocator.
        SpatialStationaryFrameOfReference stationaryReferenceFrame;

        private TimeSpan previousFrameTimestamp;

        // Keep track of gamepads.
        private class GamepadWithButtonState
        {
            public Windows.Gaming.Input.Gamepad gamepad;
            public bool buttonAWasPressedLastFrame;
            public GamepadWithButtonState(
                Windows.Gaming.Input.Gamepad gamepad,
                bool buttonAWasPressedLastFrame)
            {
                this.gamepad = gamepad;
                this.buttonAWasPressedLastFrame = buttonAWasPressedLastFrame;
            }
        };
        List<GamepadWithButtonState> gamepads = new List<GamepadWithButtonState>();

        // Keep track of mouse input.
        bool pointerPressed = false;

        // Cache whether or not the HolographicCamera.Display property can be accessed.
        bool canGetHolographicDisplayForCamera = false;

        // Cache whether or not the HolographicDisplay.GetDefault() method can be called.
        bool canGetDefaultHolographicDisplay = false;

        // Cache whether or not the HolographicCameraRenderingParameters.CommitDirect3D11DepthBuffer() method can be called.
        bool canCommitDirect3D11DepthBuffer = false;

        // Cache whether or not the HolographicFrame.WaitForNextFrameReady() method can be called.
        bool canUseWaitForNextFrameReadyAPI = false;

        private bool _cameraIsRunning = false;

        private FaceTrackerProcessor faceTrackerProcessor;

        /// <summary>
        /// Loads and initializes application assets when the application is loaded.
        /// </summary>
        /// <param name="deviceResources"></param>
        public CSharpMain(DeviceResources deviceResources)
        {
            this.deviceResources = deviceResources;

            // Register to be notified if the Direct3D device is lost.
            this.deviceResources.DeviceLost += this.OnDeviceLost;
            this.deviceResources.DeviceRestored += this.OnDeviceRestored;

            // If connected, a game controller can also be used for input.
            Gamepad.GamepadAdded += this.OnGamepadAdded;
            Gamepad.GamepadRemoved += this.OnGamepadRemoved;

            foreach (var gamepad in Gamepad.Gamepads)
            {
                OnGamepadAdded(null, gamepad);
            }

            canGetHolographicDisplayForCamera = ApiInformation.IsPropertyPresent(typeof(HolographicCamera).FullName, "Display");
            canGetDefaultHolographicDisplay = ApiInformation.IsMethodPresent(typeof(HolographicDisplay).FullName, "GetDefault");
            canCommitDirect3D11DepthBuffer = ApiInformation.IsMethodPresent(typeof(HolographicCameraRenderingParameters).FullName, "CommitDirect3D11DepthBuffer");
            canUseWaitForNextFrameReadyAPI = ApiInformation.IsMethodPresent(typeof(HolographicSpace).FullName, "WaitForNextFrameReady");

            if (canGetDefaultHolographicDisplay)
            {
                // Subscribe for notifications about changes to the state of the default HolographicDisplay 
                // and its SpatialLocator.
                HolographicSpace.IsAvailableChanged += this.OnHolographicDisplayIsAvailableChanged;
            }

            // Acquire the current state of the default HolographicDisplay and its SpatialLocator.
            OnHolographicDisplayIsAvailableChanged(null, null);
        }

        public void SetHolographicSpace(HolographicSpace holographicSpace)
        {
            this.holographicSpace = holographicSpace;

            // 
            // TODO: Add code here to initialize your content.
            // 

#if DRAW_SAMPLE_CONTENT
            // Initialize the sample hologram.
            spinningCubeRenderer = new SpinningCubeRenderer(deviceResources);

            spatialInputHandler = new SpatialInputHandler();
#endif

            // Respond to camera added events by creating any resources that are specific
            // to that camera, such as the back buffer render target view.
            // When we add an event handler for CameraAdded, the API layer will avoid putting
            // the new camera in new HolographicFrames until we complete the deferral we created
            // for that handler, or return from the handler without creating a deferral. This
            // allows the app to take more than one frame to finish creating resources and
            // loading assets for the new holographic camera.
            // This function should be registered before the app creates any HolographicFrames.
            holographicSpace.CameraAdded += this.OnCameraAdded;

            // Respond to camera removed events by releasing resources that were created for that
            // camera.
            // When the app receives a CameraRemoved event, it releases all references to the back
            // buffer right away. This includes render target views, Direct2D target bitmaps, and so on.
            // The app must also ensure that the back buffer is not attached as a render target, as
            // shown in DeviceResources.ReleaseResourcesForBackBuffer.
            holographicSpace.CameraRemoved += this.OnCameraRemoved;

            // Notes on spatial tracking APIs:
            // * Stationary reference frames are designed to provide a best-fit position relative to the
            //   overall space. Individual positions within that reference frame are allowed to drift slightly
            //   as the device learns more about the environment.
            // * When precise placement of individual holograms is required, a SpatialAnchor should be used to
            //   anchor the individual hologram to a position in the real world - for example, a point the user
            //   indicates to be of special interest. Anchor positions do not drift, but can be corrected; the
            //   anchor will use the corrected position starting in the next frame after the correction has
            //   occurred.
        }

        public void Dispose()
        {
#if DRAW_SAMPLE_CONTENT
            if (spinningCubeRenderer != null)
            {
                spinningCubeRenderer.Dispose();
                spinningCubeRenderer = null;
            }
#endif
        }

        /// <summary>
        /// Updates the application state once per frame.
        /// </summary>
        public HolographicFrame Update(HolographicFrame previousFrame)
        {
            // TODO: Put CPU work that does not depend on the HolographicCameraPose here.

            // Apps should wait for the optimal time to begin pose-dependent work.
            // The platform will automatically adjust the wakeup time to get
            // the lowest possible latency at high frame rates. For manual
            // control over latency, use the WaitForNextFrameReadyWithHeadStart 
            // API.
            // WaitForNextFrameReady and WaitForNextFrameReadyWithHeadStart are the
            // preferred frame synchronization APIs for Windows Mixed Reality. When 
            // running on older versions of the OS that do not include support for
            // these APIs, your app can use the WaitForFrameToFinish API for similar 
            // (but not as optimal) behavior.
            if (canUseWaitForNextFrameReadyAPI)
            {
                try
                {
                    holographicSpace.WaitForNextFrameReady();
                }
                catch (NotImplementedException)
                {
                    // Catch a specific case where WaitForNextFrameReady() is present but not implemented
                    // and default back to WaitForFrameToFinish() in that case.
                    canUseWaitForNextFrameReadyAPI = false;
                }
            }
            else if (previousFrame != null)
            {
                previousFrame.WaitForFrameToFinish();
            }

            // Before doing the timer update, there is some work to do per-frame
            // to maintain holographic rendering. First, we will get information
            // about the current frame.

            // The HolographicFrame has information that the app needs in order
            // to update and render the current frame. The app begins each new
            // frame by calling CreateNextFrame.
            HolographicFrame holographicFrame = holographicSpace.CreateNextFrame();

            // Get a prediction of where holographic cameras will be when this frame
            // is presented.
            HolographicFramePrediction prediction = holographicFrame.CurrentPrediction;

            // Back buffers can change from frame to frame. Validate each buffer, and recreate
            // resource views and depth buffers as needed.
            deviceResources.EnsureCameraResources(holographicFrame, prediction);

#if DRAW_SAMPLE_CONTENT
            if (stationaryReferenceFrame != null)
            {
                // Check for new input state since the last frame.
                for (int i = 0; i < gamepads.Count; ++i)
                {
                    bool buttonDownThisUpdate = (gamepads[i].gamepad.GetCurrentReading().Buttons & GamepadButtons.A) == GamepadButtons.A;
                    if (buttonDownThisUpdate && !gamepads[i].buttonAWasPressedLastFrame)
                    {
                        pointerPressed = true;
                    }
                    gamepads[i].buttonAWasPressedLastFrame = buttonDownThisUpdate;
                }

                SpatialInteractionSourceState pointerState = spatialInputHandler.CheckForInput();
                SpatialPointerPose pose = null;
                if (null != pointerState)
                {
                    pose = pointerState.TryGetPointerPose(stationaryReferenceFrame.CoordinateSystem);
                }
                else if (pointerPressed)
                {
                    pose = SpatialPointerPose.TryGetAtTimestamp(stationaryReferenceFrame.CoordinateSystem, prediction.Timestamp);
                }
                pointerPressed = false;

                // When a Pressed gesture is detected, the sample hologram will be repositioned
                // two meters in front of the user.
                // spinningCubeRenderer.PositionHologram(pose); // TODO Temporarily disabled

                SpatialCoordinateSystem currentCoordinateSystem = stationaryReferenceFrame.CoordinateSystem;

                if (videoFrameProcessor != null && faceTrackerProcessor != null)
                {
                    bool isTrackingFaces = faceTrackerProcessor.IsTrackingFaces();
                    if (isTrackingFaces)
                    {
                        MediaFrameReference frame = videoFrameProcessor.GetLatestFrame(); // can return null
                        if (frame != null)
                        {
                            var faces = faceTrackerProcessor.GetLatestFaces();
                            ProcessFaces(faces, frame, currentCoordinateSystem);

                            TimeSpan currentTimeStamp = frame.SystemRelativeTime.Value.Duration();
                            if (currentTimeStamp > previousFrameTimestamp)
                            {
                                previousFrameTimestamp = currentTimeStamp;
                            }
                        }

                    }
                }



            }
#endif

            timer.Tick(() =>
            {
                //
                // TODO: Update scene objects.
                //
                // Put time-based updates here. By default this code will run once per frame,
                // but if you change the StepTimer to use a fixed time step this code will
                // run as many times as needed to get to the current step.
                //

#if DRAW_SAMPLE_CONTENT
                spinningCubeRenderer.Update(timer);
#endif
            });

            // On HoloLens 2, the platform can achieve better image stabilization results if it has
            // a stabilization plane and a depth buffer.
            // Note that the SetFocusPoint API includes an override which takes velocity as a 
            // parameter. This is recommended for stabilizing holograms in motion.
            foreach (var cameraPose in prediction.CameraPoses)
            {
#if DRAW_SAMPLE_CONTENT
                // The HolographicCameraRenderingParameters class provides access to set
                // the image stabilization parameters.
                HolographicCameraRenderingParameters renderingParameters = holographicFrame.GetRenderingParameters(cameraPose);

                // SetFocusPoint informs the system about a specific point in your scene to
                // prioritize for image stabilization. The focus point is set independently
                // for each holographic camera. When setting the focus point, put it on or 
                // near content that the user is looking at.
                // In this example, we put the focus point at the center of the sample hologram.
                // You can also set the relative velocity and facing of the stabilization
                // plane using overloads of this method.
                if (stationaryReferenceFrame != null)
                {
                    renderingParameters.SetFocusPoint(
                        stationaryReferenceFrame.CoordinateSystem,
                        spinningCubeRenderer.Position
                        );
                }
#endif
            }

            // The holographic frame will be used to get up-to-date view and projection matrices and
            // to present the swap chain.
            return holographicFrame;
        }

        /// <summary>
        /// Renders the current frame to each holographic display, according to the 
        /// current application and spatial positioning state. Returns true if the 
        /// frame was rendered to at least one display.
        /// </summary>
        public bool Render(HolographicFrame holographicFrame)
        {
            // Don't try to render anything before the first Update.
            if (timer.FrameCount == 0)
            {
                return false;
            }

            //
            // TODO: Add code for pre-pass rendering here.
            //
            // Take care of any tasks that are not specific to an individual holographic
            // camera. This includes anything that doesn't need the final view or projection
            // matrix, such as lighting maps.
            //

            // Up-to-date frame predictions enhance the effectiveness of image stablization and
            // allow more accurate positioning of holograms.
            holographicFrame.UpdateCurrentPrediction();
            HolographicFramePrediction prediction = holographicFrame.CurrentPrediction;

            // Lock the set of holographic camera resources, then draw to each camera
            // in this frame.
            return deviceResources.UseHolographicCameraResources(
                (Dictionary<uint, CameraResources> cameraResourceDictionary) =>
            {
                bool atLeastOneCameraRendered = false;

                foreach (var cameraPose in prediction.CameraPoses)
                {
                    // This represents the device-based resources for a HolographicCamera.
                    CameraResources cameraResources = cameraResourceDictionary[cameraPose.HolographicCamera.Id];

                    // Get the device context.
                    var context = deviceResources.D3DDeviceContext;
                    var renderTargetView = cameraResources.BackBufferRenderTargetView;
                    var depthStencilView = cameraResources.DepthStencilView;

                    // Set render targets to the current holographic camera.
                    context.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);

                    // Clear the back buffer and depth stencil view.
                    if (canGetHolographicDisplayForCamera &&
                        cameraPose.HolographicCamera.Display.IsOpaque)
                    {
                        SharpDX.Mathematics.Interop.RawColor4 cornflowerBlue = new SharpDX.Mathematics.Interop.RawColor4(0.392156899f, 0.58431375f, 0.929411829f, 1.0f);
                        context.ClearRenderTargetView(renderTargetView, cornflowerBlue);
                    }
                    else
                    {
                        SharpDX.Mathematics.Interop.RawColor4 transparent = new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f);
                        context.ClearRenderTargetView(renderTargetView, transparent);
                    }
                    context.ClearDepthStencilView(
                        depthStencilView,
                        SharpDX.Direct3D11.DepthStencilClearFlags.Depth | SharpDX.Direct3D11.DepthStencilClearFlags.Stencil,
                        1.0f,
                        0);

                    //
                    // TODO: Replace the sample content with your own content.
                    //
                    // Notes regarding holographic content:
                    //    * For drawing, remember that you have the potential to fill twice as many pixels
                    //      in a stereoscopic render target as compared to a non-stereoscopic render target
                    //      of the same resolution. Avoid unnecessary or repeated writes to the same pixel,
                    //      and only draw holograms that the user can see.
                    //    * To help occlude hologram geometry, you can create a depth map using geometry
                    //      data obtained via the surface mapping APIs. You can use this depth map to avoid
                    //      rendering holograms that are intended to be hidden behind tables, walls,
                    //      monitors, and so on.
                    //    * On HolographicDisplays that are transparent, black pixels will appear transparent 
                    //      to the user. On such devices, you should clear the screen to Transparent as shown 
                    //      above. You should still use alpha blending to draw semitransparent holograms. 
                    //


                    // The view and projection matrices for each holographic camera will change
                    // every frame. This function refreshes the data in the constant buffer for
                    // the holographic camera indicated by cameraPose.
                    if (stationaryReferenceFrame != null)
                    {
                        cameraResources.UpdateViewProjectionBuffer(deviceResources, cameraPose, stationaryReferenceFrame.CoordinateSystem);
                    }

                    // Attach the view/projection constant buffer for this camera to the graphics pipeline.
                    bool cameraActive = cameraResources.AttachViewProjectionBuffer(deviceResources);

#if DRAW_SAMPLE_CONTENT
                    // Only render world-locked content when positional tracking is active.
                    if (cameraActive)
                    {
                        // Draw the sample hologram.
                        spinningCubeRenderer.Render();

                        if (canCommitDirect3D11DepthBuffer)
                        {
                            // On versions of the platform that support the CommitDirect3D11DepthBuffer API, we can 
                            // provide the depth buffer to the system, and it will use depth information to stabilize 
                            // the image at a per-pixel level.
                            HolographicCameraRenderingParameters renderingParameters = holographicFrame.GetRenderingParameters(cameraPose);
                            SharpDX.Direct3D11.Texture2D depthBuffer = cameraResources.DepthBufferTexture2D;

                            // Direct3D interop APIs are used to provide the buffer to the WinRT API.
                            SharpDX.DXGI.Resource1 depthStencilResource = depthBuffer.QueryInterface<SharpDX.DXGI.Resource1>();
                            SharpDX.DXGI.Surface2 depthDxgiSurface = new SharpDX.DXGI.Surface2(depthStencilResource, 0);
                            IDirect3DSurface depthD3DSurface = InteropStatics.CreateDirect3DSurface(depthDxgiSurface.NativePointer);
                            if (depthD3DSurface != null)
                            {
                                // Calling CommitDirect3D11DepthBuffer causes the system to queue Direct3D commands to 
                                // read the depth buffer. It will then use that information to stabilize the image as
                                // the HolographicFrame is presented.
                                renderingParameters.CommitDirect3D11DepthBuffer(depthD3DSurface);
                            }
                        }
                    }
#endif
                    atLeastOneCameraRendered = true;
                }

                return atLeastOneCameraRendered;
            });
        }

        public void SaveAppState()
        {
            //
            // TODO: Insert code here to save your app state.
            //       This method is called when the app is about to suspend.
            //
            //       For example, store information in the SpatialAnchorStore.
            //
        }

        public void LoadAppState()
        {
            //
            // TODO: Insert code here to load your app state.
            //       This method is called when the app resumes.
            //
            //       For example, load information from the SpatialAnchorStore.
            //
        }

        public void OnPointerPressed()
        {
            this.pointerPressed = true;
        }

        /// <summary>
        /// Notifies renderers that device resources need to be released.
        /// </summary>
        public void OnDeviceLost(Object sender, EventArgs e)
        {

#if DRAW_SAMPLE_CONTENT
            spinningCubeRenderer.ReleaseDeviceDependentResources();
#endif

        }

        /// <summary>
        /// Notifies renderers that device resources may now be recreated.
        /// </summary>
        public void OnDeviceRestored(Object sender, EventArgs e)
        {
#if DRAW_SAMPLE_CONTENT
            spinningCubeRenderer.CreateDeviceDependentResourcesAsync();
#endif
        }

        void OnLocatabilityChanged(SpatialLocator sender, Object args)
        {
            switch (sender.Locatability)
            {
                case SpatialLocatability.Unavailable:
                    // Holograms cannot be rendered.
                    {
                        String message = "Warning! Positional tracking is " + sender.Locatability + ".";
                        Debug.WriteLine(message);
                    }
                    break;

                // In the following three cases, it is still possible to place holograms using a
                // SpatialLocatorAttachedFrameOfReference.
                case SpatialLocatability.PositionalTrackingActivating:
                // The system is preparing to use positional tracking.

                case SpatialLocatability.OrientationOnly:
                // Positional tracking has not been activated.

                case SpatialLocatability.PositionalTrackingInhibited:
                    // Positional tracking is temporarily inhibited. User action may be required
                    // in order to restore positional tracking.
                    break;

                case SpatialLocatability.PositionalTrackingActive:
                    // Positional tracking is active. World-locked content can be rendered.
                    break;
            }
        }

        public void OnCameraAdded(
            HolographicSpace sender,
            HolographicSpaceCameraAddedEventArgs args
            )
        {
            Deferral deferral = args.GetDeferral();
            HolographicCamera holographicCamera = args.Camera;

            Task task1 = new Task(() =>
            {
                //
                // TODO: Allocate resources for the new camera and load any content specific to
                //       that camera. Note that the render target size (in pixels) is a property
                //       of the HolographicCamera object, and can be used to create off-screen
                //       render targets that match the resolution of the HolographicCamera.
                //

                //camera = Camera.CreateAsync(VideoProfileFormats.BalancedVideoAndPhoto896x504x30, MediaEncodingSubtypes.Nv12).GetAwaiter().GetResult();
                videoFrameProcessor = VideoFrameProcessor.CreateAsync().GetAwaiter().GetResult();
                faceTrackerProcessor = FaceTrackerProcessor.CreateAsync(videoFrameProcessor).GetAwaiter().GetResult();

                // Create device-based resources for the holographic camera and add it to the list of
                // cameras used for updates and rendering. Notes:
                //   * Since this function may be called at any time, the AddHolographicCamera function
                //     waits until it can get a lock on the set of holographic camera resources before
                //     adding the new camera. At 60 frames per second this wait should not take long.
                //   * A subsequent Update will take the back buffer from the RenderingParameters of this
                //     camera's CameraPose and use it to create the ID3D11RenderTargetView for this camera.
                //     Content can then be rendered for the HolographicCamera.
                deviceResources.AddHolographicCamera(holographicCamera);

                // Holographic frame predictions will not include any information about this camera until
                // the deferral is completed.
                deferral.Complete();
            });
            task1.Start();
        }

        public void OnCameraRemoved(
            HolographicSpace sender,
            HolographicSpaceCameraRemovedEventArgs args
            )
        {
            Task task2 = new Task(() =>
            {
                //
                // TODO: Asynchronously unload or deactivate content resources (not back buffer 
                //       resources) that are specific only to the camera that was removed.
                //
            });
            task2.Start();

            // Before letting this callback return, ensure that all references to the back buffer 
            // are released.
            // Since this function may be called at any time, the RemoveHolographicCamera function
            // waits until it can get a lock on the set of holographic camera resources before
            // deallocating resources for this camera. At 60 frames per second this wait should
            // not take long.
            deviceResources.RemoveHolographicCamera(args.Camera);
        }

        public void OnGamepadAdded(Object o, Gamepad args)
        {
            foreach (var gamepadWithButtonState in gamepads)
            {
                if (args == gamepadWithButtonState.gamepad)
                {
                    // This gamepad is already in the list.
                    return;
                }
            }

            gamepads.Add(new GamepadWithButtonState(args, false));
        }

        public void OnGamepadRemoved(Object o, Gamepad args)
        {
            foreach (var gamepadWithButtonState in gamepads)
            {
                if (args == gamepadWithButtonState.gamepad)
                {
                    // This gamepad is in the list; remove it.
                    gamepads.Remove(gamepadWithButtonState);
                    return;
                }
            }
        }

        void OnHolographicDisplayIsAvailableChanged(Object o, Object args)
        {
            // Get the spatial locator for the default HolographicDisplay, if one is available.
            SpatialLocator spatialLocator = null;
            if (canGetDefaultHolographicDisplay)
            {
                HolographicDisplay defaultHolographicDisplay = HolographicDisplay.GetDefault();
                if (defaultHolographicDisplay != null)
                {
                    spatialLocator = defaultHolographicDisplay.SpatialLocator;
                }
            }
            else
            {
                spatialLocator = SpatialLocator.GetDefault();
            }

            if (this.spatialLocator != spatialLocator)
            {
                // If the spatial locator is disconnected or replaced, we should discard any state that was
                // based on it.
                if (this.spatialLocator != null)
                {
                    this.spatialLocator.LocatabilityChanged -= this.OnLocatabilityChanged;
                    this.spatialLocator = null;
                }

                this.stationaryReferenceFrame = null;

                if (spatialLocator != null)
                {
                    // Use the SpatialLocator from the default HolographicDisplay to track the motion of the device.
                    this.spatialLocator = spatialLocator;

                    // Respond to changes in the positional tracking state.
                    this.spatialLocator.LocatabilityChanged += this.OnLocatabilityChanged;

                    // The simplest way to render world-locked holograms is to create a stationary reference frame
                    // based on a SpatialLocator. This is roughly analogous to creating a "world" coordinate system
                    // with the origin placed at the device's position as the app is launched.
                    this.stationaryReferenceFrame = this.spatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();
                }
            }
        }

        private void ProcessFaces(List<BitmapBounds> faces, MediaFrameReference frame, SpatialCoordinateSystem worldCoordSystem)
        {
            VideoMediaFrameFormat videoFormat = frame.VideoMediaFrame.VideoFormat;
            SpatialCoordinateSystem cameraCoordinateSystem = frame.CoordinateSystem;
            CameraIntrinsics cameraIntrinsics = frame.VideoMediaFrame.CameraIntrinsics;

            // If we can't locate the camera intrinsics, this transform will be null.
            if (cameraIntrinsics == null)
            {
                return;
            }

            System.Numerics.Matrix4x4? cameraToWorld = cameraCoordinateSystem.TryGetTransformTo(worldCoordSystem);

            // If we can't locate the world, this transform will be null.
            if (cameraToWorld == null)
            {
                return;
            }

            float textureWidthInv = 1.0f / videoFormat.Width;
            float textureHeightInv = 1.0f / videoFormat.Height;

            // The face analysis returns very "tight fitting" rectangles.
            // We add some padding to make the visuals more appealing.
            int paddingForFaceRect = 24;
            float averageFaceWidthInMeters = 0.15f;

            float pixelsPerMeterAlongX = cameraIntrinsics.FocalLength.X;
            float averagePixelsForFaceAt1Meter = pixelsPerMeterAlongX * averageFaceWidthInMeters;

            // Place the cube 25cm above the center of the face.
            System.Numerics.Vector3 cubeOffsetInWorldSpace = new System.Numerics.Vector3(0.0f, 0.25f, 0.0f);
            BitmapBounds bestRect = new BitmapBounds();
            System.Numerics.Vector3 bestRectPositionInCameraSpace = System.Numerics.Vector3.Zero;
            float bestDotProduct = -1.0f;

            foreach (BitmapBounds faceRect in faces)
            {
                Point faceRectCenterPoint = new Point(faceRect.X + faceRect.Width / 2u, faceRect.Y + faceRect.Height / 2u);

                // Calculate the vector towards the face at 1 meter.
                System.Numerics.Vector2 centerOfFace = cameraIntrinsics.UnprojectAtUnitDepth(faceRectCenterPoint);

                // Add the Z component and normalize.
                System.Numerics.Vector3 vectorTowardsFace = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(centerOfFace.X, centerOfFace.Y, -1.0f));

                // Estimate depth using the ratio of the current faceRect width with the average faceRect width at 1 meter.
                float estimatedFaceDepth = averagePixelsForFaceAt1Meter / faceRect.Width;

                // Get the dot product between the vector towards the face and the gaze vector.
                // The closer the dot product is to 1.0, the closer the face is to the middle of the video image.
                float dotFaceWithGaze = System.Numerics.Vector3.Dot(vectorTowardsFace, -System.Numerics.Vector3.UnitZ);

                // Scale the vector towards the face by the depth, and add an offset for the cube.
                System.Numerics.Vector3 targetPositionInCameraSpace = vectorTowardsFace * estimatedFaceDepth;

                // Pick the faceRect that best matches the users gaze.
                if (dotFaceWithGaze > bestDotProduct)
                {
                    bestDotProduct = dotFaceWithGaze;
                    bestRect = faceRect;
                    bestRectPositionInCameraSpace = targetPositionInCameraSpace;
                }
            }

            // Transform the cube from Camera space to World space.
            System.Numerics.Vector3 bestRectPositionInWorldspace = System.Numerics.Vector3.Transform(bestRectPositionInCameraSpace, cameraToWorld.Value);

            spinningCubeRenderer.SetTargetPosition(bestRectPositionInWorldspace + cubeOffsetInWorldSpace);

            // Texture Coordinates are [0,1], but our FaceRect is [0,Width] and [0,Height], so we need to normalize these coordinates
            // We also add padding for the faceRects to make it more visually appealing.
            float normalizedWidth = (bestRect.Width + paddingForFaceRect * 2u) * textureWidthInv;
            float normalizedHeight = (bestRect.Height + paddingForFaceRect * 2u) * textureHeightInv;
            float normalizedX = (bestRect.X - paddingForFaceRect) * textureWidthInv;
            float normalizedY = (bestRect.Y - paddingForFaceRect) * textureHeightInv;

            // TODO
            //Quad
            //.SetTexCoordScaleAndOffset({ normalizedWidth, normalizedHeight }, { normalizedX, normalizedY });

        }
    }
}
