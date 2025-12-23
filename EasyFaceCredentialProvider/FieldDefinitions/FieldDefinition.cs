using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using EasyFaceCredentialProvider.Languages;

namespace EasyFaceCredentialProvider.FieldDefinitions;

internal class FieldDefinition
{
    public string Text { get; set; } = String.Empty;
    public CREDENTIAL_PROVIDER_FIELD_STATE State { get; set; }
    public CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE InteractiveState { get; set; }
    public CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR Descriptor { get; set; }

    public static readonly FieldDefinition[] Default =
    [
        new()
        {
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.TileImage,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_TILE_IMAGE,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni(Resource.TileImageDescription)),
                guidFieldType = Guid.Parse("{0x2d837775, 0xf6cd, 0x464e, {0xa7, 0x45, 0x48, 0x2f, 0xd0, 0xb4, 0x74, 0x93}}"),
            }
        },
        new()
        {
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.Label,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SMALL_TEXT,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Tooltip")),
                guidFieldType = Guid.Parse("{0x286bbff3, 0xbad4, 0x438f, {0xb0, 0x7, 0x79, 0xb7, 0x26, 0x7c, 0x3d, 0x48}}"),
            }
        },
        new()
        {
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_BOTH,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.LargeText,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_LARGE_TEXT,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("H3C credential provider")),
                guidFieldType = Guid.Empty,
            }
        },
        new()
        {
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.CameraSelector,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMBOBOX,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Camera selector")),
                guidFieldType = Guid.Empty,
            }
        },
        new()
        {
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_FOCUSED,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.Password,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_PASSWORD_TEXT,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni(Resource.PasswordTips)),
                guidFieldType = Guid.Empty,
            }
        },
        new()
        {
            Text = Resource.SubmitButtonContent,
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.SubmitButton,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_SUBMIT_BUTTON,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Submit button")),
                guidFieldType = Guid.Empty,
            }
        },
        new()
        {
            Text = Resource.EnableButtonContent,
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.EnableCommandLink,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMMAND_LINK,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Enable button")),
                guidFieldType = Guid.Parse("{0x088fa508, 0x94a6, 0x4430, {0xa4, 0xcb, 0x6f, 0xc6, 0xe3, 0xc0, 0xb9, 0xe2}}"),
            }
        },
        new()
        {
            Text = Resource.DisableButtonContent,
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.DisableCommandLink,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMMAND_LINK,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Disable button")),
                guidFieldType = Guid.Parse("{0x088fa508, 0x94a6, 0x4430, {0xa4, 0xcb, 0x6f, 0xc6, 0xe3, 0xc0, 0xb9, 0xe2}}"),
            }
        },
        new()
        {
            Text = Resource.ModifyButtonContent,
            State = CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN,
            InteractiveState = CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE.CPFIS_NONE,
            Descriptor = new()
            {
                dwFieldID = (uint)FieldIds.ModifyCommandLick,
                cpft = CREDENTIAL_PROVIDER_FIELD_TYPE.CPFT_COMMAND_LINK,
                pszLabel = new PWSTR(Marshal.StringToHGlobalUni("Modify button")),
                guidFieldType = Guid.Parse("{0x088fa508, 0x94a6, 0x4430, {0xa4, 0xcb, 0x6f, 0xc6, 0xe3, 0xc0, 0xb9, 0xe2}}"),
            }
        }
    ];
}