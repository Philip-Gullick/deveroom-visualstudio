﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deveroom.VisualStudio.SpecFlowConnector.Models;
using Deveroom.VisualStudio.SpecFlowConnector.SourceDiscovery;
using Deveroom.VisualStudio.SpecFlowConnector.SourceDiscovery.DnLib;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Bindings.Reflection;

namespace Deveroom.VisualStudio.SpecFlowConnector.Discovery
{
    public abstract class BaseDiscoverer : RemoteContextObject, ISpecFlowDiscoverer
    {
        private readonly Dictionary<Assembly, IDeveroomSymbolReader> _symbolReaders = new Dictionary<Assembly, IDeveroomSymbolReader>(2);
        private readonly Dictionary<string, int> _sourceFiles = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _typeNames = new Dictionary<string, int>();

        public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
        {
            var result = DiscoverInternal(testAssembly, testAssemblyPath, configFilePath);

            var resultJson = JsonSerialization.SerializeObject(result);
            return resultJson;
        }

        internal DiscoveryResult DiscoverInternal(Assembly testAssembly, string testAssemblyPath, string configFilePath)
        {
            var bindingRegistry = GetBindingRegistry(testAssembly, configFilePath);

            var result = new DiscoveryResult();
            var warningCollector = new WarningCollector();

            _sourceFiles.Clear();
            GetOrCreateSymbolReader(testAssembly, warningCollector, testAssemblyPath);

            result.StepDefinitions =
                GetStepDefinitions(bindingRegistry).Select(sdb =>
                        new StepDefinition
                        {
                            Type = sdb.StepDefinitionType.ToString(),
                            Regex = sdb.Regex.ToString(),
                            Method = sdb.Method.ToString(),
                            ParamTypes = GetParamTypes(sdb.Method),
                            Scope = GetScope(sdb),
                            SourceLocation = GetSourceLocation(sdb.Method, warningCollector)
                        })
                    .Distinct() //TODO: SpecFlow discoverers bindings from external assemblies twice -- needs fix
                    .ToArray();
            result.SourceFiles = _sourceFiles.ToDictionary(sf => sf.Value.ToString(), sf => sf.Key);
            result.TypeNames = _typeNames.ToDictionary(sf => sf.Value.ToString(), sf => sf.Key);
            result.Warnings = warningCollector.Warnings;
            result.SpecFlowVersion = typeof(IStepDefinitionBinding).Assembly.Location;
            return result;
        }

        protected virtual IDeveroomSymbolReader CreateSymbolReader(string assemblyFilePath, WarningCollector warningCollector)
        {
            try
            {
                return new DnLibDeveroomSymbolReader(assemblyFilePath);
            }
            catch (Exception ex)
            {
                warningCollector.AddWarning($"Unable to create symbol reader for '{assemblyFilePath}'.", ex);
                return new NullDeveroomSymbolReader();
            }
        }

        private IDeveroomSymbolReader GetOrCreateSymbolReader(Assembly assembly, WarningCollector warningCollector, string assemblyFilePath = null)
        {
            if (!_symbolReaders.TryGetValue(assembly, out var symbolReader))
            {
                assemblyFilePath = assemblyFilePath ?? new Uri(assembly.CodeBase).LocalPath;
                symbolReader = CreateSymbolReader(assemblyFilePath, warningCollector);
                _symbolReaders.Add(assembly, symbolReader);
            }
            return symbolReader;
        }

        private string GetSourceLocation(IBindingMethod bindingMethod, WarningCollector warningCollector)
        {
            if (bindingMethod is RuntimeBindingMethod runtimeBindingMethod && runtimeBindingMethod.MethodInfo.DeclaringType != null)
            {
                try
                {
                    var symbolReader = GetOrCreateSymbolReader(runtimeBindingMethod.MethodInfo.DeclaringType.Assembly, warningCollector);
                    var methodSymbol = symbolReader.ReadMethodSymbol(runtimeBindingMethod.MethodInfo.MetadataToken);
                    var startSequencePoint = methodSymbol?.SequencePoints?.FirstOrDefault(sp => !sp.IsHidden);
                    if (startSequencePoint == null)
                        return null;
                    var sourceKey = GetKey(_sourceFiles, startSequencePoint.SourcePath);
                    var sourceLocation = $"#{sourceKey}|{startSequencePoint.StartLine}|{startSequencePoint.StartColumn}";
                    var endSequencePoint = methodSymbol.SequencePoints.LastOrDefault(sp => !sp.IsHidden);
                    if (endSequencePoint != null)
                        sourceLocation = sourceLocation + $"|{endSequencePoint.EndLine}|{endSequencePoint.EndColumn}";

                    return sourceLocation;
                }
                catch (Exception ex)
                {
                    warningCollector.AddWarning("GetSourceLocation", ex);
                    return null;
                }
            }
            return null;
        }

        private int GetKey(Dictionary<string, int> dictionary, string value)
        {
            if (!dictionary.TryGetValue(value, out var key))
            {
                key = dictionary.Count;
                dictionary.Add(value, key);
            }
            return key;
        }

        private string GetParamTypes(IBindingMethod bindingMethod)
        {
            var paramTypes = string.Join("|", bindingMethod.Parameters.Select(GetParamType));
            return paramTypes.Length == 0 ? null : paramTypes;
        }

        private string GetParamType(IBindingParameter bindingParameter)
        {
            var typeFullName = bindingParameter.Type.FullName;
            if (TypeShortcuts.FromType.TryGetValue(typeFullName, out var shortcut))
                return shortcut;

            var key = GetKey(_typeNames, typeFullName);
            return $"#{key}";
        }

        protected abstract IBindingRegistry GetBindingRegistry(Assembly testAssembly, string configFilePath);
        protected abstract IEnumerable<IStepDefinitionBinding> GetStepDefinitions(IBindingRegistry bindingRegistry);

        private string GetScope(IStepDefinitionBinding stepDefinitionBinding)
        {
            if (!stepDefinitionBinding.IsScoped)
                return null;
            if (stepDefinitionBinding.BindingScope.Tag == null)
                return null;

            return "@" + stepDefinitionBinding.BindingScope.Tag;
        }

        public void Dispose()
        {
            foreach (var symbolReader in _symbolReaders.Values)
            {
                symbolReader.Dispose();
            }
            _symbolReaders.Clear();
        }
    }
}
