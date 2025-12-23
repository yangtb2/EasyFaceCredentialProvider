using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using EasyFaceCredentialProvider.FieldDefinitions;

namespace EasyFaceCredentialProvider;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("H3CCredentialProvider")]
[Guid(Constants.ProviderGuid)]
public class EasyFaceCredentialProvider : ICredentialProvider, ICredentialProviderSetUserArray
{
    private CREDENTIAL_PROVIDER_USAGE_SCENARIO _cpus;
    private readonly List<EasyFaceCredential> _credentials = new();
    private ICredentialProviderUserArray? _userArray;
    private ICredentialProviderEvents? _advise;
    private UIntPtr _adviseContext;
    private bool _recreateCredentials;

    public EasyFaceCredentialProvider()
    {
    }

    ~EasyFaceCredentialProvider()
    {
    }

    public void SetUsageScenario(CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, uint dwFlags)
    {
        _cpus = cpus;
        switch (cpus)
        {
            case CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_LOGON:
            case CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_UNLOCK_WORKSTATION:
                _recreateCredentials = true;
                return;
            case CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CHANGE_PASSWORD:
            case CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI:
                throw new NotImplementedException();
            default:
                throw new InvalidOperationException();
        }
    }

    public unsafe void SetSerialization(CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs)
    {
        throw new NotImplementedException();
    }

    public void Advise(ICredentialProviderEvents pcpe, UIntPtr upAdviseContext)
    {
        _advise = pcpe;
        _adviseContext = upAdviseContext;
    }

    public void UnAdvise()
    {
        _advise = null;
        _adviseContext = UIntPtr.Zero;
    }

    public void GetFieldDescriptorCount(out uint pdwCount)
    {
        pdwCount = (uint)FieldIds.FieldsCount;
    }

    public unsafe void GetFieldDescriptorAt(uint dwIndex, CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd)
    {
        if (dwIndex < (uint)FieldIds.FieldsCount && ppcpfd != null)
        {
            try
            {
                *ppcpfd = (CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR*)Marshal.AllocCoTaskMem(
                    Marshal.SizeOf<CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR>());
                try
                {
                    var description = FieldDefinition.Default[dwIndex].Descriptor;
                    description.pszLabel = new PWSTR(Marshal.StringToCoTaskMemUni(description.pszLabel.ToString()));
                    Marshal.StructureToPtr(description, (IntPtr)(*ppcpfd), false);
                }
                catch
                {
                    Marshal.FreeCoTaskMem((IntPtr)(*ppcpfd));
                    throw;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw new InvalidOperationException();
            }
        }
    }

    public unsafe void GetCredentialCount(out uint pdwCount, out uint pdwDefault, BOOL* pbAutoLogonWithDefault)
    {
        pdwDefault = unchecked((uint)(-1));
        *pbAutoLogonWithDefault = false;
        if (_recreateCredentials)
        {
            _EnumCredentials();
        }
        pdwCount = (uint)_credentials.Count;
    }

    public void GetCredentialAt(uint dwIndex, out ICredentialProviderCredential ppcpc)
    {
        ppcpc =  _credentials[(int)dwIndex];
    }

    public void SetUserArray(ICredentialProviderUserArray users)
    {
        _userArray = users;
    }

    private void _EnumCredentials()
    {
        if (_userArray == null)
        {
            throw new InvalidOperationException();
        }

        _userArray.GetCount(out var count);
        _credentials.Clear();
        for (uint i = 0; i < count; i++)
        {
            try
            {
                _userArray.GetAt(i, out var user);
                var credential = new EasyFaceCredential(user, _cpus, CredentialChangedCallback);
                _credentials.Add(credential);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }

    private void CredentialChangedCallback()
    {
        _advise?.CredentialsChanged(_adviseContext);
    }
}