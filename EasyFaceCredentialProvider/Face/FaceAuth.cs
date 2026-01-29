using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ViewFaceCore;
using ViewFaceCore.Configs;
using ViewFaceCore.Core;
using ViewFaceCore.Model;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace EasyFaceCredentialProvider;

public class FaceAuth : IDisposable
{
    private readonly FaceDetector _faceDetector;
    private readonly FaceAntiSpoofing _faceAntiSpoofing;
    private readonly FaceRecognizer _recognizer;
    private readonly FaceLandmarker _faceLandmarker;
    private readonly MediaCaptureInitializationSettings _cameraSettings = new();

    public FaceAuth(string cameraId)
    {
        _faceDetector = new();
        _faceAntiSpoofing = new(new FaceAntiSpoofingConfig() { Global = true });
        _recognizer = new(new FaceRecognizeConfig(FaceType.Normal));
        _faceLandmarker = new();
        _cameraSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
        _cameraSettings.VideoDeviceId = cameraId;
    }

    private FaceDetectResult _ImageAnalysis(Image image)
    {
        var faceImage = image.ToFaceImage();
        var faces = _faceDetector.Detect(faceImage);
        if (!faces.Any())
        {
            Log.Info("检测不到人脸");
            return new FaceDetectResult(AntiSpoofingStatus.Error, null);
        }
        var points = _faceLandmarker.Mark(faceImage, faces[0]);
        var fasRes = _faceAntiSpoofing.AntiSpoofingVideo(faceImage, faces[0], points);
        Log.Info($"活体检测结果为{fasRes.Status}");

        float[]? recRes = null;
        if (fasRes.Status is AntiSpoofingStatus.Real)
        {
            Log.Info("识别成功");
            recRes = _recognizer.Extract(faceImage, points);
            using var memoryStream = new MemoryStream();
        }

        return new FaceDetectResult(fasRes.Status, recRes);
    }

    public async IAsyncEnumerable<FaceDetectResult> StartDetect([EnumeratorCancellation] CancellationToken token)
    {
        var camera = new MediaCapture();
        await camera.InitializeAsync(_cameraSettings);
        while (token is not {IsCancellationRequested:true})
        {
            IRandomAccessStream stream = new InMemoryRandomAccessStream();
            await camera.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateBmp(), stream);
            yield return _ImageAnalysis(Image.FromStream(stream.AsStream()));
        }
    }

    public void Dispose()
    {
    }
}