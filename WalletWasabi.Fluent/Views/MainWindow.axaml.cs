using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WalletWasabi.Fluent.Views
{
	public class Item
	{
		public string Date { get; set; }
		public string Description { get; set; }
		public string Outgoing { get; set; }
		public string Incoming { get; set; }
		public string Status { get; set; }
	}

	public class MainWindow : Window
	{
		public ObservableCollection<Item> Items { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			Items = new ObservableCollection<Item>()
			{
				new ()
				{
					Date = "7/12/2020",
					Description = "Purchase MotorBike",
					Outgoing = "1.2 BTC",
					Incoming = "",
					Status = "Pending"
				},
				new ()
				{
					Date = "7/12/2020",
					Description = "Salary",
					Outgoing = "",
					Incoming = "100.0 BTC",
					Status = "Confirmed"
				},
				new ()
				{
					Date = "4/12/2020",
					Description = "Rent",
					Outgoing = "1.0 BTC",
					Incoming = "",
					Status = "Confirmed"
				},
				new ()
				{
					Date = "4/12/2020",
					Description = "Spending",
					Outgoing = "1.0 BTC",
					Incoming = "",
					Status = "Confirmed"
				},
				new ()
				{
					Date = "4/11/2020",
					Description = "Salary",
					Outgoing = "",
					Incoming = "100.0 BTC",
					Status = "Confirmed"
				}
			};

			var itemsDataGrid = this.FindControl<DataGrid>("ItemsDataGrid");
			if (itemsDataGrid is not null)
			{
				itemsDataGrid.Items = Items;
			}
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			this.AttachDevTools();
		}
	}
}
