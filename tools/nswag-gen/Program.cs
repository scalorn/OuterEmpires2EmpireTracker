using System;
using System.IO;
using NSwag;
using NSwag.CodeGeneration.CSharp;

string repoRoot = args.Length > 0 ? args[0] : @"D:\projects\OuterEmpires2\OE2EmpireTracker";
string swaggerPath = Path.Combine(repoRoot, "docs", "game-api-swagger.json");
string outputPath = Path.Combine(repoRoot, "OE2EmpireTracker.Common", "Client", "Generated", "GameApiGeneratedClient.cs");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var doc = await OpenApiDocument.FromFileAsync(swaggerPath);
var settings = new CSharpClientGeneratorSettings
{
    ClassName = "GameApiGeneratedClient",
    CSharpGeneratorSettings =
    {
        Namespace = "OE2EmpireTracker.Common.Client.Generated",
        JsonLibrary = NJsonSchema.CodeGeneration.CSharp.CSharpJsonLibrary.SystemTextJson,
    },
    UseBaseUrl = false,
    GenerateClientInterfaces = true,
    GenerateDtoTypes = true,
    OperationNameGenerator = new NSwag.CodeGeneration.OperationNameGenerators.SingleClientFromOperationIdOperationNameGenerator(),
};
var generator = new CSharpClientGenerator(doc, settings);
var code = generator.GenerateFile();
File.WriteAllText(outputPath, code);
Console.WriteLine($"Generated {code.Length} chars to {outputPath}");
