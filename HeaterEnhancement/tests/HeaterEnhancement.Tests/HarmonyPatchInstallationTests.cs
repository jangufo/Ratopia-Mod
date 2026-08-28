using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HeaterEnhancement.Core;
using Mono.Cecil;
using Xunit;

namespace HeaterEnhancement.Tests
{
    public sealed class HarmonyPatchInstallationTests
    {
        private static readonly string RatopiaDir = typeof(HarmonyPatchInstallationTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RatopiaDir")
            .Value;

        private static bool _supportedFinalizerObservedState;

        [Fact]
        public void BundledHarmonyRejectsRunOriginalInsideFinalizers()
        {
            WithRatopiaAssemblyResolver(() =>
            {
                var harmony = new Harmony("cn.ratopia.heaterenhancement.tests.reject-run-original");
                var exception = Record.Exception(() => harmony.Patch(
                    TargetMethod(nameof(UnsupportedTarget)),
                    finalizer: new HarmonyMethod(typeof(HarmonyPatchInstallationTests),
                        nameof(UnsupportedFinalizer))));

                Assert.NotNull(exception);
                Assert.Contains(
                    "Parameter __runOriginal does not contain a valid index",
                    exception.ToString());
            });
        }

        [Fact]
        public void BundledHarmonyPassesPrefixStateToFinalizers()
        {
            WithRatopiaAssemblyResolver(() =>
            {
                var harmony = new Harmony("cn.ratopia.heaterenhancement.tests.prefix-state");
                _supportedFinalizerObservedState = false;

                try
                {
                    harmony.Patch(
                        TargetMethod(nameof(SupportedTarget)),
                        prefix: new HarmonyMethod(typeof(HarmonyPatchInstallationTests),
                            nameof(SupportedPrefix)),
                        finalizer: new HarmonyMethod(typeof(HarmonyPatchInstallationTests),
                            nameof(SupportedFinalizer)));

                    SupportedTarget();
                    Assert.True(_supportedFinalizerObservedState);
                }
                finally
                {
                    harmony.UnpatchSelf();
                }
            });
        }

        [Fact]
        public void PluginFinalizersUseHarmonyCompatiblePrefixState()
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(typeof(HeaterRangeCalculator).Assembly.Location))
            {
                foreach (var patchTypeName in new[]
                         {
                             "HeaterEnhancement.Patches.HeaterBuildingUpdatePatch",
                             "HeaterEnhancement.Patches.HeaterWorkingUpdatePatch",
                             "HeaterEnhancement.Patches.HeaterWireCheckPatch"
                         })
                {
                    var patchType = assembly.MainModule.GetType(patchTypeName);
                    Assert.NotNull(patchType);

                    var prefix = patchType.Methods.Single(method => method.Name == "Prefix");
                    var finalizer = patchType.Methods.Single(method => method.Name == "Finalizer");
                    Assert.DoesNotContain(finalizer.Parameters,
                        parameter => parameter.Name == "__runOriginal");
                    Assert.Contains(prefix.Parameters, IsBooleanStateByReference);
                    Assert.Contains(finalizer.Parameters, IsBooleanStateByValue);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UnsupportedTarget()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SupportedTarget()
        {
        }

        private static MethodInfo TargetMethod(string name)
        {
            return typeof(HarmonyPatchInstallationTests).GetMethod(
                name,
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static Exception UnsupportedFinalizer(bool __runOriginal, Exception __exception)
        {
            return __exception;
        }

        private static bool SupportedPrefix(out bool __state)
        {
            __state = true;
            return true;
        }

        private static Exception SupportedFinalizer(bool __state, Exception __exception)
        {
            _supportedFinalizerObservedState = __state;
            return __exception;
        }

        private static bool IsBooleanStateByReference(ParameterDefinition parameter)
        {
            return parameter.Name == "__state" &&
                   parameter.ParameterType.FullName == "System.Boolean&";
        }

        private static bool IsBooleanStateByValue(ParameterDefinition parameter)
        {
            return parameter.Name == "__state" &&
                   parameter.ParameterType.FullName == "System.Boolean";
        }

        private static void WithRatopiaAssemblyResolver(Action action)
        {
            ResolveEventHandler resolver = ResolveRatopiaAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += resolver;

            try
            {
                action();
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }

        private static Assembly ResolveRatopiaAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";
            foreach (var directory in new[]
                     {
                         Path.Combine(RatopiaDir, "BepInEx", "core"),
                         Path.Combine(RatopiaDir, "Ratopia_Data", "Managed")
                     })
            {
                var candidate = Path.Combine(directory, assemblyName);
                if (File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }

            return null;
        }
    }
}
