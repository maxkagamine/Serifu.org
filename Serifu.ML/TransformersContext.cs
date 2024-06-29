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

using Python.Runtime;
using Serilog;

namespace Serifu.ML;

public class TransformersContext
{
    private readonly ILogger logger;

    public TransformersContext(ILogger logger)
    {
        this.logger = logger.ForContext<TransformersContext>();

        Initialize();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "Analyzer doesn't seem to understand 'dynamic'.")]
    private void Initialize()
    {
        // Python.NET doesn't have good support for venv's. The below hack of deferring the "site" module and setting
        // prefixes comes courtesy of https://github.com/pythonnet/pythonnet/issues/1478#issuecomment-897933730
        Runtime.PythonDLL = VirtualEnv.PythonDll;

        _ = PythonEngine.Version; // Load the dll
        PythonEngine.SetNoSiteFlag(); // Disable running the "site" module on startup

        PythonEngine.Initialize();

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
            logger.Information("python version: {Version}", sys.version);

            dynamic torch = Py.Import("torch");
            logger.Information("torch version: {Version}", torch.__version__);

            PyObject transformers = Py.Import("transformers");
            logger.Information("transformers version: {Version}", transformers.GetAttr("__version__"));

            // Check if we can use the GPU
            if (torch.cuda.is_available())
            {
                logger.Information("CUDA is available.");
            }
            else
            {
                logger.Warning("CUDA is not available. PyTorch will run on the CPU.");
            }
        }
    }
}
