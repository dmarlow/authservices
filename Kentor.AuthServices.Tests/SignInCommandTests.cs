﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net;
using System.Web;
using System.Linq;
using NSubstitute;
using System.IO.Compression;
using System.IO;
using System.Xml.Linq;
using Kentor.AuthServices.TestHelpers;

namespace Kentor.AuthServices.Tests
{
    [TestClass]
    public class SignInCommandTests
    {
        [TestMethod]
        public void SignInCommand_Run_ReturnsAuthnRequestForDefaultIdp()
        {
            var defaultDestination = IdentityProvider.ActiveIdentityProviders.First()
                .AssertionConsumerServiceUrl;

            var subject = new SignInCommand().Run(new HttpRequestData("GET", new Uri("http://example.com")));

            var expected = new CommandResult()
            {
                HttpStatusCode = HttpStatusCode.SeeOther,
                Cacheability = HttpCacheability.NoCache,
                Location = new Uri(defaultDestination + "?SAMLRequest=XYZ")
            };

            subject.ShouldBeEquivalentTo(expected, options => options.Excluding(cr => cr.Location));
            subject.Location.Host.Should().Be(defaultDestination.Host);

            var queries = HttpUtility.ParseQueryString(subject.Location.Query);

            queries.Should().HaveCount(1);
            queries.Keys[0].Should().Be("SAMLRequest");
            queries[0].Should().NotBeEmpty();
        }

        [TestMethod]
        public void SignInCommand_Run_MapsReturnUrl()
        {
            var defaultDestination = IdentityProvider.ActiveIdentityProviders.First()
                .AssertionConsumerServiceUrl;

            var httpRequest = new HttpRequestData("GET", new Uri("http://localhost/signin?ReturnUrl=/Return.aspx"));

            var subject = new SignInCommand().Run(httpRequest);

            var idp = IdentityProvider.ActiveIdentityProviders.First();

            var authnRequest = idp.CreateAuthenticateRequest(null);

            var requestId = AuthnRequestHelper.GetRequestId(subject.Location);

            StoredRequestState storedAuthnData;
            PendingAuthnRequests.TryRemove(new System.IdentityModel.Tokens.Saml2Id(requestId), out storedAuthnData);

            storedAuthnData.ReturnUri.Should().Be("http://localhost/Return.aspx");
        }

        [TestMethod]
        public void SignInCommand_Run_With_Idp2_ReturnsAuthnRequestForSecondIdp()
        {
            var secondIdp = IdentityProvider.ActiveIdentityProviders.Skip(1).First();
            var secondDestination = secondIdp.AssertionConsumerServiceUrl;
            var secondEntityId = secondIdp.EntityId;

            var request = new HttpRequestData("GET", new Uri("http://sp.example.com?idp=" +
            HttpUtility.UrlEncode(secondEntityId.Id)));
            var subject = new SignInCommand().Run(request);

            subject.Location.Host.Should().Be(secondDestination.Host);
        }

        [TestMethod]
        public void SignInCommand_Run_With_InvalidIdp_ThrowsException()
        {
            var request = new HttpRequestData("GET", new Uri("http://localhost/signin?idp=no-such-idp-in-config"));
            
            Action a = () => new SignInCommand().Run(request);

            a.ShouldThrow<InvalidOperationException>().WithMessage("Unknown idp");
        }

        [TestMethod]
        public void SignInCommand_Run_NullCheck()
        {
            Action a = () => new SignInCommand().Run(null);

            a.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("request");
        }
    }
}
