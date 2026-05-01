using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Reflection-based structural tests for read-only wrapper classes.
    /// Feature: readonly-data-wrappers
    /// Validates: Requirements 1.1-1.6, 1.12, 2.1-2.4, 3.1, 12.1
    /// </summary>
    [TestFixture]
    public class ReadOnlyWrapperStructuralTests
    {
        /// <summary>
        /// All ReadOnly wrapper types discovered via reflection from the main assembly.
        /// </summary>
        private static readonly Type[] AllWrapperTypes = typeof(ReadOnlyBlueprint).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "OE2EmpireTracker.Models" && t.Name.StartsWith("ReadOnly") && t.IsClass && !t.IsAbstract)
            .ToArray();

        // ---------------------------------------------------------------
        // Task 11.2: No Mutation Surface
        // Validates: Requirements 1.6, 2.3
        // ---------------------------------------------------------------

        [Test]
        public void AllWrappers_HaveNoPublicSetters()
        {
            foreach (var type in AllWrapperTypes)
            {
                var setters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToArray();
                Assert.That(setters, Is.Empty,
                    $"{type.Name} has public setters: {string.Join(", ", setters.Select(p => p.Name))}");
            }
        }

        [Test]
        public void AllWrappers_HaveNoMutationMethods()
        {
            var excludedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetHashCode", "GetString", "GetDecimal", "GetLong", "GetBoolean",
                "GetLockedQuantity", "GetLocksForProcess", "GetSkill", "GetSkillGroup",
                "GetUnallocatedPresent", "GetType"
            };

            var mutationPrefixes = new[] { "Add", "Remove", "Set", "Clear" };

            foreach (var type in AllWrapperTypes)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .Where(m => mutationPrefixes.Any(prefix => m.Name.StartsWith(prefix)))
                    .Where(m => !excludedMethods.Contains(m.Name))
                    .ToArray();

                Assert.That(methods, Is.Empty,
                    $"{type.Name} has mutation methods: {string.Join(", ", methods.Select(m => m.Name))}");
            }
        }

        [Test]
        public void AllWrappers_DoNotExposeWrappedEntityType()
        {
            foreach (var type in AllWrapperTypes)
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) continue;
                var entityType = ctor.GetParameters().FirstOrDefault()?.ParameterType;
                if (entityType == null) continue;

                // Check public and internal properties
                var exposedProps = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.PropertyType == entityType)
                    .Where(p => p.GetMethod != null && (p.GetMethod.IsPublic || p.GetMethod.IsAssembly))
                    .ToArray();

                Assert.That(exposedProps, Is.Empty,
                    $"{type.Name} exposes wrapped entity type {entityType.Name} via properties: {string.Join(", ", exposedProps.Select(p => p.Name))}");

                // Check public and internal fields
                var exposedFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.FieldType == entityType)
                    .Where(f => f.IsPublic || f.IsAssembly)
                    .ToArray();

                Assert.That(exposedFields, Is.Empty,
                    $"{type.Name} exposes wrapped entity type {entityType.Name} via fields: {string.Join(", ", exposedFields.Select(f => f.Name))}");
            }
        }

        // ---------------------------------------------------------------
        // Task 11.3: Single Field
        // Validates: Requirement 3.1
        // ---------------------------------------------------------------

        [Test]
        public void AllWrappers_HaveSinglePrivateReadonlyField()
        {
            foreach (var type in AllWrapperTypes)
            {
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => !f.Name.Contains("__"))
                    .ToArray();

                Assert.That(fields.Length, Is.EqualTo(1),
                    $"{type.Name} has {fields.Length} instance fields (expected 1): {string.Join(", ", fields.Select(f => f.Name))}");
                Assert.That(fields[0].IsInitOnly, Is.True,
                    $"{type.Name} field '{fields[0].Name}' is not readonly");
            }
        }

        // ---------------------------------------------------------------
        // Task 11.4: No JSON Attributes
        // Validates: Requirement 1.12
        // ---------------------------------------------------------------

        [Test]
        public void AllWrappers_HaveNoJsonAttributes()
        {
            var jsonAttributeNames = new HashSet<string>
            {
                "JsonPropertyAttribute",
                "JsonConverterAttribute",
                "JsonIgnoreAttribute"
            };

            foreach (var type in AllWrapperTypes)
            {
                // Check class-level attributes
                var classAttrs = type.GetCustomAttributes(true)
                    .Where(a => jsonAttributeNames.Contains(a.GetType().Name))
                    .ToArray();
                Assert.That(classAttrs, Is.Empty,
                    $"{type.Name} has JSON attributes on the class: {string.Join(", ", classAttrs.Select(a => a.GetType().Name))}");

                // Check property-level attributes
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var propAttrs = prop.GetCustomAttributes(true)
                        .Where(a => jsonAttributeNames.Contains(a.GetType().Name))
                        .ToArray();
                    Assert.That(propAttrs, Is.Empty,
                        $"{type.Name}.{prop.Name} has JSON attributes: {string.Join(", ", propAttrs.Select(a => a.GetType().Name))}");
                }

                // Check method-level attributes
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var methodAttrs = method.GetCustomAttributes(true)
                        .Where(a => jsonAttributeNames.Contains(a.GetType().Name))
                        .ToArray();
                    Assert.That(methodAttrs, Is.Empty,
                        $"{type.Name}.{method.Name} has JSON attributes: {string.Join(", ", methodAttrs.Select(a => a.GetType().Name))}");
                }
            }
        }

        // ---------------------------------------------------------------
        // Task 11.5: No Casting Path
        // Validates: Requirements 2.1, 2.2, 2.4
        // ---------------------------------------------------------------

        [Test]
        public void AllWrappers_HaveNoSharedInterfacesWithMutableEntity()
        {
            foreach (var type in AllWrapperTypes)
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) continue;
                var entityType = ctor.GetParameters().FirstOrDefault()?.ParameterType;
                if (entityType == null) continue;

                var wrapperInterfaces = type.GetInterfaces();
                var entityInterfaces = entityType.GetInterfaces();
                var shared = wrapperInterfaces.Intersect(entityInterfaces).ToArray();

                Assert.That(shared, Is.Empty,
                    $"{type.Name} shares interfaces with {entityType.Name}: {string.Join(", ", shared.Select(i => i.Name))}");
            }
        }

        [Test]
        public void AllWrappers_DoNotInheritFromMutableEntity()
        {
            foreach (var type in AllWrapperTypes)
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) continue;
                var entityType = ctor.GetParameters().FirstOrDefault()?.ParameterType;
                if (entityType == null) continue;

                Assert.That(type.IsSubclassOf(entityType), Is.False,
                    $"{type.Name} inherits from {entityType.Name}");
                Assert.That(entityType.IsSubclassOf(type), Is.False,
                    $"{entityType.Name} inherits from {type.Name}");
            }
        }

        [Test]
        public void AllWrappers_HaveNoConversionOperators()
        {
            foreach (var type in AllWrapperTypes)
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) continue;
                var entityType = ctor.GetParameters().FirstOrDefault()?.ParameterType;
                if (entityType == null) continue;

                var wrapperOps = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    .Where(m => m.ReturnType == entityType || m.GetParameters().Any(p => p.ParameterType == entityType))
                    .ToArray();

                Assert.That(wrapperOps, Is.Empty,
                    $"{type.Name} has conversion operators involving {entityType.Name}");

                var entityOps = entityType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    .Where(m => m.ReturnType == type || m.GetParameters().Any(p => p.ParameterType == type))
                    .ToArray();

                Assert.That(entityOps, Is.Empty,
                    $"{entityType.Name} has conversion operators involving {type.Name}");
            }
        }

        // ---------------------------------------------------------------
        // Task 11.6: Null Constructor Guard
        // Validates: Design Error Handling
        // ---------------------------------------------------------------

        [Test]
        public void AllWrappers_ThrowArgumentNullExceptionForNullConstructorArg()
        {
            foreach (var type in AllWrapperTypes)
            {
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) continue;

                var ex = Assert.Throws<TargetInvocationException>(() =>
                    ctor.Invoke(new object[] { null }),
                    $"{type.Name} did not throw when constructed with null");

                Assert.That(ex.InnerException, Is.TypeOf<ArgumentNullException>(),
                    $"{type.Name} threw {ex.InnerException?.GetType().Name} instead of ArgumentNullException");
            }
        }

        // ---------------------------------------------------------------
        // Task 11.7: Empty Collections
        // Validates: Requirements 1.9, 1.10
        // ---------------------------------------------------------------

        [Test]
        public void Colony_EmptyCollections_ReturnsEmptyNotNull()
        {
            var colony = new Colony
            {
                UUID = Guid.NewGuid().ToString(),
                Structures = new List<ColonyStructure>(),
                Commodities = new List<CommodityRequested>()
            };

            var ro = new ReadOnlyColony(colony);

            Assert.That(ro.Structures, Is.Not.Null);
            Assert.That(ro.Structures, Is.Empty);
            Assert.That(ro.Commodities, Is.Not.Null);
            Assert.That(ro.Commodities, Is.Empty);
        }

        [Test]
        public void DeliveryRoute_EmptyStops_ReturnsEmptyNotNull()
        {
            var route = new DeliveryRoute
            {
                UUID = Guid.NewGuid().ToString(),
                Stops = new List<RouteStop>()
            };

            var ro = new ReadOnlyDeliveryRoute(route);

            Assert.That(ro.Stops, Is.Not.Null);
            Assert.That(ro.Stops, Is.Empty);
        }

        [Test]
        public void BuildPlan_EmptyItems_ReturnsEmptyNotNull()
        {
            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Items = new List<BuildItem>()
            };

            var ro = new ReadOnlyBuildPlan(plan);

            Assert.That(ro.Items, Is.Not.Null);
            Assert.That(ro.Items, Is.Empty);
        }
    }
}
