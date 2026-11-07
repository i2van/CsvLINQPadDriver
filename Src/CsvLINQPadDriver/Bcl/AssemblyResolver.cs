using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CsvLINQPadDriver.Bcl
{
    // LINQPad 5 does not apply binding redirects to drivers, so dependencies referencing
    // older versions of the shipped assemblies (e.g. CsvHelper -> System.Buffers 4.0.3.0)
    // are resolved to the same or newer versions located in the driver folder.
    internal static class AssemblyResolver
    {
        private static readonly string DriverDirectory = Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location)!;

        [ModuleInitializer]
        internal static void Initialize() =>
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                var requested = new AssemblyName(args.Name);

                var loaded = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => IsCompatible(assembly.GetName(), requested));

                if (loaded is not null)
                {
                    return loaded;
                }

                var path = Path.Combine(DriverDirectory, $"{requested.Name}.dll");

                return File.Exists(path) && IsCompatible(AssemblyName.GetAssemblyName(path), requested)
                    ? Assembly.LoadFrom(path)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsCompatible(AssemblyName candidate, AssemblyName requested) =>
            string.Equals(candidate.Name, requested.Name, StringComparison.OrdinalIgnoreCase) &&
            (requested.Version is null || candidate.Version >= requested.Version) &&
            (requested.GetPublicKeyToken() is not { Length: > 0 } requestedToken ||
            (candidate.GetPublicKeyToken() ?? []).SequenceEqual(requestedToken));
    }
}
