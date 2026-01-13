using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Security.Credentials;
using Windows.Win32.UI.Shell;
using EasyFaceCredentialProvider.Languages;

namespace EasyFaceCredentialProvider;

public static class CredentialUtils
{
    public static unsafe bool GetUnPackCredentialFromPrompt(string userName, out void* buffer, out uint size)
    {
        uint inCredSize = 1024;
        IntPtr inCredBuffer = IntPtr.Zero;
        IntPtr tipString = IntPtr.Zero;
        buffer = (void*)0;
        size = 0;
        try
        {
            inCredBuffer = Marshal.AllocCoTaskMem((int)inCredSize);
            if (!PInvoke.CredPackAuthenticationBuffer(CRED_PACK_FLAGS.CRED_PACK_GENERIC_CREDENTIALS,
                    new PWSTR(Marshal.StringToCoTaskMemUni(userName)),
                    new PWSTR(Marshal.StringToCoTaskMemUni(string.Empty)),
                    (byte*)inCredBuffer,
                    &inCredSize))
            {
                Log.Info(
                    $"CredPackAuthenticationBuffer failed, {Marshal.GetLastPInvokeError()}:{Marshal.GetLastPInvokeErrorMessage()}");
                return false;
            }

            if (!RetrieveNegotiateAuthPackage(out var packageId))
            {
                return false;
            }

            tipString = Marshal.StringToCoTaskMemUni(Resource.ValidataPassword);
            var credUiInfo = new CREDUI_INFOW()
            {
                cbSize = (uint)Marshal.SizeOf<CREDUI_INFOW>(),
                pszMessageText = new PWSTR(tipString),
                hwndParent = HWND.Null,
            };
            var hr = PInvoke.CredUIPromptForWindowsCredentials(
                credUiInfo,
                0,
                ref packageId,
                new ReadOnlySpan<byte>((void*)inCredBuffer, (int)inCredSize),
                out buffer,
                out size,
                CREDUIWIN_FLAGS.CREDUIWIN_GENERIC);

            if (hr != 0)
            {
                Log.Info($"CredUIPromptForWindowsCredentials failed, {hr}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
        finally
        {
            if (inCredBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(inCredBuffer);
            }

            if (tipString != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(tipString);
            }
        }
    }

    public static unsafe bool GetUnpackCredentialString(void* buffer, uint size, out string userName, out string domain, out string password)
    {
        userName = string.Empty;
        domain = string.Empty;
        password = string.Empty;
        IntPtr userNameBuffer = IntPtr.Zero;
        IntPtr domainBuffer = IntPtr.Zero;
        IntPtr passwordBuffer = IntPtr.Zero;
        uint maxUserName = 1024;
        uint maxDomainName = 1024;
        uint maxPassword = 1024;
        try
        {
            var buffer1 = Marshal.AllocCoTaskMem((int)maxUserName);
            var sc1 = new Span<char>((void*)buffer1, (int)maxUserName);
            var buffer2 = Marshal.AllocCoTaskMem((int)maxDomainName);
            var sc2 = new Span<char>((void*)buffer2, (int)maxDomainName);
            var buffer3 = Marshal.AllocCoTaskMem((int)maxPassword);
            var sc3 = new Span<char>((void*)buffer3, (int)maxPassword);
            if (!PInvoke.CredUnPackAuthenticationBuffer(
                    CRED_PACK_FLAGS.CRED_PACK_GENERIC_CREDENTIALS,
                    new ReadOnlySpan<byte>(buffer, (int)size),
                    sc1,
                    ref maxUserName,
                    sc2,
                    ref maxDomainName,
                    sc3,
                    ref maxPassword))
            {
                Log.Info(
                    $"CredUnPackAuthenticationBuffer failed, {Marshal.GetLastPInvokeError()}:{Marshal.GetLastPInvokeErrorMessage()}");
                return false;
            }
            userName = sc1[..(int)maxUserName].ToString();
            domain = sc2[..(int)maxDomainName].ToString();
            password = sc3[..(int)maxPassword].ToString();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
        finally
        {
            if (userNameBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(userNameBuffer);
            }
            if (domainBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(domainBuffer);
            }
            if (passwordBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(passwordBuffer);
            }
        }
    }

    public static bool RetrieveNegotiateAuthPackage(out uint authPackage)
    {
        authPackage = 0;
        try
        {
            if (PInvoke.LsaConnectUntrusted(out var handle).SeverityCode is NTSTATUS.Severity.Success)
            {
                IntPtr nameString = IntPtr.Zero;
                try
                {
                    nameString = Marshal.StringToCoTaskMemAnsi(Constants.NEGOSSP_NAME);
                    var name = new LSA_STRING()
                    {
                        Buffer = new PSTR(nameString),
                        Length = (ushort)(Constants.NEGOSSP_NAME.Length),
                        MaximumLength = (ushort)(Constants.NEGOSSP_NAME.Length + 1),
                    };
                    if (PInvoke.LsaLookupAuthenticationPackage(handle, name, out var package).SeverityCode is NTSTATUS.Severity.Success)
                    {
                        authPackage = package;
                        return true;
                    }
                    Marshal.FreeCoTaskMem(nameString);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    if (nameString != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(nameString);
                    }
                }
                finally
                {
                    PInvoke.LsaDeregisterLogonProcess(handle);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return false;
    }

    public static bool KerbInteractiveUnlockLogonPack(string domain, string name, string password,
        CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, out IntPtr buffer, out int length)
    {
        buffer = IntPtr.Zero;
        length = 0;
        var kiul = new KERB_INTERACTIVE_UNLOCK_LOGON();
        try
        {
            length = Marshal.SizeOf(kiul) + domain.Length * 2 + name.Length * 2 + password.Length * 2;
            buffer = Marshal.AllocCoTaskMem(length);
            kiul.Logon.MessageType = cpus switch
            {
                CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_UNLOCK_WORKSTATION => KERB_LOGON_SUBMIT_TYPE
                    .KerbWorkstationUnlockLogon,
                CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_LOGON => KERB_LOGON_SUBMIT_TYPE.KerbInteractiveLogon,
                CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI => 0,
                _ => throw new Exception()
            };

            var offset = Marshal.SizeOf(kiul);
            kiul.Logon.LogonDomainName = new()
            {
                Buffer = new PWSTR(offset),
                Length = (ushort)(domain.Length * 2),
                MaximumLength = (ushort)(domain.Length * 2)
            };
            Marshal.Copy(domain.ToCharArray(), 0, buffer + offset, domain.Length);

            offset += domain.Length * 2;
            kiul.Logon.UserName = new()
            {
                Buffer = new PWSTR(offset),
                Length = (ushort)(name.Length * 2),
                MaximumLength = (ushort)(name.Length * 2)
            };
            Marshal.Copy(name.ToCharArray(), 0, buffer + offset, name.Length);

            offset += name.Length * 2;
            kiul.Logon.Password = new()
            {
                Buffer = new PWSTR(offset),
                Length = (ushort)(password.Length * 2),
                MaximumLength = (ushort)(password.Length * 2)
            };
            Marshal.Copy(password.ToCharArray(), 0, buffer + offset, password.Length);
            Marshal.StructureToPtr(kiul, buffer, true);
        }
        catch (Exception e)
        {
            Log.Error(e);

            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(buffer);
            }

            return false;
        }

        return true;
    }

    public static string ProtectStringIfNecessary(string password, CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        try
        {
            var strMem = Marshal.StringToCoTaskMemUni(password);
            try
            {
                if ((PInvoke.CredIsProtected(new PWSTR(strMem), out var type) &&
                     type is not CRED_PROTECTION_TYPE.CredUnprotected)
                    || cpus == CREDENTIAL_PROVIDER_USAGE_SCENARIO.CPUS_CREDUI)
                {
                    return password;
                }
                else
                {
                    return _ProtectString(password);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(strMem);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        return String.Empty;
    }

    private static string _ProtectString(string password)
    {
        try
        {
            var strMem = Marshal.StringToCoTaskMemUni(password);
            try
            {
                var result = new Span<char>();
                uint counts = 0;
                var pw = new PWSTR(strMem);
                PInvoke.CredProtect(false, pw, (uint)(pw.Length + 1), result, ref counts);
                result = new Span<char>(new char[counts]);
                return PInvoke.CredProtect(false, pw, (uint)(pw.Length + 1), result, ref counts)
                    ? result.ToString()
                    : throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                Marshal.FreeCoTaskMem(strMem);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            return string.Empty;
        }
    }
}