// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Syntax;
using Microsoft.Extensions.Logging;

namespace Fresh.Compiler.Services;

public sealed class SyntaxService : ISyntaxService
{
    private readonly ILogger<SyntaxService> logger;
    private readonly IInputService inputService;

    public SyntaxService(
        ILogger<SyntaxService> logger,
        IInputService inputService)
    {
        this.logger = logger;
        this.inputService = inputService;
    }

    public ModuleDeclarationSyntax SyntaxTree(string file)
    {
        this.logger.LogInformation("Syntax tree requested for {File}", file);
        var sourceText = this.inputService.SourceText(file);
        var lexer = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenParser.ParseSyntaxTokens(lexer);
        var syntaxTree = Parser.Parse(syntaxTokens);
        this.logger.LogInformation("Syntax tree parsed for {File}", file);
        return syntaxTree;
    }
}
