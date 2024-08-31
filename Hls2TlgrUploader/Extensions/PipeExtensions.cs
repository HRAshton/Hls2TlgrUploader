using System.IO.Pipelines;

namespace Hls2TlgrUploader.Extensions;

/// <summary>
/// Extensions for <see cref="Pipe"/>.
/// </summary>
internal static class PipeExtensions
{
    /// <summary>
    /// Asynchronously copies data from a pipe to the destination stream until the pipe is closed.
    /// </summary>
    /// <param name="pipeReader">Source pipe.</param>
    /// <param name="outputStream">Destination stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous task.</returns>
    public static async Task PumpToAsync(
        this PipeReader pipeReader,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeReader);
        ArgumentNullException.ThrowIfNull(outputStream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReadResult result = await pipeReader.ReadAsync(cancellationToken);
            System.Buffers.ReadOnlySequence<byte> buffer = result.Buffer;

            foreach (ReadOnlyMemory<byte> segment in buffer)
            {
                await outputStream.WriteAsync(segment, cancellationToken);
            }

            pipeReader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        outputStream.Close();
    }
}
