//using System;
//using System.Collections.Generic;
//using System.Text;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Options;

//// https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-asp-net-core/
//// https://github.com/dotnet/runtime/issues/36391

////namespace MithrilShards.Core.MithrilShards
////{
////   public class SettingValidationStartupFilter : IStartupFilter
////   {
////      readonly IEnumerable<IValidatable> _validatableObjects;
////      public SettingValidationStartupFilter(IEnumerable<IValidatable> validatableObjects)
////      {
////         _validatableObjects = validatableObjects;
////      }

////      public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
////      {
////         foreach (var validatableObject in _validatableObjects)
////         {
////            validatableObject.Validate();
////         }

////         //don't alter the configuration
////         return next;
////      }
////   }

//public static class OptionsBuilderValidationExtensions
//{
//   public static IHost ValidateOptions<T>(this IHost host)
//   {
//      object options = host.Services.GetService(typeof(IOptions<>).MakeGenericType(typeof(T)));
//      if (options != null)
//      {
//         // Retrieve the value to trigger validation
//         var optionsValue = ((IOptions<object>)options).Value;
//      }
//      return host;
//   }
//}