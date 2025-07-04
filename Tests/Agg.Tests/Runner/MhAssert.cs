/*
Copyright (c) 2025, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agg.Tests.Agg
{
    public class MhAssert
    {
        public static void Equal(int expected, int actual)
        {
            if (expected != actual)
            {
                throw new Exception($"Expected {expected} but was {actual}");
            }
        }

        public static void Equal(double expected, double actual, double error = .001)
        {
            if (Math.Abs(expected - actual) > error)
            {
                throw new Exception($"Expected {expected} but was {actual}");
            }
        }

        public static void Equal(string expected, string actual)
        {
            if (expected != actual)
            {
                throw new Exception($"Expected {expected} but was {actual}");
            }
        }

        public static void Equal(object expected, object actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                throw new Exception($"Expected {expected} but was {actual}");
            }

            if (expected.GetType() != actual.GetType())
            {
                throw new Exception($"Expected type {expected.GetType()} but was {actual.GetType()}");
            }

            if (expected is Array array1 && actual is Array array2)
            {
                if (array1.Length != array2.Length)
                {
                    throw new Exception("Array lengths do not match.");
                }

                for (int i = 0; i < array1.Length; i++)
                {
                    Equal(array1.GetValue(i), array2.GetValue(i));
                }
            }
            else if (!expected.Equals(actual))
            {
                throw new Exception($"Expected {expected} but was {actual}");
            }
        }

        // Overloads with message parameter for better test documentation
        public static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
            {
                throw new Exception($"{message}. Expected {expected} but was {actual}");
            }
        }

        public static void Equal(double expected, double actual, string message, double error = .001)
        {
            if (Math.Abs(expected - actual) > error)
            {
                throw new Exception($"{message}. Expected {expected} but was {actual}");
            }
        }

        public static void Equal(string expected, string actual, string message)
        {
            if (expected != actual)
            {
                throw new Exception($"{message}. Expected '{expected}' but was '{actual}'");
            }
        }

        public static void Equal(object expected, object actual, string message)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null || actual == null)
            {
                throw new Exception($"{message}. Expected {expected} but was {actual}");
            }

            if (expected.GetType() != actual.GetType())
            {
                throw new Exception($"{message}. Expected type {expected.GetType()} but was {actual.GetType()}");
            }

            if (expected is Array array1 && actual is Array array2)
            {
                if (array1.Length != array2.Length)
                {
                    throw new Exception($"{message}. Array lengths do not match.");
                }

                for (int i = 0; i < array1.Length; i++)
                {
                    Equal(array1.GetValue(i), array2.GetValue(i), $"{message}. Array element at index {i}");
                }
            }
            else if (!expected.Equals(actual))
            {
                throw new Exception($"{message}. Expected {expected} but was {actual}");
            }
        }

        public static void False(bool condition, string message = "Expected false but was true")
        {
            if (condition)
            {
                throw new Exception(message);
            }
        }

        public static void True(bool condition, string message = "Expected true but was false")
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        public static void NotNull(object obj)
        {
            if (obj == null)
            {
                throw new Exception("Expected not null but was null");
            }
        }

        public static void NotNull(object obj, string message)
        {
            if (obj == null)
            {
                throw new Exception($"{message}. Expected not null but was null");
            }
        }

        public static void Null(object obj)
        {
            if (obj != null)
            {
                throw new Exception("Expected null but was not null");
            }
        }

        public static void Null(object obj, string message)
        {
            if (obj != null)
            {
                throw new Exception($"{message}. Expected null but was not null");
            }
        }

        public static T Single<T>(IEnumerable<T> collection)
        {
            if (collection.Count() != 1)
            {
                throw new Exception($"Expected 1 but was {collection.Count()}");
            }

            return collection.First();
        }

        public static void IsType<T>(object obj)
        {
            if (!(obj is T))
            {
                throw new Exception($"Expected {typeof(T)} but was {obj.GetType()}");
            }
        }

        public static void IsNotType<T>(object obj)
        {
            if (obj is T)
            {
                throw new Exception($"Expected not {typeof(T)} but was {obj.GetType()}");
            }
        }

        public static void NotEqual(object expected, object actual)
        {
            if (expected == null && actual == null)
            {
                throw new Exception("Both objects are null, but they should be different.");
            }

            if (expected == null || actual == null)
            {
                return; // One is null and the other isn't, so they're not equal
            }

            if (expected.GetType() != actual.GetType())
            {
                return; // Different types, so they're not equal
            }

            if (expected is Array array1 && actual is Array array2)
            {
                if (array1.Length != array2.Length)
                {
                    return; // Different lengths, so they're not equal
                }

                try
                {
                    for (int i = 0; i < array1.Length; i++)
                    {
                        Equal(array1.GetValue(i), array2.GetValue(i));
                    }
                }
                catch
                {
                    return; // If Equal throws an exception, it means elements are different, so arrays are not equal
                }

                // If we've made it here, all elements are equal
                throw new Exception("Arrays are equal, but they should be different.");
            }
            else if (expected.Equals(actual))
            {
                throw new Exception($"Objects are equal, but they should be different. Value: {expected}");
            }
        }

        public static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
                throw new Exception($"Expected exception of type {typeof(T).Name} was not thrown.");
            }
            catch (T)
            {
                // Exception of type T was thrown as expected.
            }
            catch (Exception ex)
            {
                throw new Exception($"Expected exception of type {typeof(T).Name} but {ex.GetType().Name} was thrown.", ex);
            }
        }

        public static void Empty(IEnumerable items)
        {
            if (items.GetEnumerator().MoveNext())
            {
                throw new Exception("Expected empty but was not empty");
            }
        }

        public static void Fail(string description = "")
        {
            throw new NotImplementedException();
        }
    }
}
