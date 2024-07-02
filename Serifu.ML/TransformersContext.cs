// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using Microsoft.Win32;
using Python.Runtime;
using Serifu.ML.Abstractions;
using Serilog;
using System.Runtime.Serialization;

namespace Serifu.ML;

/// <summary>
/// Encapsulates the Python transformers interop and provides methods for running machine learning tasks.
/// </summary>
/// <remarks>
/// This class can only be instantiated once and must be disposed when stopping the application. Note: Some native
/// Python modules (like numpy, at least &lt;2.0.0) mess with the SIGINT handler. Ensure this class is only constructed
/// once our own handlers are set up (which is the normal DI flow).
/// </remarks>
public sealed class TransformersContext : ITransformersContext
{
    private readonly ILogger logger;
    private readonly int device = 0; // -1 = CPU, 0 = GPU (CUDA)
    private bool disposed;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "Analyzer doesn't seem to understand 'dynamic'.")]
    public TransformersContext(ILogger logger)
    {
        this.logger = logger = logger.ForContext<TransformersContext>();

        // Python.NET doesn't have good support for venv's. The below hack of deferring the "site" module and setting
        // prefixes comes courtesy of https://github.com/pythonnet/pythonnet/issues/1478#issuecomment-897933730
        Runtime.PythonDLL = VirtualEnv.PythonDll;

        _ = PythonEngine.Version; // Load the dll
        PythonEngine.SetNoSiteFlag(); // Disable running the "site" module on startup

        // https://github.com/pythonnet/pythonnet/issues/2107
        RuntimeData.FormatterType = typeof(NoopFormatter);

        PythonEngine.Initialize(initSigs: false);
        PythonEngine.BeginAllowThreads();

        using (Py.GIL())
        {
            // Set prefixes to point at the venv
            dynamic sys = Py.Import("sys");
            sys.prefix = VirtualEnv.VirtualEnvDirectory;
            sys.exec_prefix = VirtualEnv.VirtualEnvDirectory;

            dynamic site = Py.Import("site");
            site.PREFIXES = new PyObject[] { sys.prefix, sys.exec_prefix };
            site.main();

            // Ensure packages are installed and print version info
            logger.Information("Python version: {Version}", sys.version);

            dynamic torch = Py.Import("torch");
            logger.Information("PyTorch version: {Version}", torch.__version__);

            PyObject transformers = Py.Import("transformers");
            logger.Information("Transformers version: {Version}", transformers.GetAttr("__version__"));

            // Check if we can use the GPU
            if (torch.cuda.is_available())
            {
                logger.Information("CUDA is available.");

                DeviceName = torch.cuda.get_device_name(device);
            }
            else
            {
                logger.Warning("CUDA is not available. PyTorch will run on the CPU.");

                device = -1;
                DeviceName = OperatingSystem.IsOSPlatform("Windows") ?
                    Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null)?.ToString()?.Trim() ?? "CPU" : "CPU";
            }

            this.logger = logger.ForContext(nameof(DeviceName), DeviceName);
        }
    }

    public string DeviceName { get; }

    /// <summary>
    /// Runs <paramref name="action"/> within a Python global interpreter lock, sending an interrupt to the Python
    /// thread (raises <c>KeyboardInterrupt</c>) if <paramref name="cancellationToken"/> is canceled.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="action">A single-threaded, interruptible operation to perform within the GIL.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to abort the thread.</param>
    /// <returns>The return value of <paramref name="action"/>.</returns>
    /// <exception cref="OperationCanceledException"/>
    internal async Task<T> Run<T>(Func<T> action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        ulong? pythonThreadId = new();

        try
        {
            Task<T> task = Task.Run(() =>
            {
                using (Py.GIL())
                {
                    pythonThreadId = PythonEngine.GetPythonThreadID();
                    cancellationToken.ThrowIfCancellationRequested();
                    return action();
                }
            }, cancellationToken);

            return await task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (pythonThreadId.HasValue)
            {
                using (Py.GIL())
                {
                    PythonEngine.Interrupt(pythonThreadId.Value);
                }
            }

            throw;
        }
    }

    public async Task<IQuestionAnsweringPipeline> QuestionAnswering(
        string model,
        int batchSize = 1,
        CancellationToken cancellationToken = default) => await Run(() =>
        {
            dynamic transformers = Py.Import("transformers");
            dynamic pipe = transformers.pipeline("question-answering", model: model, batch_size: batchSize, device: device);

            return new QuestionAnsweringPipeline(this, pipe);
        }, cancellationToken);

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // No managed resources to free
            }

            PythonEngine.Shutdown();
            disposed = true;
        }
    }

    ~TransformersContext()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

// https://github.com/pythonnet/pythonnet/issues/2107
#pragma warning disable SYSLIB0011 // Type or member is obsolete
#pragma warning disable SYSLIB0050 // Type or member is obsolete
internal class NoopFormatter : IFormatter
{
    public object Deserialize(Stream s) => throw new NotImplementedException();
    public void Serialize(Stream s, object o) { }

    public SerializationBinder? Binder { get; set; }
    public StreamingContext Context { get; set; }
    public ISurrogateSelector? SurrogateSelector { get; set; }
}
#pragma warning restore SYSLIB0050 // Type or member is obsolete
#pragma warning restore SYSLIB0011 // Type or member is obsolete
