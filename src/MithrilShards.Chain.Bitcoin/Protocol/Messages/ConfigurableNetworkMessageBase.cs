using System.Collections.Generic;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   public abstract class ConfigurableNetworkMessageBase : INetworkMessage
   {
      private Dictionary<string, object>? _serializationOptions;

      protected abstract string Command { get; }

      string INetworkMessage.Command => this.Command;

      protected void SetSerializationOptions(params (string Key, object Value)[] options)
      {
         if (this._serializationOptions != null)
         {
            this._serializationOptions.Clear();
         }

         if ((options?.Length ?? 0) == 0) return;

         this._serializationOptions ??= new Dictionary<string, object>();

         foreach ((string Key, object Value) option in options!)
         {
            this._serializationOptions.Add(option.Key, option.Value);
         }
      }

      public void PopulateSerializerOption(ref ProtocolTypeSerializerOptions? options)
      {
         if (this._serializationOptions == null) return;

         options ??= new ProtocolTypeSerializerOptions();

         foreach (KeyValuePair<string, object> option in this._serializationOptions)
         {
            options.Set(option.Key, option.Value);
         }
      }
   }
}
