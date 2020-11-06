using System;

#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices.Core;
using Windows.Perception.Spatial;
using CSharp.Shared;
#endif

public class CameraCapture : IDisposable
{
#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
    /// <summary>
    /// The guid for getting the view transform from the frame sample.
    /// See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera
    /// </summary>
    static Guid MFSampleExtension_Spatial_CameraViewTransform = new Guid("4E251FA4-830F-4770-859A-4B8D99AA809B");

    /// <summary>
    /// The guid for getting the projection transform from the frame sample.
    /// See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera
    /// </summary>
    static Guid MFSampleExtension_Spatial_CameraProjectionTransform = new Guid("47F9FCB5-2A02-4F26-A477-792FDF95886A");

    /// <summary>
    /// The guid for getting the camera coordinate system for the frame sample.
    /// See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera
    /// </summary>
    static Guid MFSampleExtension_Spatial_CameraCoordinateSystem = new Guid("9D13C82F-2199-4E67-91CD-D1A4181F2534");

    /// <summary>
    /// The guid for getting the camera extrinsics for the frame sample.
    /// See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera
    /// </summary>
    static Guid MFSampleExtension_CameraExtrinsics = new Guid("6B761658-B7EC-4C3B-8225-8623CABEC31D");

    private readonly MediaFrameReference _frameReference;
    private readonly SpatialCoordinateSystem _coordinateSystem;
    //public VideoFrame _videoFrame;
    public SoftwareBitmap Bitmap;
    private CameraIntrinsics _cameraIntrinsics;

    public CameraCapture(MediaFrameReference frameReference)
    {
        _frameReference = frameReference ?? throw new ArgumentNullException(nameof(frameReference));
        Bitmap = frameReference.VideoMediaFrame?.SoftwareBitmap;
        _coordinateSystem = frameReference.CoordinateSystem;
        _cameraIntrinsics = frameReference.VideoMediaFrame?.CameraIntrinsics;

        if (_frameReference.Properties.ContainsKey(MFSampleExtension_Spatial_CameraViewTransform))
        {
            Debug.Log("We got the camera view!");
        }

        if (_frameReference.Properties.ContainsKey(MFSampleExtension_CameraExtrinsics))
        {
            Debug.Log("Got CameraExtrinsics. Don't know how to convert yet.");
        }

        if (_frameReference.Properties.ContainsKey(MFSampleExtension_Spatial_CameraProjectionTransform))
        {
            Debug.Log("We got the projection transform!");
        }
    }
#endif

    public void Dispose()
    {
#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
        Bitmap?.Dispose();
        _frameReference?.Dispose();
#endif
    }
}
