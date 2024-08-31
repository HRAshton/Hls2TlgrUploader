using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;
using Hls2TlgrUploader.Configuration;
using Hls2TlgrUploader.Extensions;
using Hls2TlgrUploader.Interfaces;
using Hls2TlgrUploader.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hls2TlgrUploader.Services;

/// <inheritdoc />
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global", Justification = "Disposable class")]
[SuppressMessage("Performance", "CA1852:Make the partial class sealed", Justification = "Disposable class")]
public partial class FfmpegService : IFfmpegService
{
    private readonly Regex _resolutionRegex = ResolutionRegex();
    private readonly Regex _durationRegex = DurationRegex();

    private readonly ProcessingConfig _processingConfig;
    private readonly Pipe _pipe = new();
    private readonly Stream _pipeReaderStream;
    private readonly StringBuilder _ffmpegStderrOutputBuilder = new();
    private string? _temporaryFilePath;
    private Process? _ffmpegProcess;
    private Task? _stdinStreamTask;
    private Task? _errorsTask;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="videoServiceConfig">Video service configuration.</param>
    /// <param name="processWrapper">Process wrapper.</param>
    /// <param name="ioHelper">I/O helper.</param>
    public FfmpegService(
        ILogger<FfmpegService> logger,
        IOptions<ProcessingConfig> videoServiceConfig,
        IProcessWrapper processWrapper,
        IIoHelper ioHelper)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(videoServiceConfig);
        ArgumentNullException.ThrowIfNull(processWrapper);
        ArgumentNullException.ThrowIfNull(ioHelper);

        _processingConfig = videoServiceConfig.Value;
        Logger = logger;

        _pipeReaderStream = _pipe.Reader.AsStream();
        ProcessWrapper = processWrapper;
        IoHelper = ioHelper;
    }

    /// <inheritdoc />
    public PipeWriter PipeWriter => _pipe.Writer;

    private ILogger<FfmpegService> Logger { get; }

    private IProcessWrapper ProcessWrapper { get; }

    private IIoHelper IoHelper { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void Run(string fileId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        _temporaryFilePath = string.Format(
            CultureInfo.InvariantCulture,
            _processingConfig.TempVideoFilePattern,
            fileId);

        var path = Path.GetDirectoryName(_temporaryFilePath);
        if (path is not null and not "")
        {
            IoHelper.CreateDirectory(path);
        }

        RunInternal(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VideoInfo> GetResultAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_temporaryFilePath);
        ArgumentNullException.ThrowIfNull(_ffmpegProcess);
        ArgumentNullException.ThrowIfNull(_stdinStreamTask);

        await _stdinStreamTask;
        await _ffmpegProcess!.WaitForExitAsync(cancellationToken);

        var ffmpegStderrOutput = _ffmpegStderrOutputBuilder.ToString();
        GroupCollection resolution = _resolutionRegex.Matches(ffmpegStderrOutput).Last().Groups;
        GroupCollection durationParts = _durationRegex.Matches(ffmpegStderrOutput).Last().Groups;
        var duration = (int.Parse(durationParts[1].Value, CultureInfo.InvariantCulture) * 3600)
                       + (int.Parse(durationParts[2].Value, CultureInfo.InvariantCulture) * 60)
                       + int.Parse(durationParts[3].Value, CultureInfo.InvariantCulture)
                       + (int)Math.Ceiling(int.Parse(durationParts[4].Value, CultureInfo.InvariantCulture) / 100.0);

        return new VideoInfo(
            _temporaryFilePath,
            int.Parse(resolution[1].Value, CultureInfo.InvariantCulture),
            int.Parse(resolution[2].Value, CultureInfo.InvariantCulture),
            duration);
    }

    /// <inheritdoc cref="IDisposable" />
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            Log.DisposingFfmpegService(Logger);

            _pipe.Writer.Complete();
            _stdinStreamTask?.Wait();
            _errorsTask?.Wait();
            _ffmpegProcess?.Dispose();
            _pipeReaderStream.Dispose();
            if (_temporaryFilePath is not null && File.Exists(_temporaryFilePath))
            {
                File.Delete(_temporaryFilePath);
            }

            Log.FfmpegServiceDisposed(Logger);
        }

        _isDisposed = true;
    }

    private void RunInternal(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_temporaryFilePath);

        Log.StartingFfmpegInstance(Logger, _temporaryFilePath);

        _ffmpegProcess = RunFfmpeg(_temporaryFilePath);
        CancellationTokenRegistration killRegistration = cancellationToken.Register(() => _ffmpegProcess.Kill());
        _ffmpegProcess.Exited += (_, _) =>
        {
            Log.FfmpegProcessExited(Logger, _ffmpegProcess.ExitCode);
            _ = killRegistration.Unregister();
        };

        _errorsTask = Task.Run(
            () =>
            {
                while (!_ffmpegProcess.StandardError.EndOfStream)
                {
                    var line = _ffmpegProcess.StandardError.ReadLine();
                    _ = _ffmpegStderrOutputBuilder.AppendLine(line);
                    Log.FfmpegStderrOutput(Logger, line);
                }
            },
            cancellationToken);

        _stdinStreamTask = _pipe.Reader.PumpToAsync(_ffmpegProcess.StandardInput.BaseStream, cancellationToken);
    }

    private Process RunFfmpeg(string filePath)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = "-i pipe:0 -f mp4 -c copy " + filePath,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process process = ProcessWrapper.Start(processStartInfo)
                          ?? throw new InvalidOperationException("Failed to start ffmpeg process");
        return process;
    }

    [GeneratedRegex(@"Video: .*? (\d{3,4})x(\d{3,4})", RegexOptions.Compiled)]
    private static partial Regex ResolutionRegex();

    [GeneratedRegex(@"time=(\d{2}):(\d{2}):(\d{2}).(\d{2})", RegexOptions.Compiled)]
    private static partial Regex DurationRegex();
}
