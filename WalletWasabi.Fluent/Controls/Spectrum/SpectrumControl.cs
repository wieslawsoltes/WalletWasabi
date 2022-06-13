using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace WalletWasabi.Fluent.Controls.Spectrum;

public class SpectrumControl : TemplatedControl, ICustomDrawOperation
{
	private const int NumBins = 250;

	private readonly AuraSpectrumDataSource _auraSpectrumDataSource;
	private readonly SplashEffectDataSource _splashEffectDataSource;

	private readonly SpectrumDataSource[] _sources;

	private IBrush? _lineBrush;
	private SKColor _lineColor;

	private float[] _data;

	private bool _isAuraActive;
	private bool _isSplashActive;

	public static readonly StyledProperty<bool> IsActiveProperty =
		AvaloniaProperty.Register<SpectrumControl, bool>(nameof(IsActive));

	public static readonly StyledProperty<bool> IsDockEffectVisibleProperty =
		AvaloniaProperty.Register<SpectrumControl, bool>(nameof(IsDockEffectVisible));

	public SpectrumControl()
	{
		SetVisibility();
		_data = new float[NumBins];
		_auraSpectrumDataSource = new AuraSpectrumDataSource(NumBins);
		_splashEffectDataSource = new SplashEffectDataSource(NumBins);

		_auraSpectrumDataSource.GeneratingDataStateChanged += OnAuraGeneratingDataStateChanged;
		_splashEffectDataSource.GeneratingDataStateChanged += OnSplashGeneratingDataStateChanged;

		_sources = new SpectrumDataSource[] { _auraSpectrumDataSource };

		Background = new RadialGradientBrush()
		{
			GradientStops =
			{
				new GradientStop { Color = Color.Parse("#00000D21"), Offset = 0 },
				new GradientStop { Color = Color.Parse("#FF000D21"), Offset = 1 }
			}
		};

		DispatcherTimer.Run(
			() =>
			{
				if (IsVisible)
				{
					InvalidateVisual();
				}

				return true;
			},
			TimeSpan.FromMilliseconds((float)1000/60),
			DispatcherPriority.Render);
	}

	public bool IsActive
	{
		get => GetValue(IsActiveProperty);
		set => SetValue(IsActiveProperty, value);
	}

	public bool IsDockEffectVisible
	{
		get => GetValue(IsDockEffectVisibleProperty);
		set => SetValue(IsDockEffectVisibleProperty, value);
	}

	private void OnSplashGeneratingDataStateChanged(object? sender, bool e)
	{
		_isSplashActive = e;
		SetVisibility();
	}

	private void OnAuraGeneratingDataStateChanged(object? sender, bool e)
	{
		_isAuraActive = e;
		SetVisibility();
	}

	private void SetVisibility()
	{
		var isVisible = _isSplashActive || _isAuraActive;

		IsVisible = isVisible;
	}

	private void OnIsActiveChanged()
	{
		_auraSpectrumDataSource.IsActive = IsActive;

		if (IsActive)
		{
			_auraSpectrumDataSource.Start();
		}
	}

	protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == IsActiveProperty)
		{
			OnIsActiveChanged();
		}
		else if (change.Property == IsDockEffectVisibleProperty)
		{
			if (change.NewValue.GetValueOrDefault<bool>() && !IsActive)
			{
				_splashEffectDataSource.Start();
			}
		}
		else if (change.Property == ForegroundProperty)
		{
			_lineBrush = Foreground ?? Brushes.Magenta;

			if (_lineBrush is ImmutableSolidColorBrush brush)
			{
				_lineColor = brush.Color.ToSKColor();
			}

			InvalidateArrange();
		}
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		for (int i = 0; i < NumBins; i++)
		{
			_data[i] = 0;
		}

		foreach (var source in _sources)
		{
			source.Render(ref _data);
		}

		context.Custom(this);
	}

	private void RenderBars(SKCanvas context, float offsetX, float offsetY)
	{
		var width = Bounds.Width;
		var height = Bounds.Height;
		var thickness = width / NumBins;
		var center = (width / 2);

		double x = 0;

		using var linePaint = new SKPaint()
		{
			Color = _lineColor,
			IsAntialias = false,
			Style = SKPaintStyle.Fill
		};

		using var path = new SKPath();

		for (int i = 0; i < NumBins; i++)
		{
			var dCenter = Math.Abs(x - center);
			var multiplier = 1 - (dCenter / center);
			var rect = new SKRect(
				(float) x + offsetX,
				(float) height + offsetY,
				(float) (x + thickness + offsetX),
				(float) (height - multiplier * _data[i] * (height * 0.8)) + offsetY);
			path.AddRect(rect);

			x += thickness;
		}

		context.DrawPath(path, linePaint);
	}

	void IDisposable.Dispose()
	{
		// nothing to do.
	}

	bool IDrawOperation.HitTest(Point p) => Bounds.Contains(p);


	private SKSurface? _surface;
	private Size _lastSize = Size.Empty;
	private SKPaint _effect = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(24, 24, SKShaderTileMode.Clamp) };
	private SKImage? _frame1;
	private int frame = 0;
	private int frames = 60;


	void IDrawOperation.Render(IDrawingContextImpl context)
	{
		var bounds = Bounds;

		if (context is not ISkiaDrawingContextImpl skia)
		{
			return;
		}

		if (_surface is null || bounds.Size != _lastSize)
		{
			_surface?.Dispose();
			_lastSize = bounds.Size;

			_surface = SKSurface.Create(skia.GrContext, false, new SKImageInfo((int)_lastSize.Width, (int)_lastSize.Height * frames));

			_surface.Canvas.Clear();

			//_surface.Canvas.SaveLayer(_effect);

			_auraSpectrumDataSource.IsActive = true;
			_auraSpectrumDataSource.OnMixData();

			for (int i = 0; i < frames; i++)
			{
				_auraSpectrumDataSource.Render(ref _data);
				RenderBars(_surface.Canvas, 0f, (float) (_lastSize.Height * i));
			}

			//_surface.Canvas.Restore();

			_frame1 = _surface.Snapshot();
		}

		skia.SkCanvas.DrawImage(_frame1,
			 SKRect.Create(0, (float)(frame * _lastSize.Height), (float)_lastSize.Width, (float)_lastSize.Height),
			 SKRect.Create(0,0, (float)_lastSize.Width, (float)_lastSize.Height));
		frame++;
		if (frame >= frames)
		{
			frame = 0;
		}
	}

	bool IEquatable<ICustomDrawOperation>.Equals(ICustomDrawOperation? other) => false;
}