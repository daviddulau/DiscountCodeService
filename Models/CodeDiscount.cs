namespace DiscountCodeService.Models;

public sealed class CodeDiscount
{
	public int Id { get; set; }
	public string Code { get; set; } = string.Empty;
	public bool Used { get; set; }
}
