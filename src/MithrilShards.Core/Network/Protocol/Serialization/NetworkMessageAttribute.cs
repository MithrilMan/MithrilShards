using System;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   /// <summary>
   /// Enables the mapping between message commands payload and defined protocol messages.
   /// </summary>
   [AttributeUsage(AttributeTargets.Class)]
   public class NetworkMessageAttribute : Attribute
   {
      const int MAX_COMMAND_LENGTH = 12;
      /// <summary>
      /// The command name.
      /// </summary>
      public string Command { get; }

      /// <summary>
      /// Initialize a new instance of the object.
      /// </summary>
      /// <param name="commandName"></param>
      public NetworkMessageAttribute(string commandName)
      {
         if (commandName.Length > MAX_COMMAND_LENGTH)
         {
            throw new ArgumentException($"Protocol violation: command name is limited to {MAX_COMMAND_LENGTH} characters.");
         }

         this.Command = commandName;
      }
   }
}
