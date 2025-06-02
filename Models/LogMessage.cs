namespace DiscountCodeService.Models;

public readonly record struct LogMessage(
DateTime TimestampUtc,
LogLevel Level,
string Category,
EventId EventId,
string Message,
Exception? Exception = null)
{
	public string Format() =>
		$"{TimestampUtc:O} [{Level}] {Category} ({EventId.Id}) {Message}" +
		(Exception is null ? string.Empty : $"{Environment.NewLine}{Exception}");
}
