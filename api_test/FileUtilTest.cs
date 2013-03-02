using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModernMinas.Update.Api;

namespace ApiTest
{
    /// <summary>
    /// Tests all functions for file entry manipulation
    /// </summary>
    [TestClass]
    public class FileFilterUtilTest
    {
        private TestContext testContextInstance;

        /// <summary>
        /// The current test context
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        string[] testEntries = {
                                       "net/test/1.class",
                                       "net/test/2.class",
                                       "org/test/3.class",
                                       @"weird\field",
                                       "1+2=3",
                                       "1|2|3",
                                       "1?2?3",
                                       "1_2_3",
                                       "1-2-3"
                                   };

        public void ThreeCharTest(string c, bool negativeIsSuccess = false)
        {
            var result = FileFilterUtil.FilterFiles(testEntries, "*" + c + "*" + c + "*");
            var expected = new[] { "1" + c + "2" + c + "3" };
            Console.WriteLine("Expected: {0}", string.Join(", ", expected));
            Console.WriteLine("Actual result: {0}", string.Join(", ", result));
            if (!result.SequenceEqual(expected) && !negativeIsSuccess)
                throw new Exception("3 char search with " + c + " symbol failed (positive expected, but got negative)");
            else if (result.SequenceEqual(expected) && negativeIsSuccess)
                throw new Exception("3 char search with " + c + " symbol failed (negative expected, but got positive)");
        }

        [TestMethod]
        public void RegexTransform_SubstringSearch()
        {
            var result = FileFilterUtil.FilterFiles(testEntries, "*test*");
            var expected = new[] { "net/test/1.class", "net/test/2.class", "org/test/3.class" };
            Console.WriteLine("Expected: {0}", string.Join(", ", expected));
            Console.WriteLine("Actual result: {0}", string.Join(", ", result));
            if (!result.SequenceEqual(expected))
                throw new Exception("Substring search failed");
        }

    [TestMethod]
        public void RegexTransform_DoubleSubstringSearch()
        {
            var result = FileFilterUtil.FilterFiles(testEntries, "net*2*");
            var expected = new[] { "net/test/2.class" };
            Console.WriteLine("Expected: {0}", string.Join(", ", expected));
            Console.WriteLine("Actual result: {0}", string.Join(", ", result));
            if (!result.SequenceEqual(expected))
                throw new Exception("Double substring char search failed");
    }

        [TestMethod]
        public void RegexTransform_SubstringCharSearch()
        {
            var result = FileFilterUtil.FilterFiles(testEntries, "org/test/?.class");
            var expected = new[] { "org/test/3.class" };
            Console.WriteLine("Expected: {0}", string.Join(", ", expected));
            Console.WriteLine("Actual result: {0}", string.Join(", ", result));
            if (!result.SequenceEqual(expected))
                throw new Exception("Substring char search failed");
        }

        [TestMethod]
        public void RegexTransform_GroupBypassExploitation()
        {
            if (FileFilterUtil.FilterFiles(testEntries, "(net|org)*").Any())
                throw new Exception("Negative regex handling hole check failed");
        }

        [TestMethod]
        public void RegexTransform_ThreeCharMinus()
        {
            ThreeCharTest("-");
        }

        [TestMethod]
        public void RegexTransform_ThreeCharVerticalLine()
        {
            ThreeCharTest("|");
        }
        
        [TestMethod]
        public void RegexTransform_QuestionMark()
        {
            var c = "?";
            var result = FileFilterUtil.FilterFiles(testEntries, "*" + c + "*" + c + "*");
            var expected = testEntries;
            Console.WriteLine("Expected: {0}", string.Join(", ", expected));
            Console.WriteLine("Actual result: {0}", string.Join(", ", result));
            if (!result.SequenceEqual(expected))
                throw new Exception("3 char search with " + c + " symbol failed (didn't get complete entry list as expected)");
        }

        [TestMethod]
        public void RegexTransform_Plus()
        {
            ThreeCharTest("+", true);
        }

        [TestMethod]
        public void RegexTransform_Equals()
        {
            ThreeCharTest("=", true);
        }
    }
}
