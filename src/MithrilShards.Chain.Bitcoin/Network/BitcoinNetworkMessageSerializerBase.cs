using System;
using System.Buffers;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public abstract class BitcoinNetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType> where TMessageType : INetworkMessage, new()
   {
      public BitcoinNetworkMessageSerializerBase(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, IPeerContext peerContext)
      {
         return this.Deserialize(ref reader, protocolVersion, (BitcoinPeerContext)peerContext);
      }

      public override void Serialize(TMessageType message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output)
      {
         Serialize(message, protocolVersion, (BitcoinPeerContext)peerContext, output);
      }

      public abstract TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext);

      public abstract void Serialize(TMessageType message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output);
   }
}