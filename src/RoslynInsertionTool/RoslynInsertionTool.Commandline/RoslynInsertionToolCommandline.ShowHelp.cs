// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.IO;
using Colorful;
using static Colorful.Console;

partial class RoslynInsertionToolCommandline
{
    private static void PrintSplashScreen()
    {
        var figlet = new Figlet(FigletFont.Load(File.OpenRead("cyberlarge.flf")));
        var roslyn = figlet.ToAscii("Roslyn");
        var insertion = figlet.ToAscii("Insertion");
        var tool = figlet.ToAscii("Tool");
        var red = 244;
        var green = 212;
        const int blue = 255;
        Write(roslyn, Color.FromArgb(red, green, blue));
        red -= 18;
        green -= 36;
        Write(insertion, Color.FromArgb(red, green, blue));
        red -= 18;
        green -= 36;
        Write(tool, Color.FromArgb(red, green, blue));
    }
}
