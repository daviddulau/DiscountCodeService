using DiscountCodeService.Grpc;

namespace DiscountCodeService.Repositories;

public interface IDiscountRepository
{
	ValueTask<IReadOnlyCollection<string>> GenerateAsync(int count, int length, CancellationToken token);
	ValueTask<UseCodeResult> UseCodeAsync(string code, CancellationToken token);
}
