using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using WalletWasabi.Fluent.ViewModels;

namespace WalletWasabi.Fluent;

[StaticViewLocator]
public partial class ViewLocator : IDataTemplate
{
	private readonly Dictionary<object, Control> _cache = new();

	public IControl Build(object data)
	{
		var type = data.GetType();

		Control? control;
		if (_cache.TryGetValue(data, out control))
		{
#if DEBUG
			Console.WriteLine($"[CACHED] '{data}'='{control}'");
#endif
			return control;
		}

		if (s_views.TryGetValue(type, out var func))
		{
			control = func.Invoke();
			_cache[data] = control;
#if DEBUG
			Console.WriteLine($"[ADD] '{data}'='{control}'");
#endif
			return control;
		}
		throw new Exception($"Unable to create view for type: {type}");
	}

	public bool Match(object data)
	{
		return data is ViewModelBase;
	}
}
