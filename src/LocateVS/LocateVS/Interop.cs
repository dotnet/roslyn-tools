// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace LocateVS
{
    internal static class Interop
    {
        public const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        [DllImport("x64\\Microsoft.VisualStudio.Setup.Configuration.Native.dll", BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetSetupConfiguration", ExactSpelling = true, PreserveSig = true, SetLastError = false, ThrowOnUnmappableChar = false)]
        public static extern int GetSetupConfiguration_x64(
            [Out, MarshalAs(UnmanagedType.Interface)] out ISetupConfiguration setupConfiguration,
            [In] IntPtr pReserved
        );

        [DllImport("x86\\Microsoft.VisualStudio.Setup.Configuration.Native.dll", BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetSetupConfiguration", ExactSpelling = true, PreserveSig = true, SetLastError = false, ThrowOnUnmappableChar = false)]
        public static extern int GetSetupConfiguration_x86(
            [Out, MarshalAs(UnmanagedType.Interface)] out ISetupConfiguration setupConfiguration,
            [In] IntPtr pReserved
        );

        public static void GetSetupConfiguration(out ISetupConfiguration setupConfiguration, IntPtr reserved)
        {
            if (Environment.Is64BitProcess)
            {
                GetSetupConfiguration_x64(out setupConfiguration, reserved);
            }
            else
            {
                GetSetupConfiguration_x86(out setupConfiguration, reserved);
            }
        }
    }
}
