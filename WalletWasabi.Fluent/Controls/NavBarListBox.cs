using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Styling;
using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace WalletWasabi.Fluent.Controls
{
	public class NavBarListBox : ListBox, IStyleable
	{
		public static readonly StyledProperty<bool> ReSelectSelectedItemProperty =
			AvaloniaProperty.Register<NavBarListBox, bool>(nameof(ReSelectSelectedItem), true);

		public bool ReSelectSelectedItem
		{
			get => GetValue(ReSelectSelectedItemProperty);
			set => SetValue(ReSelectSelectedItemProperty, value);
		}

		Type IStyleable.StyleKey => typeof(ListBox);

		protected override IItemContainerGenerator CreateItemContainerGenerator()
		{
			return new ItemContainerGenerator<NavBarItem>(
				this,
				ContentControl.ContentProperty,
				ContentControl.ContentTemplateProperty);
		}

		static NavBarListBox()
		{
			//SelectedItemProperty.OverrideMetadata<SelectingItemsControl>(new DirectPropertyMetadata<object?>(null, BindingMode.TwoWay, false));
		}

		public NavBarListBox()
		{
			this.GetObservable(SelectedItemProperty).Subscribe(x =>
			{
				Console.WriteLine($"[Selection.NavBarListBox.SelectedItemProperty] [{Name}].SelectedItem='{x}'");
			});
		}

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);

			if (change.Property.Name == "SelectedItem")
			{
				Console.WriteLine($"[Selection.NavBarListBox.OnPropertyChanged] [{Name}].SelectedItem='{SelectedItem}'");
			}
		}

		protected override void UpdateDataValidation<T>(AvaloniaProperty<T> property, BindingValue<T> value)
		{
			//base.UpdateDataValidation(property, value);
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			var previousSelectedItem = SelectedItem;

			Console.WriteLine($"[Selection.NavBarListBox.OnPointerPressed] [BEFORE] [{Name}].SelectedItem is '{SelectedItem}'");
			base.OnPointerPressed(e);
			Console.WriteLine($"[Selection.NavBarListBox.OnPointerPressed] [AFTER] [{Name}].SelectedItem is '{SelectedItem}'");

			// Trigger SelectedItem change notification on pointer pressed event when it was already selected.
			// This enables view model to receive change notification on pointer pressed events using SelectedItem observable.
			if (ReSelectSelectedItem)
			{
				var isSameSelectedItem = previousSelectedItem is not null && previousSelectedItem == SelectedItem;
				if (isSameSelectedItem)
				{
					Console.WriteLine($"[Selection.NavBarListBox.OnPointerPressed] [{Name}].SelectedItem is '{SelectedItem}'");
					Console.WriteLine($"[Selection.NavBarListBox.OnPointerPressed] [{Name}].SelectedItem='null'");
					SelectedItem = null;
					Console.WriteLine($"[Selection.NavBarListBox.OnPointerPressed] [{Name}].SelectedItem='{previousSelectedItem}'");
					SelectedItem = previousSelectedItem;
				}
			}
		}
	}
}
