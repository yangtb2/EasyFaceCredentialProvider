namespace EasyFaceCredentialProvider;

public enum FaceDetectResultType
{
    Success,
    NoFace,
    Spoofed,
}

public record FaceDetectResult(FaceDetectResultType ResultType, float[]? Data)
{
    public static FaceDetectResult Success(float[] data) => new(FaceDetectResultType.Success, data);
    public static FaceDetectResult NoFace() => new(FaceDetectResultType.NoFace, null);
    public static FaceDetectResult Spoofed() => new(FaceDetectResultType.Spoofed, null);
}