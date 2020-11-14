using System;

namespace MithrilShards.Dev.Controller
{
   /// <summary>
   /// Attribute that has to be put on controller that has to be included into the swagger exposed by MithrilShards.Dev.Controller shard.
   /// </summary>
   /// <seealso cref="System.Attribute" />
   [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
   public sealed class DevControllerAttribute : Attribute
   {
   }
}
