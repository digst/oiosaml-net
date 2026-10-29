using System;
using System.IO;

namespace DK.Gov.Oio.Saml.Configuration;

// Relative paths in the configuration are relative to the application's own folder, not to the process working directory,
// which differs between hosts (a Windows service runs in System32, a container in its WORKDIR).
internal static class ConfiguredPath
{
    public static string Resolve(string path) => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
