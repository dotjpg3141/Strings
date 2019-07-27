import * as ts from "typescript";
import * as fs from "fs";

async function main(args: string[]) {

	if (args.length !== 2) {
		console.error("Expected exactly two arguments: <input-file> <output-file>");
		process.exit(1);
	}

	const fileOptions = { encoding: "utf8" };

	const [inputListPath, outputPath] = args;

	const inputFiles = fs.readFileSync(inputListPath, fileOptions).trim().split(/[\r\n]+/g)

	const stream = fs.createWriteStream(outputPath, fileOptions);
	for (const inputPath of inputFiles) {
		let sourceText = fs.readFileSync(inputPath, fileOptions);

		// strip BOM
		if (sourceText[0] === "\uFEFF") {
			sourceText = sourceText.substr(1);
		}

		const source = ts.createSourceFile(inputPath, sourceText, ts.ScriptTarget.Latest, true);
		const searchResult = extractStringLiterals(source);
		const text = searchResult.map((item) => item.toString()).join("\r\n");
		await new Promise((resolve) => stream.write(text, resolve));
	}
	stream.close();
}

function extractStringLiterals(source: ts.SourceFile) {
	const result: SearchResult[] = [];
	visitNode(source);
	return result;

	function visitNode(node: ts.Node) {
		switch (node.kind) {
			case ts.SyntaxKind.StringLiteral:
			case ts.SyntaxKind.TemplateExpression:
			case ts.SyntaxKind.FirstTemplateToken:
				report(node);
				break;
		}

		ts.forEachChild(node, visitNode);
	}

	function report(node: ts.Node) {
		const item = new SearchResult();
		item.path = source.fileName;
		item.startIndex = node.getFullStart();
		item.endIndex = item.startIndex + node.getFullWidth();
		const { line, character } = source.getLineAndCharacterOfPosition(item.startIndex);
		item.line = line;
		item.character = character;
		item.text = node.getFullText();
		item.source1 = "typescript";
		item.source2 = ts.SyntaxKind[node.kind];
		item.source3 = "";
		result.push(item);
	}
}

class SearchResult {

	public path: string;
	public startIndex: number;
	public endIndex: number;
	public line: number;
	public character: number;
	public text: string;
	public source1: string;
	public source2: string;
	public source3: string;

	public toString() {

		let result = "";
		writeLengthEncoded(this.path);
		writeCell(this.startIndex);
		writeCell(this.endIndex);
		writeCell(this.line);
		writeCell(this.character);
		writeLengthEncoded(this.source1);
		writeLengthEncoded(this.source2);
		writeLengthEncoded(this.source3);
		writeLengthEncoded(this.text);
		return result;

		function writeLengthEncoded(value: string) {
			value = value || "";
			writeCell(value.length);
			writeCell(value);
		}

		function writeCell(value: any) {
			result += value + ";";
		}
	}
}

main(process.argv.slice(2)).catch((err) => {
	console.error(err);
	process.exit(4);
});
