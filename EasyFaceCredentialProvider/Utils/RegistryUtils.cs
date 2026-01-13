using Microsoft.Win32;

namespace EasyFaceCredentialProvider;

internal static class RegistryUtils
{
    /// <summary>
    /// Reads a value from the specified registry key and value name.
    /// </summary>
    /// <param name="keyPath">The path of the registry key.</param>
    /// <param name="valueName">The name of the value to read.</param>
    /// <returns>The value read from the registry, or null if not found.</returns>
    public static object? ReadRegistryValue(string keyPath, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: false);
            return key?.GetValue(valueName);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return null;
        }
    }

    /// <summary>
    /// Writes a value to the specified registry key and value name.
    /// </summary>
    /// <param name="keyPath">The path of the registry key.</param>
    /// <param name="valueName">The name of the value to write.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>True if the operation succeeds, otherwise false.</returns>
    public static bool WriteRegistryValue(string keyPath, string valueName, object value)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            if (key == null) return false;

            key.SetValue(valueName, value);
            return true;
        }
        catch(Exception e)
        {
            Log.Error(e);
            return false;
        }
    }

    /// <summary>
    /// Writes a value to the specified registry key and value name.
    /// </summary>
    /// <param name="keyPath">The path of the registry key.</param>
    /// <param name="valueName">The name of the value to write.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="kind">The value kind.</param>
    /// <returns>True if the operation succeeds, otherwise false.</returns>
    public static bool WriteRegistryValue(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(keyPath);
            if (key == null) return false;

            key.SetValue(valueName, value, kind);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
    }

    /// <summary>
    /// Deletes a value from the specified registry key.
    /// </summary>
    /// <param name="keyPath">The path of the registry key.</param>
    /// <param name="valueName">The name of the value to delete.</param>
    /// <returns>True if the operation succeeds, otherwise false.</returns>
    public static bool DeleteRegistryValue(string keyPath, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true);
            if (key == null) return false;

            key.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch(Exception e)
        {
            Log.Error(e);
            return false;
        }
    }
}