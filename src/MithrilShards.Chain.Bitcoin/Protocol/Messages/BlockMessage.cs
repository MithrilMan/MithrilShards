using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// The headers packet returns block headers in response to a getheaders packet.
   /// </summary>
   /// <seealso cref="INetworkMessage" />
   [NetworkMessage(COMMAND)]
   public sealed class BlockMessage : ConfigurableNetworkMessageBase
   {
      private const string COMMAND = "block";

      protected override string Command => COMMAND;

      public Block? Block { get; set; }



      public BlockMessage SetSerializerOption(bool useWitness)
      {
         this.SetSerializationOptions((SerializerOptions.SERIALIZE_WITNESS, useWitness));

         return this;
      }
   }
}