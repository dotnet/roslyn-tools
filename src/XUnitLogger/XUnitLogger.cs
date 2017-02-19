using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

[FriendlyName(FriendlyName)]
[ExtensionUri(ExtensionUri)]
internal class XUnitLogger : ITestLoggerWithParameters
{
    /// <summary>
    /// Uri used to uniquely identify the logger.
    /// </summary>
    public const string ExtensionUri = "logger://Microsoft/TestPlatform/XUnitLogger/v1";

    /// <summary>
    /// Alternate user friendly string to uniquely identify the console logger.
    /// </summary>
    public const string FriendlyName = "xunit";

    /// <summary>
    /// Prefix of the data collector
    /// </summary>
    public const string DataCollectorUriPrefix = "dataCollector://";

    public const string LogFilePathKey = "LogFilePath";
    public const string EnvironmentKey = "Environment";
    public const string XUnitVersionKey = "XUnitVersion";

    private string _outputFilePath;
    private string _environmentOpt;
    private string _xunitVersionOpt;

    private readonly object _resultsGuard = new object();
    private List<TestResultInfo> _results;
    private DateTime _localStartTime;

    private struct TestResultInfo
    {
        public readonly TestOutcome Outcome;
        public readonly string AssemblyPath;
        public readonly string Type;
        public readonly string Method;
        public readonly string Name;
        public readonly TimeSpan Time;
        public readonly string ErrorMessage;
        public readonly string ErrorStackTrace;
        public readonly TraitCollection Traits;

        public TestResultInfo(
            TestOutcome outcome,
            string assemblyPath,
            string type,
            string method,
            string name,
            TimeSpan time,
            string errorMessage,
            string errorStackTrace,
            TraitCollection traits)
        {
            Outcome = outcome;
            AssemblyPath = assemblyPath;
            Type = type;
            Method = method;
            Name = name;
            Time = time;
            ErrorMessage = errorMessage;
            ErrorStackTrace = errorStackTrace;
            Traits = traits;
        }
    }

    public void Initialize(TestLoggerEvents events, string testResultsDirPath)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        if (testResultsDirPath == null)
        {
            throw new ArgumentNullException(nameof(testResultsDirPath));
        }

