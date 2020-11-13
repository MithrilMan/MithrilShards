namespace MithrilShards.Chain.Bitcoin.Network.Bedrock
{
   public static class ProtocolDefinition
   {
      /// <summary>
      /// The default maximum protocol message length.
      /// </summary>
      public const uint DEFAULT_MAX_PROTOCOL_MESSAGE_LENGTH = 4_000_000;

      public const int SIZE_MAGIC = 4;
      public const int SIZE_COMMAND = 12;
      public const int SIZE_PAYLOAD_LENGTH = 4;
      public const int SIZE_CHECKSUM = 4;
      public const int HEADER_LENGTH = SIZE_MAGIC + SIZE_COMMAND + SIZE_PAYLOAD_LENGTH + SIZE_CHECKSUM;
   }
}
