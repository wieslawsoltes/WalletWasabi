using System;
using NBitcoin;
using ReactiveUI;
using System.Reactive;
using WalletWasabi.Gui.ViewModels;
using WalletWasabi.Fluent.ViewModels.Dialogs;
using Global = WalletWasabi.Gui.Global;
using WalletWasabi.Fluent.ViewModels.NavBar;

namespace WalletWasabi.Fluent.ViewModels
{
	public class MainViewModel : ViewModelBase, IScreen
	{
		private readonly Global _global;
		private StatusBarViewModel _statusBar;
		private string _title = "Wasabi Wallet";
		private DialogScreenViewModel? _dialogScreen;
		private NavBarViewModel _navBar;
		private bool _isMainContentEnabled;

		public MainViewModel(Global global)
		{
			_global = global;

			_dialogScreen = new DialogScreenViewModel();

			var navigationState = new NavigationStateViewModel(() => this, () => _dialogScreen);

			Network = global.Network;

			_isMainContentEnabled = true;

			_statusBar = new StatusBarViewModel(global.DataDir, global.Network, global.Config, global.HostedServices, global.BitcoinStore.SmartHeaderChain, global.Synchronizer, global.LegalDocuments);

			var walletManager = new WalletManagerViewModel(navigationState, global.WalletManager, global.UiConfig);

			var addWalletPage = new AddWalletPageViewModel(navigationState, global.WalletManager, global.BitcoinStore, global.Network);

			_navBar = new NavBarViewModel(navigationState, Router, walletManager, addWalletPage);

			this.WhenAnyValue(x => x.DialogScreen!.IsDialogVisible)
				.Subscribe(x => IsMainContentEnabled = !x);
		}

		public bool IsMainContentEnabled
		{
			get => _isMainContentEnabled;
			set => this.RaiseAndSetIfChanged(ref _isMainContentEnabled, value);
		}

		public static MainViewModel? Instance { get; internal set; }

		public RoutingState Router { get; } = new RoutingState();

		public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

		private Network Network { get; }

		public DialogScreenViewModel? DialogScreen
		{
			get => _dialogScreen;
			set => this.RaiseAndSetIfChanged(ref _dialogScreen, value);
		}

		public NavBarViewModel NavBar
		{
			get => _navBar;
			set => this.RaiseAndSetIfChanged(ref _navBar, value);
		}

		public StatusBarViewModel StatusBar
		{
			get => _statusBar;
			set => this.RaiseAndSetIfChanged(ref _statusBar, value);
		}

		public string Title
		{
			get => _title;
			internal set => this.RaiseAndSetIfChanged(ref _title, value);
		}

		public void Initialize()
		{
			// Temporary to keep things running without VM modifications.
			MainWindowViewModel.Instance = new MainWindowViewModel(_global.Network, _global.UiConfig, _global.WalletManager, null!, null!, false);

			StatusBar.Initialize(_global.Nodes.ConnectedNodes);

			if (Network != Network.Main)
			{
				Title += $" - {Network}";
			}
		}
	}
}
