using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT || NETFX_CORE
using Windows.Foundation.Metadata;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
#endif

namespace Shared
{
    public class VideoFrameProcessor
    {
#if ENABLE_WINMD_SUPPORT || NETFX_CORE
        MediaCapture m_mediaCapture;
        MediaFrameReader m_mediaFrameReader;

        MediaFrameSource m_mediaFrameSource;
        MediaFrameReference m_latestFrame;

        public VideoFrameProcessor(MediaCapture mediaCapture, MediaFrameReader reader, MediaFrameSource source)
        {
            m_mediaCapture = mediaCapture;
            m_mediaFrameReader = reader;
            m_mediaFrameSource = source;

            // Listen for new frames, so we know when to update our m_latestFrame
            m_mediaFrameReader.FrameArrived += OnFrameArrived;
        }

        public static async Task<VideoFrameProcessor> CreateAsync()
        {
            IReadOnlyList<MediaFrameSourceGroup> groups = await MediaFrameSourceGroup.FindAllAsync();
            MediaFrameSourceGroup selectedGroup = null;
            MediaFrameSourceInfo selectedSourceInfo = null;

            // Pick first color source.
            foreach (var sourceGroup in groups)
            {
                foreach (var sourceInfo in sourceGroup.SourceInfos)
                {
                    if (sourceInfo.MediaStreamType == MediaStreamType.VideoPreview
                        && sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                    {
                        selectedSourceInfo = sourceInfo;
                        break;
                    }
                }
                if (selectedSourceInfo != null)
                {
                    selectedGroup = sourceGroup;
                    break;
                }
            }

            // No valid camera was found. This will happen on the emulator.
            if (selectedGroup == null || selectedSourceInfo == null)
            {
                return null;
            }

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            settings.MemoryPreference = MediaCaptureMemoryPreference.Cpu; // Need SoftwareBitmaps for FaceAnalysis
            settings.StreamingCaptureMode = StreamingCaptureMode.Video;   // Only need to stream video
            settings.SourceGroup = selectedGroup;

            MediaCapture mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(settings);
            MediaFrameSource selectedSource = mediaCapture.FrameSources[selectedSourceInfo.Id];
            MediaFrameReader reader = await mediaCapture.CreateFrameReaderAsync(selectedSource);
            MediaFrameReaderStartStatus status = await reader.StartAsync();

            // Only create a VideoFrameProcessor if the reader successfully started
            if (status == MediaFrameReaderStartStatus.Success)
            {
                return new VideoFrameProcessor(mediaCapture, reader, selectedSource);
            }

            return null;
        }


        public MediaFrameReference GetLatestFrame()
        {
            return m_latestFrame;
        }

        public VideoMediaFrameFormat GetCurrentFormat()
        {
            return m_mediaFrameSource.CurrentFormat.VideoFormat;
        }

        void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            MediaFrameReference frame = sender.TryAcquireLatestFrame();
            if (frame != null)
            {
                Interlocked.Exchange(ref m_latestFrame, frame);
            }
        }
#endif
    }
}
