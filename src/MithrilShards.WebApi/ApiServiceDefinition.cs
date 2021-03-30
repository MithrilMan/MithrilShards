using System;
using System.Collections.Generic;
using MithrilShards.Core;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MithrilShards.WebApi
{
   public class ApiServiceDefinition
   {
      /// <summary>
      /// The name of the endpoint, used to generate a SwaggerEndpoint.
      /// </summary>
      public string Name { get; set; } = string.Empty;

      /// <summary>
      /// The API service description.
      /// </summary>
      public string? Description { get; set; } = string.Empty;

      /// <summary>
      /// Used to group all controllers that belong to this area (specified by AreaAttribute) in this specific endpoint
      /// </summary>
      public string Area { get; set; } = string.Empty;

      /// <summary>
      /// Gets or sets the Web API service version.
      /// </summary>
      public string Version { get; set; } = "v1";

      /// <summary>
      /// Gets or sets a value indicating whether this <see cref="ApiServiceDefinition"/> is enabled.
      /// </summary>
      /// <value>
      ///   <c>true</c> if enabled; otherwise, <c>false</c>.
      /// </value>
      public bool Enabled { get; set; } = true;

      /// <summary>
      /// Allow to inject custom DocumentFilters into swagger.
      /// </summary>
      public List<IDocumentFilter> DocumentFilters { get; } = new List<IDocumentFilter>();

      /// <summary>
      /// A configuration action that allow to configure SwaggerGen options like adding filters.
      /// SwaggerDoc is already configured, no need to configure it in this action.
      /// </summary>
      public Action<SwaggerGenOptions>? SwaggerGenConfiguration { get; set; }

      public void CheckValidity()
      {
         if (Area.Length == 0)
         {
            ThrowHelper.ThrowArgumentException($"{nameof(Area)} must be specified.");
         }
         if (Version.Length == 0)
         {
            ThrowHelper.ThrowArgumentException($"{nameof(Version)} must be specified.");
         }
         if (Name.Length == 0)
         {
            ThrowHelper.ThrowArgumentException($"{nameof(Name)} must be specified.");
         }
      }
   }
}
