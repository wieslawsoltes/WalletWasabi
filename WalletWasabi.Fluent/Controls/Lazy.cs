using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace WalletWasabi.Fluent.Controls
{
	public enum LazyLoad
	{
		/// <summary>
		/// Load control instantly
		/// </summary>
		Normal,

		/// <summary>
		/// Load control on Visibility == Visible
		/// </summary>
		Lazy
	}

	public enum LazyUnload
	{
		/// <summary>
		/// Unload control on default unload
		/// </summary>
		Normal,

		/// <summary>
		/// Unload control on Visibility != Visible
		/// </summary>
		OnHide,

		/// <summary>
		/// Unload control after Visibility != Visible on a specified period of time
		/// </summary>
		Lazy
	}

	public class Lazy : Control
	{
		#region LoadProperty

		/// <summary>
		///     The DependencyProperty for Load.
		///     Default value: Lazy
		/// </summary>
		public static readonly StyledProperty<LazyLoad> LoadProperty = AvaloniaProperty.Register<Lazy, LazyLoad>(
			nameof(Load), LazyLoad.Lazy);

		/// <summary>
		///     Get or set the Load property.
		///     Default value: Lazy
		/// </summary>
		public LazyLoad Load
		{
			get => GetValue(LoadProperty);
			set => SetValue(LoadProperty, value);
		}

		#endregion

		#region UnloadProperty

		/// <summary>
		///     The DependencyProperty for Unload.
		///     Default value: Lazy
		/// </summary>
		public static readonly StyledProperty<LazyUnload> UnloadProperty = AvaloniaProperty.Register<Lazy, LazyUnload>(
			nameof(Unload), LazyUnload.Lazy);

		/// <summary>
		///     Get or set the Unload property.
		///     Default value: Lazy
		/// </summary>
		public LazyUnload Unload
		{
			get => GetValue(UnloadProperty);
			set => SetValue(UnloadProperty, value);
		}

		#endregion

		#region TypeProperty

		/// <summary>
		///     The DependencyProperty for Type.
		///     Default value: Lazy
		/// </summary>
		public static readonly StyledProperty<Type?>
			TypeProperty = AvaloniaProperty.Register<Lazy, Type?>(nameof(Type));

		/// <summary>
		///     Get or set the Type property.
		///     Default value: null
		/// </summary>
		public Type? Type
		{
			get => GetValue(TypeProperty);
			set => SetValue(TypeProperty, value);
		}

		#endregion

		private readonly TimeSpan _unloadDelay = TimeSpan.FromMinutes(
#if DEBUG
			0.2 //16 sec
#else
            2
#endif
		);

		private Task? _currentUnloadWaitTask;
		private IControl? _child;
		public bool IsLoadInProgress { get; private set; }

		[Content]
		public IControl? Child
		{
			get => _child;
			set
			{
				var oldChild = _child as Control;
				var newChild = value as Control;

				_child = value;

				if (oldChild != null)
				{
					((ISetLogicalParent)oldChild).SetParent(null);
					LogicalChildren.Clear();
					VisualChildren.Remove(oldChild);
				}

				if (newChild != null)
				{
					((ISetLogicalParent)newChild).SetParent(this);
					VisualChildren.Add(newChild);
					LogicalChildren.Add(newChild);
				}

				InvalidateMeasure();
			}
		}

		static Lazy()
		{
			IsVisibleProperty.Changed.AddClassHandler<Lazy>((x, e) => x.OnIsVisibleChanged(e));
			TypeProperty.Changed.AddClassHandler<Lazy>((x, e) => x.Name ??= $"<{(e.NewValue as Type)?.Name}>");
		}

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			_currentUnloadWaitTask = null;

			if (Type == null ||
			    IsLoadInProgress ||
			    Parent == null)
			{
				return;
			}

			if (Load == LazyLoad.Normal)
			{
				LazyQueue.LoadInQueue(this);
			}
			else if (IsVisible && Load == LazyLoad.Lazy && Child == null)
			{
				LazyQueue.LoadInQueue(this);
			}
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			_currentUnloadWaitTask = null;
			base.OnDetachedFromVisualTree(e);
		}

		private void LoadChild()
		{
			if (Type == null ||
			    IsLoadInProgress ||
			    Parent == null)
			{
				return;
			}

			IsLoadInProgress = true;
			if (Child != null)
			{
				Child = null;
			}

			Child = Activator.CreateInstance(Type) as Control;
			IsLoadInProgress = false;
			if (Child != null)
			{
				Child.IsVisible = true;
			}
		}

		private void OnIsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
		{
			_currentUnloadWaitTask = null;

			if (Type == null ||
			    IsLoadInProgress ||
			    Parent == null)
			{
				return;
			}

			if (e.NewValue is true)
			{
				if (Child == null && Load == LazyLoad.Lazy)
				{
					LazyQueue.LoadInQueue(this);
				}
			}
			else
			{
				if (Unload == LazyUnload.Lazy)
				{
					UnloadOnDelay();
				}
				else if (Unload == LazyUnload.OnHide && Child != null)
				{
					Child = null;
				}
			}
		}

		private async void UnloadOnDelay()
		{
			var task = _currentUnloadWaitTask = Task.Delay(_unloadDelay);
			await task;
			if (!ReferenceEquals(task, _currentUnloadWaitTask) ||
			    Child == null)
			{
				return;
			}

			Dispatcher.UIThread.Post(() => Child = null);
		}

		protected override Size MeasureOverride(Size availableSize)
			=> LayoutHelper.MeasureChild(Child, availableSize, new Thickness());

		protected override Size ArrangeOverride(Size finalSize)
			=> LayoutHelper.ArrangeChild(Child, finalSize, new Thickness());

		private static class LazyQueue
		{
			private static readonly Queue<Lazy> _loadQueue = new();
			private static bool _isQueueStarted;

			public static void LoadInQueue(Lazy item)
			{
				_loadQueue.Enqueue(item);
				if (!_isQueueStarted)
				{
					Start();
				}
			}

			private static async void Start()
			{
				_isQueueStarted = true;
				while (_loadQueue.TryDequeue(out var item))
				{
					item.LoadChild();
					await Task.Delay(1);
				}

				_isQueueStarted = false;
			}
		}
	}
}