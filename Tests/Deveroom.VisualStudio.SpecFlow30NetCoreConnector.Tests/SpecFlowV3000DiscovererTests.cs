﻿using System;
using System.Linq;
using System.Reflection;
using Deveroom.VisualStudio.SpecFlowConnector.Discovery.V3000;
using Deveroom.VisualStudio.SpecFlowConnector.Models;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;

namespace Deveroom.VisualStudio.SpecFlow30NetCoreConnector.Tests
{
    public class SpecFlowV3000DiscovererTests
    {
        private SpecFlowV3000Discoverer CreateSut()
        {
            var stubDiscoverer = new SpecFlowV3000Discoverer();
            return stubDiscoverer;
        }

        private string GetTestAssemblyPath()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        private DiscoveryResult PerformDiscover(SpecFlowV3000Discoverer sut)
        {
            var testAssemblyPath = GetTestAssemblyPath();
            var testAssembly = Assembly.LoadFrom(testAssemblyPath);
            return sut.DiscoverInternal(testAssembly, testAssemblyPath, null);
        }

        [Binding]
        public class SampleBindings
        {
            [When(@"I press add")]
            public void WhenIPressAdd() { }

            public static bool BeforeTestRunHookCalled = false;
            [BeforeTestRun]
            public static void BeforeTestRunHook()
            {
                BeforeTestRunHookCalled = true;
            }

            public static bool AfterTestRunHookCalled = false;
            [AfterTestRun]
            public static void AfterTestRunHook()
            {
                AfterTestRunHookCalled = true;
            }
        }

        [Fact]
        public void Discovers_step_definitions()
        {
            var sut = CreateSut();

            var result = PerformDiscover(sut);

            result.StepDefinitions.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Should_not_invoke_BeforeAfterTestRun_hook_during_discovery_Issue_27()
        {
            var sut = CreateSut();
            SampleBindings.BeforeTestRunHookCalled = false;
            SampleBindings.AfterTestRunHookCalled = false;

            var result = PerformDiscover(sut);

            result.StepDefinitions.Should().NotBeNullOrEmpty();
            SampleBindings.AfterTestRunHookCalled.Should().BeFalse();
            SampleBindings.BeforeTestRunHookCalled.Should().BeFalse();
        }
    }
}
