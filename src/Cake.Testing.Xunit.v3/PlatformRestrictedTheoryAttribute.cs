// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Core;
using Cake.Core.Polyfill;
using Xunit;

namespace Cake.Testing.Xunit
{
    public abstract class PlatformRestrictedTheoryAttribute : TheoryAttribute
    {
        private static readonly PlatformFamily _family;
#if !XUNIT3
        private string _skip;
#endif

        /// <summary>
        /// Initializes the static platform family field with the current platform family.
        /// </summary>
        static PlatformRestrictedTheoryAttribute()
        {
            _family = EnvironmentHelper.GetPlatformFamily();
        }

        /// <summary>
        /// Initializes the attribute to conditionally skip a theory test based on the current platform family.
        /// </summary>
        /// <param name="requiredFamily">The platform family required for the test to run.</param>
        /// <param name="invert">If true, skips the test on the specified platform family instead of requiring it.</param>
        /// <param name="reason">Optional custom reason for skipping the test; if not provided, a default message is generated.</param>
        protected PlatformRestrictedTheoryAttribute(
            PlatformFamily requiredFamily,
            bool invert,
            string reason = null)
        {
            if ((requiredFamily != _family) ^ invert)
            {
                if (string.IsNullOrEmpty(reason))
                {
                    var platformName = Enum.GetName(typeof(PlatformFamily), requiredFamily);
                    if (invert)
                    {
                        platformName = $"Non-{platformName}";
                    }

                    reason = $"{platformName} test.";
                }

                Reason = reason;
#if XUNIT3
                if (!string.IsNullOrEmpty(reason) && string.IsNullOrEmpty(Skip))
                {
                    Skip = reason;
                }
#endif
            }
        }

        protected string Reason { get; }

#if !XUNIT3
        public override string Skip
        {
            get => _skip ?? Reason;
            set => _skip = value;
        }
#endif
    }
}