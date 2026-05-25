#!/usr/bin/env node
/**
 * TypeScript Type Generator
 *
 * Parses C# model files from the server project and generates
 * TypeScript interfaces. No external tools required — just Node.js.
 *
 * Usage: node scripts/generate-types.js
 */

const fs = require('fs');
const path = require('path');

const SERVER_DIR = path.resolve(__dirname, '../../OE2EmpireTracker.Server/Storage');
const OUTPUT_FILE = path.resolve(__dirname, '../src/api/types/generated.ts');

// C# files to parse for models
const MODEL_FILES = [
  path.join(SERVER_DIR, 'Models.cs'),
  path.join(SERVER_DIR, 'PermissionModels.cs'),
];

// C# type → TypeScript type mapping
const TYPE_MAP = {
  'string': 'string',
  'int': 'number',
  'long': 'number',
  'float': 'number',
  'double': 'number',
  'decimal': 'number',
  'bool': 'boolean',
  'boolean': 'boolean',
  'DateTime': 'string',
  'Guid': 'string',
};

function mapCSharpType(csType) {
  // Handle nullable: string? → string | null
  const isNullable = csType.endsWith('?');
  const baseType = isNullable ? csType.slice(0, -1) : csType;

  // Handle List<T>
  const listMatch = baseType.match(/^(?:List|IList|IReadOnlyList|IEnumerable)<(.+)>$/);
  if (listMatch) {
    const inner = mapCSharpType(listMatch[1]);
    return isNullable ? `${inner}[] | null` : `${inner}[]`;
  }

  // Handle Dictionary<K,V>
  const dictMatch = baseType.match(/^(?:Dictionary|IDictionary)<(.+),\s*(.+)>$/);
  if (dictMatch) {
    const key = mapCSharpType(dictMatch[1]);
    const val = mapCSharpType(dictMatch[2]);
    return isNullable ? `Record<${key}, ${val}> | null` : `Record<${key}, ${val}>`;
  }

  // Direct mapping
  const mapped = TYPE_MAP[baseType] || baseType;
  return isNullable ? `${mapped} | null` : mapped;
}

function toCamelCase(name) {
  // Handle all-uppercase acronyms at the start (UUID → uuid, ID → id)
  if (name === name.toUpperCase()) {
    return name.toLowerCase();
  }
  // Handle leading acronyms (UUIDs → uuids, HTTPClient → httpClient)
  const match = name.match(/^([A-Z]+)([A-Z][a-z])/);
  if (match) {
    return match[1].toLowerCase() + match[2] + name.slice(match[0].length);
  }
  // Standard PascalCase → camelCase
  return name.charAt(0).toLowerCase() + name.slice(1);
}

function parseFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');

  const enums = [];
  const classes = [];

  let currentEnum = null;
  let currentClass = null;
  let braceDepth = 0;

  for (const line of lines) {
    const trimmed = line.trim();

    // Skip comments and empty lines for parsing (but track XML doc comments)
    if (trimmed.startsWith('//') || trimmed.startsWith('///') || trimmed === '') {
      continue;
    }

    // Detect enum declaration
    const enumMatch = trimmed.match(/^public\s+enum\s+(\w+)/);
    if (enumMatch) {
      currentEnum = { name: enumMatch[1], values: [] };
      currentClass = null;
      braceDepth = 0;
      continue;
    }

    // Detect class declaration
    const classMatch = trimmed.match(/^public\s+(?:sealed\s+)?class\s+(\w+)/);
    if (classMatch) {
      currentClass = { name: classMatch[1], properties: [] };
      currentEnum = null;
      braceDepth = 0;
      continue;
    }

    // Track braces
    if (trimmed === '{') {
      braceDepth++;
      continue;
    }
    if (trimmed === '}') {
      braceDepth--;
      if (braceDepth <= 0) {
        if (currentEnum) {
          enums.push(currentEnum);
          currentEnum = null;
        }
        if (currentClass) {
          classes.push(currentClass);
          currentClass = null;
        }
      }
      continue;
    }

    // Parse enum values
    if (currentEnum && braceDepth === 1) {
      const valueMatch = trimmed.match(/^(\w+),?$/);
      if (valueMatch) {
        currentEnum.values.push(valueMatch[1]);
      }
      continue;
    }

    // Parse class properties
    if (currentClass && braceDepth === 1) {
      const propMatch = trimmed.match(
        /^public\s+(.+?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}/
      );
      if (propMatch) {
        currentClass.properties.push({
          type: propMatch[1],
          name: propMatch[2],
        });
      }
    }
  }

  return { enums, classes };
}

