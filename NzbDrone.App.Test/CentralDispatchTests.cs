﻿using Autofac;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class CentralDispatchTests : TestBase
    {
        [Test]
        public void Kernel_can_get_kernel()
        {
            CentralDispatch.Container.Should().NotBeNull();
        }

        [Test]
        public void Kernel_should_return_same_kernel()
        {
            var firstKernel = CentralDispatch.Container;
            var secondKernel = CentralDispatch.Container;

            firstKernel.Should().BeSameAs(secondKernel);
        }

        [Test]
        public void Kernel_should_be_able_to_resolve_ApplicationServer()
        {
            var appServer = CentralDispatch.Container.Resolve<ApplicationServer>();

            appServer.Should().NotBeNull();
        }

        [Test]
        public void Kernel_should_resolve_same_ApplicationServer_instance()
        {
            var appServer1 = CentralDispatch.Container.Resolve<ApplicationServer>();
            var appServer2 = CentralDispatch.Container.Resolve<ApplicationServer>();

            appServer1.Should().BeSameAs(appServer2);
        }

    }
}
