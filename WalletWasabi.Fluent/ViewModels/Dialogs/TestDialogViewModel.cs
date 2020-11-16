using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace WalletWasabi.Fluent.ViewModels.Dialogs
{
	public class TestDialogViewModel : DialogViewModelBase<bool>
	{
		private string _message;

		public TestDialogViewModel(NavigationStateViewModel navigationState, NavigationTarget navigationTarget, string message) : base(navigationState, navigationTarget)
		{
			_message = message;

			var backCommandCanExecute = this.WhenAnyValue(x => x.IsDialogOpen).ObserveOn(RxApp.MainThreadScheduler);

			var cancelCommandCanExecute = this.WhenAnyValue(x => x.IsDialogOpen).ObserveOn(RxApp.MainThreadScheduler);

			var nextCommandCanExecute = this.WhenAnyValue(x => x.IsDialogOpen).ObserveOn(RxApp.MainThreadScheduler);

			BackCommand = ReactiveCommand.Create(() => Close(false), backCommandCanExecute);
			CancelCommand = ReactiveCommand.Create(() => Close(false), cancelCommandCanExecute);
			NextCommand = ReactiveCommand.Create(() => Close(true), nextCommandCanExecute);
		}

		public string Message
		{
			get => _message;
			set => this.RaiseAndSetIfChanged(ref _message, value);
		}

		public ICommand NextCommand { get; }

		protected override void OnDialogClosed()
		{
			GoBack();
		}

		public void Close()
		{
			Close(false);
		}
	}
}