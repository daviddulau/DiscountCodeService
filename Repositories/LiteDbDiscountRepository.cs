using DiscountCodeService.Grpc;
using LiteDB;
using System.Security.Cryptography;
using DiscountCodeService.Models;

namespace DiscountCodeService.Repositories;

public class LiteDbDiscountRepository : IDiscountRepository, IDisposable
{
	private readonly LiteDatabase _db;
	private readonly ILiteCollection<CodeDiscount> _col;
	private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
	private static readonly char[] _alpha = "ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

	public LiteDbDiscountRepository()
	{
		_db = new("Filename=discounts.db;Mode=Shared");
		_col = _db.GetCollection<CodeDiscount>("codes");
		_col.EnsureIndex(x => x.Code, true);
	}

	public ValueTask<IReadOnlyCollection<string>> GenerateAsync(int count, int len, CancellationToken token)
	{
		return ValueTask.FromResult((IReadOnlyCollection<string>)GenerateInternal(count, len, token));
	}

	public ValueTask<UseCodeResult> UseCodeAsync(string code, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(code))
			return ValueTask.FromResult(UseCodeResult.NotFound);

		var doc = _col.FindOne(x => x.Code == code.ToUpperInvariant());
		if (doc is null) return ValueTask.FromResult(UseCodeResult.NotFound);
		if (doc.Used) return ValueTask.FromResult(UseCodeResult.AlreadyUsed);

		doc.Used = true;
		_col.Update(doc);
		return ValueTask.FromResult(UseCodeResult.Success);
	}

	private List<string> GenerateInternal(int count, int len, CancellationToken token)
	{
		var result = new List<string>(count);
		Span<byte> buffer = stackalloc byte[len];

		while (result.Count < count)
		{
			token.ThrowIfCancellationRequested();
			_rng.GetBytes(buffer);
			Span<char> chars = stackalloc char[len];
			for (int i = 0; i < len; ++i)
				chars[i] = _alpha[buffer[i] % _alpha.Length];

			var code = new string(chars);
			if (_col.Exists(x => x.Code == code))
				continue; // collision – try again

			_col.Insert(new CodeDiscount { Code = code, Used = false });
			result.Add(code);
		}
		return result;
	}

	public void Dispose() => _db.Dispose();
}
