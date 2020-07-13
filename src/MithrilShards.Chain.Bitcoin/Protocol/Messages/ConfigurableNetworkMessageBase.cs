using System.Collections.Generic;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   public abstract class ConfigurableNetworkMessageBase : INetworkMessage
   {
      private Dictionary<string, object>? serializationOptions;

      protected abstract string Command { get; }

      string INetworkMessage.Command => this.Command;

      protected void SetSerializationOptions(params (string Key, object Value)[] options)
      {
         if (this.serializationOptions != null)
         {
            this.serializationOptions.Clear();
         }

         if ((options?.Length ?? 0) == 0) return;

         this.serializationOptions ??= new Dictionary<string, object>();

         foreach (var option in options!)
         {
            this.serializationOptions.Add(option.Key, option.Value);
         }
      }

      public void PopulateSerializerOption(ref ProtocolTypeSerializerOptions? options)
      {
         if (this.serializationOptions == null) return;

         options ??= new ProtocolTypeSerializerOptions();

         foreach (var option in this.serializationOptions)
         {
            options.Set(option.Key, option.Value);
         }
      }
   }
}
