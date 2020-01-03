﻿using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Single bit type (bool).
   /// </summary>
   public class Bit : ISerializableProtocolType<Bit> {
      public string InternalName => "bool";

      public int Length => 1;

      public void Deserialize(ref SequenceReader<byte> reader) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer) {
         throw new NotImplementedException();
      }
   }
}
