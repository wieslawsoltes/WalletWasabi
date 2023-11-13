using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WalletWasabi.Fluent.Views.Wallets.Home.History;

public class TreeDataGridEx : Avalonia.Controls.TreeDataGrid
{
	protected override Type StyleKeyOverride => typeof(Avalonia.Controls.TreeDataGrid);

	protected override Size MeasureOverride(Size availableSize)
	{
		try
		{
			return base.MeasureOverride(availableSize);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);

		}
		return default;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		try
		{
			return base.ArrangeOverride(finalSize);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);

		}

		return default;
	}
}

public class HistoryTable : UserControl
{
	public HistoryTable()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
}
