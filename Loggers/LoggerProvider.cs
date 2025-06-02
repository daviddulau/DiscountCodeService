using DiscountCodeService.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace DiscountCodeService.Loggers;

public class LoggerProvider : ILoggerProvider
{
	private readonly Channel<LogMessage> _channel;
	private readonly CancellationTokenSource _cts = new();
	private readonly Task _backgroundWriter;
	private static readonly ConcurrentDictionary<string, Lazy<StreamWriter>> Writers = new();

	public LoggerProvider(string logPath, IFormatProvider? formatProvider = null)
	{
		_channel = Channel.CreateBounded<LogMessage>(new BoundedChannelOptions(10_000)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.DropWrite
		});

		// ensure directory exists
		Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

		// single writer per *file*, shared across providers
		var writer = Writers.GetOrAdd(logPath, p => new Lazy<StreamWriter>(() =>
		{
			var fs = new FileStream(
				p,
				FileMode.Append,
				FileAccess.Write,
				FileShare.ReadWrite | FileShare.Delete,
				4096,
				FileOptions.Asynchronous | FileOptions.WriteThrough);

			return new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
		})).Value;

		_backgroundWriter = Task.Run(() => ProcessQueueAsync(writer, _cts.Token));
	}

	public ILogger CreateLogger(string categoryName)
		=> new ChannelLogger(categoryName, _channel.Writer);

	public void Dispose()
	{
		_cts.Cancel();
		_backgroundWriter.Wait();
	}

	private async Task ProcessQueueAsync(StreamWriter writer, CancellationToken ct)
	{
		try
		{
			await foreach (var msg in _channel.Reader.ReadAllAsync(ct))
			{
				await writer.WriteLineAsync(msg.Format());
			}
		}
		catch (OperationCanceledException) { /* normal shutdown */ }
		catch (Exception ex)
		{
			// last-chance logging; swallow so the app never crashes
			try
			{
				File.AppendAllText("fatal.log", ex.ToString());
			}
			catch { }
		}
	}
}
