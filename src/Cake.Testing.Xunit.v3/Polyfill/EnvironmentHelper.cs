// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Cake.Core.Polyfill
{
    internal static class EnvironmentHelper
    {
        private static readonly FrameworkName NetStandardFramework = new FrameworkName(".NETStandard,Version=v2.0");
        private static bool? _isCoreClr;
        private static FrameworkName _netCoreAppFramework;

        /// <summary>
        /// Determines whether the current operating system is 64-bit (x64 or ARM64).
        /// </summary>
        /// <returns>True if the OS architecture is x64 or ARM64; otherwise, false.</returns>
        public static bool Is64BitOperativeSystem()
        {
            return RuntimeInformation.OSArchitecture == Architecture.X64
                   || RuntimeInformation.OSArchitecture == Architecture.Arm64;
        }

        /// <summary>
        /// Determines the current platform family by checking for OSX, Linux, Windows, or FreeBSD support.
        /// </summary>
        /// <returns>The detected <see cref="PlatformFamily"/>; returns <c>PlatformFamily.Unknown</c> if the platform cannot be determined.</returns>
        public static PlatformFamily GetPlatformFamily()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return PlatformFamily.OSX;
                }
            }
            catch (PlatformNotSupportedException)
            {
            }
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return PlatformFamily.Linux;
                }
            }
            catch (PlatformNotSupportedException)
            {
            }
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return PlatformFamily.Windows;
                }
            }
            catch (PlatformNotSupportedException)
            {
            }
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                {
                    return PlatformFamily.FreeBSD;
                }
            }
            catch (PlatformNotSupportedException)
            {
            }

            return PlatformFamily.Unknown;
        }

        /// <summary>
        /// Determines whether the current runtime is CoreCLR (.NET Core or later).
        /// </summary>
        /// <returns>True if running on CoreCLR; otherwise, false.</returns>
        public static bool IsCoreClr()
        {
            if (_isCoreClr == null)
            {
                _isCoreClr = Environment.Version.Major >= 5
                             || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");
            }
            return _isCoreClr.Value;
        }

        /// <summary>
        /// Determines whether the specified platform family represents Windows.
        /// </summary>
        /// <param name="family">The platform family to check.</param>
        /// <returns>True if the platform family is Windows; otherwise, false.</returns>
        public static bool IsWindows(PlatformFamily family)
        {
            return family == PlatformFamily.Windows;
        }

        /// <summary>
        /// Determines whether the current operating system is a Unix-like platform (Linux, OSX, or FreeBSD).
        /// </summary>
        /// <returns>True if the OS is Linux, OSX, or FreeBSD; otherwise, false.</returns>
        public static bool IsUnix()
        {
            return IsUnix(GetPlatformFamily());
        }

        /// <summary>
        /// Determines whether the specified platform family is a Unix-like operating system (Linux, OSX, or FreeBSD).
        /// </summary>
        /// <param name="family">The platform family to check.</param>
        /// <returns>True if the platform family is Linux, OSX, or FreeBSD; otherwise, false.</returns>
        public static bool IsUnix(PlatformFamily family)
        {
            return family == PlatformFamily.Linux
                   || family == PlatformFamily.OSX
                   || family == PlatformFamily.FreeBSD;
        }

        /// <summary>
        /// Determines whether the specified platform family is macOS (OSX).
        /// </summary>
        /// <param name="family">The platform family to check.</param>
        /// <returns>True if the platform family is OSX; otherwise, false.</returns>
        public static bool IsOSX(PlatformFamily family)
        {
            return family == PlatformFamily.OSX;
        }

        /// <summary>
        /// Determines whether the specified platform family is Linux.
        /// </summary>
        /// <param name="family">The platform family to check.</param>
        /// <returns>True if the platform family is Linux; otherwise, false.</returns>
        public static bool IsLinux(PlatformFamily family)
        {
            return family == PlatformFamily.Linux;
        }

        /// <summary>
        /// Determines whether the specified platform family is FreeBSD.
        /// </summary>
        /// <param name="family">The platform family to check.</param>
        /// <returns>True if the platform family is FreeBSD; otherwise, false.</returns>
        public static bool IsFreeBSD(PlatformFamily family)
        {
            return family == PlatformFamily.FreeBSD;
        }

        /// <summary>
        /// Determines the current runtime type.
        /// </summary>
        /// <returns>The runtime as <see cref="Runtime.CoreClr"/> if running on CoreCLR; otherwise, <see cref="Runtime.Clr"/>.</returns>
        public static Runtime GetRuntime()
        {
            if (IsCoreClr())
            {
                return Runtime.CoreClr;
            }
            return Runtime.Clr;
        }

        /// <summary>
        /// Determines the framework version the application is running on by inspecting the assembly path of <c>System.Runtime.GCSettings</c>.
        /// </summary>
        /// <returns>
        /// A <see cref="FrameworkName"/> representing the detected .NET Core App framework version, or .NET Standard 2.0 if the version cannot be determined.
        /// </returns>
        public static FrameworkName GetBuiltFramework()
        {
            if (_netCoreAppFramework != null)
            {
                return _netCoreAppFramework;
            }

            var assemblyPath = typeof(System.Runtime.GCSettings)?.GetTypeInfo()
                ?.Assembly
#if NETCOREAPP3_1
                ?.CodeBase;
#else
#pragma warning disable 0618
                ?.Location;
#pragma warning restore 0618
#endif
            if (string.IsNullOrEmpty(assemblyPath))
            {
                return NetStandardFramework;
            }

            const string microsoftNetCoreApp = "Microsoft.NETCore.App";
            var runtimeBasePathLength = assemblyPath.IndexOf(microsoftNetCoreApp) + microsoftNetCoreApp.Length + 1;
            var netCoreAppVersion = string.Concat(assemblyPath.Skip(runtimeBasePathLength).Take(3));
            if (string.IsNullOrEmpty(netCoreAppVersion))
            {
                return NetStandardFramework;
            }

            return _netCoreAppFramework = Version.TryParse(netCoreAppVersion, out var version)
                                            ? new FrameworkName(".NETCoreApp", version)
                                            : NetStandardFramework;
        }
    }
}