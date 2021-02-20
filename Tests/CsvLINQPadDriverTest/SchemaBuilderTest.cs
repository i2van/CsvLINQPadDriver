﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using CsvLINQPadDriver;

using FluentAssertions;

using LINQPad.Extensibility.DataContext;

using NUnit.Framework;

namespace CsvLINQPadDriverTest
{
    [TestFixture]
    public class SchemaBuilderTest
    {
        [Test]
        [TestCaseSource(nameof(CsvDataContextDriverProperties))]
        public void GetSchemaAndBuildAssembly_CreatedAssembly_AsExpected((ICsvDataContextDriverProperties, string) testData)
        {
            var (properties, id) = testData;

            File.WriteAllText("TestA.csv",
@"a,b,c,TestAID
1,2,3,1
1,2,3,2
x");
            File.WriteAllText("TestB.csv",
@"c,d,e,TestAID
1,2,3,1
1,2,3,2
x");

            var nameSpace = "TestContextNamespace";
            var contextTypeName = "TestContextClass";
            var contextAssemblyName = new AssemblyName($"TestContextAssembly{id}")
            {
                CodeBase = $"TestContextAssembly{id}.dll"
            };

            if (File.Exists(contextAssemblyName.CodeBase))
            {
                File.Delete(contextAssemblyName.CodeBase);
            }

            var explorerItems = SchemaBuilder.GetSchemaAndBuildAssembly(
                properties,
                contextAssemblyName,
                ref nameSpace,
                ref contextTypeName
            ).ToList();

            // Debug info to console.
            explorerItems.ForEach(item => Console.WriteLine(item.DragText));

            // Check returned explorer tree.
            explorerItems.Should().HaveCount(3);

            explorerItems = explorerItems.Where(i => i.Kind == ExplorerItemKind.QueryableObject).ToList();

            explorerItems.Select(i => i.DragText)
                .Should()
                .BeEquivalentTo(Split("TestA,TestB"));

            explorerItems.SelectMany(i => i.Children.Select(c => c.DragText))
                .Should()
                .BeEquivalentTo(Split("a,b,c,TestAID,TestB,c,d,e,TestAID,TestA"));

            // Check compiled assembly.
            var contextAssembly = Assembly.Load(contextAssemblyName);
            contextAssembly.GetExportedTypes().Select(type => type.Name)
                .Should()
                .BeEquivalentTo(Split("CsvDataContext,TTestA,TTestB"));

            var contextType = contextAssembly.GetType($"{nameSpace}.{contextTypeName}");
            contextType.Should().NotBeNull("ContextType in assembly");

            // Check generated context runtime.
            var contextInstance = contextType!.GetConstructor(new Type[] {})!
                .Should()
                .NotBeNull()
                .And
                .Subject.Invoke(new object[] {});

            contextInstance.Should().NotBeNull("context created");

            dynamic dataFirst = Enumerable.ToArray(((dynamic)contextInstance).TestA)[0];

            ((string)dataFirst.c).Should().Be("3");
            ((IEnumerable)dataFirst.TestB).Should().HaveCount(1);

            static IEnumerable<string> Split(string str) =>
                str.Split(",");
        }

        private static IEnumerable<(ICsvDataContextDriverProperties, string)> CsvDataContextDriverProperties()
        {
            var files = Path.Combine(Directory.GetCurrentDirectory(), "*.csv");
            var parsedFiles = new[] { files };

            yield return (new PropertiesMock
            {
                Files = files,
                ParsedFiles = parsedFiles,
                DebugInfo = true,
                DetectRelations = true,
                IgnoreInvalidFiles = true,
                IsCacheEnabled = true,
                HideRelationsFromDump = true,
                Persist = true
            }, "1");

            yield return (new PropertiesMock
            {
                Files = files,
                ParsedFiles = parsedFiles,
                DebugInfo = true,
                DetectRelations = true,
                IgnoreInvalidFiles = false,
                IsCacheEnabled = false,
                HideRelationsFromDump = false,
                Persist = false
            }, "2");
        }

        private class PropertiesMock : ICsvDataContextDriverProperties
        {
            public bool Persist { get; set; }
            public string Files { get; set; }
            public string[] ParsedFiles { get; set; }
            public string CsvSeparator { get; set; }
            public char? CsvSeparatorChar { get; } = null;
            public bool DetectRelations { get; set; }
            public bool HideRelationsFromDump { get; set; }
            public bool DebugInfo { get; set; }
            public bool IgnoreInvalidFiles { get; set; }
            public bool IsStringInternEnabled { get; set; }
            public bool IsCacheEnabled { get; set; }
        }
    }
}
