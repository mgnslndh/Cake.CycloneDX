// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Core;

namespace Cake.Testing.Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class WindowsTheoryAttribute : PlatformRestrictedTheoryAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsTheoryAttribute"/> class, restricting the test method to run only on Windows.
        /// </summary>
        /// <param name="reason">Optional reason explaining why the test is limited to Windows.</param>
        public WindowsTheoryAttribute(string reason = null)
            : base(PlatformFamily.Windows, false, reason)
        {
        }
    }
}