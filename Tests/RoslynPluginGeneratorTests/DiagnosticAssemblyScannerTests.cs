/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RuleGeneratorTests
{
    [TestClass]
    public class DiagnosticAssemblyScannerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void InstantiateDiags_CSharp_NoFiles()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp /* no files */);

            // Assert
            result.Should().NotBeNull("Not expecting InstantiateDiagnostics to return null");
            result.Any().Should().BeFalse("Not expecting any diagnostics to have been found");
        }

        [TestMethod]
        public void InstantiateDiags_VB_NoAnalyzers()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string corLibDllPath = typeof(object).Assembly.Location;
            string thisDllPath = GetType().Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.VisualBasic,
                corLibDllPath,
                thisDllPath);

            // Assert
            result.Should().NotBeNull("Not expecting InstantiateDiagnostics to return null");
            result.Any().Should().BeFalse("Not expecting any diagnostics to have been found");
        }

        [TestMethod]
        public void InstantiateDiags_CSharp_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger, TestContext.DeploymentDirectory);

            string RoslynAnalyzer34DllPath = typeof(RoslynAnalyzer34.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp, RoslynAnalyzer34DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.ConfigurableAnalyzer));
            Assert_AnalyzerIsPresent(result, "RoslynAnalyzer34.InternalAnalyzer");

            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer34.AbstractAnalyzer)); // not expecting abstract analyzers
            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer34.UnattributedAnalyzer)); // not expecting analyzers without attributes

            result.Count().Should().Be(3, "Expecting 3 C# analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_VB_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string RoslynAnalyzer34DllPath = typeof(RoslynAnalyzer34.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.VisualBasic, RoslynAnalyzer34DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.VBAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer34.AbstractAnalyzer)); // not expecting abstract analyzers

            result.Count().Should().Be(2, "Expecting 2 VB analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_MultipleAssemblies_AnalyzersFound()
        {
            // This test expects that we can load analyzers from multiple paths at once.
            // SFSRAP-26: We should be able to load analyzers compiled using both Roslyn 1.1 and 1.0
            // (RoslynAnalyzer34 and RoslynAnalyzer10, respectively)

            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string RoslynAnalyzer34DllPath = typeof(RoslynAnalyzer34.CSharpAnalyzer).Assembly.Location;
            string nonAnalyzerAssemblyPath = GetType().Assembly.Location;
            //string roslynAnalyzer10DllPath = typeof(RoslynAnalyzer10.ExampleAnalyzer2).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp,
                RoslynAnalyzer34DllPath,
                nonAnalyzerAssemblyPath/*,
                roslynAnalyzer10DllPath*/);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer34.ConfigurableAnalyzer));
            Assert_AnalyzerIsPresent(result, "RoslynAnalyzer34.InternalAnalyzer");

            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer34.AbstractAnalyzer)); // not expecting abstract analyzers

            //Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer10.ExampleAnalyzer2));

            result.Should().HaveCount(4, "Unexpected number of C# analyzers returned");
        }

        #region Private Methods

        private void Assert_AnalyzerIsPresent(IEnumerable<DiagnosticAnalyzer> analyzers, string fullExpectedTypeName)
        {
            analyzers.SingleOrDefault(d => d.GetType().FullName == fullExpectedTypeName).Should()
                .NotBeNull("Expected an analyzer with name: " + fullExpectedTypeName);
        }

        private void Assert_AnalyzerIsPresent(IEnumerable<DiagnosticAnalyzer> analyzers, Type expected)
        {
            Assert_AnalyzerIsPresent(analyzers, expected.FullName);
        }

        private void Assert_AnalyzerNotPresent(IEnumerable<DiagnosticAnalyzer> analyzers, Type expected)
        {
            string analyzerName = expected.FullName;
            analyzers.SingleOrDefault(d => d.GetType().FullName == analyzerName).Should()
                .BeNull("Expected no analyzers with name: " + analyzerName);
        }

        #endregion Private Methods
    }
}