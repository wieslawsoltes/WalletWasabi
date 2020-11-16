using ReactiveUI;
using System;
using WalletWasabi.Fluent.ViewModels.Dialogs;

namespace WalletWasabi.Fluent.ViewModels
{
	public enum NavigationTarget
	{
		Default = 0,
		HomeScreen = 1,
		DialogScreen = 2
	}

	public class NavigationStateViewModel
	{
		public NavigationStateViewModel(Func<IScreen> homeScreen, Func<IScreen> dialogScreen)
		{
			HomeScreen = homeScreen;
			DialogScreen = dialogScreen;
		}

		public Func<IScreen> HomeScreen { get; }
		public Func<IScreen> DialogScreen { get; }
	}
}