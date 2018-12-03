using System.IO;

namespace roslyn.optprof.runsettings.generator
{
    internal class FileWriter : IFileWriter
    {
        public int WriteOutFile(string outputFolder, string runSettings)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string filePath = Path.Combine(outputFolder, "RoslynOptProf.runsettings");
            File.WriteAllText(filePath, runSettings);

            return 0;
        }
    }
}