        var outputPath = Path.Combine(testResultsDirPath, "TestResults.xml");
        InitializeImpl(events, outputPath);
    }

    public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (parameters.TryGetValue(LogFilePathKey, out string outputPath))
        {
            InitializeImpl(events, outputPath);
        }
        else if (parameters.TryGetValue(DefaultLoggerParameterNames.TestRunDirectory, out string outputDir))
        {
            Initialize(events, outputDir);
        }
        else
        {
            throw new ArgumentException($"Expected {LogFilePathKey} or {DefaultLoggerParameterNames.TestRunDirectory} parameter", nameof(parameters));
        }

        parameters.TryGetValue(EnvironmentKey, out _environmentOpt);
        parameters.TryGetValue(XUnitVersionKey, out _xunitVersionOpt);
    }

    private void InitializeImpl(TestLoggerEvents events, string outputPath)
    {
        events.TestRunMessage += TestMessageHandler;
        events.TestResult += TestResultHandler;
        events.TestRunComplete += TestRunCompleteHandler;

        _outputFilePath = Path.GetFullPath(outputPath);

        lock (_resultsGuard)
        {
            _results = new List<TestResultInfo>();
        }

        _localStartTime = DateTime.Now;
    }

    /// <summary>
    /// Called when a test message is received.
    /// </summary>
    internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
    {
    }

    /// <summary>
    /// Called when a test result is received.
    /// </summary>
    internal void TestResultHandler(object sender, TestResultEventArgs e)
    {
        TestResult result = e.Result;

        string assemblyPath = result.TestCase.Source;
        if (TryParseName(result.TestCase.FullyQualifiedName, out var typeName, out var methodName, out _))
        {
            lock (_resultsGuard)
            {
                _results.Add(new TestResultInfo(
                    result.Outcome,
                    result.TestCase.Source,
                    typeName,
                    methodName,
                    result.DisplayName,
                    result.Duration,
                    result.ErrorMessage,
                    result.ErrorStackTrace,
                    result.Traits));
            }
        }
    }

    internal static bool TryParseName(string testCaseName, out string metadataTypeName, out string metadataMethodName, out string metadataMethodArguments)
    {
        // This is fragile. The FQN is constructed by a test adapter. 
        // There is no enforcement that the FQN starts with metadata type name.

        string typeAndMethodName;
        var methodArgumentsStart = testCaseName.IndexOf('(');

        if (methodArgumentsStart == -1)
        {
            typeAndMethodName = testCaseName.Trim();
            metadataMethodArguments = string.Empty;
        }
        else
        {
            typeAndMethodName = testCaseName.Substring(0, methodArgumentsStart).Trim();
            metadataMethodArguments = testCaseName.Substring(methodArgumentsStart).Trim();

            if (metadataMethodArguments[metadataMethodArguments.Length - 1] != ')')
            {
                metadataTypeName = null;
                metadataMethodName = null;
                metadataMethodArguments = null;
                return false;
            }
        }

        var typeNameLength = typeAndMethodName.LastIndexOf('.');
        var methodNameStart = typeNameLength + 1;

        if (typeNameLength <= 0 || methodNameStart == typeAndMethodName.Length) // No typeName is available
        {
            metadataTypeName = null;
            metadataMethodName = null;
            metadataMethodArguments = null;
            return false;
        }

        metadataTypeName = typeAndMethodName.Substring(0, typeNameLength).Trim();
        metadataMethodName = typeAndMethodName.Substring(methodNameStart).Trim();
        return true;
    }

    /// <summary>
    /// Called when a test run is completed.
    /// </summary>
    internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
    {
        List<TestResultInfo> results;
        lock (_resultsGuard)
        {
            results = _results;
            _results = new List<TestResultInfo>();
        }

        var doc = new XDocument(CreateAssembliesElement(results));
        doc.Save(File.OpenWrite(_outputFilePath));
    }

    private XElement CreateAssembliesElement(List<TestResultInfo> results)
    {
        var element = new XElement("assemblies",
            from result in results
            group result by result.AssemblyPath into resultsByAssembly
            orderby resultsByAssembly.Key
            select CreateAssemblyElement(resultsByAssembly));

        element.SetAttributeValue("timestamp", _localStartTime.ToString(CultureInfo.InvariantCulture));

        return element;
    }

    private XElement CreateAssemblyElement(IGrouping<string, TestResultInfo> resultsByAssembly)
    {
        var assemblyPath = resultsByAssembly.Key;

        var collections = from resultsInAssembly in resultsByAssembly
                          group resultsInAssembly by resultsInAssembly.Type into resultsByType
                          orderby resultsByType.Key
                          select CreateCollection(resultsByType);

        int total = 0;
        int passed = 0;
        int failed = 0;
        int skipped = 0;
        var time = TimeSpan.Zero;

        var element = new XElement("assembly");
        element.Add(new XElement("errors"));

        foreach (var collection in collections)
        {
            total += collection.total;
            passed += collection.passed;
            failed += collection.failed;
            skipped += collection.skipped;
            time += collection.time;

            element.Add(collection.element);
        }

        element.SetAttributeValue("name", assemblyPath);

        if (_environmentOpt != null)
        {
            element.SetAttributeValue("environment", _environmentOpt);
        }

        if (_xunitVersionOpt != null)
        {
            element.SetAttributeValue("test-framework", "xUnit.net " + _xunitVersionOpt);
        }

        element.SetAttributeValue("run-date", _localStartTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        element.SetAttributeValue("run-time", _localStartTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture));

        var configFile = assemblyPath + ".config";
        if (File.Exists(configFile))
        {
            element.SetAttributeValue("config-file", configFile);
        }

        element.SetAttributeValue("total", total);
        element.SetAttributeValue("passed", passed);
        element.SetAttributeValue("failed", failed);
        element.SetAttributeValue("skipped", skipped);
        element.SetAttributeValue("time", time.TotalSeconds.ToString("N3", CultureInfo.InvariantCulture));

        element.SetAttributeValue("errors", 0);

        return element;
    }

    private static (XElement element, int total, int passed, int failed, int skipped, TimeSpan time) CreateCollection(IGrouping<string, TestResultInfo> resultsByType)
    {
        var element = new XElement("collection");

        int total = 0;
        int passed = 0;
        int failed = 0;
        int skipped = 0;
        var time = TimeSpan.Zero;

        foreach (var result in resultsByType)
        {
            total++;

            switch (result.Outcome)
            {
                case TestOutcome.Failed:
                    failed++;
                    break;

                case TestOutcome.Passed:
                    passed++;
                    break;

                case TestOutcome.Skipped:
                    skipped++;
                    break;
            }

            time += result.Time;

            element.Add(CreateTestElement(result));
        }

        element.SetAttributeValue("total", total);
        element.SetAttributeValue("passed", passed);
        element.SetAttributeValue("failed", failed);
        element.SetAttributeValue("skipped", skipped);
        element.SetAttributeValue("name", $"Test collection for {resultsByType.Key}");
        element.SetAttributeValue("time", time.TotalSeconds.ToString("N3", CultureInfo.InvariantCulture));

        return (element, total, passed, failed, skipped, time);
    }

    private static XElement CreateTestElement(TestResultInfo result)
    {
        var element = new XElement("test",
            new XAttribute("name", result.Name),
            new XAttribute("type", result.Type),
            new XAttribute("method", result.Method),
            new XAttribute("time", result.Time.TotalSeconds.ToString("N7", CultureInfo.InvariantCulture)),
            new XAttribute("result", OutcomeToString(result.Outcome)));

        if (result.Outcome == TestOutcome.Failed)
        {
            element.Add(new XElement("failure",
                new XElement("message", result.ErrorMessage),
                new XElement("stack-trace", result.ErrorStackTrace)));
        }

        if (result.Traits != null)
        {
            element.Add(new XElement("traits",
                from trait in result.Traits
                select new XElement("trait", new XAttribute("name", trait.Name), new XAttribute("value", trait.Value))));
        }

        return element;
    }

    private static string OutcomeToString(TestOutcome outcome)
    {
        switch (outcome)
        {
            case TestOutcome.Failed:
                return "Fail";

            case TestOutcome.Passed:
                return "Pass";

            case TestOutcome.Skipped:
                return "Skipped";

            default:
                return "Unknown";
        }
    }
}
