import * as ts from "typescript";

let sourceFile = ts.createSourceFile("hello.ts", `
console.log("Hello World");
console.log('Hello World');
console.log(\`asd\${11}\`);
`, ts.ScriptTarget.Latest, true);

// https://github.com/Microsoft/TypeScript/wiki/Using-the-Compiler-API#traversing-the-ast-with-a-little-linter

let stringNodes = [];
visitNode(sourceFile);
while (true) { }

function visitNode(node: ts.Node) {
	switch (node.kind) {
		case ts.SyntaxKind.StringLiteral:
			report(node, "string literal");
			break;

		case ts.SyntaxKind.TemplateExpression:
			report(node, "string template");
	}

	ts.forEachChild(node, visitNode);
}

function report(node: ts.Node, message: string) {
	const { line, character } = sourceFile.getLineAndCharacterOfPosition(node.getStart());
	console.log(`${sourceFile.fileName} (${line + 1},${character + 1}): ${message} | ` + ts.SyntaxKind[node.kind] + " | " + node.getFullText());
	stringNodes.push(node.getFullText());
}
