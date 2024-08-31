using System.Diagnostics;

namespace Hls2TlgrUploader.Interfaces;

/// <summary>
/// Wrapper for the Process class.
/// </summary>
public interface IProcessWrapper
{
    /// <inheritdoc cref="Process.Start(ProcessStartInfo)" />
    Process? Start(ProcessStartInfo startInfo);
}
