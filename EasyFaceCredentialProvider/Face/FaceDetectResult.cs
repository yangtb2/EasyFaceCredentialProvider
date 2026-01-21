using ViewFaceCore.Model;

namespace EasyFaceCredentialProvider;

public record FaceDetectResult(AntiSpoofingStatus Status, float[]? Data);