using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Debug = Shared.Debug;
#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
using System.Diagnostics;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Perception.Spatial;
using Windows.Foundation.Collections;
using Windows.Media.Effects;
using CSharp.Shared;

#endif

public class Camera
{
#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
    private readonly MediaFrameReader _reader;
    private MediaFrameSource _source;
    private MediaCapture _mediaCapture;
    public bool IsRunning = false;

    // https://docs.microsoft.com/en-us/previous-versions/windows/apps/hh868174(v=win.10)?redirectedfrom=MSDN
    private readonly Guid rotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

    public Camera(MediaFrameReader reader, MediaFrameSource source, MediaCapture mediaCapture)
    {
        _mediaCapture = mediaCapture ?? throw new ArgumentNullException(nameof(mediaCapture));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        mediaCapture.RecordLimitationExceeded += OnRecordLimitationExceeded;
        IsRunning = true;
    }
    public async Task StopCapturingAsync()
    {
        IsRunning = false;
        if (_mediaCapture != null && _mediaCapture.CameraStreamState != CameraStreamState.Shutdown)
        {
            await _reader.StopAsync();
            _reader.Dispose();
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
    }

    public static async Task<Camera> CreateAsync(VideoProfileFormats videoProfileFormats, string mediaEncodingSubtype)
    {
        VideoProfileFormatService videoProfileFormatService = new VideoProfileFormatService();
        VideoProfileFormatService.VideoProfileFormat videoProfileFormat = videoProfileFormatService.GetVideoProfileFormat(videoProfileFormats);

        bool useLegacyMode = videoProfileFormat.Legacy;
        if (useLegacyMode)
        {
            Debug.Log("Using legacy mode. The device does not support profiles, the profiles does not exist, or legacy mode is enforced.");
            var videoStreamType = MediaStreamType.VideoPreview;
            var allFrameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();                                              //Returns IReadOnlyList<MediaFrameSourceGroup>
            var candidateFrameSourceGroups = allFrameSourceGroups.Where(group => group.SourceInfos.Any(sourceInfo => (sourceInfo.MediaStreamType == videoStreamType &&
                                                                                                                      sourceInfo.SourceKind == MediaFrameSourceKind.Color)));   //Returns IEnumerable<MediaFrameSourceGroup>
            var selectedFrameSourceGroup = candidateFrameSourceGroups.FirstOrDefault();                                         //Returns a single MediaFrameSourceGroup
            if (selectedFrameSourceGroup == null) throw new CameraInitializationException("selectedFrameSourceGroup was null");
            var selectedFrameSourceInfo = selectedFrameSourceGroup.SourceInfos.FirstOrDefault(); //Returns a MediaFrameSourceInfo
            if (selectedFrameSourceInfo == null) throw new CameraInitializationException("selectedFrameSourceInfo was null");
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);   //Returns DeviceCollection
            var deviceInformation = devices.FirstOrDefault();                               //Returns a single DeviceInformation
            if (deviceInformation == null) throw new CameraInitializationException("deviceInformation was null");
            //var videoCapture = new Camera(selectedFrameSourceGroup, selectedFrameSourceInfo, deviceInformation);
            var mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = deviceInformation.Id,
                SourceGroup = selectedFrameSourceGroup,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video
                //SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            });
            mediaCapture.VideoDeviceController.Focus.TrySetAuto(true);
            var mediaFrameSource = mediaCapture.FrameSources[selectedFrameSourceInfo.Id]; //Returns a MediaFrameSource
            var reader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, MediaEncodingSubtypes.Bgra8);
            await reader.StartAsync();
            var allPropertySets = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(videoStreamType).Select((x) => x as VideoEncodingProperties)
                .Where((x) =>
                {
                    if (x == null) return false;
                    if (x.FrameRate.Denominator == 0) return false;

                    double calculatedFrameRate = (double)x.FrameRate.Numerator / (double)x.FrameRate.Denominator;

                    return
                        x.Width == videoProfileFormat.Width &&
                        x.Height == videoProfileFormat.Height &&
                        (int)Math.Round(calculatedFrameRate) == videoProfileFormat.FrameRate;
                }); //Returns IEnumerable<VideoEncodingProperties>
            var chosenPropertySet = allPropertySets.FirstOrDefault();
            IVideoEffectDefinition ved = new VideoMRCSettings(false, false, 0, 0.0f);
            await mediaCapture.AddVideoEffectAsync(ved, videoStreamType);
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(videoStreamType, chosenPropertySet);
            Debug.Log("Creating camera with legacy mode.");
            return new Camera(reader, mediaFrameSource, mediaCapture);
        }
        else
        {
            DeviceInformation deviceInformation = await FindVideoCaptureDevice();
            bool isVideoProfileSupported = IsVideoProfileSupported(deviceInformation);
            if (!isVideoProfileSupported) throw new CameraInitializationException("Video profiles are not supported. Use legacy mode.");
            Debug.Log("Using video profiles supported by device.");
            MediaCaptureVideoProfile profile = CreateVideoProfile(deviceInformation, videoProfileFormat);
            MediaCapture mediaCapture = await CreateMediaCaptureAsync(profile, deviceInformation);
            MediaFrameSource source = await CreateMediaSourceAsync(mediaCapture, videoProfileFormat);
            MediaFrameReader reader = await CreateMediaFrameReaderAsync(mediaCapture, source, videoProfileFormat, mediaEncodingSubtype);
            await StartCapturingAsync(reader);
            return new Camera(reader, source, mediaCapture);
        }
    }
    public class VideoMRCSettings : IVideoEffectDefinition
    {
        public string ActivatableClassId
        {
            get
            {
                return "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";
            }
        }

        public IPropertySet Properties
        {
            get; private set;
        }

        public VideoMRCSettings(bool HologramCompositionEnabled, bool VideoStabilizationEnabled, int VideoStabilizationBufferLength, float GlobalOpacityCoefficient)
        {
            Properties = (IPropertySet)new PropertySet();
            Properties.Add("HologramCompositionEnabled", HologramCompositionEnabled);
            Properties.Add("VideoStabilizationEnabled", VideoStabilizationEnabled);
            Properties.Add("VideoStabilizationBufferLength", VideoStabilizationBufferLength);
            Properties.Add("GlobalOpacityCoefficient", GlobalOpacityCoefficient);
        }
    }

    private static async Task StartCapturingAsync(MediaFrameReader reader)
    {
        MediaFrameReaderStartStatus status = await reader.StartAsync();
        if (status != MediaFrameReaderStartStatus.Success) throw new CameraInitializationException($"Error creating video frame processor with status {Enum.Parse(status.GetType(), status.ToString())}");
    }

    private void OnRecordLimitationExceeded(MediaCapture sender)
    {
        // This can happen if the MediaFrameReference cannot be reused as the SoftwareBitmap or VideoFrame were not disposed.
        Debug.LogWarning("Record limitation exceeded.");
    }

    public MediaFrameReference GetLatestFrame()
    {
        MediaFrameReference frame = _reader.TryAcquireLatestFrame();
        if (frame == null)
        {
            Debug.LogWarning("Acquired empty frame.");
            return null;
        }

        return frame;
        // TODO: Check this
    }
    public CameraCapture GetLatestCapture()
    {
        MediaFrameReference frame = _reader.TryAcquireLatestFrame();
        if (frame == null)
        {
            Debug.LogWarning("Acquired empty frame.");
            return null;
        }

        SoftwareBitmap bitmap = frame.VideoMediaFrame?.SoftwareBitmap;
        if (bitmap == null)
        {
            Debug.LogWarning(("Acquired frame with empty bitmap."));
            return null;
        }

        SpatialCoordinateSystem coordinates = frame.CoordinateSystem;
        if (coordinates == null)
        {
            Debug.LogWarning("Aquired frame with empty Coordinate System");
            return null;
        }

        //return new CameraCapture(bitmap, coordinates);
        return new CameraCapture(frame);
    }

    private static async Task<MediaFrameReader> CreateMediaFrameReaderAsync(MediaCapture mediaCapture, MediaFrameSource source, VideoProfileFormatService.VideoProfileFormat videoProfileFormat, string mediaEncodingSubtype)
    {
        // TODO: Check if resizing using the outputSize here is faster than converting later.
        BitmapSize outputSize = new BitmapSize { Width = videoProfileFormat.Width, Height = videoProfileFormat.Height };
        // Using an overload creates copies of the original frame data, in order to not cause frame acquisition to halt when they are retained due to conversion.
        //MediaFrameReader reader = await mediaCapture.CreateFrameReaderAsync(source, MediaEncodingSubtypes.Bgra8, outputSize);
        MediaFrameReader reader = await mediaCapture.CreateFrameReaderAsync(source, mediaEncodingSubtype);
        // This mode works well for scenarios where processing the most current frame is prioritized, such as real-time computer vision applications.
        // https://docs.microsoft.com/en-us/uwp/api/windows.media.capture.frames.mediaframereaderacquisitionmode?view=winrt-19041
        reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
        return reader;
    }

    private static async Task<MediaFrameSource> CreateMediaSourceAsync(MediaCapture mediaCapture, VideoProfileFormatService.VideoProfileFormat videoProfileFormat)
    {
        MediaFrameSourceKind color = MediaFrameSourceKind.Color;
        MediaStreamType videoPreview = MediaStreamType.VideoPreview; // select video preview for transform matrices
        MediaFrameSource source = mediaCapture.FrameSources.First(frameSource => frameSource.Value.Info.SourceKind == color
                                                                                 && frameSource.Value.Info.MediaStreamType == videoPreview).Value;
        MediaFrameFormat format = GetFrameFormat(source, videoProfileFormat);
        await source.SetFormatAsync(format);
        return source;
    }

    private static MediaFrameFormat GetFrameFormat(MediaFrameSource colorFrameSource, VideoProfileFormatService.VideoProfileFormat videoProfileFormat)
    {
        IReadOnlyList<MediaFrameFormat> formats = colorFrameSource.SupportedFormats;
        MediaFrameFormat format = formats.FirstOrDefault(supportedFormat => supportedFormat.VideoFormat.Width == videoProfileFormat.Width
                                                                            && supportedFormat.VideoFormat.Height == videoProfileFormat.Height
                                                                            && supportedFormat.FrameRate.Numerator == videoProfileFormat.FrameRate);
        if (format == null) throw new CameraInitializationException("The selected profile and format is not supported.");
        return format;
    }

    private static bool IsVideoProfileSupported(DeviceInformation deviceInformation)
    {
        string deviceId = deviceInformation.Id;
        bool videoProfileSupported = MediaCapture.IsVideoProfileSupported(deviceId);
        return videoProfileSupported;
    }

    private static MediaCaptureVideoProfile CreateVideoProfile(DeviceInformation deviceInformation, VideoProfileFormatService.VideoProfileFormat videoProfileFormat)
    {
        string deviceId = deviceInformation.Id;
        KnownVideoProfile profile = videoProfileFormat.Profile;
        MediaCaptureVideoProfile videoProfile = MediaCapture.FindKnownVideoProfiles(deviceId, profile).FirstOrDefault();
        if (videoProfile == null) throw new CameraInitializationException($"No profile {Enum.GetName(typeof(KnownVideoProfile), profile)} found for device");
       Debug.LogFormat("Selected profile with id {0}", videoProfile.Id);
        return videoProfile;
    }

    private static async Task<MediaCapture> CreateMediaCaptureAsync(MediaCaptureVideoProfile videoProfile,
        DeviceInformation deviceInformation)
    {
        string deviceId = deviceInformation.Id;
        MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
        {
            VideoDeviceId = deviceId,
            VideoProfile = videoProfile,
            // This media capture can share streaming with other apps.
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            // Set to CPU to ensure frames always contain CPU SoftwareBitmap images instead of preferring GPU D3DSurface images.
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            // Initialize only video and no audio capture devices.
            StreamingCaptureMode = StreamingCaptureMode.Video
        };
        MediaCapture mediaCapture = new MediaCapture();
        try
        {
            await mediaCapture.InitializeAsync(settings);
        }
        catch (UnauthorizedAccessException)
        {
           Debug.LogErrorFormat("Access to the camera device with name {0} denied.", deviceInformation.Name);
        }

        mediaCapture.VideoDeviceController.Focus.TrySetAuto(true);
        return mediaCapture;
    }

    private static async Task<DeviceInformation> FindVideoCaptureDevice()
    {
        DeviceInformationCollection deviceInformationCollection =
            await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        DeviceInformation deviceInformation =
            deviceInformationCollection.FirstOrDefault(information => information.EnclosureLocation.Panel == Panel.Back);
        Debug.LogFormat("Selected device with name: {0}", deviceInformation.Name);
        return deviceInformation;
    }

#endif
}