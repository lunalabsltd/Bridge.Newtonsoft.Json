﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bridge.Test.NUnit;

namespace Newtonsoft.Json.Tests
{
	[Category("ListOptimizations")]
    [TestFixture]
    public class ListOptimizationTests
    {
        /// <summary>
        /// The list deserialize optimization should apply to this data because
        /// each item in the list is of exactly the same type
        /// </summary>
        [Test]
        public static void DeserializationWorks()
        {
            var items = new NonNullList<KeyValuePairDataModel>(new[]
            {
                new KeyValuePairDataModel(),
                new KeyValuePairDataModel()
            });
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };

            var json = JsonConvert.SerializeObject(items, settings);
            var cloneAsArray = JsonConvert.DeserializeObject<NonNullList<KeyValuePairDataModel>>(json, settings).ToArray();

            Assert.AreEqual(2, cloneAsArray.Length, "Optimized deserialization length");
            Assert.AreEqual(typeof(KeyValuePairDataModel), cloneAsArray[0].GetType(), "Optimized deserialization index 0 data type");
            Assert.AreEqual(typeof(KeyValuePairDataModel), cloneAsArray[1].GetType(), "Optimized deserialization index 1 data type");
        }

        /// <summary>
        /// The optimization should NOT apply to this data since there are
        /// different types in the list (this test is to ensure that the
        /// optimization checks didn't break anything in the non-optimization
        /// paths)
        /// </summary>
        [Test]
        public static void NoOptDeserializationWorks()
        {
            var items = new NonNullList<KeyValuePairDataModelBase>(new KeyValuePairDataModelBase[]
            {
                new KeyValuePairDataModel(),
                new AlternativeKeyValuePairDataModel()
            });
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };

            var json = JsonConvert.SerializeObject(items, settings);
            var cloneAsArray = JsonConvert.DeserializeObject<NonNullList<KeyValuePairDataModelBase>>(json, settings).ToArray();

            Assert.AreEqual(2, cloneAsArray.Length, "Non-optimized deserialization length");
            Assert.AreEqual(typeof(KeyValuePairDataModel), cloneAsArray[0].GetType(), "Non-optimized deserialization index 0 data type");
            Assert.AreEqual(typeof(AlternativeKeyValuePairDataModel), cloneAsArray[1].GetType(), "Non-optimized deserialization index 1 data type");
        }

        public sealed class KeyValuePairDataModel : KeyValuePairDataModelBase
        {
        }

        public sealed class AlternativeKeyValuePairDataModel : KeyValuePairDataModelBase
        {
        }

        public abstract class KeyValuePairDataModelBase
        {
        }
        public sealed class NonNullList<T> : IEnumerable<T>
        {
            private readonly Node _headIfAny;
            private NonNullList(Node headIfAny)
            {
                _headIfAny = headIfAny;
            }
            public NonNullList(IEnumerable<T> values)
            {
                Node node = null;
                foreach (var value in values.Reverse())
                {
                    node = new Node
                    {
                        Count = ((node == null) ? 0 : node.Count) + 1,
                        Item = value,
                        NextIfAny = node
                    };
                }
                _headIfAny = node;
            }

            public uint Count { get { return (_headIfAny == null) ? 0 : (uint)_headIfAny.Count; } }

            public IEnumerator<T> GetEnumerator()
            {
                var node = _headIfAny;
                while (node != null)
                {
                    yield return node.Item;
                    node = node.NextIfAny;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

            private sealed class Node
            {
                public int Count;
                public T Item;
                public Node NextIfAny;
            }
        }
    }
}