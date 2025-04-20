namespace RJCP.MSBuildTasks.Infrastructure.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class IniSectionTest
    {
        [Test]
        public void EmptyIniSection()
        {
            IniSection section = new("header");
            Assert.That(section, Is.Empty);
            Assert.That(section.IsReadOnly, Is.False);

            int count = 0;
            foreach (KeyValuePair<string, string> item in section) {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyIniSectionKeys()
        {
            IniSection section = new("header");
            Assert.That(section.Keys, Is.Empty);
        }

        [Test]
        public void EmptyIniSectionValues()
        {
            IniSection section = new("header");
            Assert.That(section.Values, Is.Empty);
        }

        [Test]
        public void AddNullKey()
        {
            IniSection section = new("header");
            Assert.That(() => { section.Add(null, "value"); }, Throws.TypeOf<ArgumentNullException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void AddNullValue()
        {
            IniSection section = new("header");
            Assert.That(() => { section.Add("key", null); }, Throws.TypeOf<ArgumentNullException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void AddNull()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>(null, null));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void AddPrivateNullKey()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>(null, "value"));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void AddPrivateNullValue()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", null));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void AddKeyValuePair()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(section.ContainsKey("key"), Is.True);
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddKeyValuePairInsensitive1()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(
                () => {
                    section.Add("KEY", "value2");
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddKeyValuePairInsensitive2()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(
                () => {
                    section.Add("Key", "value2");
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void KeyValuePairContainsCaseInsensitive()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(section.ContainsKey("KEY"), Is.True);
            Assert.That(section.ContainsKey("keY"), Is.True);
            Assert.That(section.ContainsKey("kEY"), Is.True);
            Assert.That(section.ContainsKey("kEy"), Is.True);
            Assert.That(section.ContainsKey("KeY"), Is.True);
            Assert.That(section.ContainsKey("KEy"), Is.True);
        }

        [Test]
        public void KeyValuePairPrivateContains()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KEY", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KEy", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KeY", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("Key", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("kEY", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("kEy", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("keY", "value")), Is.True);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("key", "value")), Is.True);

            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KEY", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KEy", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("KeY", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("Key", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("kEY", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("kEy", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("keY", "value2")), Is.False);
            Assert.That(((IDictionary<string, string>)section).Contains(new KeyValuePair<string, string>("key", "value2")), Is.False);
        }

        [Test]
        public void AddKeyValuePairTwice()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(() => { section.Add("key", "value2"); }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePair()
        {
            IniSection section = new("header");
            ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value"));
            Assert.That(section.ContainsKey("key"), Is.True);
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePairContainsCaseInsensitive()
        {
            IniSection section = new("header");
            ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value"));
            Assert.That(section.ContainsKey("KEY"), Is.True);
            Assert.That(section.ContainsKey("keY"), Is.True);
            Assert.That(section.ContainsKey("kEY"), Is.True);
            Assert.That(section.ContainsKey("kEy"), Is.True);
            Assert.That(section.ContainsKey("KeY"), Is.True);
            Assert.That(section.ContainsKey("KEy"), Is.True);
        }

        [Test]
        public void AddPrivateKeyValuePairTwice1()
        {
            IniSection section = new("header");
            ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value"));
            Assert.That(
                () => {
                    section.Add("key", "value2");
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePairTwice2()
        {
            IniSection section = new("header");
            ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value"));
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value2"));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePairTwice3()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("key", "value2"));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePairInsensitive1()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("KEY", "value2"));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddPrivateKeyValuePairInsensitive2()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).Add(new KeyValuePair<string, string>("Key", "value2"));
                }, Throws.TypeOf<ArgumentException>());
            Assert.That(section, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddMultipleKeys1()
        {
            IniSection section = new("header") {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            Assert.That(section, Has.Count.EqualTo(2));
        }

        [Test]
        public void AddMultipleKeys2()
        {
            IniSection section = new("header") {
                { "key1", "value" },
                { "key2", "value" }
            };
            Assert.That(section, Has.Count.EqualTo(2));
        }

        [Test]
        public void ClearKeys1()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            section.Clear();
            Assert.That(section, Is.Empty);

            int count = 0;
            foreach (KeyValuePair<string, string> item in section) {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void ClearKeys2()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };
            section.Clear();
            Assert.That(section, Is.Empty);

            int count = 0;
            foreach (KeyValuePair<string, string> item in section) {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void RemoveKeyFromEmpty()
        {
            IniSection section = new("header");
            Assert.That(section.Remove("key"), Is.False);
        }

        [Test]
        public void RemoveKeyLastItem()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(section.Remove("key"), Is.True);
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void RemoveSingleKeyItem1()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value" }
            };
            Assert.That(section.Remove("key"), Is.True);
            Assert.That(section, Has.Count.EqualTo(1));
            Assert.That(section.ContainsKey("key2"), Is.True);
            Assert.That(section.ContainsKey("key"), Is.False);
        }

        [Test]
        public void RemoveSingleKeyItem2()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value" }
            };
            Assert.That(section.Remove("key2"), Is.True);
            Assert.That(section, Has.Count.EqualTo(1));
            Assert.That(section.ContainsKey("key"), Is.True);
            Assert.That(section.ContainsKey("key2"), Is.False);
        }

        [Test]
        public void TryGetKeyValuePairEmpty()
        {
            IniSection section = new("header");
            Assert.That(section.TryGetValue("key", out _), Is.False);
        }

        [Test]
        public void TryGetKeyValuePairSingleCorrect()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(section.TryGetValue("key", out string value), Is.True);
            Assert.That(value, Is.EqualTo("value"));
        }

        [Test]
        public void TryGetKeyValuePairSingleIncorrect()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };
            Assert.That(section.TryGetValue("key2", out _), Is.False);
        }

        [Test]
        public void TryGetKeyValuePairSingleCorrectCaseInsensitive()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };

            Assert.That(section.TryGetValue("KEY", out string value1), Is.True);
            Assert.That(value1, Is.EqualTo("value"));

            Assert.That(section.TryGetValue("Key", out string value2), Is.True);
            Assert.That(value2, Is.EqualTo("value"));
        }

        [Test]
        public void TryGetKeyValuePairMultiIncorrect()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(section.TryGetValue("foo", out _), Is.False);
        }

        [Test]
        public void TryGetKeyValuePairMultiCorrect1()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(section.TryGetValue("key", out string value1), Is.True);
            Assert.That(value1, Is.EqualTo("value"));

            Assert.That(section.TryGetValue("key2", out string value2), Is.True);
            Assert.That(value2, Is.EqualTo("value2"));
        }

        [Test]
        public void EmptyKeyCollection()
        {
            IniSection section = new("header");
            Assert.That(section.Keys, Is.Empty);

            int count = 0;
            foreach (string key in section.Keys) {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void SingleKeyInCollection()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };

            int count = 0;
            bool keyFound = false;
            foreach (string key in section.Keys) {
                count++;
                if (key.Equals("key", StringComparison.InvariantCulture)) keyFound = true;
            }

            Assert.That(count, Is.EqualTo(1));
            Assert.That(keyFound, Is.True);
        }

        [Test]
        public void SingleKeyInCollectionCaseSensitive()
        {
            IniSection section = new("header") {
                { "KEY", "value" }
            };

            int count = 0;
            bool keyFound = false;
            foreach (string key in section.Keys) {
                count++;
                if (key.Equals("KEY", StringComparison.InvariantCulture)) keyFound = true;
            }

            Assert.That(count, Is.EqualTo(1));
            Assert.That(keyFound, Is.True);
        }

        [Test]
        public void KeyCollectionReadOnly()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            ICollection<string> keys = section.Keys;
            Assert.That(keys.IsReadOnly, Is.True);
            Assert.That(() => { keys.Add("foo"); }, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => { keys.Clear(); }, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => { keys.Remove("key"); }, Throws.TypeOf<NotSupportedException>());
            Assert.That(() => { keys.Remove("foo"); }, Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void KeyCollectionContains()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            ICollection<string> keys = section.Keys;
            Assert.That(keys.Contains("key2"), Is.True);
            Assert.That(keys.Contains("key"), Is.True);
            Assert.That(keys.Contains("keY"), Is.True);
            Assert.That(keys.Contains("kEy"), Is.True);
            Assert.That(keys.Contains("kEY"), Is.True);
            Assert.That(keys.Contains("Key"), Is.True);
            Assert.That(keys.Contains("KeY"), Is.True);
            Assert.That(keys.Contains("KEy"), Is.True);
            Assert.That(keys.Contains("KEY"), Is.True);
            Assert.That(keys.Contains("foo"), Is.False);
        }

        [Test]
        public void KeyCollectionEnumerator()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            HashSet<string> foundKeys = new();
            ICollection<string> keys = section.Keys;
            foreach (string key in keys) {
                foundKeys.Add(key);
            }

            Assert.That(foundKeys, Has.Member("key"));
            Assert.That(foundKeys, Has.Member("key2"));
        }

        [Test]
        public void KeyCollectionCopyToEmpty1()
        {
            IniSection section = new("header");
            string[] keys = new string[1];
            keys[0] = "foo";

            section.Keys.CopyTo(keys, 0);
            Assert.That(keys[0], Is.EqualTo("foo"));
        }

        [Test]
        public void KeyCollectionCopyToEmpty2()
        {
            IniSection section = new("header");
            string[] keys = new string[1];

            Assert.That(() => {
                // Even though there is no space in the array (length=1, index=1), no exception should be raised as
                // there is nothing to copy.
                section.Keys.CopyTo(keys, 1);
            }, Throws.Nothing);
        }

        [Test]
        public void KeyCollectionCopyTo()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] keys = new string[2];
            section.Keys.CopyTo(keys, 0);

            // The ordering is undefined
            Assert.That(keys[0], Is.EqualTo("key").Or.EqualTo("key2"));
            Assert.That(keys[1], Is.EqualTo("key").Or.EqualTo("key2"));
            Assert.That(keys[0], Is.Not.EqualTo(keys[1]));
        }

        [Test]
        public void KeyCollectionCopyTo2()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] keys = new string[5];
            keys[0] = "a";
            keys[1] = "b";
            keys[2] = "c";
            keys[3] = "d";
            keys[4] = "e";
            section.Keys.CopyTo(keys, 3);

            // The ordering is undefined
            Assert.That(keys[3], Is.EqualTo("key").Or.EqualTo("key2"));
            Assert.That(keys[4], Is.EqualTo("key").Or.EqualTo("key2"));
            Assert.That(keys[3], Is.Not.EqualTo(keys[4]));
            Assert.That(keys[0], Is.EqualTo("a"));
            Assert.That(keys[1], Is.EqualTo("b"));
            Assert.That(keys[2], Is.EqualTo("c"));
        }

        [Test]
        public void KeyCollectionCopyToBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] keys = new string[5];
            Assert.That(
                () => {
                    section.Keys.CopyTo(keys, 4);
                }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void KeyCollectionCopyToArrayBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] keys = new string[5];
            Assert.That(
                () => {
                    section.Keys.CopyTo(keys, 10);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => {
                    section.Keys.CopyTo(keys, -1);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void KeyCollectionCopyToNull()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(
                () => {
                    section.Keys.CopyTo(null, 7);
                }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void EmptyValueCollection()
        {
            IniSection section = new("header");
            Assert.That(section.Values, Is.Empty);

            int count = 0;
            foreach (string value in section.Values) {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void SingleKeyValueInCollection()
        {
            IniSection section = new("header") {
                { "key", "value" }
            };

            int count = 0;
            bool valueFound = false;
            foreach (string value in section.Values) {
                count++;
                if (value.Equals("value", StringComparison.InvariantCulture)) valueFound = true;
            }

            Assert.That(count, Is.EqualTo(1));
            Assert.That(valueFound, Is.True);
        }

        [Test]
        public void ValueCollectionCopyToBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] values = new string[5];
            Assert.That(
                () => {
                    section.Values.CopyTo(values, 4);
                }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ValueCollectionCopyToArrayBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            string[] values = new string[5];
            Assert.That(
                () => {
                    section.Values.CopyTo(values, 10);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => {
                    section.Values.CopyTo(values, -1);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ValueCollectionCopyToNull()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(
                () => {
                    section.Values.CopyTo(null, 7);
                }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void KeyValuePrivateCopyTo()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[2];
            ((IDictionary<string, string>)section).CopyTo(array, 0);

            var copiedKeys = from item in array select item.Key;
            Assert.That(copiedKeys, Has.Member("key"));
            Assert.That(copiedKeys, Has.Member("key2"));
        }

        [Test]
        public void KeyValuePrivateCopyTo2()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[8];
            ((IDictionary<string, string>)section).CopyTo(array, 0);

            var copiedKeys = from item in array select item.Key;
            Assert.That(copiedKeys, Has.Member("key"));
            Assert.That(copiedKeys, Has.Member("key2"));
        }

        [Test]
        public void KeyValuePrivateCopyTo3()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[8];
            ((IDictionary<string, string>)section).CopyTo(array, 6);

            var copiedKeys = from item in array select item.Key;
            Assert.That(copiedKeys, Has.Member("key"));
            Assert.That(copiedKeys, Has.Member("key2"));
        }

        [Test]
        public void KeyValuePrivateCopyToBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[8];
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).CopyTo(array, 7);
                }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void KeyValuePrivateCopyToArrayBoundaries()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[8];
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).CopyTo(array, 10);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).CopyTo(array, -1);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void KeyValuePrivateCopyToNull()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(
                () => {
                    ((IDictionary<string, string>)section).CopyTo(null, 7);
                }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ItemGetter()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(section["key"], Is.EqualTo("value"));
            Assert.That(section["KEY"], Is.EqualTo("value"));
            Assert.That(section["KEY2"], Is.EqualTo("value2"));
        }

        [Test]
        public void ItemGetterMissing()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            Assert.That(
                () => {
                    _ = section["foo"];
                }, Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void ItemGetterNullKey()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    _ = section[null];
                }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ItemSetterNullKey()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    section[null] = "value";
                }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ItemSetterNullValue()
        {
            IniSection section = new("header");
            Assert.That(
                () => {
                    section["key"] = null;
                }, Throws.TypeOf<ArgumentNullException>());
            Assert.That(section, Is.Empty);
        }

        [Test]
        public void ItemSetterNewKey()
        {
            IniSection section = new("header") {
                ["key"] = "value"
            };
            Assert.That(section, Has.Count.EqualTo(1));
            Assert.That(section["key"], Is.EqualTo("value"));
        }

        [Test]
        public void PrivateEnumerator()
        {
            IniSection section = new("header") {
                { "key", "value" },
                { "key2", "value2" }
            };

            HashSet<string> foundKeys = new();
            foreach (KeyValuePair<string, string> item in section) {
                foundKeys.Add(item.Key);
            }

            Assert.That(foundKeys, Has.Count.EqualTo(2));
            Assert.That(foundKeys, Has.Member("key"));
            Assert.That(foundKeys, Has.Member("key2"));
        }

        [Test]
        [Category("ManualTest")]
        public void DictionaryTest()
        {
            // For information about this test, see
            //  http://www.nimaara.com/2016/03/06/beware-of-the-idictionary-tkey-tvalue/
            // Results in HELIOS-672

            int gc0 = GC.CollectionCount(0);
            int gc1 = GC.CollectionCount(1);
            int gc2 = GC.CollectionCount(2);
            var result = new System.Text.StringBuilder();
            result.AppendFormat("[Gen-0]\t{0}", GC.CollectionCount(0));
            result.AppendLine();
            result.AppendFormat("[Gen-1]\t{0}", GC.CollectionCount(1));
            result.AppendLine();
            result.AppendFormat("[Gen-2]\t{0}", GC.CollectionCount(2));

            Console.WriteLine(result.ToString());

            IniSection dic = new("new");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < 100000000; i++) {
                foreach (var item in dic) { /* Iterate */ }
            }
            sw.Stop();

            // compose the result
            result = new System.Text.StringBuilder();
            result.AppendFormat("[Time]\t{0}", sw.Elapsed);
            result.AppendLine();
            result.AppendFormat("[Gen-0]\t{0}", GC.CollectionCount(0) - gc0);
            result.AppendLine();
            result.AppendFormat("[Gen-1]\t{0}", GC.CollectionCount(1) - gc1);
            result.AppendLine();
            result.AppendFormat("[Gen-2]\t{0}", GC.CollectionCount(2) - gc2);

            Console.WriteLine(result.ToString());

            Assert.Pass();
        }
    }
}
