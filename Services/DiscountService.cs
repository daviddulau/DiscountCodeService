using DiscountCodeService.Grpc;
using DiscountCodeService.Repositories;
using Grpc.Core;

namespace DiscountCodeService.Services;

public class DiscountService
{
	private readonly IDiscountRepository _repo;
	private readonly ILogger<DiscountService> _log;

	public DiscountService(IDiscountRepository repo, ILogger<DiscountService> log)
	{
		_repo = repo;
		_log = log;
	}

	public async Task<GenerateResponse> Generate(GenerateRequest request, ServerCallContext ctx)
	{
		if (request.Count is 0 or > 2_000 || request.Length is < 7 or > 8)
			return new() { Result = false };

		var codes = await _repo.GenerateAsync((int)request.Count, (int)request.Length, ctx.CancellationToken);
		return new GenerateResponse { Result = true, Codes = { codes } };
	}

	public async Task<UseCodeResponse> UseCode(UseCodeRequest request, ServerCallContext ctx)
	{
		var result = await _repo.UseCodeAsync(request.Code, ctx.CancellationToken);
		return new UseCodeResponse { Result = result };
	}
}
