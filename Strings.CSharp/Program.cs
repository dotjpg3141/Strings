using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace Strings.CSharp
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var tree = CSharpSyntaxTree.ParseText(
@"using System;
using System.Collections;
using System.Linq;
using System.Text;
 
namespace HelloWorld
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(""Hello, World!"");
			Console.WriteLine(@""Hello, World!"");
			Console.WriteLine($""Hello, {1}!"");
			Console.WriteLine($@""Hello, {1}!"");
		}
	}
}");

			var root = (CompilationUnitSyntax)tree.GetRoot();
			var collector = new StringCollector();
			root.Accept(collector);

			var expressions = collector.Expressions.OrderBy(expr => expr.GetFirstToken().SpanStart);
			foreach (var expr in expressions)
			{
				Console.WriteLine(expr);
			}

			if (Debugger.IsAttached)
			{
				Console.WriteLine("Done.");
				Console.ReadKey();
			}
		}
	}

	internal class StringCollector : CSharpSyntaxWalker
	{
		private readonly HashSet<ExpressionSyntax> expressions = new HashSet<ExpressionSyntax>();

		public IEnumerable<ExpressionSyntax> Expressions => this.expressions;

		public override void VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			if (node.Token.Kind() == SyntaxKind.StringLiteralToken)
			{
				this.expressions.Add(node);
			}
			base.VisitLiteralExpression(node);
		}

		public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
		{
			this.expressions.Add(node);
			base.VisitInterpolatedStringExpression(node);
		}
	}
}
