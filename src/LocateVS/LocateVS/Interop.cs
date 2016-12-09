// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace LocateVS
{
    internal static class Interop
    {
        public const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.x64.dll")]
        public static extern void GetSetupConfiguration_x64([MarshalAs(UnmanagedType.Interface)] out ISetupConfiguration setupConfiguration);

        [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.x86.dll")]
        public static extern void GetSetupConfiguration_x86([MarshalAs(UnmanagedType.Interface)] out ISetupConfiguration setupConfiguration);

        public static void GetSetupConfiguration(out ISetupConfiguration setupConfiguration)
        {
            if (Environment.Is64BitProcess)
            {
                GetSetupConfiguration_x64(out setupConfiguration);
            }
            else
            {
                GetSetupConfiguration_x86(out setupConfiguration);
            }
        }
    }
}
