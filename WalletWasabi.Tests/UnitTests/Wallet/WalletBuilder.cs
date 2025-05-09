using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Caching.Memory;
using NBitcoin;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WalletWasabi.Backend.Models;
using WalletWasabi.Blockchain.Blocks;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.Mempool;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Models;
using WalletWasabi.Services;
using WalletWasabi.Stores;
using WalletWasabi.WebClients.Wasabi;
using WalletWasabi.Tests.Helpers;
using System.IO;
using System.Linq;
using WalletWasabi.Wallets.FilterProcessor;
using System.Threading;
using WalletWasabi.FeeRateEstimation;
using WalletWasabi.Wallets;

namespace WalletWasabi.Tests.UnitTests.Wallet;

public class WalletBuilder : IAsyncDisposable
{
	public WalletBuilder(MockNode node, [CallerMemberName] string callerName = "NN")
	{
		DataDir = Common.GetWorkDir(nameof(WalletSynchronizationTests), callerName);
		EventBus = new EventBus();

		SmartHeaderChain smartHeaderChain = new();
		IndexStore = new IndexStore(Path.Combine(DataDir, "indexStore"), node.Network, smartHeaderChain);
		TransactionStore = new AllTransactionStore(Path.Combine(DataDir, "transactionStore"), node.Network);

		Filters = node.BuildFilters();

		var blockRepositoryMock = new MockFileSystemBlockRepository(node.BlockChain);
		BitcoinStore = new BitcoinStore(IndexStore, TransactionStore, new MempoolService(), smartHeaderChain, blockRepositoryMock);
		Cache = new MemoryCache(new MemoryCacheOptions());
		BlockDownloadService = new(BitcoinStore.BlockRepository, trustedFullNodeBlockProviders: [], p2pBlockProvider: null);
		FeeRateEstimationUpdater = new(TimeSpan.Zero, FeeRateProviders.BlockstreamAsync(new HttpClientFactory()), EventBus);
	}

	private EventBus EventBus { get; }
	private IndexStore IndexStore { get; }
	private AllTransactionStore TransactionStore { get; }
	private BitcoinStore BitcoinStore { get; }
	private MemoryCache Cache { get; }
	private BlockDownloadService BlockDownloadService { get; }
	private FeeRateEstimationUpdater FeeRateEstimationUpdater { get; }
	public IEnumerable<FilterModel> Filters { get; }
	public string DataDir { get; }

	public async Task<WalletWasabi.Wallets.Wallet> CreateRealWalletBasedOnTestWalletAsync(TestWallet wallet, int? minGapLimit = null)
	{
		await BlockDownloadService.StartAsync(CancellationToken.None).ConfigureAwait(false);
		await BitcoinStore.InitializeAsync().ConfigureAwait(false); // StartingFilter already added to IndexStore after this line.

		await BitcoinStore.IndexStore.AddNewFiltersAsync(Filters.Skip(1)).ConfigureAwait(false);
		var keyManager = KeyManager.CreateNewWatchOnly(wallet.GetSegwitAccountExtPubKey(), null!, null, minGapLimit);
		keyManager.GetKeys(_ => true); // Make sure keys are asserted.

		var serviceConfiguration = new ServiceConfiguration(new UriEndPoint(new Uri("http://www.nomatter.dontcare")), Money.Coins(WalletWasabi.Helpers.Constants.DefaultDustThreshold));

		WalletFactory walletFactory = new(DataDir, Network.RegTest, BitcoinStore, serviceConfiguration, FeeRateEstimationUpdater, BlockDownloadService);
		return walletFactory.CreateAndInitialize(keyManager);
	}

	public async ValueTask DisposeAsync()
	{
		await IndexStore.DisposeAsync().ConfigureAwait(false);
		await TransactionStore.DisposeAsync().ConfigureAwait(false);
		BlockDownloadService.Dispose();
		Cache.Dispose();
		FeeRateEstimationUpdater.Dispose();
	}
}
