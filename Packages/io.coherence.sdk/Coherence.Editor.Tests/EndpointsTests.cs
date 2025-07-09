// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.Globalization;
    using System.Threading;
    using Coherence.Tests;
    using NUnit.Framework;

    public class EndpointsTests : CoherenceTest
    {
        private bool useCustomEndpoints;
        private string customAPIDomain;
        private string cachedProjectId;
        private CultureInfo cultureInfo;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            useCustomEndpoints = ProjectSettings.instance.UseCustomEndpoints;
            customAPIDomain = ProjectSettings.instance.CustomAPIDomain;
            cachedProjectId = RuntimeSettings.Instance.ProjectID;
            cultureInfo = Thread.CurrentThread.CurrentCulture;
        }

        [TearDown]
        public override void TearDown()
        {
            ProjectSettings.instance.UseCustomEndpoints = useCustomEndpoints;
            ProjectSettings.instance.CustomAPIDomain = customAPIDomain;
            RuntimeSettings.Instance.SetProjectID(cachedProjectId);
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            base.TearDown();
        }

        [Test]
        [Description("Play property should return the default play endpoint")]
        public void Play_Contains_Standard_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            ProjectSettings.instance.CustomAPIDomain = string.Empty;

            Assert.That(Endpoints.Play, Is.EqualTo($"https://{Endpoints.apiDomain}/{Endpoints.apiVersion}/play"));
        }

        [Test]
        [Description("Play property should return the correct endpoint when a custom domain is set")]
        public void Play_Contains_Supplied_CustomDomain_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "my.custom.domain.com";

            Assert.That(Endpoints.Play,
                Is.EqualTo($"https://{ProjectSettings.instance.CustomAPIDomain}/{Endpoints.apiVersion}/play"));
        }

        [Test]
        [Description("Play property should return the correct endpoint when the custom domain is set to localhost")]
        public void Play_Contains_Port_Number_When_Localhost_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "localhost";

            Assert.That(Endpoints.Play,
                Is.EqualTo($"http://localhost:{Endpoints.localPlayPort}/{Endpoints.apiVersion}/play"));
        }

        [Test]
        [Description("Portal property should return the default play endpoint")]
        public void Portal_Contains_Standard_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            ProjectSettings.instance.CustomAPIDomain = string.Empty;

            Assert.That(Endpoints.Portal, Is.EqualTo($"https://{Endpoints.apiDomain}/{Endpoints.apiVersion}/portal"));
        }

        [Test]
        [Description("Portal property should return the correct endpoint when a custom domain is set")]
        public void Portal_Contains_Supplied_CustomDomain_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "my.custom.domain.com";

            Assert.That(Endpoints.Portal,
                Is.EqualTo($"https://{ProjectSettings.instance.CustomAPIDomain}/{Endpoints.apiVersion}/portal"));
        }

        [Test]
        [Description("Portal property should return the correct endpoint when the custom domain is set to localhost")]
        public void Portal_Contains_Port_Number_When_Localhost_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "localhost";

            Assert.That(Endpoints.Portal,
                Is.EqualTo($"http://localhost:{Endpoints.localPortalPort}/{Endpoints.apiVersion}/portal"));
        }

        [Test]
        [Description("LoginBaseURL property should return the default play endpoint")]
        public void LoginBaseURL_Contains_Standard_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            ProjectSettings.instance.CustomAPIDomain = string.Empty;

            Assert.That(Endpoints.LoginBaseURL, Is.EqualTo($"https://{Endpoints.loginDomain}"));
        }

        [Test]
        [Description("LoginBaseURL property should return the correct endpoint when a custom domain is set")]
        public void LoginBaseURL_Contains_Supplied_CustomDomain_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "my.custom.domain.com";

            Assert.That(Endpoints.LoginBaseURL, Is.EqualTo($"https://{ProjectSettings.instance.CustomAPIDomain}"));
        }

        [Test]
        [Description(
            "LoginBaseURL property should return the correct endpoint when the custom domain is set to localhost")]
        public void LoginBaseURL_Contains_Port_Number_When_Localhost_Url()
        {
            ProjectSettings.instance.UseCustomEndpoints = true;
            ProjectSettings.instance.CustomAPIDomain = "localhost";

            Assert.That(Endpoints.LoginBaseURL, Is.EqualTo($"http://localhost:{Endpoints.localWebPort}"));
        }

        [Test]
        [TestCase(Endpoints.schemasPath)]
        [TestCase(Endpoints.simUploadUrlPath)]
        [TestCase(Endpoints.gameUploadUrlPath)]
        [TestCase(Endpoints.webglUploadUrlPath)]
        [TestCase(Endpoints.registerBuildUrlPath)]
        [TestCase(Endpoints.registerSimUrlPath)]
        [TestCase(Endpoints.regionsPath)]
        [Description("Get should return the correct url for the path")]
        public void Get_Returns_Correct_Url_ForPath(string path)
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            RuntimeSettings.Instance.SetProjectID("0xdeadbeef");
            var actualPath = Endpoints.Get(Endpoints.Portal + path);

            Assert.That(actualPath, Does.Not.Contain("[projectid]"));
            Assert.That(actualPath, Does.Not.Contain("[orgid]"));
        }

        [Test]
        [TestCase(Endpoints.schemasPath)]
        [TestCase(Endpoints.simUploadUrlPath)]
        [TestCase(Endpoints.gameUploadUrlPath)]
        [TestCase(Endpoints.webglUploadUrlPath)]
        [TestCase(Endpoints.registerBuildUrlPath)]
        [TestCase(Endpoints.registerSimUrlPath)]
        [TestCase(Endpoints.regionsPath)]
        [Description("Get should return the correct url for the path with a different culture")]
        public void Get_Returns_Correct_Url_With_Different_Culture_ForPath(string path)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            ProjectSettings.instance.UseCustomEndpoints = false;
            RuntimeSettings.Instance.SetProjectID("0xdeadbeef");
            var actualPath = Endpoints.Get(Endpoints.Portal + path);

            Assert.That(actualPath, Does.Not.Contain("[projectid]"));
            Assert.That(actualPath, Does.Not.Contain("[orgid]"));
        }

        [Test]
        [TestCase(Endpoints.schemasPath)]
        [TestCase(Endpoints.simUploadUrlPath)]
        [TestCase(Endpoints.gameUploadUrlPath)]
        [TestCase(Endpoints.webglUploadUrlPath)]
        [TestCase(Endpoints.registerBuildUrlPath)]
        [TestCase(Endpoints.registerSimUrlPath)]
        [TestCase(Endpoints.regionsPath)]
        [Description("TryGet should return true for valid paths")]
        public void TryGet_Returns_Correct_Url_ForPath(string path)
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            var orgId = "0xdecafbad";
            var projectId = "0xdeadbeef";
            var result = Endpoints.TryGet(Endpoints.Portal + path, orgId, projectId,  out var actualPath, out _);

            Assert.That(result, Is.True);
            Assert.That(actualPath, Does.Not.Contain("[projectid]"));
            Assert.That(actualPath, Does.Not.Contain("[orgid]"));
        }

        [Test]
        [TestCase("/endpoint/[orgid]/method", "", "")]
        [TestCase("/endpoint/[projectid]/method", "", "")]
        [Description("")]
        public void TryGet_Returns_False_For_Bad_Url(string path, string orgId, string projectId)
        {
            ProjectSettings.instance.UseCustomEndpoints = false;
            var result = Endpoints.TryGet(Endpoints.Portal + path, orgId, projectId,  out _, out _);
            Assert.That(result, Is.False);
        }
    }
}
