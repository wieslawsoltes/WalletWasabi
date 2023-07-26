using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Controls;
using ReactiveUI;

namespace WalletWasabi.Fluent.Behaviors;

// TODO: This is a workaround for this Avalonia issue (https://github.com/AvaloniaUI/Avalonia/issues/11850)
// Please, remove this Behavior and its unique usage after it's been fixed.
public class RestartRendererInLinuxBehavior : DisposingBehavior<Window>
{
	protected override void OnAttached(CompositeDisposable disposables)
	{
		if (AssociatedObject is null)
		{
			return;
		}

		PropertyInfo rendererProperty = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Instance | BindingFlags.NonPublic);
		var rendererValue = rendererProperty.GetValue(AssociatedObject);
		var rendererType = rendererValue.GetType();
		var startMethod = rendererType.GetMethod("Start");

		AssociatedObject
			.WhenAnyValue(x => x.WindowState)
			.Where(state => state == WindowState.Normal)
			.Where(_ => OperatingSystem.IsLinux())
			.Do(_ =>
			{
				startMethod.Invoke(rendererValue, null);
				AssociatedObject.Activate();
			})
			.Subscribe()
			.DisposeWith(disposables);
	}
}
