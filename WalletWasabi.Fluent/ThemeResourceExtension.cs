using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
	internal static class Extensions
    {
        public static T GetService<T>(this IServiceProvider sp) => (T)sp?.GetService(typeof(T));


        public static Uri GetContextBaseUri(this IServiceProvider ctx) => ctx.GetService<IUriContext>().BaseUri;

        public static T GetFirstParent<T>(this IServiceProvider ctx) where T : class
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>().FirstOrDefault();

        public static T GetLastParent<T>(this IServiceProvider ctx) where T : class
            => ctx.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>().LastOrDefault();

        public static IEnumerable<T> GetParents<T>(this IServiceProvider sp)
        {
            return sp.GetService<IAvaloniaXamlIlParentStackProvider>().Parents.OfType<T>();


        }

        public static Type ResolveType(this IServiceProvider ctx, string namespacePrefix, string type)
        {
            var tr = ctx.GetService<IXamlTypeResolver>();
            string name = string.IsNullOrEmpty(namespacePrefix) ? type : $"{namespacePrefix}:{type}";
            return tr?.Resolve(name);
        }

        public static object GetDefaultAnchor(this IServiceProvider provider)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            object anchor = provider.GetFirstParent<IControl>();

            if (anchor is null)
            {
                // Try to find IDataContextProvider, this was added to allow us to find
                // a datacontext for Application class when using NativeMenuItems.
                anchor = provider.GetFirstParent<IDataContextProvider>();
            }

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                   provider.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                   provider.GetLastParent<IStyle>();
        }
    }

    public class ThemeResourceExtension
    {
	    private static Dictionary<(object, Type), object> s_resources = new Dictionary<(object, Type), object>();

	    public ThemeResourceExtension()
	    {
	    }

	    public ThemeResourceExtension(object resourceKey)
	    {
		    ResourceKey = resourceKey;
	    }

	    public object ResourceKey { get; set; }

	    public object ProvideValue(IServiceProvider serviceProvider)
	    {
		    var stack = serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>();
		    var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

		    var targetType = provideTarget.TargetProperty switch
		    {
			    AvaloniaProperty ap => ap.PropertyType,
			    PropertyInfo pi => pi.PropertyType,
			    _ => null,
		    };

		    if (provideTarget.TargetObject is Setter setter)
		    {
			    targetType = setter.Property.PropertyType;
		    }

		    if (s_resources.ContainsKey((ResourceKey, targetType)))
		    {
			    //Console.WriteLine($"[ThemeResource] cached '{ResourceKey}' '{targetType}'");
			    return s_resources[(ResourceKey, targetType)];
		    }


		    // Look upwards though the ambient context for IResourceHosts and IResourceProviders
		    // which might be able to give us the resource.
		    foreach (var e in stack.Parents)
		    {
			    object value;

			    if (e is IResourceHost host && host.TryGetResource(ResourceKey, out value))
			    {
				    var resource = Convert(value, targetType);
					if (resource is { })
				    {
					    //Console.WriteLine($"[ThemeResource] add '{ResourceKey}' '{targetType}'");
					    s_resources[(ResourceKey, targetType)] = resource;
				    }

				    return resource;
			    }
			    else if (e is IResourceProvider provider && provider.TryGetResource(ResourceKey, out value))
			    {
				    var resource =  Convert(value, targetType);
				    if (resource is { })
				    {
					    //Console.WriteLine($"[ThemeResource] add '{ResourceKey}' '{targetType}'");
					    s_resources[(ResourceKey, targetType)] = resource;
				    }

				    return resource;
			    }
		    }

		    if (provideTarget.TargetObject is IControl target &&
		        provideTarget.TargetProperty is PropertyInfo property)
		    {
			    DelayedBinding.Add(target, property, x =>
			    {
				    var resource = GetValue(x, targetType);
				    if (resource is { })
				    {
					    //Console.WriteLine($"[ThemeResource] add '{ResourceKey}' '{targetType}'");
					    s_resources[(ResourceKey, targetType)] = resource;
				    }

				    return resource;
			    });
			    return AvaloniaProperty.UnsetValue;
		    }

		    //throw new KeyNotFoundException($"Static resource '{ResourceKey}' not found.");
		    //Console.WriteLine($"[ThemeResource] Static resource '{ResourceKey}' not found.");
		    return AvaloniaProperty.UnsetValue;
	    }

	    private object GetValue(IStyledElement control, Type targetType)
	    {
		    var value = control.FindResource(ResourceKey);
		    if (value is IBrush && targetType == typeof(IBrush))
		    {
			    return value;
		    }
		    if (targetType == typeof(IBrush))
		    {
			    return ColorToBrushConverter.Convert(value, typeof(IBrush));
		    }
		    return value;
	    }

	    private object Convert(object value, Type targetType)
	    {
		    if (value is IBrush && targetType == typeof(IBrush))
		    {
			    return value;
		    }
		    if (targetType == typeof(IBrush))
		    {
			    return ColorToBrushConverter.Convert(value, typeof(IBrush));
		    }
		    return value;
	    }
    }
}
