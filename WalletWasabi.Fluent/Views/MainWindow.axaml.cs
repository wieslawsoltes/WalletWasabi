using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Core2D.Screenshot;

namespace WalletWasabi.Fluent.Views
{
	public class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
#if DEBUG
			this.AttachDevTools();
			this.AttachCapture();
#endif
		}
	}
}
