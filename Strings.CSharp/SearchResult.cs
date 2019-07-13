using System.IO;

namespace Strings.CSharp
{
	public class SearchResult
	{
		public string Path { get; set; }
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
		public int Line { get; set; }
		public int Character { get; set; }
		public string Text { get; set; }
		public string Source1 { get; set; }
		public string Source2 { get; set; }
		public string Source3 { get; set; }

		public void WriteTo(TextWriter writer)
		{
			WriteLengthEncoded(this.Path);
			WriteCell(this.StartIndex);
			WriteCell(this.EndIndex);
			WriteCell(this.Line);
			WriteCell(this.Character);
			WriteLengthEncoded(this.Source1);
			WriteLengthEncoded(this.Source2);
			WriteLengthEncoded(this.Source3);
			WriteLengthEncoded(this.Text);
			writer.WriteLine();

			void WriteLengthEncoded(string value)
			{
				value = value ?? "";
				WriteCell(value.Length);
				WriteCell(value);
			}

			void WriteCell(object value)
			{
				writer.Write(value);
				writer.Write(";");
			}
		}
	}
}
