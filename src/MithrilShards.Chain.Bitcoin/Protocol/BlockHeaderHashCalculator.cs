using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class BlockHeaderHashCalculator : IBlockHeaderHashCalculator
   {
      private readonly IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer;

      public BlockHeaderHashCalculator(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer)
      {
         this.blockHeaderSerializer = blockHeaderSerializer;
      }

      public UInt256 ComputeHash(BlockHeader header, int protocolVersion)
      {
         ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>(80);
         this.blockHeaderSerializer.Serialize(header, protocolVersion, buffer);

         return new UInt256(HashGenerator.DoubleSha256(buffer.WrittenSpan));
      }
   }
}
