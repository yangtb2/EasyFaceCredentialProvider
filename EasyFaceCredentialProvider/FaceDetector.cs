using ViewFaceCore.Configs;
using ViewFaceCore.Core;
using ViewFaceCore.Model;

namespace EasyFaceCredentialProvider;

public class FaceDetector
{
    private readonly FaceDetector _faceDetector;
    private readonly FaceAntiSpoofing _faceAntiSpoofing;
    private readonly FaceRecognizer _recognizer;
    private readonly FaceLandmarker _faceLandmarker;

    public FaceDetector()
    {
        _faceDetector = new();
        _faceAntiSpoofing = new(new FaceAntiSpoofingConfig() { Global = true });
        _recognizer = new(new FaceRecognizeConfig(FaceType.Normal));
        _faceLandmarker = new();
    }

    public void StartDetect()
    {

    }
}