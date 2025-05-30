using System.Threading.Channels;

namespace DiscountCodeService.Loggers;

public class LoggerProvider : ILoggerProvider
{
	private readonly Channel<LogEntry> _channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = false
	});
	private readonly Task _processor;
	private readonly StreamWriter _file;
	private volatile bool _disposed;

	public LoggerProvider(string filePath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
		_file = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 8192, true));
		_processor = Task.Run(ProcessQueueAsync);
	}

	public ILogger CreateLogger(string category) => new Logger(category, _channel.Writer);

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_channel.Writer.TryComplete();
		try { _processor.Wait(); } catch { /* ignored */ }
		_file.Dispose();
	}

	private async Task ProcessQueueAsync()
	{
		await foreach (var e in _channel.Reader.ReadAllAsync())
		{
			try
			{
				await _file.WriteLineAsync($"{e.Timestamp:O}|{e.Level}|{e.Category}|{e.Message}");
				if (e.Exception is not null)
					await _file.WriteLineAsync(e.Exception.ToString());
			}
			catch (Exception ex)
			{
				// swallow to avoid crashing app per requirement 4
				Console.Error.WriteLine(ex);
			}
		}
		await _file.FlushAsync();
	}

	private record LogEntry(DateTime Timestamp, LogLevel Level, string Category, string Message, Exception? Exception);

	private class Logger : ILogger
	{
		private readonly string _category;
		private readonly ChannelWriter<LogEntry> _writer;

		public Logger(string category, ChannelWriter<LogEntry> writer)
		{
			_category = category;
			_writer = writer;
		}

		public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> formatter)
		{
			_writer.TryWrite(new LogEntry(DateTime.UtcNow, level, _category, formatter(state, ex), ex));
		}

		private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
	}
}
