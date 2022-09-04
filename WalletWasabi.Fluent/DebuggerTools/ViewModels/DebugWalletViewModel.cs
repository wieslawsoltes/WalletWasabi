using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Backend.Models;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.Fluent.ViewModels;
using WalletWasabi.Models;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.DebuggerTools.ViewModels;

public partial class DebugWalletViewModel : ViewModelBase
{
	private readonly Wallet _wallet;
	private readonly IObservable<Unit> _updateTrigger;
	private ICoinsView? _coins;
	[AutoNotify] private DebugCoinViewModel? _selectedCoin;
	[AutoNotify] private DebugTransactionViewModel? _selectedTransaction;

	public DebugWalletViewModel(Wallet wallet)
	{
		_wallet = wallet;

		WalletName = _wallet.WalletName;

		Coins = new ObservableCollection<DebugCoinViewModel>();

		Transactions = new ObservableCollection<DebugTransactionViewModel>();

		_updateTrigger =
			Observable
				.FromEventPattern(_wallet, nameof(Wallet.WalletRelevantTransactionProcessed)).Select(_ => Unit.Default)
				.Merge(Observable.FromEventPattern(_wallet, nameof(Wallet.NewFilterProcessed)).Select(_ => Unit.Default))
				.Throttle(TimeSpan.FromSeconds(0.1))
				.ObserveOn(RxApp.MainThreadScheduler);

		_updateTrigger.Subscribe(_ => Update());

		Observable
			.FromEventPattern<ProcessedResult>(_wallet, nameof(Wallet.WalletRelevantTransactionProcessed))
			.Select(x => x.EventArgs)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(x =>
			{
				// TODO: Show in log list.
			});

		// TODO: Wallet.InitializingChanged ?

		Observable
			.FromEventPattern<FilterModel>(_wallet, nameof(Wallet.NewFilterProcessed))
			.Select(x => x.EventArgs)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(x =>
			{
				// TODO: Show in log list.
			});

		Observable
			.FromEventPattern<Block>(_wallet, nameof(Wallet.NewBlockProcessed))
			.Select(x => x.EventArgs)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(x =>
			{
				// TODO: Show in log list.
			});

		Observable
			.FromEventPattern<WalletState>(_wallet, nameof(Wallet.StateChanged))
			.Select(x => x.EventArgs)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(state =>
			{
				// TODO: Show in log list.

				if (state == WalletState.Started)
				{
					Update();
				}
			});

		Update();

		CreateCoinsSource();

		CreateTransactionsSource();
	}

	private void Update()
	{
		if (_wallet.Coins is { })
		{
			_coins = ((CoinsRegistry)_wallet.Coins).AsAllCoinsView();
		}

		var selectedCoin = SelectedCoin;
		var selectedTransaction = SelectedTransaction;

		Coins.Clear();
		SelectedCoin = null;

		Transactions.Clear();
		SelectedTransaction = null;

		if (_coins is { })
		{
			var coins = _coins.Select(x => new DebugCoinViewModel(x, _updateTrigger));

			foreach (var coin in coins)
			{
				Coins.Add(coin);
			}

			var transactionsDict = MapTransactions();

			var existingTransactions = new HashSet<uint256>();

			foreach (var coin in Coins)
			{
				if (!existingTransactions.Contains(coin.TransactionId))
				{
					foreach (var transactionCoin in transactionsDict[coin.TransactionId])
					{
						coin.Transaction.Coins.Add(transactionCoin);
					}

					Transactions.Add(coin.Transaction);

					existingTransactions.Add(coin.TransactionId);
				}

				if (coin.SpenderTransactionId is { } && coin.SpenderTransaction is { })
				{
					if (!existingTransactions.Contains(coin.SpenderTransactionId))
					{
						foreach (var spenderCoin in transactionsDict[coin.SpenderTransactionId])
						{
							coin.SpenderTransaction.Coins.Add(spenderCoin);
						}

						Transactions.Add(coin.SpenderTransaction);

						existingTransactions.Add(coin.SpenderTransactionId);
					}
				}
			}
		}

		if (selectedCoin is { })
		{
			var coin = Coins.FirstOrDefault(x => x.TransactionId == selectedCoin.TransactionId);
			if (coin is { })
			{
				SelectedCoin = coin;
			}
		}

		if (selectedTransaction is { })
		{
			var transaction = Transactions.FirstOrDefault(x => x.TransactionId == selectedTransaction.TransactionId);
			if (transaction is { })
			{
				SelectedTransaction = transaction;
			}
		}
	}

