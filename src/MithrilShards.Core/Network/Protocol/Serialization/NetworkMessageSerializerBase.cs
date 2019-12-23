﻿using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public abstract class NetworkMessageSerializerBase<TMessageType> : INetworkMessageSerializer<TMessageType> where TMessageType : INetworkMessage {
      public abstract TMessageType Deserialize(ReadOnlySpan<byte> data);

      public abstract byte[] Serialize(TMessageType message);

      public Type GetMessageType() {
         return typeof(TMessageType);
      }

      public byte[] Serialize(INetworkMessage message) {
         return this.Serialize((TMessageType)message);
      }

      INetworkMessage INetworkMessageSerializer.Deserialize(ReadOnlySpan<byte> data) {
         return (TMessageType)this.Deserialize(data);
      }
   }
}