using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerator;
using Xunit;
using Xunit.Abstractions;

namespace SourceGeneratorTests
{
    public class Tests
    {
		private readonly ITestOutputHelper _output;

		public Tests(ITestOutputHelper output)
		{
            _output = output;
		}

        [Fact]
        public void Test1()
        {
            string source = @"
namespace Foo
{
    class C
    {
        void M()
        {
        }
    }
}";
            string output = GetGeneratedOutput(source);

            Assert.NotNull(output);

            Assert.Equal("class Foo { }", output);
        }

        private string GetGeneratedOutput(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            var compilation = CSharpCompilation.Create("foo", new SyntaxTree[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics();
            Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + diagnostics.FirstOrDefault()?.GetMessage());

            ISourceGenerator generator = new Generator();

            var driver = new CSharpGeneratorDriver(compilation.SyntaxTrees[0].Options, ImmutableArray.Create(generator), default, ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out var outputCompilation, out diagnostics);
            Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + diagnostics.FirstOrDefault()?.GetMessage());

            string output = outputCompilation.SyntaxTrees.Last().ToString();

            _output.WriteLine(output);

            return output;
        }
    }
}
