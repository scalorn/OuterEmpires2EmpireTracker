#!/usr/bin/env node
/**
 * fwrite-mcp.js - MCP server wrapping fwrite.js for shell-free file operations.
 *
 * Exposes write, append, replace, and batch as MCP tools.
 * Content passes directly as JSON string parameters - no PowerShell, no heredocs,
 * no temp files, no BOM issues.
 *
 * Protocol: JSON-RPC 2.0 over stdio (MCP standard)
 */

const fs = require('fs');
const path = require('path');
const readline = require('readline');
const fwrite = require('./fwrite.js');

// ============================================================================
// MCP Protocol Implementation
// ============================================================================

const SERVER_INFO = {
    name: 'fwrite-mcp',
    version: '1.0.0'
};

const TOOLS = [
    {
        name: 'write_file',
        description: 'Write content to a file (atomic write with verification). Creates parent directories if needed. Overwrites existing files using write-to-temp + rename strategy.',
        inputSchema: {
            type: 'object',
            properties: {
                path: { type: 'string', description: 'Target file path (relative or absolute)' },
                content: { type: 'string', description: 'Content to write to the file' }
            },
            required: ['path', 'content']
        }
    },
    {
        name: 'append_file',
        description: 'Append content to a file (creates file if it does not exist). Uses lock retry on Windows.',
        inputSchema: {
            type: 'object',
            properties: {
                path: { type: 'string', description: 'Target file path (relative or absolute)' },
                content: { type: 'string', description: 'Content to append to the file' }
            },
            required: ['path', 'content']
        }
    },
    {
        name: 'replace_in_file',
        description: 'Replace a unique string in a file. Normalizes line endings for matching, restores original convention. Fails if old_text appears 0 or >1 times (idempotent if new_text already present).',
        inputSchema: {
            type: 'object',
            properties: {
                path: { type: 'string', description: 'Target file path' },
                old_text: { type: 'string', description: 'Text to find (must appear exactly once)' },
                new_text: { type: 'string', description: 'Replacement text' }
            },
            required: ['path', 'old_text', 'new_text']
        }
    },
    {
        name: 'batch_write',
        description: 'Execute multiple file operations atomically. All succeed or all are rolled back. Operations: write, append, replace.',
        inputSchema: {
            type: 'object',
            properties: {
                operations: {
                    type: 'array',
                    description: 'Array of operations to execute in sequence',
                    items: {
                        type: 'object',
                        properties: {
                            op: { type: 'string', enum: ['write', 'append', 'replace'], description: 'Operation type' },
                            path: { type: 'string', description: 'Target file path' },
                            content: { type: 'string', description: 'Content for write/append operations' },
                            old_text: { type: 'string', description: 'Text to find (replace only)' },
                            new_text: { type: 'string', description: 'Replacement text (replace only)' }
                        },
                        required: ['op', 'path']
                    }
                }
            },
            required: ['operations']
        }
    }
];

// ============================================================================
// Tool Handlers
// ============================================================================

async function handleWriteFile(args) {
    var targetPath = args.path;
    var content = args.content;
    if (!targetPath) return { error: "Missing required parameter: path" };
    if (content === undefined || content === null) return { error: "Missing required parameter: content" };

    try {
        var writeResult = await fwrite.atomicWrite(targetPath, content);
        fwrite.verifyWrite(targetPath, writeResult.bytesWritten);
        var msg = "Wrote " + content.length + " chars to " + targetPath;
        if (writeResult.retryAttempt > 0) msg += " (retry " + writeResult.retryAttempt + ")";
        return { success: true, message: msg, chars: content.length, path: targetPath };
    } catch (err) {
        return { error: fwrite.mapFsError(err), path: targetPath };
    }
}

async function handleAppendFile(args) {
    var targetPath = args.path;
    var content = args.content;
    if (!targetPath) return { error: "Missing required parameter: path" };
    if (content === undefined || content === null) return { error: "Missing required parameter: content" };

    try {
        var resolvedPath = path.resolve(targetPath);
        var existingBytes = 0;
        try { existingBytes = fs.statSync(resolvedPath).size; } catch (e) { /* new file */ }
        var appendResult = await fwrite.appendToFile(targetPath, content);
        var expectedTotal = existingBytes + appendResult.bytesWritten;
        fwrite.verifyAppend(targetPath, expectedTotal);
        var msg = "Appended " + content.length + " chars to " + targetPath;
        if (appendResult.retryAttempt > 0) msg += " (retry " + appendResult.retryAttempt + ")";
        return { success: true, message: msg, chars: content.length, path: targetPath };
    } catch (err) {
        return { error: fwrite.mapFsError(err), path: targetPath };
    }
}

async function handleReplaceInFile(args) {
    var targetPath = args.path;
    var oldText = args.old_text;
    var newText = args.new_text;
    if (!targetPath) return { error: "Missing required parameter: path" };
    if (!oldText) return { error: "Missing required parameter: old_text" };
    if (newText === undefined || newText === null) return { error: "Missing required parameter: new_text" };

    try {
        var result = await fwrite.replaceInFile(targetPath, oldText, newText);
        if (result.success) {
            if (result.idempotent) {
                return { success: true, message: "Idempotent match in " + targetPath, idempotent: true, path: targetPath };
            }
            return { success: true, message: "Replaced " + result.oldLen + " chars with " + result.newLen + " chars in " + targetPath, path: targetPath };
        } else {
            return { error: result.message, exitCode: result.exitCode, path: targetPath };
        }
    } catch (err) {
        return { error: fwrite.mapFsError(err), path: targetPath };
    }
}

