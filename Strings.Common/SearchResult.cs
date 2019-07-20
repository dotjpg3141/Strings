using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Strings.Common
{
	public class SearchResult
	{
		private static readonly Regex numberPattern = new Regex(@"^\d+$", RegexOptions.Compiled);

		public string Path { get; set; }
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
		public int Line { get; set; }
		public int Character { get; set; }
		public string Text { get; set; }
		public string Source1 { get; set; }
		public string Source2 { get; set; }
		public string Source3 { get; set; }

		public string Extension
		{
			get
			{
				// NOTE(jpg): Path.GetExtension returns for file "foo.d.ts" the extension ".ts"
				// but required is the extension ".d.ts"

				var name = System.IO.Path.GetFileName(this.Path);

				int index = GetStartIndex();
				var extension = index == -1 ? "" : name.Substring(index);
				return extension.ToLowerInvariant();

				int GetStartIndex()
				{
					int dotIndex1 = name.LastIndexOf('.');
					if (dotIndex1 == -1 || dotIndex1 == 0)
					{
						return dotIndex1;
					}

					int dotIndex2 = name.LastIndexOf('.', dotIndex1 - 1);
					if (dotIndex2 == -1)
					{
						return dotIndex1;
					}

					return dotIndex2;
				}
			}
		}

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

		public static SearchResult ReadFrom(TextReader reader)
		{
			var result = new SearchResult();
			result.Path = ReadLengthEncoded();
			result.StartIndex = ReadInt();
			result.EndIndex = ReadInt();
			result.Line = ReadInt();
			result.Character = ReadInt();
			result.Source1 = ReadLengthEncoded();
			result.Source2 = ReadLengthEncoded();
			result.Source3 = ReadLengthEncoded();
			result.Text = ReadLengthEncoded();

			while (reader.Peek() == '\r' || reader.Peek() == '\n')
			{
				reader.Read();
			}

			return result;

			string ReadLengthEncoded()
			{
				var length = ReadInt();
				var buffer = new char[length];
				if (reader.ReadBlock(buffer, 0, length) != length)
				{
					throw new EndOfStreamException();
				}

				int last = reader.Read();
				if (last != -1 && last != ';')
				{
					throw new InvalidOperationException($"Unexpected character '{(char)last}' expected ';'.");
				}

				return new string(buffer);
			}

			int ReadInt()
			{
				var text = new StringBuilder();
				int character;
				while ((character = reader.Read()) != -1 && character != ';')
				{
					text.Append((char)character);
				}

				return int.Parse(text.ToString());
			}
		}


		public static void WriteCsvHeader(TextWriter writer)
		{
			WriteCsvCell(writer, nameof(Path));
			WriteCsvCell(writer, nameof(Extension));
			WriteCsvCell(writer, "Length");
			WriteCsvCell(writer, nameof(Line));
			WriteCsvCell(writer, nameof(Character));
			WriteCsvCell(writer, nameof(Source1));
			WriteCsvCell(writer, nameof(Source2));
			WriteCsvCell(writer, nameof(Source3));
			WriteCsvCell(writer, nameof(Text));
			writer.WriteLine();
		}

		public void WriteCsv(TextWriter writer)
		{
			var text = this.Text?.Trim() ?? "";

			WriteCsvCell(writer, System.IO.Path.GetFullPath(this.Path));
			WriteCsvCell(writer, this.Extension);
			WriteCsvCell(writer, text.Length);
			WriteCsvCell(writer, this.Line);
			WriteCsvCell(writer, this.Character);
			WriteCsvCell(writer, this.Source1);
			WriteCsvCell(writer, this.Source2);
			WriteCsvCell(writer, this.Source3);
			WriteCsvCell(writer, text);
			writer.WriteLine();
		}

		private static void WriteCsvCell(TextWriter writer, object value)
		{
			var text = value == null ? "" : value.ToString();
			if (numberPattern.IsMatch(text))
			{
				writer.Write(text);
			}
			else
			{
				writer.Write("\"" + text.Replace("\"", "\"\"") + "\"");
			}
			writer.Write(",");
		}
	}
}
