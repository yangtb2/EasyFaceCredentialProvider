namespace EasyFaceCredentialProvider;

internal static class Constants
{
    public const string ProviderGuid = "EF2D9D52-0C10-4A3D-B447-3CA44E2B1E9B";

    public const string NEGOSSP_NAME = "Negotiate";

    public const string RegVal_SelectedCamera = "SelectedCamera";

    public const string RegVal_FaceEnable = "FaceEnable";

    public const string RegVal_FaceData = "FaceData";

    public const string RegVal_Password = "00";

    public static readonly string RegistryPath = Path.Combine(
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers",
        Guid.Parse(Constants.ProviderGuid).ToString("B"));

}