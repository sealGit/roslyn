﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    /// <summary>
    /// This class contains a variety of helper methods for determining whether a
    /// position is within the scope (and not just the span) of a node.  In general,
    /// general, the scope extends from the first token up to, but not including,
    /// the last token. For example, the open brace of a block is within the scope
    /// of the block, but the close brace is not.
    /// </summary>
    internal static class LookupPosition
    {
        /// <summary>
        /// A position is considered to be inside a block if it is on or after
        /// the open brace and strictly before the close brace.
        /// </summary>
        internal static bool IsInBlock(int position, BlockSyntax blockOpt)
        {
            return blockOpt != null && IsBeforeToken(position, blockOpt, blockOpt.CloseBraceToken);
        }

        internal static bool IsInExpressionBody(
            int position,
            ArrowExpressionClauseSyntax expressionBodyOpt,
            SyntaxToken semicolonToken)
        {
            return expressionBodyOpt != null
                && IsBeforeToken(position, expressionBodyOpt, semicolonToken);
        }

        /// <summary>
        /// A position is inside a property body only if it is inside an expression body.
        /// All block bodies for properties are part of the accessor declaration (a type
        /// of BaseMethodDeclaration), not the property declaration.
        /// </summary>
        internal static bool IsInBody(int position,
            PropertyDeclarationSyntax property)
        {
            var exprOpt = property.GetExpressionBodySyntax();
            return IsInExpressionBody(position, exprOpt, property.Semicolon);
        }

        /// <summary>
        /// A position is inside a property body only if it is inside an expression body.
        /// All block bodies for properties are part of the accessor declaration (a type
        /// of BaseMethodDeclaration), not the property declaration.
        /// </summary>
        internal static bool IsInBody(int position,
            IndexerDeclarationSyntax indexer)
        {
            var exprOpt = indexer.GetExpressionBodySyntax();
            return IsInExpressionBody(position, exprOpt, indexer.Semicolon);
        }

        /// <summary>
        /// A position is inside a body if it is inside the block or expression
        /// body. 
        ///
        /// A position is considered to be inside a block if it is on or after
        /// the open brace and strictly before the close brace. A position is
        /// considered to be inside an expression body if it is on or after
        /// the '=>' and strictly before the semicolon.
        /// </summary>
        internal static bool IsInBody(int position, BaseMethodDeclarationSyntax method)
        {
            var exprOpt = method.GetExpressionBodySyntax();

            return IsInExpressionBody(position, exprOpt, method.SemicolonToken)
                || IsInBlock(position, method.Body);
        }

        internal static bool IsBetweenTokens(int position, SyntaxToken firstIncluded, SyntaxToken firstExcluded)
        {
            return position >= firstIncluded.SpanStart && IsBeforeToken(position, firstExcluded);
        }

        /// <summary>
        /// Returns true if position is within the given node and before the first excluded token.
        /// </summary>
        private static bool IsBeforeToken(int position, CSharpSyntaxNode node, SyntaxToken firstExcluded)
        {
            return IsBeforeToken(position, firstExcluded) && position >= node.SpanStart;
        }

        private static bool IsBeforeToken(int position, SyntaxToken firstExcluded)
        {
            return firstExcluded.Kind() == SyntaxKind.None || position < firstExcluded.SpanStart;
        }

        internal static bool IsInAttributeSpecification(int position, SyntaxList<AttributeListSyntax> attributesSyntaxList)
        {
            int count = attributesSyntaxList.Count;
            if (count == 0)
            {
                return false;
            }

            var startToken = attributesSyntaxList[0].OpenBracketToken;
            var endToken = attributesSyntaxList[count - 1].CloseBracketToken;
            return IsBetweenTokens(position, startToken, endToken);
        }

        internal static bool IsInTypeParameterList(int position, TypeDeclarationSyntax typeDecl)
        {
            var typeParameterListOpt = typeDecl.TypeParameterList;
            return typeParameterListOpt != null && IsBeforeToken(position, typeParameterListOpt, typeParameterListOpt.GreaterThanToken);
        }

        internal static bool IsInParameterList(int position, BaseMethodDeclarationSyntax methodDecl)
        {
            var parameterList = methodDecl.ParameterList;
            return IsBeforeToken(position, parameterList, parameterList.CloseParenToken);
        }

        internal static bool IsInMethodDeclaration(int position, BaseMethodDeclarationSyntax methodDecl)
        {
            Debug.Assert(methodDecl != null);

            var body = methodDecl.Body;
            SyntaxToken lastToken = body == null ? methodDecl.SemicolonToken : body.CloseBraceToken;
            return IsBeforeToken(position, methodDecl, lastToken);
        }

        internal static bool IsInMethodDeclaration(int position, AccessorDeclarationSyntax accessorDecl)
        {
            Debug.Assert(accessorDecl != null);

            var body = accessorDecl.Body;
            SyntaxToken lastToken = body == null ? accessorDecl.SemicolonToken : body.CloseBraceToken;
            return IsBeforeToken(position, accessorDecl, lastToken);
        }

        internal static bool IsInDelegateDeclaration(int position, DelegateDeclarationSyntax delegateDecl)
        {
            Debug.Assert(delegateDecl != null);

            return IsBeforeToken(position, delegateDecl, delegateDecl.SemicolonToken);
        }

        internal static bool IsInTypeDeclaration(int position, BaseTypeDeclarationSyntax typeDecl)
        {
            Debug.Assert(typeDecl != null);

            return IsBeforeToken(position, typeDecl, typeDecl.CloseBraceToken);
        }

        internal static bool IsInNamespaceDeclaration(int position, NamespaceDeclarationSyntax namespaceDecl)
        {
            Debug.Assert(namespaceDecl != null);

            return IsBetweenTokens(position, namespaceDecl.NamespaceKeyword, namespaceDecl.CloseBraceToken);
        }

        internal static bool IsInConstructorParameterScope(int position, ConstructorDeclarationSyntax constructorDecl)
        {
            Debug.Assert(constructorDecl != null);

            var initializerOpt = constructorDecl.Initializer;
            var blockOpt = constructorDecl.Body;

            if (blockOpt == null)
            {
                var nextToken = (SyntaxToken)SyntaxNavigator.Instance.GetNextToken(constructorDecl, predicate: null, stepInto: null);
                return initializerOpt == null ?
                    position >= constructorDecl.ParameterList.CloseParenToken.Span.End && IsBeforeToken(position, nextToken) :
                    IsBetweenTokens(position, initializerOpt.ColonToken, nextToken);
            }

            return initializerOpt == null ?
                IsInBlock(position, blockOpt) :
                IsBetweenTokens(position, initializerOpt.ColonToken, blockOpt.CloseBraceToken);
        }

        internal static bool IsInMethodTypeParameterScope(int position, MethodDeclarationSyntax methodDecl)
        {
            Debug.Assert(methodDecl != null);
            Debug.Assert(IsInMethodDeclaration(position, methodDecl));

            if (methodDecl.TypeParameterList == null)
            {
                // no type parameters => nothing can be in their scope
                return false;
            }

            // optimization for a common case - when position is in the ReturnType, we can see type parameters
            if (methodDecl.ReturnType.FullSpan.Contains(position))
            {
                return true;
            }

            // Must be in the method, but not in an attribute on the method.
            if (IsInAttributeSpecification(position, methodDecl.AttributeLists))
            {
                return false;
            }

            var explicitInterfaceSpecifier = methodDecl.ExplicitInterfaceSpecifier;
            var firstNameToken = explicitInterfaceSpecifier == null ? methodDecl.Identifier : explicitInterfaceSpecifier.GetFirstToken();

            var typeParams = methodDecl.TypeParameterList;
            var firstPostNameToken = typeParams == null ? methodDecl.ParameterList.OpenParenToken : typeParams.LessThanToken;

            // Scope does not include method name.
            return !IsBetweenTokens(position, firstNameToken, firstPostNameToken);
        }

        /// <remarks>
        /// Used to determine whether it would be appropriate to use the binder for the statement (if any).
        /// Not used to determine whether the position is syntactically within the statement.
        /// </remarks>
        internal static bool IsInStatementScope(int position, StatementSyntax statement)
        {
            Debug.Assert(statement != null);

            if (statement.Kind() == SyntaxKind.EmptyStatement)
            {
                return false;
            }

            // CONSIDER: the check for default(SyntaxToken) could go in IsBetweenTokens,
            // but this is where it has special meaning.
            SyntaxToken firstIncludedToken = GetFirstIncludedToken(statement);
            return firstIncludedToken != default(SyntaxToken) &&
                   IsBetweenTokens(position, firstIncludedToken, GetFirstExcludedToken(statement));
        }

        /// <remarks>
        /// Used to determine whether it would be appropriate to use the binder for the statement (if any).
        /// Not used to determine whether the position is syntactically within the statement.
        /// </remarks>
        internal static bool IsInCatchBlockScope(int position, CatchClauseSyntax catchClause)
        {
            Debug.Assert(catchClause != null);

            return IsBetweenTokens(position, catchClause.Block.OpenBraceToken, catchClause.Block.CloseBraceToken);
        }

        /// <remarks>
        /// Used to determine whether it would be appropriate to use the binder for the statement (if any).
        /// Not used to determine whether the position is syntactically within the statement.
        /// </remarks>
        internal static bool IsInCatchFilterScope(int position, CatchFilterClauseSyntax filterClause)
        {
            Debug.Assert(filterClause != null);

            return IsBetweenTokens(position, filterClause.OpenParenToken, filterClause.CloseParenToken);
        }

        private static SyntaxToken GetFirstIncludedToken(StatementSyntax statement, bool inRecursiveCall = false)
        {
            Debug.Assert(statement != null);
            switch (statement.Kind())
            {
                case SyntaxKind.Block:
                    return ((BlockSyntax)statement).OpenBraceToken;
                case SyntaxKind.BreakStatement:
                    return ((BreakStatementSyntax)statement).BreakKeyword;
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                    return ((CheckedStatementSyntax)statement).Keyword;
                case SyntaxKind.ContinueStatement:
                    return ((ContinueStatementSyntax)statement).ContinueKeyword;
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.LocalDeclarationStatement:
                    return statement.GetFirstToken();
                case SyntaxKind.DoStatement:
                    return ((DoStatementSyntax)statement).DoKeyword;
                case SyntaxKind.EmptyStatement:
                    return default(SyntaxToken); //The caller will have to check for this.
                case SyntaxKind.FixedStatement:
                    return ((FixedStatementSyntax)statement).FixedKeyword;
                case SyntaxKind.ForEachStatement:
                    // NB: iteration variable is only in scope in body.
                    ForEachStatementSyntax forEachSyntax = (ForEachStatementSyntax)statement;
                    if (inRecursiveCall)
                    {
                        return forEachSyntax.ForEachKeyword;
                    }
                    return GetFirstIncludedToken(forEachSyntax.Statement, inRecursiveCall: true);
                case SyntaxKind.ForStatement:
                    // Section 8.8.3 of the spec says that the scope of the loop variable starts at 
                    // its declaration.  If it's not there, then the scope we are interested in is
                    // the loop body.
                    ForStatementSyntax forSyntax = (ForStatementSyntax)statement;
                    if (inRecursiveCall)
                    {
                        return forSyntax.ForKeyword;
                    }
                    VariableDeclarationSyntax declOpt = forSyntax.Declaration;
                    return declOpt == null ? GetFirstIncludedToken(forSyntax.Statement, inRecursiveCall: true) : declOpt.GetFirstToken();
                case SyntaxKind.GotoDefaultStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoStatement:
                    return ((GotoStatementSyntax)statement).GotoKeyword;
                case SyntaxKind.IfStatement:
                    return ((IfStatementSyntax)statement).IfKeyword;
                case SyntaxKind.LabeledStatement:
                    return ((LabeledStatementSyntax)statement).Identifier;
                case SyntaxKind.LockStatement:
                    return ((LockStatementSyntax)statement).LockKeyword;
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)statement).ReturnKeyword;
                case SyntaxKind.SwitchStatement:
                    return ((SwitchStatementSyntax)statement).OpenBraceToken;
                case SyntaxKind.ThrowStatement:
                    return ((ThrowStatementSyntax)statement).ThrowKeyword;
                case SyntaxKind.TryStatement:
                    return ((TryStatementSyntax)statement).TryKeyword;
                case SyntaxKind.UnsafeStatement:
                    return ((UnsafeStatementSyntax)statement).UnsafeKeyword;
                case SyntaxKind.UsingStatement:
                    return ((UsingStatementSyntax)statement).UsingKeyword;
                case SyntaxKind.WhileStatement:
                    return ((WhileStatementSyntax)statement).WhileKeyword;
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.YieldBreakStatement:
                    return ((YieldStatementSyntax)statement).YieldKeyword;
                default:
                    throw ExceptionUtilities.UnexpectedValue(statement.Kind());
            }
        }

        private static SyntaxToken GetFirstExcludedToken(StatementSyntax statement)
        {
            Debug.Assert(statement != null);
            switch (statement.Kind())
            {
                case SyntaxKind.Block:
                    return ((BlockSyntax)statement).CloseBraceToken;
                case SyntaxKind.BreakStatement:
                    return ((BreakStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                    return ((CheckedStatementSyntax)statement).Block.CloseBraceToken;
                case SyntaxKind.ContinueStatement:
                    return ((ContinueStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.DoStatement:
                    return ((DoStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.EmptyStatement:
                    return ((EmptyStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.ExpressionStatement:
                    return ((ExpressionStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.FixedStatement:
                    return GetFirstExcludedToken(((FixedStatementSyntax)statement).Statement);
                case SyntaxKind.ForEachStatement:
                    return GetFirstExcludedToken(((ForEachStatementSyntax)statement).Statement);
                case SyntaxKind.ForStatement:
                    return GetFirstExcludedToken(((ForStatementSyntax)statement).Statement);
                case SyntaxKind.GotoDefaultStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoStatement:
                    return ((GotoStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.IfStatement:
                    IfStatementSyntax ifStmt = (IfStatementSyntax)statement;
                    ElseClauseSyntax elseOpt = ifStmt.Else;
                    return GetFirstExcludedToken(elseOpt == null ? ifStmt.Statement : elseOpt.Statement);
                case SyntaxKind.LabeledStatement:
                    return GetFirstExcludedToken(((LabeledStatementSyntax)statement).Statement);
                case SyntaxKind.LockStatement:
                    return GetFirstExcludedToken(((LockStatementSyntax)statement).Statement);
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.SwitchStatement:
                    return ((SwitchStatementSyntax)statement).CloseBraceToken;
                case SyntaxKind.ThrowStatement:
                    return ((ThrowStatementSyntax)statement).SemicolonToken;
                case SyntaxKind.TryStatement:
                    TryStatementSyntax tryStmt = (TryStatementSyntax)statement;

                    FinallyClauseSyntax finallyClause = tryStmt.Finally;
                    if (finallyClause != null)
                    {
                        return finallyClause.Block.CloseBraceToken;
                    }

                    CatchClauseSyntax lastCatch = tryStmt.Catches.LastOrDefault();
                    if (lastCatch != null)
                    {
                        return lastCatch.Block.CloseBraceToken;
                    }
                    return tryStmt.Block.CloseBraceToken;
                case SyntaxKind.UnsafeStatement:
                    return ((UnsafeStatementSyntax)statement).Block.CloseBraceToken;
                case SyntaxKind.UsingStatement:
                    return GetFirstExcludedToken(((UsingStatementSyntax)statement).Statement);
                case SyntaxKind.WhileStatement:
                    return GetFirstExcludedToken(((WhileStatementSyntax)statement).Statement);
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.YieldBreakStatement:
                    return ((YieldStatementSyntax)statement).SemicolonToken;
                default:
                    throw ExceptionUtilities.UnexpectedValue(statement.Kind());
            }
        }

        internal static bool IsInAnonymousFunctionOrQuery(int position, CSharpSyntaxNode anonymousFunction)
        {
            SyntaxToken firstIncluded;
            CSharpSyntaxNode body;

            switch (anonymousFunction.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    SimpleLambdaExpressionSyntax simple = (SimpleLambdaExpressionSyntax)anonymousFunction;
                    firstIncluded = simple.Parameter.Identifier;
                    body = simple.Body;
                    break;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    ParenthesizedLambdaExpressionSyntax parenthesized = (ParenthesizedLambdaExpressionSyntax)anonymousFunction;
                    firstIncluded = parenthesized.ParameterList.OpenParenToken;
                    body = parenthesized.Body;
                    break;
                case SyntaxKind.AnonymousMethodExpression:
                    AnonymousMethodExpressionSyntax anon = (AnonymousMethodExpressionSyntax)anonymousFunction;
                    firstIncluded = anon.DelegateKeyword;
                    body = anon.Block;
                    break;
                default:
                    // OK, so we have some kind of query clause.  They all start with a keyword token, so we'll skip that.
                    firstIncluded = anonymousFunction.GetFirstToken().GetNextToken();
                    return IsBetweenTokens(position, firstIncluded, anonymousFunction.GetLastToken().GetNextToken());
            }

            var bodyStatement = body as StatementSyntax;
            var firstExcluded = bodyStatement != null ?
                GetFirstExcludedToken(bodyStatement) :
                (SyntaxToken)SyntaxNavigator.Instance.GetNextToken(body, predicate: null, stepInto: null);

            return IsBetweenTokens(position, firstIncluded, firstExcluded);
        }

        internal static bool IsInXmlAttributeValue(int position, XmlAttributeSyntax attribute)
        {
            return IsBetweenTokens(position, attribute.StartQuoteToken, attribute.EndQuoteToken);
        }
    }
}
