using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Razor;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;

namespace Strings.Razor
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var razorEngineHost = new RazorEngineHost(new CSharpRazorCodeLanguage());

			var source =
@"@page
@model RazorPagesMovie.Pages.Movies.IndexModel

@{
	ViewData[""Title""] = ""Index"";
	Console.WriteLine(""Hello, World!"");
	Console.WriteLine(@""Hello, World!"");
	Console.WriteLine($""Hello, {1}!"");
	Console.WriteLine($@""Hello, {1}!"");
}

<h1>Index</h1>

<div
	a=""@(""Hello, World!"")""
	b=""@(@""Hello, World!"")""
	c=""@($""Hello, {1}!"")""
	d=""@($@""Hello, {1}!"")""
</div>

@* Hello World *@

<p>
	<a asp-page=""Create"">Create New</a>
</p>
<table class=""table"">
	<tbody>
@foreach (var item in Model.Movie) {
		<tr>
			<td>
				@Html.DisplayFor(modelItem => item.Title)
			</td>
			<td>
				@Html.DisplayFor(modelItem => item.ReleaseDate)
			</td>
			<td>
				@Html.DisplayFor(modelItem => item.Genre)
			</td>
			<td>
				@Html.DisplayFor(modelItem => item.Price)
			</td>
			<td>
				<a asp-page=""./Edit"" asp-route-id=""@item.ID"">Edit</a> |
				<a asp-page=""./Details"" asp-route-id=""@item.ID"">Details</a> |
				<a asp-page=""./Delete"" asp-route-id=""@item.ID"">Delete</a>
			</td>
		</tr>
}
	</tbody>
</table>
";

			var parser = new RazorParser(new CSharpCodeParser(), new HtmlMarkupParser());
			parser.Parse(new StringReader(source), new RazorParserVisitor());

			if (Debugger.IsAttached)
			{
				Console.WriteLine("Done.");
				Console.ReadKey();
			}
		}

		private class RazorParserVisitor : ParserVisitor
		{
			public override void VisitBlock(Block block)
			{
				base.VisitBlock(block);
			}

			public override void VisitStartBlock(Block block)
			{
				base.VisitStartBlock(block);
			}

			public override void VisitEndBlock(Block block)
			{
				base.VisitEndBlock(block);
			}

			public override void VisitSpan(Span span)
			{
				if (span.Kind != SpanKind.Transition
					&& span.Kind != SpanKind.MetaCode
					&& span.Kind != SpanKind.Comment
					&& !string.IsNullOrWhiteSpace(span.Content))
				{
					Console.WriteLine($"[{span.Kind}]:");
					Console.ForegroundColor = (int)span.Kind + ConsoleColor.Blue;
					Console.WriteLine(span.Content);
					Console.ResetColor();
				}

				base.VisitSpan(span);
			}
		}
	}
}
