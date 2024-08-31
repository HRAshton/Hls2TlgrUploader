using System.Diagnostics;
using Hls2TlgrUploader.Interfaces;

namespace Hls2TlgrUploader.Helpers;

/// <inheritdoc />
public class ProcessWrapper : IProcessWrapper
{
    /// <inheritdoc />
    public Process? Start(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo);
    }
}
