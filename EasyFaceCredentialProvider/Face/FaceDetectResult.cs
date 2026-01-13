namespace EasyFaceCredentialProvider;

public enum FaceDetectResultType
{
    Success,
    NoFace,
    Spoofed,
}

public record FaceDetectResult(FaceDetectResultType ResultType, float Similarity, float[]? Data)
{
    public static FaceDetectResult Success(float similarity, float[] data) => new(FaceDetectResultType.Success, similarity, data);
    public static FaceDetectResult NoFace() => new(FaceDetectResultType.NoFace, 0, null);
    public static FaceDetectResult Spoofed() => new(FaceDetectResultType.Spoofed, 0, null);
}