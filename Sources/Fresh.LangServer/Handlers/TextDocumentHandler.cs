// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fresh.Compiler.Services;
using Fresh.Syntax;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Fresh.LangServer.Handlers;

internal sealed class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private static readonly DocumentSelector documentSelector = new(
        new DocumentFilter
        {
            Pattern = "**/*.fresh"
        });

    // TODO: Make this incremental
    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    private readonly ILogger<TextDocumentHandler> logger;
    private readonly ILanguageServerFacade server;
    private readonly IInputService inputService;
    private readonly ISyntaxService syntaxService;

    public TextDocumentHandler(
        ILogger<TextDocumentHandler> logger,
        ILanguageServerFacade server,
        IInputService inputService,
        ISyntaxService syntaxService)
    {
        this.logger = logger;
        this.server = server;
        this.inputService = inputService;
        this.syntaxService = syntaxService;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "fresh");

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = documentSelector,
            Change = this.Change,
            Save = new SaveOptions()
            {
                // TODO: Probably not really needed
                IncludeText = true,
            }
        };

    public override Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Text file opened!");
        var textDocument = request.TextDocument;
        var path = textDocument.Uri.GetFileSystemPath();
        this.inputService.SetSourceText(path, SourceText.FromString(path, textDocument.Text));
        this.PublishDiagnostics(textDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Text file changed!");
        var textDocument = request.TextDocument;
        var path = textDocument.Uri.GetFileSystemPath();
        // TODO: Incremental!
        this.inputService.SetSourceText(path, SourceText.FromString(path, request.ContentChanges.First().Text));
        this.PublishDiagnostics(textDocument.Uri);
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        // TODO
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        // TODO
        return Unit.Task;
    }

    private void PublishDiagnostics(DocumentUri uri)
    {
        static OmniSharp.Extensions.LanguageServer.Protocol.Models.Position ConvertPosition(Syntax.Position position) =>
            new(line: position.Line, character: position.Column);
        static OmniSharp.Extensions.LanguageServer.Protocol.Models.Range ConvertRange(Syntax.Range range) =>
            new(start: ConvertPosition(range.Start), end: ConvertPosition(range.End));

        var path = uri.GetFileSystemPath();
        var syntaxTree = this.syntaxService.SyntaxTree(path);
        var errors = syntaxTree.CollectErrors();

        var diagnostics = new Container<Diagnostic>(errors.Select(err => new Diagnostic
        {
            Range = ConvertRange(err.Location.Range),
            Message = err.Message,
        }));

        this.server.TextDocument.PublishDiagnostics(new()
        {
            Uri = uri,
            Diagnostics = diagnostics,
        });
    }
}
