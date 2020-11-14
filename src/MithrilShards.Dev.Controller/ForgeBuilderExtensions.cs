using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MithrilShards.Core.Forge;

namespace MithrilShards.Dev.Controller
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Uses the bitcoin chain.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <param name="assemblyScaffoldEnabler">Action to wake up assembly that doesn't have an entry point, allowing to discover Dev Controllers in that assembly.
      /// Useful to include these assemblies that didn't have an entry point and wouldn't be loaded.</param>
      /// <param name="configurationFile">The configuration file.</param>
      /// <returns></returns>
      public static IForgeBuilder UseDevController(this IForgeBuilder forgeBuilder, Action<DevAssemblyScaffolder> assemblyScaffoldEnabler = null, string? configurationFile = null)
      {
         var scaffolder = new DevAssemblyScaffolder();
         assemblyScaffoldEnabler?.Invoke(scaffolder);

         forgeBuilder.AddShard<DevControllerShard, DevControllerSettings>(
            (hostBuildContext, services) =>
            {
               services.AddSingleton<DevAssemblyScaffolder>(scaffolder);
            });

         return forgeBuilder;
      }
   }
}