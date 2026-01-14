using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Security.Credentials;
using Windows.Win32.UI.Shell;
using Accord.Video.DirectShow;
using EasyFaceCredentialProvider.FieldDefinitions;
using EasyFaceCredentialProvider.Languages;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace EasyFaceCredentialProvider;

public class EasyFaceCredential : ICredentialProviderCredential2, ICredentialProviderCredentialWithFieldOptions
{
    private ICredentialProviderCredentialEvents? _events;
    private ICredentialProviderCredentialEvents2? _events2;
    private Action? _credentialChangedCallback;
    private readonly ICredentialProviderUser _user;
    private readonly CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus;
    private readonly string _userSid;
    private bool _faceEnable;
    private byte[] _faceData;
    private readonly string _userName;
    private List<FilterInfo> _cameras = new();
    private string _selectedCamera;
    private bool _selAuthenticate;
    private bool _isSelected;

    public unsafe EasyFaceCredential(ICredentialProviderUser user, CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, Action? credentialChangedCallback)
    {
        _user = user;
        _cpus = cpus;
        _credentialChangedCallback = credentialChangedCallback;

        var sid = new PWSTR();
        _user.GetSid(&sid);
        _userSid = sid.ToString();

        var userNameKey = new PROPERTYKEY()
        {
            fmtid = Guid.Parse("{0xDA520E51, 0xF4E9, 0x4739, {0xAC, 0x82, 0x02, 0xE0, 0xA9, 0x5C, 0x90, 0x30}}"),
            pid = 100
        };
        _user.GetStringValue(userNameKey, out var userName);
        _userName = userName.ToString();

        _selectedCamera =
            RegistryUtils.ReadRegistryValue(Constants.RegistryPath, Constants.RegVal_SelectedCamera) as string ??
            string.Empty;

        var registryPath = Path.Combine(Constants.RegistryPath, _userSid);
        _faceEnable = (RegistryUtils.ReadRegistryValue(registryPath, Constants.RegVal_FaceEnable) as bool?) ?? false;
        _faceData = (RegistryUtils.ReadRegistryValue(registryPath, Constants.RegVal_FaceData) as byte[]) ?? [];

        try
        {
            _cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        var field = FieldDefinition.Default[(uint)FieldIds.CameraSelector];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;

        field = FieldDefinition.Default[(uint)FieldIds.Password];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE;

        field = FieldDefinition.Default[(uint)FieldIds.SubmitButton];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE;

        field = FieldDefinition.Default[(uint)FieldIds.EnableCommandLink];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE;

        field = FieldDefinition.Default[(uint)FieldIds.DisableCommandLink];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;

        field = FieldDefinition.Default[(uint)FieldIds.ModifyCommandLick];
        field.State = _faceEnable
            ? CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_DISPLAY_IN_SELECTED_TILE
            : CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN;
    }

    public unsafe void GetUserSid(PWSTR* sid)
    {
        *sid = new PWSTR(Marshal.StringToCoTaskMemUni(_userSid));
    }

    public void Advise(ICredentialProviderCredentialEvents pcpce)
    {
        _events = pcpce;
        _events2 = pcpce as ICredentialProviderCredentialEvents2;
    }

    public void UnAdvise()
    {
        _events = null;
    }

    public unsafe void SetSelected(BOOL* pbAutoLogon)
    {
    }

    public void SetDeselected()
    {
    }

    public unsafe void GetFieldState(uint dwFieldID, CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
        CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis)
    {
        if (dwFieldID < (uint)FieldIds.FieldsCount)
        {
            *pcpfs = FieldDefinition.Default[dwFieldID].State;
            *pcpfis = FieldDefinition.Default[dwFieldID].InteractiveState;
        }
    }

    public unsafe void GetStringValue(uint dwFieldID, PWSTR* ppsz)
    {
        if (dwFieldID < (uint)FieldIds.FieldsCount)
        {
            *ppsz = new PWSTR(Marshal.StringToCoTaskMemUni(FieldDefinition.Default[dwFieldID].Text));
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public unsafe void GetBitmapValue(uint dwFieldID, HBITMAP* phbmp)
    {
        if (dwFieldID == (uint)FieldIds.TileImage)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("EasyFaceCredentialProvider.Resources.ProviderIcon.png");
            if (stream != null)
            {
                var bitmap = (Bitmap)Image.FromStream(stream);
                *phbmp = new HBITMAP(bitmap.GetHbitmap(Color.FromArgb(0x007f7f7f)));
                return;
            }
        }

        throw new NotImplementedException();
    }

    public unsafe void GetCheckboxValue(uint dwFieldID, BOOL* pbChecked, PWSTR* ppszLabel)
    {
        throw new NotImplementedException();
    }

    public void GetSubmitButtonValue(uint dwFieldID, out uint pdwAdjacentTo)
    {
        pdwAdjacentTo = (uint)FieldIds.Password;
    }

    public void GetComboBoxValueCount(uint dwFieldID, out uint pcItems, out uint pdwSelectedItem)
    {
        if (dwFieldID != (uint)FieldIds.CameraSelector)
        {
            throw new NotImplementedException();
        }

        if (!_cameras.Any())
        {
            pcItems = 1;
            pdwSelectedItem = 0;
            return;
        }

        pcItems = (uint)_cameras.Count + 1;
        pdwSelectedItem = 0;
        var selected = RegistryUtils.ReadRegistryValue(Constants.RegistryPath, Constants.RegVal_SelectedCamera) as string;
        if (!string.IsNullOrEmpty(selected))
        {
            var index = _cameras.FindIndex(c =>
                c.MonikerString.Equals(selected, StringComparison.InvariantCultureIgnoreCase));
            pdwSelectedItem = (uint)Math.Max(0, index);
        }
    }

    public unsafe void GetComboBoxValueAt(uint dwFieldID, uint dwItem, PWSTR* ppszItem)
    {
        if (dwFieldID != (uint)FieldIds.CameraSelector)
        {
            throw new NotImplementedException();
        }

        if (dwItem < _cameras.Count)
        {
            *ppszItem = new PWSTR(Marshal.StringToCoTaskMemUni($"{Resource.Camera}{dwItem:D}: {_cameras[(int)dwItem].Name}"));
        }
        else
        {
            *ppszItem = new PWSTR(Marshal.StringToCoTaskMemUni(Resource.RefreshCameraList));
        }
    }

    public void SetStringValue(uint dwFieldID, PCWSTR psz)
    {
        if (dwFieldID < (uint)FieldIds.FieldsCount)
        {
            FieldDefinition.Default[dwFieldID].Text = psz.ToString();
        }
    }

    public void SetCheckboxValue(uint dwFieldID, BOOL bChecked)
    {
        throw new NotImplementedException();
    }

    public void SetComboBoxSelectedValue(uint dwFieldID, uint dwSelectedItem)
    {
        if (dwFieldID != (uint)FieldIds.CameraSelector)
        {
            throw new NotImplementedException();
        }

        if (dwSelectedItem < _cameras.Count)
        {
            _selectedCamera = _cameras[(int)dwSelectedItem].MonikerString;
            RegistryUtils.WriteRegistryValue(Constants.RegistryPath, Constants.RegVal_SelectedCamera, _selectedCamera);
        }
        else if(dwSelectedItem == _cameras.Count)
        {
            _credentialChangedCallback?.Invoke();
        }
    }

    public void CommandLinkClicked(uint dwFieldID)
    {
        switch ((FieldIds)dwFieldID)
        {
            case FieldIds.EnableCommandLink:
                _EnableCommand();
                break;
            case FieldIds.DisableCommandLink:
                _DisableCommand();
                break;
            case FieldIds.ModifyCommandLick:
                _ModifyCommand();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public unsafe void GetSerialization(CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
        CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs, PWSTR* ppszOptionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
    {
        try
        {
            var splits = _userName.Split('\\');
            var domain = splits[0];
            var name = splits[1];
            var password = CredentialUtils.ProtectStringIfNecessary(FieldDefinition.Default[(int)FieldIds.Password].Text, _cpus);
            if (CredentialUtils.KerbInteractiveUnlockLogonPack(domain, name, password, _cpus, out var buffer, out var length))
            {
                if (CredentialUtils.RetrieveNegotiateAuthPackage(out var package))
                {
                    pcpcs->rgbSerialization = (byte*)buffer;
                    pcpcs->cbSerialization = (uint)length;
                    pcpcs->ulAuthenticationPackage = package;
                    pcpcs->clsidCredentialProvider = Guid.Parse(Constants.ProviderGuid);
                    *pcpgsr = CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE.CPGSR_RETURN_CREDENTIAL_FINISHED;
                }
                else
                {
                    Marshal.FreeCoTaskMem(buffer);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public unsafe void ReportResult(NTSTATUS ntsStatus, NTSTATUS ntsSubstatus, PWSTR* ppszOptionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon)
    {
        Log.Info($"ReportResult:{ntsStatus}\t{ntsSubstatus}");
        throw new NotImplementedException();
    }

    public unsafe void GetFieldOptions(uint fieldID, CREDENTIAL_PROVIDER_CREDENTIAL_FIELD_OPTIONS* options)
    {
        throw new NotImplementedException();
    }

    private unsafe async void _EnableCommand()
    {
        _SetUIforEnableCommand();

        uint lastError = 0;
        while (true)
        {
            lastError = CredentialUtils.GetUnPackCredentialFromPrompt(_userName, lastError, out var buffer,
                out var size);
            if (0 == lastError)
            {
                if (CredentialUtils.GetUnpackCredentialString(buffer, size, out string userName, out string domain,
                        out var password))
                {
                    if (string.IsNullOrEmpty(domain))
                    {
                        var splits = userName.Split('\\');
                        domain = splits[0];
                        userName = splits[1];
                    }
                    if (PInvoke.LogonUser(userName, domain, password, LOGON32_LOGON.LOGON32_LOGON_NETWORK,
                            LOGON32_PROVIDER.LOGON32_PROVIDER_DEFAULT, out var token))
                    {
                        token.Close();
                        RegistryUtils.WriteRegistryValue(Path.Combine(Constants.RegistryPath, _userSid),
                            Constants.RegVal_Password, password, RegistryValueKind.String);
                        break;
                    }
                }

                lastError = (uint)Marshal.GetLastPInvokeError();
            }
            else
            {
                if (lastError == 1223) //cancelled
                {
                    _SetUIforDefault();
                    return;
                }
            }
        }

        try
        {
            //while (_isSelected)
            //{

            //}
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    private void _SetUIforDefault()
    {
        var events2 = _events2;
        var events = _events2 ?? _events;
        events2?.BeginFieldUpdates();
        events?.SetFieldState(this, (uint)FieldIds.EnableCommandLink, FieldDefinition.Default[(int)FieldIds.EnableCommandLink].State);
        events?.SetFieldState(this, (uint)FieldIds.SubmitButton, FieldDefinition.Default[(int)FieldIds.SubmitButton].State);
        events?.SetFieldState(this, (uint)FieldIds.Password, FieldDefinition.Default[(int)FieldIds.Password].State);
        events?.SetFieldString(this, (uint)FieldIds.LargeText, new PWSTR(Marshal.StringToCoTaskMemUni(FieldDefinition.Default[(int)FieldIds.LargeText].Text)));
        events2?.EndFieldUpdates();

    }

    private void _SetUIforEnableCommand()
    {
        var events2 = _events2;
        var events = _events2 ?? _events;
        events2?.BeginFieldUpdates();
        events?.SetFieldState(this, (uint)FieldIds.EnableCommandLink, CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN);
        events?.SetFieldState(this, (uint)FieldIds.SubmitButton, CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN);
        events?.SetFieldState(this, (uint)FieldIds.Password, CREDENTIAL_PROVIDER_FIELD_STATE.CPFS_HIDDEN);
        events?.SetFieldString(this, (uint)FieldIds.LargeText, new PWSTR(Marshal.StringToCoTaskMemUni(Resource.FaceRecognizing)));
        events2?.EndFieldUpdates();
    }

    private void _DisableCommand()
    {
    }

    private void _ModifyCommand()
    {

    }
}