// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal interface IExportPal : IDisposable
    {
        byte[]? Export(X509ContentType contentType, SafePasswordHandle password);
        byte[] ExportPkcs12(Pkcs12ExportPbeParameters exportParameters, SafePasswordHandle password);
        byte[] ExportPkcs12(PbeParameters exportParameters, SafePasswordHandle password);
    }
}
