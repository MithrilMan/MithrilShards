using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class BlockHeaderHashCalculator : IBlockHeaderHashCalculator
   {
      private readonly IProtocolTypeSerializer<BlockHeader> _blockHeaderSerializer;

      public BlockHeaderHashCalculator(IProtocolTypeSerializer<BlockHeader> blockHeaderSerializer)
      {
         this._blockHeaderSerializer = blockHeaderSerializer;
      }

      public UInt256 ComputeHash(BlockHeader header, int protocolVersion)
      {
         var buffer = new ArrayBufferWriter<byte>(80);
         this._blockHeaderSerializer.Serialize(header, protocolVersion, buffer);

         //slicing first 80 bytes because the header includes the tx varint value that doesn't need to be included to compute the hash
         return HashGenerator.DoubleSha256AsUInt256(buffer.WrittenSpan.Slice(0, 80));
      }
   }
}