#!/usr/bin/env node
/**
 * fwrite-mcp.js - MCP server wrapping fwrite.js for shell-free file operations.
 *
 * Built with the official @modelcontextprotocol/sdk.
 * Exposes write, append, replace, and batch as MCP tools.
 * Content passes directly as JSON string parameters - no PowerShell, no heredocs,
 * no temp files, no BOM issues.
 */

const { McpServer } = require("@modelcontextprotocol/sdk/server/mcp.js");
const { StdioServerTransport } = require("@modelcontextprotocol/sdk/server/stdio.js");
const { z } = require("zod");
const fs = require("fs");
const path = require("path");
const fwrite = require("./fwrite.js");

// ============================================================================
// Server Setup
// ============================================================================

const server = new McpServer({
    name: "fwrite-mcp",
    version: "1.0.0"
});

// ============================================================================
// Tool: write_file
// ============================================================================

server.tool(
    "write_file",
    "Write content to a file (atomic write with verification). Creates parent directories if needed. Overwrites existing files using write-to-temp + rename strategy.",
    {
        path: z.string().describe("Target file path (relative or absolute)"),
        content: z.string().describe("Content to write to the file")
    },
    async ({ path: targetPath, content }) => {
        try {
            const writeResult = await fwrite.atomicWrite(targetPath, content);
            fwrite.verifyWrite(targetPath, writeResult.bytesWritten);
            let msg = "Wrote " + content.length + " chars to " + targetPath;
            if (writeResult.retryAttempt > 0) msg += " (retry " + writeResult.retryAttempt + ")";
            return { content: [{ type: "text", text: JSON.stringify({ success: true, message: msg, chars: content.length, path: targetPath }, null, 2) }] };
        } catch (err) {
            return { content: [{ type: "text", text: JSON.stringify({ error: fwrite.mapFsError(err), path: targetPath }, null, 2) }], isError: true };
        }
    }
);

// ============================================================================
// Tool: append_file
// ============================================================================

server.tool(
    "append_file",
    "Append content to a file (creates file if it does not exist). Uses lock retry on Windows.",
    {
        path: z.string().describe("Target file path (relative or absolute)"),
        content: z.string().describe("Content to append to the file")
    },
    async ({ path: targetPath, content }) => {
        try {
            const resolvedPath = path.resolve(targetPath);
            let existingBytes = 0;
            try { existingBytes = fs.statSync(resolvedPath).size; } catch (e) { /* new file */ }
            const appendResult = await fwrite.appendToFile(targetPath, content);
            const expectedTotal = existingBytes + appendResult.bytesWritten;
            fwrite.verifyAppend(targetPath, expectedTotal);
            let msg = "Appended " + content.length + " chars to " + targetPath;
            if (appendResult.retryAttempt > 0) msg += " (retry " + appendResult.retryAttempt + ")";
            return { content: [{ type: "text", text: JSON.stringify({ success: true, message: msg, chars: content.length, path: targetPath }, null, 2) }] };
        } catch (err) {
            return { content: [{ type: "text", text: JSON.stringify({ error: fwrite.mapFsError(err), path: targetPath }, null, 2) }], isError: true };
        }
    }
);

// ============================================================================
// Tool: replace_in_file
// ============================================================================

server.tool(
    "replace_in_file",
    "Replace a unique string in a file. Normalizes line endings for matching, restores original convention. Fails if old_text appears 0 or >1 times (idempotent if new_text already present).",
    {
        path: z.string().describe("Target file path"),
        old_text: z.string().describe("Text to find (must appear exactly once)"),
        new_text: z.string().describe("Replacement text")
    },
    async ({ path: targetPath, old_text: oldText, new_text: newText }) => {
        try {
            const result = await fwrite.replaceInFile(targetPath, oldText, newText);
            if (result.success) {
                if (result.idempotent) {
                    return { content: [{ type: "text", text: JSON.stringify({ success: true, message: "Idempotent match in " + targetPath, idempotent: true, path: targetPath }, null, 2) }] };
                }
                return { content: [{ type: "text", text: JSON.stringify({ success: true, message: "Replaced " + result.oldLen + " chars with " + result.newLen + " chars in " + targetPath, path: targetPath }, null, 2) }] };
            } else {
                return { content: [{ type: "text", text: JSON.stringify({ error: result.message, exitCode: result.exitCode, path: targetPath }, null, 2) }], isError: true };
            }
        } catch (err) {
            return { content: [{ type: "text", text: JSON.stringify({ error: fwrite.mapFsError(err), path: targetPath }, null, 2) }], isError: true };
        }
    }
);

// ============================================================================
// Tool: batch_write
// ============================================================================

server.tool(
    "batch_write",
    "Execute multiple file operations atomically. All succeed or all are rolled back. Operations: write, append, replace.",
    {
        operations: z.array(z.object({
            op: z.enum(["write", "append", "replace"]).describe("Operation type"),
            path: z.string().describe("Target file path"),
            content: z.string().optional().describe("Content for write/append operations"),
            old_text: z.string().optional().describe("Text to find (replace only)"),
            new_text: z.string().optional().describe("Replacement text (replace only)")
        })).describe("Array of operations to execute in sequence")
    },
    async ({ operations }) => {
        const tempFiles = [];
        const manifest = { operations: [] };

        try {
            for (let i = 0; i < operations.length; i++) {
                const op = operations[i];
                if (op.op === "write" || op.op === "append") {
                    const cf = path.resolve("_mcp_batch_" + i + "_" + Date.now() + ".tmp");
                    fs.writeFileSync(cf, op.content || "", "utf8");
                    tempFiles.push(cf);
                    manifest.operations.push({ op: op.op, target: op.path, content_file: cf });
                } else if (op.op === "replace") {
                    const of_ = path.resolve("_mcp_batch_old_" + i + "_" + Date.now() + ".tmp");
                    const nf = path.resolve("_mcp_batch_new_" + i + "_" + Date.now() + ".tmp");
                    fs.writeFileSync(of_, op.old_text || "", "utf8");
                    fs.writeFileSync(nf, op.new_text || "", "utf8");
                    tempFiles.push(of_, nf);
                    manifest.operations.push({ op: "replace", target: op.path, old_file: of_, new_file: nf });
                }
            }

            const result = await fwrite.executeBatch(manifest);
            cleanupTempFiles(tempFiles);

            if (result.rolledBack) {
                return { content: [{ type: "text", text: JSON.stringify({ error: "Batch failed at operation " + result.failedOp + ": " + result.error + ". All " + result.completed + " prior operations rolled back.", rolledBack: true }, null, 2) }], isError: true };
            }
            return { content: [{ type: "text", text: JSON.stringify({ success: true, message: "Batch completed: " + result.completed + " operations", completed: result.completed }, null, 2) }] };
        } catch (err) {
            cleanupTempFiles(tempFiles);
            return { content: [{ type: "text", text: JSON.stringify({ error: err.message || String(err) }, null, 2) }], isError: true };
        }
    }
);

function cleanupTempFiles(files) {
    for (let i = 0; i < files.length; i++) {
        try { fs.unlinkSync(files[i]); } catch (e) { /* ignore */ }
    }
}

// ============================================================================
// Start Server
// ============================================================================

async function main() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
}

main().catch(console.error);
