﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Encoding;
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

         //slicing first 80 bytes because the header includes the tx varint value that doesn't need to be included to compute the hash
         return new UInt256(HashGenerator.DoubleSha256(buffer.WrittenSpan.Slice(0, 80)));
      }
   }
}