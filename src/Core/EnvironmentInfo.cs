using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string FrameworkDescription,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string TMF);

public static class EnvironmentInfo
{

#if NET10_0_OR_GREATER
const string BuildNote = "збірка під net10.0";
#else
    const string BuildNote = "збірка під net8.0";
#endif
    public static EnvironmentReport Collect() => new(
        OsDescription: RuntimeInformation.OSDescription,
        FrameworkDescription: RuntimeInformation.FrameworkDescription,
        ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
        DetectedRid: DetectedRid(),
        ReportedRid: RuntimeInformation.RuntimeIdentifier,
        BaseDirectory: AppContext.BaseDirectory,
        TMF: BuildNote
    );

    public static string DetectedRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";


        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}