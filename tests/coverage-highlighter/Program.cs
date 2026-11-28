// dnlib: See LICENSE.txt for more info

// Classifies every C# file of a project directory with Roslyn (the classifier Visual Studio uses for its
// colors) and writes the result as JSON. See ../coverage-html.py
//
// usage: CoverageHighlighter <dnlib's src directory> <output.json>
//
// The code is compiled like dnlib's net10.0 build: the preprocessor symbols are the ones of dnlib.csproj and
// the references are the assemblies of the .NET runtime this tool runs on.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

if (args.Length != 2) {
	Console.Error.WriteLine("usage: CoverageHighlighter <dnlib's src directory> <output.json>");
	return 1;
}
var sourceDirectory = Path.GetFullPath(args[0]);
var outputFile = args[1];
const string targetFramework = "net10.0";

var symbols = GetPreprocessorSymbols(Path.Combine(sourceDirectory, "dnlib.csproj"), targetFramework);

// Only the runtime's assemblies: the list also has the assemblies of this tool (eg. Roslyn)
var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
	.Where(path => Path.GetDirectoryName(path) == runtimeDirectory)
	.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

using var workspace = new AdhocWorkspace();
var projectId = ProjectId.CreateNewId();
var solution = workspace.CurrentSolution.AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "dnlib", "dnlib", LanguageNames.CSharp,
	compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable),
	parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, preprocessorSymbols: symbols),
	metadataReferences: references));

var fileHashes = new Dictionary<DocumentId, (string name, string hash)>();
foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal)) {
	var name = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
	if (name.StartsWith("obj/", StringComparison.Ordinal) || name.StartsWith("bin/", StringComparison.Ordinal))
		continue;
	var bytes = File.ReadAllBytes(file);
	var documentId = DocumentId.CreateNewId(projectId);
	// The offsets are UTF-16 code units from the start of the text, without the byte order mark
	solution = solution.AddDocument(documentId, name, SourceText.From(bytes, bytes.Length, Encoding.UTF8), filePath: file);
	fileHashes.Add(documentId, (name, Convert.ToHexStringLower(SHA256.HashData(bytes))));
}

var project = solution.GetProject(projectId)!;
var errors = (await project.GetCompilationAsync())!.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
if (errors.Length != 0) {
	// Not fatal, the code is still classified, but names that can't be resolved have no color
	Console.Error.WriteLine($"warning: the code has {errors.Length} compiler errors, the first ones:");
	foreach (var error in errors.Take(5))
		Console.Error.WriteLine("  " + error);
}

var kinds = new ConcurrentDictionary<string, int>();
var files = new ConcurrentDictionary<string, object>();
await Parallel.ForEachAsync(project.Documents, async (document, cancellationToken) => {
	var text = await document.GetTextAsync(cancellationToken);
	var spans = await Classifier.GetClassifiedSpansAsync(document, new TextSpan(0, text.Length), cancellationToken);
	// start, length, kind of every span, in the order of the classifier
	var data = new List<int>();
	foreach (var span in spans) {
		data.Add(span.TextSpan.Start);
		data.Add(span.TextSpan.Length);
		data.Add(kinds.GetOrAdd(span.ClassificationType, static (_, all) => { lock (all) return all.Count; }, kinds));
	}
	var (name, hash) = fileHashes[document.Id];
	files[name] = new { sha256 = hash, spans = data };
});

var result = new {
	targetFramework,
	symbols,
	kinds = kinds.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray(),
	files = files.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value),
};
File.WriteAllBytes(outputFile, JsonSerializer.SerializeToUtf8Bytes(result));
Console.Error.WriteLine($"Classified {files.Count} files ({targetFramework})");
return 0;

// The symbols of #if: dnlib.csproj's own ones (eg. THREAD_SAFE) and the ones the SDK adds (eg. NETCOREAPP)
static string[] GetPreprocessorSymbols(string projectFile, string targetFramework) {
	var startInfo = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, UseShellExecute = false };
	foreach (var arg in new[] { "msbuild", projectFile, "-nologo", $"-p:TargetFramework={targetFramework}", "-t:AddImplicitDefineConstants", "-getProperty:DefineConstants" })
		startInfo.ArgumentList.Add(arg);
	using var process = Process.Start(startInfo)!;
	var output = process.StandardOutput.ReadToEnd();
	process.WaitForExit();
	var symbols = output.Split([';', '\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries);
	if (process.ExitCode != 0 || symbols.Length == 0)
		throw new InvalidOperationException($"Couldn't get the preprocessor symbols of {projectFile}: {output}");
	return symbols;
}
