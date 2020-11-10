using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Shared;
using UnityEngine.Serialization;
#if ENABLE_WINMD_SUPPORT
using UnityEngine.XR.WSA;
#endif

#if ENABLE_WINMD_SUPPORT || NETFX_CORE
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Foundation;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices.Core;
using Windows.Perception.Spatial;
using Windows.Graphics.Holographic;
using Windows.Perception;
using Windows.UI.Input.Spatial;
using Debug = Shared.Debug;
#endif

public class HoloFaceTracker : MonoBehaviour
{
    private VideoFrameProcessor _videoFrameProcessor;
    public TextMesh statusBlock;
    private bool _isReadyToRender;
    public GameObject cube;
    private TimeSpan _previousFrameTimestamp;
    private FaceTrackerProcessor _faceTrackerProcessor;
    private bool _isTrackingFaces;
    public CubeRenderer cubeRenderer;

    // Start is called before the first frame update
    async void Start()
    {
#if ENABLE_WINMD_SUPPORT
        statusBlock.text = "Starting camera...";
        _videoFrameProcessor = await VideoFrameProcessor.CreateAsync();
        Debug.Log("Created video frame processor");
        _faceTrackerProcessor = await FaceTrackerProcessor.CreateAsync(_videoFrameProcessor);

        statusBlock.text = "No faces detected";
        _isReadyToRender = true;
#endif
    }
#if ENABLE_WINMD_SUPPORT

    void OnPreRender()
    {
        // If we are tracking any faces, then we render the cube over their head, and the video image on the quad.
        if (_isTrackingFaces)
        {
            statusBlock.text = "";
            cube.SetActive(true);
        }
        // Otherwise we render the status message on the quad.
        else
        {
            statusBlock.text = "No faces detected";
            cube.SetActive(false);
        }
    }

    private void ProcessFaces(List<BitmapBounds> faces, MediaFrameReference frame, SpatialCoordinateSystem worldCoordSystem)
    {
        VideoMediaFrameFormat videoFormat = frame.VideoMediaFrame.VideoFormat;
        SpatialCoordinateSystem cameraCoordinateSystem = frame.CoordinateSystem;
        CameraIntrinsics cameraIntrinsics = frame.VideoMediaFrame.CameraIntrinsics;

        System.Numerics.Matrix4x4? cameraToWorld = cameraCoordinateSystem.TryGetTransformTo(worldCoordSystem);

        // If we can't locate the world, this transform will be null.
        if (!cameraToWorld.HasValue)
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

        cubeRenderer.SetTargetPosition(bestRectPositionInWorldspace + cubeOffsetInWorldSpace);

        // Texture Coordinates are [0,1], but our FaceRect is [0,Width] and [0,Height], so we need to normalize these coordinates
        // We also add padding for the faceRects to make it more visually appealing.
        float normalizedWidth = (bestRect.Width + paddingForFaceRect * 2u) * textureWidthInv;
        float normalizedHeight = (bestRect.Height + paddingForFaceRect * 2u) * textureHeightInv;
        float normalizedX = (bestRect.X - paddingForFaceRect) * textureWidthInv;
        float normalizedY = (bestRect.Y - paddingForFaceRect) * textureHeightInv;
    }
#endif

    // Update is called once per frame
    void Update()
    {
#if ENABLE_WINMD_SUPPORT
        if (!_isReadyToRender)
        {
            return;
        }

        // The HolographicFrame has information that the app needs in order
        // to update and render the current frame. The app begins each new
        // frame by calling CreateNextFrame.
        //HolographicFrame ^ holographicFrame = m_holographicSpace->CreateNextFrame();

        // Get a prediction of where holographic cameras will be when this frame
        // is presented.
        //HolographicFramePrediction prediction = holographicFrame->CurrentPrediction;

        IntPtr spatialCoordinateSystemPtr = WorldManager.GetNativeISpatialCoordinateSystemPtr();
        SpatialCoordinateSystem unityWorldOrigin = Marshal.GetObjectForIUnknown(spatialCoordinateSystemPtr) as SpatialCoordinateSystem;
        SpatialCoordinateSystem currentCoordinateSystem = unityWorldOrigin;

        _isTrackingFaces = _faceTrackerProcessor.IsTrackingFaces();

        if (_isTrackingFaces)
        {
            MediaFrameReference frame = _videoFrameProcessor.GetLatestFrame();
            if (frame == null)
            {
                return;
            }
            var faces = _faceTrackerProcessor.GetLatestFaces();
            ProcessFaces(faces, frame, currentCoordinateSystem);


            TimeSpan currentTimeStamp = frame.SystemRelativeTime.Value.Duration();
            if (currentTimeStamp > _previousFrameTimestamp)
            {
                // TODO: copy to texture
                _previousFrameTimestamp = frame.SystemRelativeTime.Value.Duration();
            }
        }

        SpatialPointerPose pointerPose = SpatialPointerPose.TryGetAtTimestamp(currentCoordinateSystem, PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));
#endif
    }
}
