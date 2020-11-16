using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using WalletWasabi.Gui.ViewModels;

namespace WalletWasabi.Fluent.ViewModels.Dialogs
{
	public class DialogScreenViewModel : ViewModelBase, IScreen
	{
		private bool _isClosing;
		private bool _isDialogVisible;

		public DialogScreenViewModel()
		{
			Observable.FromEventPattern(Router.NavigationStack, nameof(Router.NavigationStack.CollectionChanged))
				.Subscribe(_ =>
				{
					if (!_isClosing)
					{
						IsDialogVisible = Router.NavigationStack.Count >= 1;
					}
				});

			this.WhenAnyValue(x => x.IsDialogVisible).Subscribe(x =>
			{
				if (!x)
				{
					// Reset navigation when Dialog is using IScreen for navigation instead of the default IDialogHost.
					Close();
				}
			});
		}

		public RoutingState Router { get; } = new RoutingState();

		public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

		public bool IsDialogVisible
		{
			get => _isDialogVisible;
			set => this.RaiseAndSetIfChanged(ref _isDialogVisible, value);
		}

		public void Close()
		{
			if (!_isClosing)
			{
				_isClosing = true;

				if (Router.CurrentViewModel.Latest().LastOrDefault() is DialogViewModelBase dvmb)
				{
					dvmb.CloseDialog();
				}

				if (Router.NavigationStack.Count >= 1)
				{
					Router.NavigationStack.Clear();
				}

				_isClosing = false;
			}
		}

		public void CloseDialog()
		{
			Close();
		}
	}
}