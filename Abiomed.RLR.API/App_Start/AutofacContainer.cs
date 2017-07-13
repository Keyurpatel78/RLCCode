﻿/*
 * Remote Link - Copyright 2017 ABIOMED, Inc.
 * --------------------------------------------------------
 * Description:
 * AutofacContainer.cs: Autofac Container for RLR
 * --------------------------------------------------------
 * Author: Alessandro Agnello 
*/
using Abiomed.Business;
using Abiomed.RLR.Communications;
using Abiomed.Models;
using Abiomed.Repository;
using Abiomed.Storage;
using Autofac;
using Autofac.Integration.WebApi;
using System.Reflection;
using System.Web.Http;

namespace Abiomed.RLR.API
{

    public class AutofacContainer
    {
        public static IContainer Container { get; set; }

        public AutofacContainer()
        {

            var builder = new ContainerBuilder();

            builder.RegisterType<MongoRepository>().As<IMongoDbRepository>();

            builder.RegisterType<TCPServer>().As<ITCPServer>();
            builder.RegisterType<RLMCommunication>().As<IRLMCommunication>();
            builder.RegisterType<RLMDeviceList>();
            builder.RegisterType<DataRetrieval>().As<IDataRetrieval>();
            builder.RegisterType<BlobStorage>().As<IBlobStorage>();
            builder.RegisterType<TableStorage>().As<ITableStorage>();
            builder.RegisterType<ImageManager>().As<IImageManager>();

            // Get your HttpConfiguration.
            var config = GlobalConfiguration.Configuration;

            // Register your Web API controllers.
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // OPTIONAL: Register the Autofac filter provider.
            builder.RegisterWebApiFilterProvider(config);

            // Set the dependency resolver to be Autofac.
            Container = builder.Build();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Container);            
        }
    }
}