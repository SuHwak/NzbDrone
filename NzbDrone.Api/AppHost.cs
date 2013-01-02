﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Funq;
using Ninject;
using NzbDrone.Api.QualityProfiles;
using NzbDrone.Api.QualityType;
using ServiceStack.ContainerAdapter.Ninject;
using ServiceStack.WebHost.Endpoints;
using QualityProfileService = NzbDrone.Api.QualityProfiles.QualityProfileService;

namespace NzbDrone.Api
{
    public class AppHost : AppHostBase
    {
        private IKernel _kernel;

        public AppHost(IKernel kernel) //Tell ServiceStack the name and where to find your web services
            : base("NzbDrone API", typeof(QualityProfileService).Assembly)
        {
            _kernel = kernel;
        }

        public override void Configure(Container container)
        {
            container.Adapter = new NinjectContainerAdapter(_kernel);
            SetConfig(new EndpointHostConfig { ServiceStackHandlerFactoryPath = "api" });

            Routes
                .Add<QualityProfileModel>("/qualityprofiles")
                .Add<QualityProfileModel>("/qualityprofiles/{Id}")
                .Add<QualityTypeModel>("/qualitytypes")
                .Add<QualityTypeModel>("/qualitytypes/{Id}");

            Bootstrapper.Initialize();
        }
    }
}