namespace roslyn.optprof.runsettings.generator
{
    public interface IFileWriter
    {
        int WriteOutFile(string outputFolder, string runSettings);
    }
}