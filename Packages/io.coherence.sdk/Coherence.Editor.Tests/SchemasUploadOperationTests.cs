// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor.Tests
{
    using System;
    using Coherence.Tests;
    using NUnit.Framework;
    using Portal;

    /// <summary>
    /// Tests for uploading schemas to coherence Developer Portal.
    /// </summary>
    public sealed class SchemasUploadOperationTests : CoherenceTest
    {
        /// <summary>
        /// A string that is neither null nor empty.
        /// </summary>
        private const string NonEmptyString = " ";

        /// <summary>
        /// When the tests start running, the value of <see cref="PortalLogin.OrganizationsFetched"/> is cached here,
        /// and once all the tests have finished running, the value is restored back to it original state
        /// (to avoid these test having side effects that could break other tests etc.).
        /// </summary>
        private Organization[] portalLoginOrganizationsPreviousValue;

        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            portalLoginOrganizationsPreviousValue = PortalLogin.OrganizationsFetched ? PortalLogin.organizations : null;
        }

        public override void SetUp()
        {
            base.SetUp();
            PortalLogin.DiscardFetchedOrganizations();
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            if (portalLoginOrganizationsPreviousValue is null)
            {
                PortalLogin.DiscardFetchedOrganizations();
            }
            else
            {
                PortalLogin.SetOrganizations(portalLoginOrganizationsPreviousValue);
                portalLoginOrganizationsPreviousValue = null;
            }

            base.OneTimeTearDown();
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingSchemaID_IfSchemaIDIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, null);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingSchemaID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingSchemaID_IfSchemaIDIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, "");
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingSchemaID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldNotFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, null, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldNotFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, "", NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldNotFail_WithReason_MissingPortalAndLoginTokens_IfLoginTokenIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, null, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldNotFail_WithReason_MissingPortalAndLoginTokens_IfLoginTokenIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, null, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsNull_And_LoginTokenIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, null, null, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsEmpty_And_LoginTokenIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, "", "", NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsNull_And_LoginTokenIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, null, "", NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingPortalAndLoginTokens_IfPortalTokenIsEmpty_And_LoginTokenIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, "", null, NonEmptyString, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingPortalAndLoginTokens;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingOrganizationID_IfOrganizationIDIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, null, NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingOrganizationID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingOrganizationID_IfOrganizationIDIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, "", NonEmptyString, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingOrganizationID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingProjectID_IfProjectIDIsNull()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, null, NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingProjectID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_MissingProjectID_IfProjectIDIsEmpty()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, "", NonEmptyString);
            var expected = SchemasUploadOperation.FailReason.MissingProjectID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldFail_WithReason_InvalidOrganizationID_IfOrganizationIdNotFoundAmongFetchedOrganizations()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            PortalLogin.SetOrganizations(Array.Empty<Organization>());
            var expected = SchemasUploadOperation.FailReason.InvalidOrganizationID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Upload_ShouldNotFail_WithReason_InvalidOrganizationID_IfOrganizationsHaveNotBeenFetched()
        {
            var schemas = new Schemas(new Schema[] { }, NonEmptyString);
            var uploadOperation = new SchemasUploadOperation(schemas, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString, NonEmptyString);
            PortalLogin.DiscardFetchedOrganizations();
            var expected = SchemasUploadOperation.FailReason.InvalidOrganizationID;

            var actual = uploadOperation.Upload().FailReason;

            Assert.That(actual, Is.Not.EqualTo(expected));
        }
    }
}
