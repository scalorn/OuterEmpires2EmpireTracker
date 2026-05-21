/**
 * NSwag Type Generation Script
 *
 * This script generates TypeScript interfaces from the server's C# models.
 * It requires the NSwag CLI to be installed and the .NET assemblies to be built.
 *
 * Prerequisites:
 *   dotnet tool install -g NSwag.ConsoleCore
 *
 * Usage:
 *   npx ts-node scripts/generate-types.ts
 *
 * For now, types are manually maintained in src/api/types/generated.ts.
 * Once NSwag is configured, this script will automate the generation.
 */

console.log('Type generation requires NSwag CLI.');
console.log('Install via: dotnet tool install -g NSwag.ConsoleCore');
console.log('');
console.log('Types are currently maintained manually in src/api/types/generated.ts');
console.log('To regenerate, build the .NET solution first, then run NSwag against the assemblies.');
