﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace JazSharp.Testing
{
    /// <summary>
    /// Describes a test that has been discovered.
    /// </summary>
    public class Test : IEquatable<Test>
    {
        internal TestExecution Execution { get; }

        /// <summary>
        /// The class that defined this test.
        /// </summary>
        public Type TestClass { get; }

        /// <summary>
        /// Whether or not the test (or any of it's ancestor Describes) has made it a focus. If any tests are
        /// focused, only focused tests are executed. If a test is excluded, it cannot be focused. Tests can
        /// be focused by using <see cref="Spec.fIt(string, Action, string, int)"/> or <see cref="Spec.fDescribe(string, Action)"/>.
        /// </summary>
        public bool IsFocused { get; }

        /// <summary>
        /// Whether or not the test (or any of it's ancestor Describes) has been flagged for exclusion. Excluded
        /// tests are never executed and will be reported as Skipped in some test runners. An excluded test will
        /// never be focused. Tests can be excluded by using <see cref="Spec.xIt(string, Action, string, int)"/> and
        /// <see cref="Spec.xDescribe(string, Action)"/>.
        /// </summary>
        public bool IsExcluded { get; }

        /// <summary>
        /// The path to the test. Each element in the path is comprised of the value passed into a call to
        /// <see cref="Spec.Describe(string, Action)"/> and similar methods.
        /// </summary>
        public IReadOnlyList<string> Path { get; }
        
        /// <summary>
        /// The description of the test. This is the value passed in to <see cref="Spec.It(string, Action, string, int)"/> and
        /// similar methods.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The full name of the test. This is a concatenation of the <see cref="Path"/> values and the
        /// <see cref="Description"/>.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// The filename of the source file where the <see cref="Spec.It(string, Action, string, int)"/> call was made.
        /// </summary>
        public string SourceFilename { get; }

        /// <summary>
        /// The line number within <see cref="SourceFilename"/> where the <see cref="Spec.It(string, Action, string, int)"/>
        /// call is made.
        /// </summary>
        public int LineNumber { get; }

        internal Test(
            Type testClass,
            IEnumerable<string> path,
            string description,
            TestExecution testExecution,
            bool isFocused,
            bool isExcluded,
            string sourceFilename,
            int lineNumber)
        {
            Execution = testExecution;
            TestClass = testClass;
            Path = ImmutableArray.CreateRange(path);
            Description = description;
            FullName = string.Join(" ", path) + " " + description;
            IsFocused = isFocused;
            IsExcluded = isExcluded;
            SourceFilename = sourceFilename;
            LineNumber = lineNumber;
        }

        internal Test Prepare(Assembly executionReadyAssembly, Assembly jasSharpAssembly)
        {
            var module = executionReadyAssembly.Modules.First();
            var getPreparedTestExecutionMethod =
                jasSharpAssembly
                    .GetType(typeof(SpecHelper).Namespace + "." + typeof(SpecHelper).Name)
                    .GetMethod(nameof(SpecHelper.GetTestExecutionMethods), BindingFlags.Static | BindingFlags.NonPublic);

            var testClass = executionReadyAssembly.GetTypes().First(x => x.ToString() == TestClass.ToString());
            var executionMethods = (Delegate[][])getPreparedTestExecutionMethod.Invoke(null, new object[] { testClass, FullName });

            return
                new Test(
                    TestClass,
                    Path,
                    Description,
                    new TestExecution(executionMethods[0], executionMethods[2], executionMethods[1][0]),
                    IsFocused,
                    IsExcluded,
                    SourceFilename,
                    LineNumber);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj as Test);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(Test other)
        {
            return
                other != null &&
                FullName == other.FullName &&
                TestClass == other.TestClass &&
                SourceFilename == other.SourceFilename &&
                LineNumber == other.LineNumber;
        }
    }
}
