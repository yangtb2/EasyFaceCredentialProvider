using Accord.Video.DirectShow;
using System.Text;
using ViewFaceCore;
using ViewFaceCore.Configs;
using ViewFaceCore.Core;
using ViewFaceCore.Model;

namespace EasyFaceCredentialProvider;

public class FaceAuth
{
    private readonly FaceDetector _faceDetector;
    private readonly FaceAntiSpoofing _faceAntiSpoofing;
    private readonly FaceRecognizer _recognizer;
    private readonly FaceLandmarker _faceLandmarker;
    private readonly VideoCaptureDevice _camera;
    private readonly SemaphoreSlim _slim = new(0, 1);
    private FaceDetectResult _result;

    public FaceAuth(string cameraMonikerString)
    {
        _faceDetector = new();
        _faceAntiSpoofing = new(new FaceAntiSpoofingConfig() { Global = true });
        _recognizer = new(new FaceRecognizeConfig(FaceType.Normal));
        _faceLandmarker = new();
        _camera = new VideoCaptureDevice(cameraMonikerString);
        _camera.NewFrame += _camera_NewFrame;
    }

    private void _camera_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
    {
        var faceImage = eventArgs.Frame.ToFaceImage();

        var faces = _faceDetector.Detect(faceImage);
        if (faces.Length == 0)
        {
            Log.Info("未检测到人脸");
            _result = FaceDetectResult.NoFace();
            return;
        }

        var points = _faceLandmarker.Mark(faceImage, faces[0]);
        var fasRes = _faceAntiSpoofing.AntiSpoofingVideo(faceImage, faces[0], points);
        Log.Info($"活体检测结果为{fasRes.Status}");
        if (fasRes.Status is AntiSpoofingStatus.Spoof)
        {
            Log.Info("非活体攻击");
            _result = FaceDetectResult.Spoofed();
            _slim.Release();
        }

        if (fasRes.Status is AntiSpoofingStatus.Real)
        {
            var recRes = _recognizer.Extract(faceImage, points);
            using var memoryStream = new MemoryStream();
            _result = FaceDetectResult.Success(recRes);
            _slim.Release();
        }
    }

    public void StartDetect()
    {
        _camera.Start();
        _slim.Wait();
    }
}