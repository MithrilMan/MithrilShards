using System;
namespace MithrilShards.Core.Network.Protocol
{
   /// <summary>
   /// Classes used to handle unknown messages received by other peers, that can't be deserialized using a known deserializer.
   /// </summary>
   public sealed class UnknownMessage : INetworkMessage
   {
      readonly byte[] _payload;

      public string Command { get; }

      public ReadOnlySpan<byte> Payload => _payload;

      public UnknownMessage(string command, byte[] payload)
      {
         Command = command ?? throw new ArgumentNullException(nameof(command));
         _payload = payload;
      }
   }
}
