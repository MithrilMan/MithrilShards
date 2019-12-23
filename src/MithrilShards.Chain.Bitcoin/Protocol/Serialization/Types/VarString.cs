﻿using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Generic special type that tells the serializer to infer the type by the underlying type.
   /// </summary>
   public class VarString : ISerializableProtocolType<VarString> {
      public string InternalName => "var_str";

      public int Length => -1;

      public void Deserialize(SequenceReader<byte> data) {
         throw new NotImplementedException();
      }

      public byte[] Serialize() {
         throw new NotImplementedException();
      }
   }
}