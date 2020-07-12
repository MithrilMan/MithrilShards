﻿using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// The headers packet returns block headers in response to a getheaders packet.
   /// </summary>
   /// <seealso cref="INetworkMessage" />
   [NetworkMessage(COMMAND)]
   public sealed class BlockMessage : INetworkMessage
   {
      private const string COMMAND = "block";
      string INetworkMessage.Command => COMMAND;

      public BlockHeader? Header { get; set; }


   }
}