	public string WalletName { get; private set; }

	public ObservableCollection<DebugCoinViewModel> Coins { get; private set; }

	public ObservableCollection<DebugTransactionViewModel> Transactions { get; private set; }

	public FlatTreeDataGridSource<DebugCoinViewModel> CoinsSource { get; private set; }

	public FlatTreeDataGridSource<DebugTransactionViewModel> TransactionsSource { get; private set; }

	private void CreateCoinsSource()
	{
		CoinsSource = new FlatTreeDataGridSource<DebugCoinViewModel>(Coins)
		{
			Columns =
			{
				new TextColumn<DebugCoinViewModel, DateTimeOffset>(
					"FirstSeen",
					x => x.FirstSeen,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, Money>(
					"Amount",
					x => x.Amount,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, bool>(
					"Confirmed",
					x => x.Confirmed,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, bool>(
					"CoinJoinInProgress",
					x => x.CoinJoinInProgress,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, bool>(
					"IsBanned",
					x => x.IsBanned,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, DateTimeOffset?>(
					"BannedUntilUtc",
					x => x.BannedUntilUtc,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, Height?>(
					"Height",
					x => x.Height,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, uint256>(
					"Transaction",
					x => x.TransactionId,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugCoinViewModel, uint256?>(
					"SpenderTransaction",
					x => x.SpenderTransactionId,
					new GridLength(0, GridUnitType.Auto)),
			}
		};

		CoinsSource.RowSelection!.SingleSelect = true;

		CoinsSource.RowSelection
			.WhenAnyValue(x => x.SelectedItem)
			.Subscribe(x => SelectedCoin = x);

		(CoinsSource as ITreeDataGridSource).SortBy(CoinsSource.Columns[0], ListSortDirection.Descending);
	}

	private void CreateTransactionsSource()
	{
		TransactionsSource = new FlatTreeDataGridSource<DebugTransactionViewModel>(Transactions)
		{
			Columns =
			{
				new TextColumn<DebugTransactionViewModel, DateTimeOffset>(
					"FirstSeen",
					x => x.FirstSeen,
					new GridLength(0, GridUnitType.Auto)),
				new TextColumn<DebugTransactionViewModel, uint256>(
					"TransactionId",
					x => x.TransactionId,
					new GridLength(0, GridUnitType.Auto)),
			}
		};

		TransactionsSource.RowSelection!.SingleSelect = true;

		TransactionsSource.RowSelection
			.WhenAnyValue(x => x.SelectedItem)
			.Subscribe(x => SelectedTransaction = x);

		(TransactionsSource as ITreeDataGridSource).SortBy(TransactionsSource.Columns[0], ListSortDirection.Descending);
	}

	private Dictionary<uint256, List<DebugCoinViewModel>> MapTransactions()
	{
		var transactionsDict = new Dictionary<uint256, List<DebugCoinViewModel>>();

		foreach (var coin in Coins)
		{
			if (transactionsDict.TryGetValue(coin.TransactionId, out _))
			{
				transactionsDict[coin.TransactionId].Add(coin);
			}
			else
			{
				transactionsDict[coin.TransactionId] = new List<DebugCoinViewModel> { coin };
			}

			if (coin.SpenderTransactionId is null)
			{
				continue;
			}

			if (transactionsDict.TryGetValue(coin.SpenderTransactionId, out _))
			{
				transactionsDict[coin.SpenderTransactionId].Add(coin);
			}
			else
			{
				transactionsDict[coin.SpenderTransactionId] = new List<DebugCoinViewModel> { coin };
			}
		}

		return transactionsDict;
	}
}