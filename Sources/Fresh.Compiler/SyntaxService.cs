// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fresh.Syntax;

namespace Fresh.Compiler;

public sealed class SyntaxService : ISyntaxService
{
    private readonly IInputService inputService;

    public SyntaxService(IInputService inputService)
    {
        this.inputService = inputService;
    }

    public ModuleDeclarationSyntax SyntaxTree(string file)
    {
        var sourceText = this.inputService.SourceText(file);
        var lexer = Lexer.Lex(sourceText);
        var syntaxTokens = SyntaxTokenParser.ParseSyntaxTokens(lexer);
        var syntaxTree = Parser.Parse(syntaxTokens);
        return syntaxTree;
    }
}
