using Sentinel.Services.CaseDefinitionEvaluation;
using Xunit;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class FieldResolverTests
    {
        private readonly FieldResolver _resolver;

        public FieldResolverTests()
        {
            _resolver = new FieldResolver();
        }

        #region Test Models

        private class TestPerson
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public int Age { get; set; }
            public TestAddress? Address { get; set; }
            public List<TestContact> Contacts { get; set; } = new();
        }

        private class TestAddress
        {
            public string City { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public TestCountry? Country { get; set; }
        }

        private class TestCountry
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        private class TestContact
        {
            public string Type { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        #endregion

        #region Simple Property Tests

        [Fact]
        public void ResolveFieldValue_SimpleProperty_ReturnsValue()
        {
            var person = new TestPerson { FirstName = "John" };
            var result = _resolver.ResolveFieldValue(person, "FirstName");

            Assert.Equal("John", result);
        }

        [Fact]
        public void ResolveFieldValue_IntegerProperty_ReturnsValue()
        {
            var person = new TestPerson { Age = 30 };
            var result = _resolver.ResolveFieldValue(person, "Age");

            Assert.Equal(30, result);
        }

        [Fact]
        public void ResolveFieldValue_CaseInsensitive_ReturnsValue()
        {
            var person = new TestPerson { FirstName = "John" };
            var result = _resolver.ResolveFieldValue(person, "firstname");

            Assert.Equal("John", result);
        }

        #endregion

        #region Nested Property Tests

        [Fact]
        public void ResolveFieldValue_NestedProperty_ReturnsValue()
        {
            var person = new TestPerson
            {
                Address = new TestAddress { City = "Sydney" }
            };

            var result = _resolver.ResolveFieldValue(person, "Address.City");

            Assert.Equal("Sydney", result);
        }

        [Fact]
        public void ResolveFieldValue_DeeplyNestedProperty_ReturnsValue()
        {
            var person = new TestPerson
            {
                Address = new TestAddress
                {
                    Country = new TestCountry { Name = "Australia" }
                }
            };

            var result = _resolver.ResolveFieldValue(person, "Address.Country.Name");

            Assert.Equal("Australia", result);
        }

        [Fact]
        public void ResolveFieldValue_NestedPropertyNull_ReturnsNull()
        {
            var person = new TestPerson { Address = null };
            var result = _resolver.ResolveFieldValue(person, "Address.City");

            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_IntermediatePropertyNull_ReturnsNull()
        {
            var person = new TestPerson
            {
                Address = new TestAddress { Country = null }
            };

            var result = _resolver.ResolveFieldValue(person, "Address.Country.Name");

            Assert.Null(result);
        }

        #endregion

        #region Null/Empty Handling Tests

        [Fact]
        public void ResolveFieldValue_NullSource_ReturnsNull()
        {
            var result = _resolver.ResolveFieldValue(null, "FirstName");
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_EmptyPath_ReturnsNull()
        {
            var person = new TestPerson();
            var result = _resolver.ResolveFieldValue(person, "");
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_WhitespacePath_ReturnsNull()
        {
            var person = new TestPerson();
            var result = _resolver.ResolveFieldValue(person, "   ");
            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_InvalidProperty_ReturnsNull()
        {
            var person = new TestPerson();
            var result = _resolver.ResolveFieldValue(person, "NonExistentProperty");
            Assert.Null(result);
        }

        #endregion

        #region Collection Tests

        [Fact]
        public void ResolveCollectionValues_SimpleCollection_ReturnsAllItems()
        {
            var person = new TestPerson
            {
                Contacts = new List<TestContact>
                {
                    new TestContact { Type = "Email" },
                    new TestContact { Type = "Phone" }
                }
            };

            var results = _resolver.ResolveCollectionValues(person, "Contacts");

            Assert.Equal(2, results.Count);
            Assert.IsType<TestContact>(results[0]);
        }

        [Fact]
        public void ResolveCollectionValues_WithSubPath_ReturnsPropertyValues()
        {
            var person = new TestPerson
            {
                Contacts = new List<TestContact>
                {
                    new TestContact { Type = "Email", Value = "test@example.com" },
                    new TestContact { Type = "Phone", Value = "123-456-7890" }
                }
            };

            var results = _resolver.ResolveCollectionValues(person, "Contacts", "Value");

            Assert.Equal(2, results.Count);
            Assert.Contains("test@example.com", results.Select(r => r?.ToString()));
            Assert.Contains("123-456-7890", results.Select(r => r?.ToString()));
        }

        [Fact]
        public void ResolveCollectionValues_EmptyCollection_ReturnsEmptyList()
        {
            var person = new TestPerson { Contacts = new List<TestContact>() };
            var results = _resolver.ResolveCollectionValues(person, "Contacts");

            Assert.Empty(results);
        }

        [Fact]
        public void ResolveCollectionValues_NullCollection_ReturnsEmptyList()
        {
            var person = new TestPerson();
            person.Contacts = null!;
            var results = _resolver.ResolveCollectionValues(person, "Contacts");

            Assert.Empty(results);
        }

        #endregion

        #region CollectionContains Tests

        [Fact]
        public void CollectionContains_ValueExists_ReturnsTrue()
        {
            var person = new TestPerson
            {
                Contacts = new List<TestContact>
                {
                    new TestContact { Type = "Email" },
                    new TestContact { Type = "Phone" }
                }
            };

            var result = _resolver.CollectionContains(person, "Contacts", "Type", "Email");

            Assert.True(result);
        }

        [Fact]
        public void CollectionContains_ValueNotExists_ReturnsFalse()
        {
            var person = new TestPerson
            {
                Contacts = new List<TestContact>
                {
                    new TestContact { Type = "Email" }
                }
            };

            var result = _resolver.CollectionContains(person, "Contacts", "Type", "Fax");

            Assert.False(result);
        }

        [Fact]
        public void CollectionContains_CaseInsensitive_ReturnsTrue()
        {
            var person = new TestPerson
            {
                Contacts = new List<TestContact>
                {
                    new TestContact { Type = "Email" }
                }
            };

            var result = _resolver.CollectionContains(person, "Contacts", "Type", "EMAIL");

            Assert.True(result);
        }

        [Fact]
        public void CollectionContains_EmptyCollection_ReturnsFalse()
        {
            var person = new TestPerson { Contacts = new List<TestContact>() };
            var result = _resolver.CollectionContains(person, "Contacts", "Type", "Email");

            Assert.False(result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ResolveFieldValue_TrailingDot_ReturnsNull()
        {
            var person = new TestPerson { FirstName = "John" };
            var result = _resolver.ResolveFieldValue(person, "FirstName.");

            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_LeadingDot_ReturnsNull()
        {
            var person = new TestPerson { FirstName = "John" };
            var result = _resolver.ResolveFieldValue(person, ".FirstName");

            Assert.Null(result);
        }

        [Fact]
        public void ResolveFieldValue_MultipleDots_HandlesGracefully()
        {
            var person = new TestPerson
            {
                Address = new TestAddress { City = "Sydney" }
            };

            // Should handle "Address..City" gracefully (empty segment)
            var result = _resolver.ResolveFieldValue(person, "Address..City");

            // Should return null due to invalid path
            Assert.Null(result);
        }

        #endregion
    }
}
