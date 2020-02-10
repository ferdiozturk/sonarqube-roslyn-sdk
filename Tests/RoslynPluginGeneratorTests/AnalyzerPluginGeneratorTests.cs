﻿/*
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Test.Common;
using System;
using System.IO;
using static SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests.RemoteRepoBuilder;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    /// <summary>
    /// Tests for NuGetPackageHandler.cs
    /// </summary>
    [TestClass]
    public class AnalyzerPluginGeneratorTests
    {
        public TestContext TestContext { get; set; }

        private enum Node { Root, Child1, Child2, Grandchild1_1, Grandchild2_1, Grandchild2_2 };

        [TestMethod]
        public void Generate_NoAnalyzersFoundInPackage_GenerateFails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");

            TestLogger logger = new TestLogger();

            // Create a fake remote repo containing a package that does not contain analyzers
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);
            remoteRepoBuilder.CreatePackage("no.analyzers.id", "0.9", TestUtils.CreateTextFile("dummy.txt", outputDir), License.NotRequired /* no dependencies */ );

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = CreateArgs("no.analyzers.id", "0.9", "cs", false, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeFalse("Expecting generation to fail");
            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "no.analyzers.id"));
            logger.AssertSingleWarningExists(UIResources.APG_NoAnalyzersInTargetSuggestRecurse);
            logger.AssertWarningsLogged(2);
            AssertRuleTemplateDoesNotExist(outputDir);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceNotRequired_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Multi-level dependencies: no package requires license acceptence
            IPackage grandchild = CreatePackageWithAnalyzer(remoteRepoBuilder, "grandchild.id", "1.2", License.NotRequired /* no dependencies */);
            IPackage child = CreatePackageWithAnalyzer(remoteRepoBuilder, "child.id", "1.1", License.NotRequired, grandchild);
            CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.id", "1.0", License.NotRequired, child);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. Acceptance not required -> succeeds if accept = false
            ProcessedArgs args = CreateArgs("parent.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if there are no licenses to accept");

            logger.AssertErrorsLogged(0);
            logger.AssertWarningNotLogged("parent.id"); // not expecting warnings about packages that don't require acceptance
            logger.AssertWarningNotLogged("child.id");
            logger.AssertWarningNotLogged("grandchild.id");

            // 2. Acceptance not required -> succeeds if accept = true
            args = CreateArgs("parent.id", "1.0", "cs", true /* accept licenses */, false, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if there are no licenses to accept");

            logger.AssertErrorsLogged(0);
            logger.AssertWarningNotLogged("parent.id"); // not expecting warnings about packages that don't require acceptance
            logger.AssertWarningNotLogged("child.id");
            logger.AssertWarningNotLogged("grandchild.id");
            AssertJarsGenerated(outputDir, 1);
        }

        [TestMethod]
        public void Generate_Recursive_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Multi-level dependencies: no package requires license acceptence
            IPackage grandchild = CreatePackageWithAnalyzer(remoteRepoBuilder, "grandchild.id", "1.2", License.NotRequired /* no dependencies */);
            IPackage child = CreatePackageWithAnalyzer(remoteRepoBuilder, "child.id", "1.1", License.NotRequired, grandchild);
            CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.id", "1.0", License.NotRequired, child);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // Act
            ProcessedArgs args = CreateArgs("parent.id", "1.0", "cs", false, true /* /recurse = true */, outputDir);
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeTrue("Generator should succeed if there are no licenses to accept");
            logger.AssertWarningNotLogged("parent.id"); // not expecting warnings about packages that don't require acceptance
            logger.AssertWarningNotLogged("child.id");
            logger.AssertWarningNotLogged("grandchild.id");
            logger.AssertErrorsLogged(0);
            AssertJarsGenerated(outputDir, 3);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceRequiredByMainPackage()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Parent and child: only parent requires license
            IPackage child = CreatePackageWithAnalyzer(remoteRepoBuilder, "child.id", "1.1", License.NotRequired);
            CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.requiredAccept.id", "1.0", License.Required, child);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. User does not accept -> fails with error
            ProcessedArgs args = CreateArgs("parent.requiredAccept.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Generator should fail because license has not been accepted");

            logger.AssertSingleErrorExists("parent.requiredAccept.id", "1.0"); // error listing the main package
            logger.AssertSingleWarningExists("parent.requiredAccept.id", "1.0"); // warning for each licensed package
            logger.AssertWarningsLogged(1);

            // 2. User accepts -> succeeds with warnings
            logger.Reset();
            args = CreateArgs("parent.requiredAccept.id", "1.0", "cs", true /* accept licenses */ ,
                false /* generate plugins for dependencies */, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if licenses are accepted");

            logger.AssertSingleWarningExists(UIResources.APG_NGAcceptedPackageLicenses); // warning that licenses accepted
            logger.AssertSingleWarningExists("parent.requiredAccept.id", "1.0"); // warning for each licensed package
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceRequiredByDependency()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Parent and child: only child requires license
            IPackage child = CreatePackageWithAnalyzer(remoteRepoBuilder, "child.requiredAccept.id", "2.0", License.Required);
            CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.id", "1.0", License.NotRequired, child);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. User does not accept -> fails with error
            ProcessedArgs args = CreateArgs("parent.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Generator should fail because license has not been accepted");

            logger.AssertSingleErrorExists("parent.id", "1.0"); // error listing the main package
            logger.AssertSingleWarningExists("child.requiredAccept.id", "2.0"); // warning for each licensed package
            logger.AssertWarningsLogged(1);

            // 2. User accepts -> succeeds with warnings
            logger.Reset();
            args = CreateArgs("parent.id", "1.0", "cs", true /* accept licenses */ ,
                false, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if licenses are accepted");

            logger.AssertSingleWarningExists(UIResources.APG_NGAcceptedPackageLicenses); // warning that licenses have been accepted
            logger.AssertSingleWarningExists("child.requiredAccept.id", "2.0"); // warning for each licensed package
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceRequired_ByParentAndDependencies()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Multi-level: parent and some but not all dependencies require license acceptance
            IPackage grandchild1 = CreatePackageWithAnalyzer(remoteRepoBuilder, "grandchild1.requiredAccept.id", "3.0", License.Required);
            IPackage child1 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child1.requiredAccept.id", "2.1", License.Required);
            IPackage child2 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child2.id", "2.2", License.NotRequired, grandchild1);
            CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.requiredAccept.id", "1.0", License.Required, child1, child2);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. a) Only target package. User does not accept -> fails with error
            ProcessedArgs args = CreateArgs("parent.requiredAccept.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Generator should fail because license has not been accepted");

            logger.AssertSingleErrorExists("parent.requiredAccept.id", "1.0"); // error referring to the main package

            logger.AssertSingleWarningExists("grandchild1.requiredAccept.id", "3.0"); // warning for each licensed package
            logger.AssertSingleWarningExists("child1.requiredAccept.id", "2.1");
            logger.AssertSingleWarningExists("parent.requiredAccept.id", "1.0");

            // 2. User accepts -> succeeds with warnings
            logger.Reset();
            args = CreateArgs("parent.requiredAccept.id", "1.0", "cs", true /* accept licenses */ ,
                false, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if licenses are accepted");

            logger.AssertSingleWarningExists(UIResources.APG_NGAcceptedPackageLicenses); // warning that licenses have been accepted
            logger.AssertSingleWarningExists("grandchild1.requiredAccept.id", "3.0"); // warning for each licensed package
            logger.AssertSingleWarningExists("child1.requiredAccept.id", "2.1");
            logger.AssertSingleWarningExists("parent.requiredAccept.id", "1.0");
            logger.AssertWarningsLogged(4);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceNotRequestedIfNoAnalysers()
        {
            // No point in asking the user to accept licenses for packages that don't contain analyzers

            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Parent only: requires license
            remoteRepoBuilder.CreatePackage("non-analyzer.requireAccept.id", "1.0", dummyContentFile, License.Required);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. User does not accept, but no analyzers so no license prompt -> fails due absence of analyzers
            ProcessedArgs args = CreateArgs("non-analyzer.requireAccept.id", "1.0", "cs",  false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Expecting generator to fail");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "non-analyzer.requireAccept.id"));
            logger.AssertSingleWarningExists(UIResources.APG_NoAnalyzersInTargetSuggestRecurse);
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceNotRequired_NoAnalyzersInTarget()
        {
            // If there are:
            // No required licenses
            // No analyzers in the targeted package, but analyzers in the dependencies
            // We should fail due to the absence of analyzers if we are only generating a plugin for the targeted package
            // We should succeed if we are generating plugins for the targeted package and its dependencies

            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Multi-level dependencies: no package requires license acceptence
            // Parent has no analyzers, but dependencies do
            IPackage grandchild = CreatePackageWithAnalyzer(remoteRepoBuilder, "grandchild.id", "1.2", License.NotRequired /* no dependencies */);
            IPackage child = CreatePackageWithAnalyzer(remoteRepoBuilder, "child.id", "1.1", License.NotRequired, grandchild);
            remoteRepoBuilder.CreatePackage("parent.id", "1.0", dummyContentFile, License.NotRequired, child);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. a) Only target package. Acceptance not required -> fails due to absence of analyzers
            ProcessedArgs args = CreateArgs("parent.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Expecting generator to fail");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "parent.id"));
            logger.AssertSingleWarningExists(UIResources.APG_NoAnalyzersInTargetSuggestRecurse);
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);

            // 1. b) Target package and dependencies. Acceptance not required -> succeeds if generate dependencies = true
            logger.Reset();
            args = CreateArgs("parent.id", "1.0", "cs", false /* accept licenses */ ,
                true /* generate plugins for dependencies */, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if there are no licenses to accept");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "parent.id"));
            logger.AssertWarningNotLogged("child.id");
            logger.AssertWarningNotLogged("grandchild.id");
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        public void Generate_LicenseAcceptanceRequired_NoAnalysersInTarget()
        {
            // If there are:
            // Required licenses
            // No analyzers in the targeted package, but analyzers in the dependencies
            // We should fail due to the absence of analyzers if we are only generating a plugin for the targeted package
            // We should fail with an error due to licenses if we are generating plugins for the targeted package and dependencies

            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            // Multi-level: parent and some but not all dependencies require license acceptance
            // Parent has no analyzers, but dependencies do
            IPackage child1 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child1.requiredAccept.id", "2.1", License.Required);
            IPackage child2 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child2.id", "2.2", License.NotRequired);
            remoteRepoBuilder.CreatePackage("non-analyzer.parent.requireAccept.id", "1.0", dummyContentFile, License.Required, child1, child2);

            TestLogger logger = new TestLogger();
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, logger);

            // 1. a) Only target package. User does not accept, but no analyzers so no license prompt -> fails due to absence of analyzers
            ProcessedArgs args = CreateArgs("non-analyzer.parent.requireAccept.id", "1.0", "cs", false /* accept licenses */ ,
                false, outputDir);
            bool result = apg.Generate(args);
            result.Should().BeFalse("Expecting generator to fail");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "non-analyzer.parent.requireAccept.id"));
            logger.AssertSingleWarningExists(UIResources.APG_NoAnalyzersInTargetSuggestRecurse);
            logger.AssertWarningsLogged(2);
            logger.AssertErrorsLogged(0);

            // 1. b) Target package and dependencies. User does not accept.
            // No analyzers in the target package, but analyzers in the dependencies -> fails with error
            logger.Reset();
            args = CreateArgs("non-analyzer.parent.requireAccept.id", "1.0", "cs", false /* accept licenses */ ,
                true /* generate plugins for dependencies */, outputDir);
            result = apg.Generate(args);
            result.Should().BeFalse("Generator should fail because license has not been accepted");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "non-analyzer.parent.requireAccept.id"));
            logger.AssertSingleWarningExists("non-analyzer.parent.requireAccept.id", "1.0"); // warning for each licensed package
            logger.AssertSingleWarningExists(child1.Id, child1.Version.ToString());
            logger.AssertWarningsLogged(3);
            logger.AssertSingleErrorExists("non-analyzer.parent.requireAccept.id", "1.0"); // error listing the main package
            logger.AssertErrorsLogged(1);

            // 2. b) Target package and dependencies. User accepts.
            // No analyzers in the target package, but analyzers in the dependencies -> succeeds with warnings
            logger.Reset();
            args = CreateArgs("non-analyzer.parent.requireAccept.id", "1.0", "cs", true /* accept licenses */ ,
                true /* generate plugins for dependencies */, outputDir);
            result = apg.Generate(args);
            result.Should().BeTrue("Generator should succeed if licenses are accepted");

            logger.AssertSingleWarningExists(String.Format(UIResources.APG_NoAnalyzersFound, "non-analyzer.parent.requireAccept.id"));
            logger.AssertSingleWarningExists(UIResources.APG_NGAcceptedPackageLicenses); // warning that licenses have been accepted
            logger.AssertSingleWarningExists("non-analyzer.parent.requireAccept.id", "1.0"); // warning for each licensed package
            logger.AssertSingleWarningExists(child1.Id, child1.Version.ToString());
            logger.AssertWarningsLogged(5);
            logger.AssertErrorsLogged(0);
        }
        
        [TestMethod]
        public void Generate_RulesFileNotSpecified_TemplateFileCreated()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            var logger = new TestLogger();
            var remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            var child1 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child1.requiredAccept.id", "2.1", License.NotRequired);
            var child2 = CreatePackageWithAnalyzer(remoteRepoBuilder, "child2.id", "2.2", License.NotRequired);
            var parent = CreatePackageWithAnalyzer(remoteRepoBuilder, "parent.id", "1.0", License.NotRequired, child1, child2);

            var nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);

            var testSubject = new AnalyzerPluginGenerator(nuGetHandler, logger);

            // 1. Generate a plugin for the target package only. Expecting a plugin and a template rule file.
            var args = new ProcessedArgsBuilder("parent.id", outputDir)
                .SetLanguage("cs")
                .SetPackageVersion("1.0")
                .SetRecurseDependencies(true)
                .Build();
            bool result = testSubject.Generate(args);
            result.Should().BeTrue();

            AssertRuleTemplateFileExistsForPackage(logger, outputDir, parent);

            // 2. Generate a plugin for target package and all dependencies. Expecting three plugins and associated rule files.
            logger.Reset();
            args = CreateArgs("parent.id", "1.0", "cs", false, true /* /recurse = true */, outputDir);
            result = testSubject.Generate(args);
            result.Should().BeTrue();

            logger.AssertSingleWarningExists(UIResources.APG_RecurseEnabled_RuleCustomizationNotEnabled);
            AssertRuleTemplateFileExistsForPackage(logger, outputDir, parent);
            AssertRuleTemplateFileExistsForPackage(logger, outputDir, child1);
            AssertRuleTemplateFileExistsForPackage(logger, outputDir, child2);
        }

        [TestMethod]
        public void Generate_ValidRuleFileSpecified_TemplateFileNotCreated()
        {
            // Arrange
            var outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            var remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            var apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            CreatePackageInFakeRemoteRepo(remoteRepoBuilder, "dummy.id", "1.1");

            // Create a dummy rule file
            var dummyRuleFilePath = Path.Combine(outputDir, "inputRule.xml");
            Serializer.SaveModel(new Rules(), dummyRuleFilePath);

            var args = new ProcessedArgsBuilder("dummy.id", outputDir)
                .SetLanguage("cs")
                .SetPackageVersion("1.1")
                .SetRuleFilePath(dummyRuleFilePath)
                .Build();

            // Act
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeTrue();
            AssertRuleTemplateDoesNotExist(outputDir);
        }

        [TestMethod]
        public void Generate_InvalidRuleFileSpecified_GeneratorError()
        {
            // Arrange
            var outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            var logger = new TestLogger();

            var remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            CreatePackageInFakeRemoteRepo(remoteRepoBuilder, "dummy.id", "1.1");

            var nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);

            // Create an invalid rule file
            var dummyRuleFilePath = Path.Combine(outputDir, "invalidRule.xml");
            File.WriteAllText(dummyRuleFilePath, "not valid xml");

            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);

            var args = new ProcessedArgsBuilder("dummy.id", outputDir)
                .SetLanguage("cs")
                .SetPackageVersion("1.1")
                .SetRuleFilePath(dummyRuleFilePath)
                .Build();

            // Act
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeFalse();
            AssertRuleTemplateDoesNotExist(outputDir);
            logger.AssertSingleErrorExists("invalidRule.xml"); // expecting an error containing the invalid rule file name
        }

        [TestMethod]
        public void CreatePluginManifest_AllProperties()
        {
            // All properties present, all properties expected.

            // Arrange
            DataServicePackage testPackage = CreateTestDataServicePackage();

            testPackage.Description = "Test Description";
            testPackage.Authors = "TestAuthor1,TestAuthor2";
            testPackage.ProjectUrl = new Uri("http://test.project.url");
            testPackage.Id = "Test.Id";

            testPackage.Title = "Test Title";
            testPackage.Owners = "TestOwner1,TestOwner2";
            testPackage.Version = "1.1.1-RC5";

            testPackage.LicenseUrl = new Uri("http://test.license.url");
            testPackage.LicenseNames = "Test License1;Test License2";

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();

            actualPluginManifest.Description.Should().Be(testPackage.Description);
            actualPluginManifest.Developers.Should().Be(testPackage.Authors);
            actualPluginManifest.Homepage.Should().Be(testPackage.ProjectUrl.ToString());
            actualPluginManifest.Key.Should().Be(PluginKeyUtilities.GetValidKey(testPackage.Id));

            actualPluginManifest.Name.Should().Be(testPackage.Title);
            actualPluginManifest.Organization.Should().Be(testPackage.Owners);
            actualPluginManifest.Version.Should().Be(testPackage.Version);

            actualPluginManifest.TermsConditionsUrl.Should().Be(testPackage.LicenseUrl.ToString());
            actualPluginManifest.License.Should().Be(testPackage.LicenseNames);
        }

        [TestMethod]
        public void CreatePluginManifest_TitleMissing()
        {
            // When no title is available, ID should be used as a fallback, removing the dot separators for legibility.

            // Arrange
            DataServicePackage testPackage = CreateTestDataServicePackage();
            testPackage.Title = null;
            testPackage.Id = "Foo.Bar.Test";

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();
            actualPluginManifest.Name.Should().Be("Foo Bar Test");
        }

        [TestMethod]
        public void CreatePluginManifest_FriendlyLicenseName_Available()
        {
            // When available, a short licensename assigned by NuGet.org should be used.
            // The license url is a fallback only.

            // Arrange
            DataServicePackage testPackage = CreateTestDataServicePackage();
            testPackage.LicenseNames = "Foo Bar License";
            testPackage.LicenseUrl = new Uri("http://foo.bar");

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();
            actualPluginManifest.License.Should().Be(testPackage.LicenseNames);
        }

        [TestMethod]
        public void CreatePluginManifest_FriendlyLicenseName_NotAvailable()
        {
            // When a short licensename is not assigned by NuGet.org, we should try to use the license URL instead.

            // Arrange
            DataServicePackage testPackage = CreateTestDataServicePackage();
            testPackage.LicenseNames = null;
            testPackage.LicenseUrl = new Uri("http://foo.bar");

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();
            actualPluginManifest.License.Should().Be(testPackage.LicenseUrl.ToString());
        }

        [TestMethod]
        public void CreatePluginManifest_FromLocalPackage()
        {
            // We should also be able to create a plugin manifest from an IPackage that is not a DataServicePackage

            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);
            IPackage testPackage = remoteRepoBuilder.CreatePackage("Foo.Bar", "1.0.0", TestUtils.CreateTextFile("dummy.txt", outputDir), License.NotRequired);

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();
            actualPluginManifest.License.Should().Be(testPackage.LicenseUrl.ToString());
        }

        [TestMethod]
        public void CreatePluginManifest_Owners_NotAvailable()
        {
            // When the package.Owners field is null, we should fallback to Authors for setting the organization.

            // Arrange
            DataServicePackage testPackage = CreateTestDataServicePackage();
            testPackage.Owners = null;
            testPackage.Authors = "Foo,Bar,Test";

            // Act
            PluginManifest actualPluginManifest = AnalyzerPluginGenerator.CreatePluginManifest(testPackage);

            // Assert
            actualPluginManifest.Should().NotBeNull();
            actualPluginManifest.Organization.Should().Be(testPackage.Authors);
        }

        #region Private methods

        private static ProcessedArgs CreateArgs(string packageId, string packageVersion, string language, bool acceptLicenses, bool recurseDependencies, string outputDirectory)
        {
            ProcessedArgs args = new ProcessedArgs(
                packageId,
                new SemanticVersion(packageVersion),
                language,
                null /* rule xml path */,
                acceptLicenses,
                recurseDependencies,
                outputDirectory,
                null);
            return args;
        }

        private AnalyzerPluginGenerator CreateTestSubjectWithFakeRemoteRepo(RemoteRepoBuilder remoteRepoBuilder)
        {
            return CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder, new TestLogger());
        }

        private AnalyzerPluginGenerator CreateTestSubjectWithFakeRemoteRepo(RemoteRepoBuilder remoteRepoBuilder, TestLogger logger)
        {
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
            return new AnalyzerPluginGenerator(nuGetHandler, logger);
        }

        private static IPackage CreatePackageWithAnalyzer(RemoteRepoBuilder remoteRepoBuilder, string packageId, string packageVersion, License acceptanceRequired, params IPackage[] dependencies)
        {
            return remoteRepoBuilder.CreatePackage(packageId, packageVersion, typeof(RoslynAnalyzer34.CSharpAnalyzer).Assembly.Location, acceptanceRequired, dependencies);
        }

        private void CreatePackageInFakeRemoteRepo(RemoteRepoBuilder remoteRepoBuilder, string packageId, string packageVersion)
        {
            remoteRepoBuilder.CreatePackage(packageId, packageVersion, typeof(RoslynAnalyzer34.AbstractAnalyzer).Assembly.Location, License.NotRequired /* no dependencies */ );
        }

        /// <summary>
        /// Creates a blank DataServicePackage with the required field Id already filled in.
        /// </summary>
        private DataServicePackage CreateTestDataServicePackage()
        {
            DataServicePackage newDataServicePackage = new DataServicePackage()
            {
                Id = "Foo.Bar"
            };
            return newDataServicePackage;
        }

        private string GetLocalNuGetDownloadDir()
        {
            return TestUtils.EnsureTestDirectoryExists(TestContext, ".localNuGetDownload");
        }

        private static string GetExpectedRuleTemplateFilePath(string outputDir, IPackage package)
        {
            return Path.Combine(outputDir, String.Format("{0}.{1}.rules.template.xml", package.Id, package.Version.ToString()));
        }

        private void AssertRuleTemplateFileExistsForPackage(TestLogger logger, string outputDir, IPackage package)
        {
            string expectedFilePath = GetExpectedRuleTemplateFilePath(outputDir, package);

            File.Exists(expectedFilePath).Should().BeTrue();
            this.TestContext.AddResultFile(expectedFilePath);
            logger.AssertSingleInfoMessageExists(expectedFilePath); // should be a message about the generated file
        }

        private static void AssertRuleTemplateDoesNotExist(string outputDir)
        {
            string[] matches = Directory.GetFiles(outputDir, "*rules*template*", SearchOption.AllDirectories);
            matches.Length.Should().Be(0, "Not expecting any rules template files to exist");
        }

        private static void AssertJarsGenerated(string rootDir, int expectedCount)
        {
            string[] files = Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
            files.Length.Should().Be(expectedCount, "Unexpected number of JAR files generated");
        }

        #endregion Private methods
    }
}