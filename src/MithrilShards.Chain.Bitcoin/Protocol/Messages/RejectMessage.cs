using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   [NetworkMessage("reject ")]
   public class RejectMessage : NetworkMessage {
      public enum RejectCode : byte {
         /// <summary>REJECT_MALFORMED</summary>
         Malformed = 0x01,
         /// <summary>REJECT_INVALID</summary>
         Invalid = 0x10,
         /// <summary>REJECT_OBSOLETE</summary>
         Obsolete = 0x11,
         /// <summary>REJECT_DUPLICATE</summary>
         Duplicate = 0x12,
         /// <summary>REJECT_NONSTANDARD</summary>
         Nonstandard = 0x40,
         /// <summary>REJECT_DUST</summary>
         Dust = 0x41,
         /// <summary>REJECT_INSUFFICIENTFEE</summary>
         Insufficientfee = 0x42,
         /// <summary>REJECT_CHECKPOINT</summary>
         Checkpoint = 0x43
      }

      /// <summary>Code relating to rejected message.</summary>
      public RejectCode Code { get; set; }

      /// <summary>Text version of reason for rejection.</summary>
      public string Reason { get; set; }

      /// <summary>Optional extra data provided by some errors.
      /// Currently, all errors which provide this field fill it with the TXID or block header hash of the object being
      /// rejected, so the field is 32 bytes.</summary>
      public byte[] Data { get; set; }

      public RejectMessage() : base("reject ") {
      }
   }
}