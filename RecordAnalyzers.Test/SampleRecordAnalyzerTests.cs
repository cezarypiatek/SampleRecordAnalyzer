using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace RecordAnalyzers.Test
{
    public class SampleRecordAnalyzerTests: AnalyzerTestFixture
    {
        [Test]
        public void should_report_issue_for_record_equals_when_something()
        {
            //INFO: Use [||] to surround place where you expect the diagnostic

            var inputMarkup = @"using System;
            using System.Collections.Generic;

            record Foo(List<int> Bar);

        public class SampleClass
        {
            public void SampleMethod()
            {
                var a = new Foo(new() { 1 });
                var b = new Foo(new() { 1 });

                if ([|a == b|])
                {
                    
                }
            }
        }";

            HasDiagnostic(inputMarkup, RecordAnalyzersAnalyzer.DiagnosticId);
        }

        protected override bool ThrowsWhenInputDocumentContainsError { get; } = true;
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new RecordAnalyzersAnalyzer();
    }
}
