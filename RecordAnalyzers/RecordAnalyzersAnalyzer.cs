using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RecordAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RecordAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RA001";
        private const string Category = "Language Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor
                                                            (
                                                                id: DiagnosticId,
                                                                title: "Invalid record usage",
                                                                messageFormat: "Do not use {0} in equals because....",
                                                                category: Category,
                                                                defaultSeverity: DiagnosticSeverity.Error,
                                                                isEnabledByDefault: true,
                                                                description: "Here comes the rule explanation"
                                                            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzerEquals, SyntaxKind.EqualsExpression);
        }

        private void AnalyzerEquals(SyntaxNodeAnalysisContext context)
        {

            if (context.Node is BinaryExpressionSyntax { Left: { } left, Right: { } right })
            {
                TryToReportDiagnostic(context, left);
                TryToReportDiagnostic(context, right);
            }
        }

        private void TryToReportDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax equalsSide)
        {
            if (context.SemanticModel.GetTypeInfo(equalsSide) is { Type: INamedTypeSymbol { IsRecord: true } recordSymbol })
            {
                foreach (var propertySymbol in GetAllProperties(recordSymbol))
                {
                    if (IsAllowedTypeForRecordProperty(propertySymbol.Type) == false)
                    {
                        ReportDiagnostic(context, recordSymbol, equalsSide);
                        break;
                    }
                }
            }
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, INamedTypeSymbol recordSymbol, ExpressionSyntax expressionSyntax)
        {
            var diagnostic = Diagnostic.Create(Rule, expressionSyntax.GetLocation(), recordSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private bool IsAllowedTypeForRecordProperty(ITypeSymbol propertyType)
        {
            if (propertyType?.Name == "List" && propertyType.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
            {
                return false;
            }

            return true;
        }

        private IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol recordSymbol)
        {
            return GetBaseTypesAndThis(recordSymbol).SelectMany(x=>x.GetMembers().OfType<IPropertySymbol>());
        }


        private IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            foreach (var unwrapped in UnwrapGeneric(type))
            {
                var current = unwrapped;
                while (current != null && IsSystemObject(current) == false)
                {
                    yield return current;
                    current = current.BaseType;
                }
            }
        }

        private static IEnumerable<ITypeSymbol> UnwrapGeneric(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.TypeParameter && typeSymbol is ITypeParameterSymbol namedType && namedType.Kind != SymbolKind.ErrorType)
            {
                return namedType.ConstraintTypes;
            }
            return new[] { typeSymbol };
        }

        private static bool IsSystemObject(ITypeSymbol current)
        {
            return current.Name == "Object" && current.ContainingNamespace.Name == "System";
        }
    }
}
