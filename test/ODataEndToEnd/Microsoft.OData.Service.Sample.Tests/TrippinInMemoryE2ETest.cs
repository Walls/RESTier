﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinInMemoryE2ETest : E2ETestBase<TrippinDataServiceContext>, IClassFixture<TrippinServiceFixture>
    {
        private const string baseUri = "http://localhost:21248/api/Trippin/";

        public TrippinInMemoryE2ETest()
            : base(new Uri(baseUri))
        {
        }

        [Fact]
        public void TestMetadata()
        {
            TestGetPayloadContains("$metadata", "<EntitySet Name=\"People\"");
        }

        [Fact]
        public void TestEntitySet()
        {
            TestGetPayloadContains("People", "FirstName");
        }

        [Fact]
        public void TestImperativeViewEntitySet()
        {
            TestGetPayloadContains("NewComePeople", "FirstName");
        }

        [Fact]
        public void TestCollectionOfPrimitivePropertyAccess()
        {
            TestGetPayloadContains("People(1)/Emails",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(Edm.String)\"");
            TestGetPayloadContains("People(7)/Emails",
                "\"value\":[");
        }

        [Fact]
        public void TestCollectionOfComplexPropertyAccess()
        {
            TestGetPayloadContains("People(1)/Locations",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(" +
                "Microsoft.OData.Service.Sample.TrippinInMemory.Models.Location)\"");
        }

        [Fact]
        public void TestCollectionOfEnumPropertyAccess()
        {
            TestGetPayloadContains("People(1)/Features",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(" +
                "Microsoft.OData.Service.Sample.TrippinInMemory.Models.Feature)\"");
        }

        [Fact]
        public void TestEnumPropertyAccess()
        {
            TestGetPayloadContains("People(1)/FavoriteFeature",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#People(1)/FavoriteFeature");
        }

        [Fact]
        public void TestRawValuedEnumPropertyAccess()
        {
            TestGetPayloadIs("People(1)/FavoriteFeature/$value", "Feature1");
        }

        [Fact]
        public void TestCountCollectionOfStructuralProperty()
        {
            TestGetPayloadIs("People(1)/Emails/$count", "2");
            TestGetPayloadIs("People(1)/Locations/$count", "2");
            TestGetPayloadIs("People(1)/Features/$count", "2");
        }

        [Fact]
        public void TestAutoExpandedNavigationProperty()
        {
            TestGetPayloadContains("People", "\"Friends\":[");
        }

        [Theory]
        // Single primitive property with null value 
        [InlineData("/People(5)/MiddleName", 204)]
        // Single primitive property $value with null value 
        [InlineData("/People(5)/MiddleName/$value", 204)]
        // Collection of primitive property with empty value 
        [InlineData("/People(5)/Emails", 200)]
        // Collection of primitive property $value with null value, should throw exception
        // TODO should be bad request 400 as this is not allowed, 404 is returned by WebApi Route Match method
        [InlineData("/People(5)/Emails/$value", 404)]
        // Collection of primitive property with null collection
        [InlineData("/People(7)/Emails", 200)]
        // single complex property with null value
        [InlineData("/People(5)/HomeAddress", 204)]
        // single complex property's propery and complex property has null value
        [InlineData("/People(5)/HomeAddress/Address", 404)]
        // single complex property's property with null value 
        [InlineData("/People(6)/HomeAddress/Address", 204)]
        // collection of complex property with empty collection value
        [InlineData("/People(5)/Locations", 200)]
        // collection of complex property's propery and collection of complex property has null value
        // TODO should be bad request 400 as this is not allowed, 404 is returned by WebApi Route Match method
        [InlineData("/People(5)/Locations/Address", 404)]
        // Collection of complex property with null collection
        [InlineData("/People(7)/Locations", 200)]
        // single navigation property with null value
        [InlineData("/People(5)/BestFriend", 204)]
        // single navigation property's propery and navigation property has null value
        [InlineData("/People(5)/BestFriend/MiddleName", 404)]
        // single navigation property's property with null value
        [InlineData("/People(6)/BestFriend/MiddleName", 204)]
        // collection of navigation property with empty collection value
        [InlineData("/People(5)/Friends", 200)]
        // collection of navigation property with null collection value
        [InlineData("/People(7)/Trips", 200)]
        // collection of navigation property's property and navigation property has null value
        // TODO should be bad request 400 as this is not allowed, 404 is returned by WebApi Route Match method
        [InlineData("/People(5)/Friends/MiddleName", 404)]
        public void QueryPropertyWithNullValueStatusCode(string url, int expectedCode)
        {
            TestGetStatusCodeIs(url, expectedCode);
        }

        [Theory]
        // Single primitive property
        [InlineData("/People(15)/MiddleName", 404)]
        // Single primitive property $value
        [InlineData("/People(15)/MiddleName/$value", 404)]
        // Collection of primitive property 
        [InlineData("/People(15)/Emails", 404)]
        // Collection of primitive property $value
        // TODO should be bad request 400 as this is not allowed, 404 is returned by WebApi Route Match method
        [InlineData("/People(15)/Emails/$value", 404)]
        // single complex property
        [InlineData("/People(15)/HomeAddress", 404)]
        // single complex property's property
        [InlineData("/People(15)/HomeAddress/Address", 404)]
        // collection of complex property
        [InlineData("/People(15)/Locations", 404)]
        // collection of complex property's propery
        // TODO should be bad request 400 as this is not allowed?? 404 is returned by WebApi Route Match method
        [InlineData("/People(15)/Locations/Address", 404)]
        // single navigation property
        [InlineData("/People(15)/BestFriend", 404)]
        // single navigation property's propery
        [InlineData("/People(15)/BestFriend/MiddleName", 404)]
        // collection of navigation property
        [InlineData("/People(15)/Friends", 404)]
        // collection of navigation property's property
        // TODO should be bad request 400 as this is not allowed, 404 is returned by WebApi Route Match method
        [InlineData("/People(15)/Friends/MiddleName", 404)]
        public void QueryPropertyWithNonExistEntity(string url, int expectedCode)
        {
            TestGetStatusCodeIs(url, expectedCode);
        }
    }
}
