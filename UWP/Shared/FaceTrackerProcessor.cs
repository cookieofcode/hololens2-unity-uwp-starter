using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.FaceAnalysis;
using CSharp.Common;
using CSharp.Shared;

#endif

#if ENABLE_WINMD_SUPPORT
using UnityEngine;
#endif

#if WINDOWS_UWP
public class FaceTrackerProcessor
#endif
#if ENABLE_WINMD_SUPPORT
public class FaceTrackerProcessor : MonoBehaviour
#endif
{
#if ENABLE_WINMD_SUPPORT || WINDOWS_UWP
    private FaceTracker _faceTracker;
    private VideoFrameProcessor _videoFrameProcessor;
    private List<BitmapBounds> _latestFaces;
    private int _numFramesWithoutFaces = 0;
    private bool _isRunning = false;
    private readonly object _lock = new object();

    private FaceTrackerProcessor(FaceTracker tracker, VideoFrameProcessor videoFrameProcessor)
    {
        _latestFaces = new List<BitmapBounds>();
        _faceTracker = tracker;
        _videoFrameProcessor = videoFrameProcessor;
        if (_videoFrameProcessor != null)
        {
            Task.Run(async () =>
            {
                _isRunning = true;
                while (_isRunning)
                {
                    ProcessFrame();
                }
            });
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _latestFaces = new List<BitmapBounds>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static async Task<FaceTrackerProcessor> CreateAsync(VideoFrameProcessor videoFrameProcessor)
    {
        FaceTracker tracker = await FaceTracker.CreateAsync();
        tracker.MinDetectableFaceSize = new BitmapSize() { Width = 32, Height = 32 };
        tracker.MaxDetectableFaceSize = new BitmapSize() { Width = 1024, Height = 1024 };

        return new FaceTrackerProcessor(tracker, videoFrameProcessor);
    }

    public bool IsTrackingFaces()
    {
        lock (_lock)
        {
            return _latestFaces != null && _latestFaces.Any();
        }

    }

    public List<BitmapBounds> GetLatestFaces()
    {
        lock (_lock)
        {
            return new List<BitmapBounds>(_latestFaces);
        }
    }

    public void ProcessFrame()
    {
        MediaFrameReference frame = _videoFrameProcessor.GetLatestFrame();
        if (frame != null)
        {
            VideoMediaFrame videoMediaFrame = frame.VideoMediaFrame;
            if (videoMediaFrame != null)
            {
                // Validate that the incoming frame format is compatible with the FaceTracker
                bool isBitmapPixelFormatSupported = videoMediaFrame.SoftwareBitmap != null && FaceTracker.IsBitmapPixelFormatSupported(videoMediaFrame.SoftwareBitmap.BitmapPixelFormat);
                if (isBitmapPixelFormatSupported)
                {
                    // Ask the FaceTracker to process this frame asynchronously
                    IAsyncOperation<IList<DetectedFace>> processFrameTask = _faceTracker.ProcessNextFrameAsync(videoMediaFrame.GetVideoFrame());

                    try
                    {
                        IList<DetectedFace> faces = processFrameTask.GetResults();

                        //std::lock_guard < std::shared_mutex > lock (m_propertiesLock) ;
                        lock (_lock)
                        {

                            if (faces.Count == 0)
                            {
                                ++_numFramesWithoutFaces;

                                // The FaceTracker might lose track of faces for a few frames, for example,
                                // if the person momentarily turns their head away from the videoFrameProcessor. To smooth out
                                // the tracking, we allow 30 video frames (~1 second) without faces before
                                // we say that we're no longer tracking any faces.
                                if (_numFramesWithoutFaces > 30 && _latestFaces.Any())
                                {
                                    _latestFaces.Clear();
                                }
                            }
                            else
                            {
                                _numFramesWithoutFaces = 0;
                                _latestFaces.Clear();
                                foreach (var face in faces)
                                {
                                    _latestFaces.Add(face.FaceBox);
                                }

                                //_latestFaces = faces.Select(f => f.FaceBox) as List<BitmapBounds>;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // The task might be cancelled if the FaceAnalysis failed.
                        Debug.LogException(e);
                        return;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _isRunning = false;
    }
#endif
}