function generateOutput(allEnums, allClasses) {
  const lines = [
    '// This file is auto-generated. Do not edit manually.',
    '// Regenerate with: npm run generate-types',
    `// Generated from: ${MODEL_FILES.map(f => path.basename(f)).join(', ')}`,
    `// Generated at: ${new Date().toISOString()}`,
    '',
  ];

  // Generate enums as string literal unions
  for (const e of allEnums) {
    lines.push(`export type ${e.name} = ${e.values.map(v => `'${v}'`).join(' | ')};`);
    lines.push('');
  }

  // Generate interfaces
  for (const cls of allClasses) {
    lines.push(`export interface ${cls.name} {`);
    for (const prop of cls.properties) {
      const tsType = mapCSharpType(prop.type);
      lines.push(`  ${toCamelCase(prop.name)}: ${tsType};`);
    }
    lines.push('}');
    lines.push('');
  }

  return lines.join('\n');
}

// Main
function main() {
  let allEnums = [];
  let allClasses = [];

  for (const file of MODEL_FILES) {
    if (!fs.existsSync(file)) {
      console.warn(`Warning: ${file} not found, skipping.`);
      continue;
    }
    const { enums, classes } = parseFile(file);
    allEnums = allEnums.concat(enums);
    allClasses = allClasses.concat(classes);
  }

  const output = generateOutput(allEnums, allClasses);

  // Ensure output directory exists
  const outDir = path.dirname(OUTPUT_FILE);
  if (!fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true });
  }

  fs.writeFileSync(OUTPUT_FILE, output, 'utf-8');

  // Append supplemental types not in model files (endpoint-local DTOs)
  const supplemental = `
// ============================================================
// Supplemental types (endpoint-local DTOs not in model files)
// ============================================================

export interface ColonyPlannerRequest {
  structures: PlannerStructure[];
  items?: Record<string, number>;
  playerSkills?: Record<string, number>;
}

export interface PlannerStructure {
  flatpackBlueprintUUID: string;
  isBuilt: boolean;
  isStaged: boolean;
  isOnline: boolean;
  buildQueueSequence: number;
  assignedWorkers?: Record<string, boolean>;
}

export interface ColonyStatusResult {
  powerProvided: number;
  powerRequired: number;
  habitationProvision: number;
  habitationRequired: number;
  foodProvision: number;
  foodRequired: number;
  entertainmentProvided: number;
  entertainmentRequired: number;
  warehouseCapacity: number;
  warehouseRequired: number;
}

export interface OptimizedOrderEntry {
  flatpackBlueprintUUID: string;
  buildQueueSequence: number;
}

export interface BuildOrderResult {
  optimizedOrder?: OptimizedOrderEntry[];
  steps: BuildOrderStep[];
  totalTimeEstimate: string;
}

export interface BuildOrderStep {
  sequence: number;
  structureName: string;
  blueprintType: string;
  resourcesRequired: ResourceRequirement[];
  timeEstimate: string;
}

export interface ResourceRequirement {
  resourceName: string;
  quantity: number;
}

export interface EligibilityResult {
  eligible: boolean;
  stagedCount: number;
  buildingCount: number;
  firstStagedStructure: PlannerStructure | null;
}
`;
  fs.appendFileSync(OUTPUT_FILE, supplemental, 'utf-8');

  console.log(`Generated ${allEnums.length} enums and ${allClasses.length} interfaces`);
  console.log(`Output: ${OUTPUT_FILE}`);
}

main();
