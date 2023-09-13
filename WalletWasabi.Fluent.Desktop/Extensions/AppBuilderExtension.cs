using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using WalletWasabi.Logging;

namespace WalletWasabi.Fluent.Desktop.Extensions;

public sealed class Hind2FontCollection : EmbeddedFontCollection
{
	public Hind2FontCollection() : base(
		new Uri("fonts:Hind2", UriKind.Absolute),
		new Uri("avares://WalletWasabi.Fluent/Assets/Fonts", UriKind.Absolute))
	{
	}
}
public static class AppBuilderHind2FontExtension
{
	public static AppBuilder WithHind2Font(this AppBuilder appBuilder)
	{
		return appBuilder.ConfigureFonts(fontManager =>
		{
			fontManager.AddFontCollection(new Hind2FontCollection());
		});
	}
}
public static class AppBuilderExtension
{
	public static AppBuilder SetupAppBuilder(this AppBuilder appBuilder)
	{
		bool enableGpu = Services.PersistentConfig is null ? false : Services.PersistentConfig.EnableGpu;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			appBuilder
				.UseWin32()
				.UseSkia();
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			appBuilder.UsePlatformDetect()
				.UseManagedSystemDialogs<Window>();
		}
		else
		{
			appBuilder.UsePlatformDetect();
		}

		return appBuilder
			.WithInterFont()
			// .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter, $Default" })
			.WithHind2Font()
			.With(new FontManagerOptions { DefaultFamilyName = "fonts:Hind2#Hind2, $Default" })
			.With(new SkiaOptions { MaxGpuResourceSizeBytes = 2560 * 1600 * 4 * 12 })
			.With(new Win32PlatformOptions
			{
				RenderingMode = enableGpu
					? new[] { Win32RenderingMode.AngleEgl, Win32RenderingMode.Software }
					: new[] { Win32RenderingMode.Software },
				CompositionMode = new[] { Win32CompositionMode.WinUIComposition, Win32CompositionMode.RedirectionSurface }
			})
			.With(new X11PlatformOptions
			{
				RenderingMode = enableGpu
					? new[] { X11RenderingMode.Glx, X11RenderingMode.Software }
					: new[] { X11RenderingMode.Software },
				WmClass = "Wasabi Wallet"
			})
			.With(new AvaloniaNativePlatformOptions
			{
				RenderingMode = enableGpu
					? new[] { AvaloniaNativeRenderingMode.OpenGl, AvaloniaNativeRenderingMode.Software }
					: new[] { AvaloniaNativeRenderingMode.Software },
			})
			.With(new MacOSPlatformOptions { ShowInDock = true });
	}
}