async function handleBatchWrite(args) {
    var operations = args.operations;
    if (!operations || !Array.isArray(operations)) return { error: "Missing required parameter: operations (array)" };

    // Convert inline content to temp files for the batch engine
    var tempFiles = [];
    var manifest = { operations: [] };

    try {
        for (var i = 0; i < operations.length; i++) {
            var op = operations[i];
            if (op.op === "write" || op.op === "append") {
                var cf = path.resolve("_mcp_batch_" + i + "_" + Date.now() + ".tmp");
                fs.writeFileSync(cf, op.content || "", "utf8");
                tempFiles.push(cf);
                manifest.operations.push({ op: op.op, target: op.path, content_file: cf });
            } else if (op.op === "replace") {
                var of_ = path.resolve("_mcp_batch_old_" + i + "_" + Date.now() + ".tmp");
                var nf = path.resolve("_mcp_batch_new_" + i + "_" + Date.now() + ".tmp");
                fs.writeFileSync(of_, op.old_text || "", "utf8");
                fs.writeFileSync(nf, op.new_text || "", "utf8");
                tempFiles.push(of_, nf);
                manifest.operations.push({ op: "replace", target: op.path, old_file: of_, new_file: nf });
            } else {
                cleanupTempFiles(tempFiles);
                return { error: "Invalid operation type: " + op.op + " at index " + i };
            }
        }

        var result = await fwrite.executeBatch(manifest);
        cleanupTempFiles(tempFiles);

        if (result.rolledBack) {
            return { error: "Batch failed at operation " + result.failedOp + ": " + result.error + ". All " + result.completed + " prior operations rolled back.", rolledBack: true };
        }
        return { success: true, message: "Batch completed: " + result.completed + " operations", completed: result.completed };
    } catch (err) {
        cleanupTempFiles(tempFiles);
        return { error: err.message || String(err) };
    }
}

function cleanupTempFiles(files) {
    for (var i = 0; i < files.length; i++) {
        try { fs.unlinkSync(files[i]); } catch (e) { /* ignore */ }
    }
}

// ============================================================================
// MCP JSON-RPC Transport
// ============================================================================

var buffer = "";

function sendResponse(id, result) {
    var response = { jsonrpc: "2.0", id: id, result: result };
    var json = JSON.stringify(response);
    process.stdout.write("Content-Length: " + Buffer.byteLength(json) + "\r\n\r\n" + json);
}

function sendError(id, code, message) {
    var response = { jsonrpc: "2.0", id: id, error: { code: code, message: message } };
    var json = JSON.stringify(response);
    process.stdout.write("Content-Length: " + Buffer.byteLength(json) + "\r\n\r\n" + json);
}

function sendNotification(method, params) {
    var msg = { jsonrpc: "2.0", method: method, params: params };
    var json = JSON.stringify(msg);
    process.stdout.write("Content-Length: " + Buffer.byteLength(json) + "\r\n\r\n" + json);
}

async function handleMessage(msg) {
    var method = msg.method;
    var id = msg.id;
    var params = msg.params || {};

    switch (method) {
        case "initialize":
            sendResponse(id, {
                protocolVersion: "2024-11-05",
                capabilities: { tools: {} },
                serverInfo: SERVER_INFO
            });
            break;

        case "notifications/initialized":
            // Client acknowledged initialization - no response needed
            break;

        case "tools/list":
            sendResponse(id, { tools: TOOLS });
            break;

        case "tools/call":
            var toolName = params.name;
            var toolArgs = params.arguments || {};
            var toolResult;

            switch (toolName) {
                case "write_file":
                    toolResult = await handleWriteFile(toolArgs);
                    break;
                case "append_file":
                    toolResult = await handleAppendFile(toolArgs);
                    break;
                case "replace_in_file":
                    toolResult = await handleReplaceInFile(toolArgs);
                    break;
                case "batch_write":
                    toolResult = await handleBatchWrite(toolArgs);
                    break;
                default:
                    sendError(id, -32601, "Unknown tool: " + toolName);
                    return;
            }

            var isError = !!toolResult.error;
            sendResponse(id, {
                content: [{ type: "text", text: JSON.stringify(toolResult, null, 2) }],
                isError: isError
            });
            break;

        default:
            if (id !== undefined) {
                sendError(id, -32601, "Method not found: " + method);
            }
            break;
    }
}

// ============================================================================
// Message Framing (Content-Length header protocol)
// ============================================================================

let rawBuffer = Buffer.alloc(0);

process.stdin.on("data", function(chunk) {
    rawBuffer = Buffer.concat([rawBuffer, Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk)]);
    processBuffer();
});

function processBuffer() {
    while (true) {
        var headerStr = rawBuffer.toString("utf8", 0, Math.min(rawBuffer.length, 512));
        var headerEnd = headerStr.indexOf("\r\n\r\n");
        if (headerEnd === -1) return;

        var header = headerStr.slice(0, headerEnd);
        var match = header.match(/Content-Length:\s*(\d+)/i);
        if (!match) {
            rawBuffer = rawBuffer.slice(headerEnd + 4);
            continue;
        }

        var contentLength = parseInt(match[1], 10);
        var bodyStartBytes = Buffer.byteLength(headerStr.slice(0, headerEnd + 4), "utf8");
        var totalNeeded = bodyStartBytes + contentLength;

        if (rawBuffer.length < totalNeeded) return;

        var bodyBytes = rawBuffer.slice(bodyStartBytes, bodyStartBytes + contentLength);
        rawBuffer = rawBuffer.slice(totalNeeded);

        try {
            var msg = JSON.parse(bodyBytes.toString("utf8"));
            handleMessage(msg).catch(function(err) {
                if (msg.id !== undefined) {
                    sendError(msg.id, -32603, "Internal error: " + err.message);
                }
            });
        } catch (e) {
            // Invalid JSON - skip
        }
    }
}

process.stdin.resume();