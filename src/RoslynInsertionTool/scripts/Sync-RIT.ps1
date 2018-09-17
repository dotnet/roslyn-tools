##########################################################################
# Changes to this file will NOT be automatically deployed to the server. #
#                                                                        #
# Changes should be made on both the server and in source control.       #
##########################################################################

Add-Type -TypeDefinition @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class shlwapi
{
    [DllImport("shlwapi.dll", BestFitMapping = false, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, EntryPoint = "StrCmpLogicalW", ExactSpelling = true, PreserveSig = true, SetLastError = false, ThrowOnUnmappableChar = false)]
    public static extern int StrCmpLogical(
        [In, MarshalAs(UnmanagedType.LPWStr)] string psz1,
        [In, MarshalAs(UnmanagedType.LPWStr)] string psz2
    );
}

public struct LogicalStringComparer : IComparer, IComparer<string>
{
    public static object[] Sort(object[] items)
    {
        var comparer = new LogicalStringComparer();
        Array.Sort(items, comparer);
        return items;
    }

    public int Compare(object x, object y)
    {
        return Compare(x.ToString(), y.ToString());
    }

    public int Compare(string x, string y)
    {
        return shlwapi.StrCmpLogical(x, y);
    }
}
"@

$syncBranchName = "master"
$syncLocation = "E:\prebuilt\roslyn-tools\RIT"
$arrayList = Get-ChildItem -Path "\\cpvsbuild\drops\roslyn\Roslyn-Tools\$syncBranchName"
$latestBuild = [LogicalStringComparer]::Sort($arrayList)[-1].FullName

robocopy "$latestBuild\bin\RIT\net46" $syncLocation /MIR /FFT /Z /XA:H /W:5
