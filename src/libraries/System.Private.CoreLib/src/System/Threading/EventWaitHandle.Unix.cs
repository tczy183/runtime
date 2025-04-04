// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public partial class EventWaitHandle
    {
        private void CreateEventCore(bool initialState, EventResetMode mode)
        {
            ValidateMode(mode);
            SafeWaitHandle = WaitSubsystem.NewEvent(initialState, mode);
        }

#pragma warning disable IDE0060 // Unused parameter
        private void CreateEventCore(
            bool initialState,
            EventResetMode mode,
            string? name,
            NamedWaitHandleOptionsInternal options,
            out bool createdNew)
        {
            ValidateMode(mode);

            if (name != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
            }

            SafeWaitHandle = WaitSubsystem.NewEvent(initialState, mode);
            createdNew = true;
        }
#pragma warning restore IDE0060

        private static OpenExistingResult OpenExistingWorker(
            string name,
            NamedWaitHandleOptionsInternal options,
            out EventWaitHandle? result)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
        }

        public bool Reset()
        {
            SafeWaitHandle waitHandle = ValidateHandle();
            try
            {
                WaitSubsystem.ResetEvent(waitHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        public bool Set()
        {
            SafeWaitHandle waitHandle = ValidateHandle();
            try
            {
                WaitSubsystem.SetEvent(waitHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        internal static bool Set(SafeWaitHandle waitHandle)
        {
            waitHandle.DangerousAddRef();
            try
            {
                WaitSubsystem.SetEvent(waitHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        private SafeWaitHandle ValidateHandle()
        {
            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle waitHandle = SafeWaitHandle;
            if (waitHandle.IsInvalid)
            {
                ThrowInvalidHandleException();
            }

            waitHandle.DangerousAddRef();
            return waitHandle;
        }
    }
}
