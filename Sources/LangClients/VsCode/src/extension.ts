import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind,
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	// Path for the server
	let serverPath = context.asAbsolutePath(path.join('out', 'Fresh.LangServer.exe'));

	// Server options
	let serverOptions: ServerOptions = {
		command: serverPath,
		transport: TransportKind.stdio,
	};

	// Client options
	let clientOptions: LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'fresh' }],
	};

	client = new LanguageClient(
		'freshLanguageServer',
		'Fresh Language Server',
		serverOptions,
		clientOptions
	);

	// Start the client, which also starts the server
	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) return undefined;
	return client.stop();
}
