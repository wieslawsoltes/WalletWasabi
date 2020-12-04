using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WalletWasabi.Fluent.Generators
{
	[Generator]
	public class SearchIndexGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			// System.Diagnostics.Debugger.Launch();
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
			{
				return;
			}

			var source = CreateUserControls(context, receiver.CandidateClasses);

			context.AddSource($"SearchIndex.cs", SourceText.From(source, Encoding.UTF8));
		}

		private string CreateUserControls(GeneratorExecutionContext context, List<ClassDeclarationSyntax> candidateClasses)
		{
			List<INamedTypeSymbol> namedTypeSymbols = new();

			var compilation = context.Compilation;

			foreach (var candidateClass in candidateClasses)
			{
				var semanticModel = compilation.GetSemanticModel(candidateClass.SyntaxTree);

				var namedTypeSymbol = semanticModel.GetDeclaredSymbol(candidateClass);
				if (namedTypeSymbol is null)
				{
					continue;
				}

				namedTypeSymbols.Add(namedTypeSymbol);
			}


			var userControlSymbol = compilation.GetTypeByMetadataName("Avalonia.Controls.UserControl");

			var source = new StringBuilder();

			source.Append($@"#nullable enable
using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace WalletWasabi.Fluent
{{
	public static class SearchIndex
	{{
		public static List<Func<UserControl>> UserControls {{ get; }} = new()
		{{
");

			var userControlSymbols = new List<INamedTypeSymbol>();

			var countAll = namedTypeSymbols.Count;
			for (var i = 0; i < countAll; i++)
			{
				var symbol = namedTypeSymbols[i];
				var baseTypes = GetBaseTypes(symbol);
				foreach (var baseType in baseTypes)
				{
					if (SymbolEqualityComparer.Default.Equals(baseType, userControlSymbol))
					{
						userControlSymbols.Add(symbol);
					}
				}
			}

			var countControls = userControlSymbols.Count;
			for (var i = 0; i < countControls; i++)
			{
				var control = userControlSymbols[i];
				source.Append(
					$"\t\t\t() => new {control.ToDisplayString()}(){(i < countControls - 1 ? $",{Environment.NewLine}" : "")}");
			}

			source.Append($@"
		}};
	}}
}}");
			return source.ToString();
		}

		private List<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol namedTypeSymbol)
		{
			var result = new List<INamedTypeSymbol>();
			var baseType = namedTypeSymbol.BaseType;
			while (true)
			{
				if (baseType is null)
				{
					break;
				}
				result.Add(baseType);
				baseType = baseType.BaseType;
			}
			return result;
		}

		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
				{
					CandidateClasses.Add(classDeclarationSyntax);
				}
			}
		}
	}
}