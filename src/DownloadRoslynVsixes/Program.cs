// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace DownloadRoslynVsixes
{
    class Program
    {
        static void Main(string[] args)
        {
            string destinationFolder = null;

            if (args.Length == 1)
            {
                destinationFolder = args[0];
            }
            else
            {
                destinationFolder = Environment.CurrentDirectory;

            }

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            using (var client = new WebClient())
            {
                var rssFeed = client.DownloadString("https://dotnet.myget.org/F/roslyn/vsix");
                var doc = XDocument.Parse(rssFeed);
                var entries = doc.Elements().First().Elements(XName.Get("entry", "http://www.w3.org/2005/Atom"));
                var roslynLKG = (from entry in entries
                                 where entry.Element(XName.Get("title", "http://www.w3.org/2005/Atom")).Value == "Roslyn Insiders for VS next"
                                 select entry.Element(XName.Get("content", "http://www.w3.org/2005/Atom")).Attribute("src").Value).First();
                var roslynLKGVersion = roslynLKG.Replace("https://dotnet.myget.org/F/roslyn/vsix/0b48e25b-9903-4d8b-ad39-d4cca196e3c7-", "").Replace(".vsix", "");
                var testLKG = $"https://dotnet.myget.org/F/roslyn/vsix/d0122878-51f1-4b36-95ec-dec2079a2a84-{roslynLKGVersion}.vsix";

                var vsixFolder = Path.Combine(destinationFolder, "Roslyn");
                if (!Directory.Exists(vsixFolder))
                {
                    Directory.CreateDirectory(vsixFolder);
                }

                var roslynVsixPath = Path.Combine(destinationFolder, "Roslyn", $"Roslyn.Deployment.Full.Next.vsix");
                if (!File.Exists(roslynVsixPath))
                {
                    Console.WriteLine($"Downloading Roslyn.Deployment.Full.Next.vsix from '{roslynLKG}' to '{roslynVsixPath}'");
                    client.DownloadFile(roslynLKG, roslynVsixPath);

                    Console.WriteLine($"Unzipping '{roslynVsixPath}' to '{vsixFolder}'");
                    ZipFile.ExtractToDirectory(roslynVsixPath, vsixFolder);
                }
                else
                {
                    Console.WriteLine($"Roslyn.Deployment.Full.Next.vsix already exists at location '{roslynVsixPath}'");
                }

                var testVsixPath = Path.Combine(destinationFolder, "Roslyn", "Vsixes", $"Microsoft.VisualStudio.IntegrationTest.Setup.vsix");
                if (!File.Exists(testVsixPath))
                {
                    Console.WriteLine($"Downloading Microsoft.VisualStudio.IntegrationTest.Setup.vsix from '{testLKG}' to '{testVsixPath}'");
                    client.DownloadFile(testLKG, testVsixPath);
                }
                else
                {
                    Console.WriteLine($"Microsoft.VisualStudio.IntegrationTest.Setup.vsix already exists at location '{testVsixPath}'");
                }

                Console.WriteLine($"Deployed Roslyn Vsixes to {Path.Combine(destinationFolder, "Roslyn", "Vsixes")}");
            }
        }
    }
}
