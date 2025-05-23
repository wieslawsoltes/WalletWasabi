using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NBitcoin;
using NBitcoin.RPC;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Backend.Models.Responses;
using WalletWasabi.BitcoinRpc;
using WalletWasabi.Blockchain.Analysis.FeesEstimation;
using WalletWasabi.Blockchain.BlockFilters;
using WalletWasabi.Cache;
using WalletWasabi.Extensions;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace WalletWasabi.Backend.Controllers;

/// <summary>
/// To interact with the Bitcoin Blockchain.
/// </summary>
[Produces("application/json")]
[Route("api/v" + Constants.BackendMajorVersion + "/btc/[controller]")]
public class BlockchainController : ControllerBase
{
	private static readonly MemoryCacheEntryOptions CacheEntryOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };

	public BlockchainController(IMemoryCache memoryCache, IRPCClient rpc, IndexBuilderService indexBuilderService)
	{
		Cache = new(memoryCache);
		RpcClient = rpc;
		IndexBuilderService = indexBuilderService;
	}

	private IRPCClient RpcClient { get; }
	private IdempotencyRequestCache Cache { get; }
	private IndexBuilderService IndexBuilderService { get; }

	/// <summary>
	/// Get all fees.
	/// </summary>
	/// <param name="estimateSmartFeeMode">Bitcoin Core's estimatesmartfee mode: ECONOMICAL/CONSERVATIVE.</param>
	/// <returns>A dictionary of fee targets and estimations.</returns>
	/// <response code="200">A dictionary of fee targets and estimations.</response>
	/// <response code="400">Invalid estimation mode is provided, possible values: ECONOMICAL/CONSERVATIVE.</response>
	[HttpGet("all-fees")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> GetAllFeesAsync([FromQuery, Required] string estimateSmartFeeMode, CancellationToken cancellationToken)
	{
		if (!Enum.TryParse(estimateSmartFeeMode, ignoreCase: true, out EstimateSmartFeeMode mode))
		{
			return BadRequest("Invalid estimation mode is provided, possible values: ECONOMICAL/CONSERVATIVE.");
		}

		AllFeeEstimate estimation = await GetAllFeeEstimateAsync(mode, cancellationToken);

		return Ok(estimation.Estimations);
	}

	internal Task<AllFeeEstimate> GetAllFeeEstimateAsync(EstimateSmartFeeMode mode, CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{nameof(GetAllFeeEstimateAsync)}_{mode}";

		return Cache.GetCachedResponseAsync(
			cacheKey,
			action: (string request, CancellationToken token) => RpcClient.EstimateAllFeeAsync(token),
			options: CacheEntryOptions,
			cancellationToken);
	}

	/// <summary>
	/// Gets mempool hashes.
	/// </summary>
	/// <param name="compactness">Can strip the last x characters from the hashes.</param>
	/// <returns>A collection of transaction hashes.</returns>
	/// <response code="200">A collection of transaction hashes.</response>
	/// <response code="400">Invalid model state.</response>
	[HttpGet("mempool-hashes")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	[ResponseCache(Duration = 5)]
	public async Task<IActionResult> GetMempoolHashesAsync([FromQuery] int compactness = 64, CancellationToken cancellationToken = default)
	{
		if (compactness is < 1 or > 64)
		{
			return BadRequest("Invalid compactness parameter is provided.");
		}

		IEnumerable<string> fulls = await GetRawMempoolStringsWithCacheAsync(cancellationToken);

		if (compactness == 64)
		{
			return Ok(fulls);
		}
		else
		{
			IEnumerable<string> compacts = fulls.Select(x => x[..compactness]);
			return Ok(compacts);
		}
	}

	internal async Task<IEnumerable<string>> GetRawMempoolStringsWithCacheAsync(CancellationToken cancellationToken = default)
	{
		var cacheKey = $"{nameof(GetRawMempoolStringsWithCacheAsync)}";
		var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3) };

		return await Cache.GetCachedResponseAsync(
			cacheKey,
			action: (request, token) => GetRawMempoolStringsNoCacheAsync(token),
			options: cacheOptions,
			cancellationToken);
	}

	private async Task<IEnumerable<string>> GetRawMempoolStringsNoCacheAsync(CancellationToken cancellationToken = default)
	{
		uint256[] transactionHashes = await RpcClient.GetRawMempoolAsync(cancellationToken).ConfigureAwait(false);
		return transactionHashes.Select(x => x.ToString());
	}

	/// <summary>
	/// Attempts to broadcast a transaction.
	/// </summary>
	/// <remarks>
	/// Sample request:
	///
	///     POST /broadcast
	///     "01000000014b6b6fced23fa0d772f83fd849ce2f4e8fa51ea49cc12710ebcdc722d74c87f5000000006a47304402206bf1118e381342d0387e47807c83d2c1e919e2e3792f2673579a9ce87a380db002207e471504f96d7830dc9cbb7442332d747a25dcfd5d1530feea92b8a302aa57f4012102a40230b345856cc18ca1d745e7ea52319a012753b050e24d7be64ca0b978fb3effffffff0235662803000000001976a9146adfacaab3dc7c51b3300c4256b184f95cc48f4288acd0dd0600000000001976a91411ff558b1790b8d57cb25b9c07094591cfd2051c88ac00000000"
	///
	/// </remarks>
	/// <param name="hex">The hex string of the raw transaction.</param>
	/// <returns>200 Ok on successful broadcast or 400 BadRequest on failure.</returns>
	/// <response code="200">If broadcast is successful.</response>
	/// <response code="400">If broadcast fails.</response>
	[HttpPost("broadcast")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> BroadcastAsync([FromBody, Required] string hex, CancellationToken cancellationToken)
	{
		Transaction transaction;
		try
		{
			transaction = Transaction.Parse(hex, RpcClient.Network);
		}
		catch (Exception ex)
		{
			Logger.LogDebug(ex);
			return BadRequest("Invalid hex.");
		}

		try
		{
			await RpcClient.SendRawTransactionAsync(transaction, cancellationToken);
		}
		catch (RPCException ex) when (ex.Message.Contains("already in block chain", StringComparison.InvariantCultureIgnoreCase))
		{
			return Ok("Transaction is already in the blockchain.");
		}
		catch (RPCException ex)
		{
			Logger.LogDebug(ex);
			return BadRequest($"{ex.Message}");
		}

		return Ok("Transaction is successfully broadcast.");
	}

	/// <summary>
	/// Gets block filters from the provided block hash.
	/// </summary>
	/// <remarks>
	/// Filter examples:
	///
	///     Main: 0000000000000000001c8018d9cb3b742ef25114f27563e3fc4a1902167f9893
	///     TestNet: 00000000000f0d5edcaeba823db17f366be49a80d91d15b77747c2e017b8c20a
	///     RegTest: 0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206
	///
	/// </remarks>
	/// <param name="bestKnownBlockHash">The best block hash the client knows its filter.</param>
	/// <param name="count">The number of filters to return.</param>
	/// <returns>The best height and an array of block hash : element count : filter pairs.</returns>
	/// <response code="200">The best height and an array of block hash : element count : filter pairs.</response>
	/// <response code="204">When the provided hash is the tip.</response>
	/// <response code="400">The provided hash was malformed or the count value is out of range</response>
	/// <response code="404">If the hash is not found. This happens at blockchain reorg.</response>
	[HttpGet("filters")]
	[ProducesResponseType(200)] // Note: If you add typeof(IList<string>) then swagger UI visualization will be ugly.
	[ProducesResponseType(204)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	[ResponseCache(Duration = 60)]
	public async Task<IActionResult> GetFilters([FromQuery, Required] string bestKnownBlockHash, [FromQuery, Required] int count, CancellationToken cancellationToken)
	{
		if (count <= 0)
		{
			return BadRequest("Invalid block hash or count is provided.");
		}

		var knownHash = new uint256(bestKnownBlockHash);

		var (bestHeight, filters, found) = await IndexBuilderService.GetFilterLinesExcludingAsync(knownHash, count, cancellationToken);

		if (!found)
		{
			return NotFound($"Provided {nameof(bestKnownBlockHash)} is not found: {bestKnownBlockHash}.");
		}

		if (!filters.Any())
		{
			return NoContent();
		}

		var response = new FiltersResponse
		{
			BestHeight = bestHeight,
			Filters = filters
		};

		return Ok(response);
	}

	[HttpGet("unconfirmed-transaction-chain")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public IActionResult GetUnconfirmedTransactionChain([FromQuery, Required] string transactionId, CancellationToken cancellationToken)
	{
		return Ok(Array.Empty<uint256>());
	}
}
