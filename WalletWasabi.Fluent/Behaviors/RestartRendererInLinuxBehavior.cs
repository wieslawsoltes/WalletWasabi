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

		var property = typeof(TopLevel).GetProperty("Renderer", BindingFlags.Instance | BindingFlags.NonPublic);

		if (property is not { } renderedProperty)
		{
			return;
		}

		var rendererValue = renderedProperty.GetValue(AssociatedObject);
		if (rendererValue is not { } renderer)
		{
			return;
		}

		var rendererType = renderer.GetType();
		var startMethod = rendererType.GetMethod("Start");

		if (startMethod is not { } start)
		{
			return;
		}

		AssociatedObject
			.WhenAnyValue(x => x.WindowState)
			.Where(state => state == WindowState.Normal)
			.Where(_ => OperatingSystem.IsLinux())
			.Do(_ =>
			{
				start.Invoke(rendererValue, null);
				AssociatedObject.Activate();
			})
			.Subscribe()
			.DisposeWith(disposables);
	}
}
