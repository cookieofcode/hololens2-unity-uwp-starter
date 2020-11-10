using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT || NETFX_CORE
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.FaceAnalysis;
#endif

#if ENABLE_WINMD_SUPPORT
using UnityEngine;
#endif

#if NETFX_CORE
using CSharp.Common;
#endif

namespace Shared
{
    public class FaceTrackerProcessor
#if ENABLE_WINMD_SUPPORT 
        : MonoBehaviour 
#endif
    {
#if ENABLE_WINMD_SUPPORT || NETFX_CORE
    private readonly FaceTracker faceTracker;
        private readonly VideoFrameProcessor videoFrameProcessor;
        private List<BitmapBounds> latestFaces;
        private int numFramesWithoutFaces = 0;
        private bool isRunning = false;
        private readonly object @lock = new object();

        private FaceTrackerProcessor(FaceTracker tracker, VideoFrameProcessor videoFrameProcessor)
        {
            latestFaces = new List<BitmapBounds>();
            faceTracker = tracker;
            this.videoFrameProcessor = videoFrameProcessor;
            if (this.videoFrameProcessor != null)
            {
                Task.Run(async () =>
                {
                    isRunning = true;
                    while (isRunning)
                    {
                        ProcessFrame();
                    }
                });
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            latestFaces = new List<BitmapBounds>(); // TODO: Check in Unity if needed
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
            lock (@lock)
            {
                return latestFaces != null && latestFaces.Any();
            }

        }

        public List<BitmapBounds> GetLatestFaces()
        {
            lock (@lock)
            {
                return new List<BitmapBounds>(latestFaces);
            }
        }

        public void ProcessFrame()
        {
            MediaFrameReference frame = videoFrameProcessor.GetLatestFrame();
            VideoMediaFrame videoMediaFrame = frame?.VideoMediaFrame;
            if (videoMediaFrame == null) return;
            // Validate that the incoming frame format is compatible with the FaceTracker
            bool isBitmapPixelFormatSupported = videoMediaFrame.SoftwareBitmap != null && FaceTracker.IsBitmapPixelFormatSupported(videoMediaFrame.SoftwareBitmap.BitmapPixelFormat);
            if (!isBitmapPixelFormatSupported) return;
            // Ask the FaceTracker to process this frame asynchronously
            IAsyncOperation<IList<DetectedFace>> processFrameTask = faceTracker.ProcessNextFrameAsync(videoMediaFrame.GetVideoFrame());

            try
            {
                IList<DetectedFace> faces = processFrameTask.GetResults();

                lock (@lock)
                {

                    if (faces.Count == 0)
                    {
                        ++numFramesWithoutFaces;

                        // The FaceTracker might lose track of faces for a few frames, for example,
                        // if the person momentarily turns their head away from the videoFrameProcessor. To smooth out
                        // the tracking, we allow 30 video frames (~1 second) without faces before
                        // we say that we're no longer tracking any faces.
                        if (numFramesWithoutFaces > 30 && latestFaces.Any())
                        {
                            latestFaces.Clear();
                        }
                    }
                    else
                    {
                        numFramesWithoutFaces = 0;
                        latestFaces.Clear();
                        foreach (var face in faces)
                        {
                            latestFaces.Add(face.FaceBox);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // The task might be cancelled if the FaceAnalysis failed.
                Debug.LogException(e);
            }
        }

        public void Dispose()
        {
            isRunning = false;
        }
#endif
    }
}
