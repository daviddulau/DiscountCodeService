using DiscountCodeService.Models;
using System.Threading.Channels;

namespace DiscountCodeService.Loggers
{
	public class ChannelLogger : ILogger
	{
		private readonly string _category;
		private readonly ChannelWriter<LogMessage> _writer;

		public ChannelLogger(string category, ChannelWriter<LogMessage> writer)
		{
			_category = category;
			_writer = writer;
		}

		public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel)) return;

			_writer.TryWrite(new LogMessage(
				TimestampUtc: DateTime.UtcNow,
				Level: logLevel,
				Category: _category,
				EventId: eventId,
				Message: formatter(state, exception),
				Exception: exception));
		}

		private sealed class NullScope : IDisposable
		{
			public static readonly NullScope Instance = new();
			public void Dispose() { }
		}
	}
}
