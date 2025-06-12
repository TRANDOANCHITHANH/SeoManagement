using Microsoft.Extensions.Hosting;
using Python.Runtime;
using System.Runtime.InteropServices;

namespace SeoManagement.Infrastructure.Services
{
	public class ModelLoadingService : IHostedService
	{
		public dynamic Classifier { get; private set; }

		public Task StartAsync(CancellationToken cancellationToken)
		{
			try
			{
				Console.WriteLine($"Running in {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} mode");
				Console.WriteLine($"Current Directory: {Environment.CurrentDirectory}");

				string pythonHome = @"C:\Users\tranc\AppData\Local\Programs\Python\Python39";
				string pythonDll = @"C:\Users\tranc\AppData\Local\Programs\Python\Python39\python39.dll";

				if (!File.Exists(pythonDll))
				{
					throw new FileNotFoundException($"Python DLL not found at {pythonDll}");
				}

				IntPtr dllHandle = LoadLibrary(pythonDll);
				if (dllHandle == IntPtr.Zero)
				{
					throw new Exception($"Failed to load DLL {pythonDll}. Error: {Marshal.GetLastWin32Error()}");
				}
				Console.WriteLine($"Successfully loaded DLL: {pythonDll}");

				Runtime.PythonDLL = pythonDll;
				Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
				Environment.SetEnvironmentVariable("PATH", $"{pythonHome};{Environment.GetEnvironmentVariable("PATH")}");

				PythonEngine.Initialize();

				using (Py.GIL())
				{
					dynamic sys = Py.Import("sys");
					sys.executable = new PyString(@"C:\Users\tranc\AppData\Local\Programs\Python\Python39\python.exe");
					Console.WriteLine($"Python sys.version: {sys.version}");
					Console.WriteLine($"Python sys.executable: {sys.executable}");
					sys.path.append($@"{pythonHome}\Lib\site-packages");
					sys.path.append($@"{pythonHome}\Scripts");
					Console.WriteLine($"Python sys.path: {sys.path}");

					dynamic os = Py.Import("os");
					os.environ[new PyString("NETWORKX_BACKEND")] = new PyString("default");

					dynamic transformers = Py.Import("transformers");
					Console.WriteLine($"Transformers module: {transformers}");
					Console.WriteLine($"Transformers __file__: {transformers.__file__}");

					string pythonCode = """
                        from transformers import AutoTokenizer, AutoModelForSequenceClassification
                        tokenizer_class = AutoTokenizer
                        model_class = AutoModelForSequenceClassification
                        """;
					dynamic scope = Py.CreateScope();
					scope.Exec(pythonCode);
					dynamic tokenizer_class = scope.Get("tokenizer_class");
					dynamic model_class = scope.Get("model_class");
					Console.WriteLine($"AutoTokenizer: {tokenizer_class}");
					Console.WriteLine($"AutoModelForSequenceClassification: {model_class}");

					dynamic torch = Py.Import("torch");
					bool cuda_available = torch.cuda.is_available();
					Console.WriteLine($"CUDA available: {cuda_available}");
					string cuda_version = torch.version.cuda?.ToString() ?? "None";
					Console.WriteLine($"PyTorch CUDA version: {cuda_version}");
					string device_name = cuda_available ? torch.cuda.get_device_name(0).ToString() : "No GPU";
					Console.WriteLine($"Device name: {device_name}");
					int device_count = cuda_available ? (int)torch.cuda.device_count() : 0;
					Console.WriteLine($"CUDA device count: {device_count}");

					string model_path = @"D:\Models\finetuned_phobert_model";
					dynamic tokenizer = tokenizer_class.from_pretrained(model_path);
					dynamic model = model_class.from_pretrained(model_path);

					int device = cuda_available ? 0 : -1;
					string deviceStr = device >= 0 ? $"cuda:{device}" : "cpu";
					Console.WriteLine($"Using device: {deviceStr}");
					if (device >= 0)
					{
						model.to(torch.device(deviceStr));
						model.half(); // Sử dụng FP16 để tăng tốc và giảm VRAM
					}

					Classifier = transformers.pipeline(
						"text-classification",
						model: model,
						tokenizer: tokenizer,
						device: new PyInt(device),
						max_length: new PyInt(64), // Giảm max_length để tăng tốc
						truncation: true,
						batch_size: new PyInt(8) // Thêm batch_size
					);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error initializing Python: {ex.Message}\n{ex}");
				throw;
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			PythonEngine.Shutdown();
			return Task.CompletedTask;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string dllToLoad);
	}
}