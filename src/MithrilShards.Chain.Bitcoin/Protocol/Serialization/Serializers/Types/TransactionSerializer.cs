using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;

public class TransactionSerializer : IProtocolTypeSerializer<Transaction>
{
   readonly IProtocolTypeSerializer<TransactionInput> _transactionInputSerializer;
   readonly IProtocolTypeSerializer<TransactionOutput> _transactionOutputSerializer;
   readonly IProtocolTypeSerializer<TransactionWitness> _transactionWitnessSerializer;

   public TransactionSerializer(IProtocolTypeSerializer<TransactionInput> transactionInputSerializer,
                                IProtocolTypeSerializer<TransactionOutput> transactionOutputSerializer,
                                IProtocolTypeSerializer<TransactionWitness> transactionWitnessSerializer)
   {
      _transactionInputSerializer = transactionInputSerializer;
      _transactionOutputSerializer = transactionOutputSerializer;
      _transactionWitnessSerializer = transactionWitnessSerializer;
   }

   public Transaction Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
   {
      bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, false) ?? false;
      var tx = new Transaction();

      tx.Version = reader.ReadInt();

      bool isWitnessFormat = false; // True if marker AND flag indicate SegWit format
      byte bip144Flag = 0;          // Stores the actual flag byte if SegWit

      // Check for SegWit marker and flag if witness is allowed in this context
      if (allowWitness && reader.TryPeek(out byte markerCandidate) && markerCandidate == 0x00)
      {
         // Potential SegWit. Create a temporary reader to peek the flag byte
         // without advancing the main reader unless we confirm it's SegWit.
         var tempReaderForFlagCheck = reader;
         tempReaderForFlagCheck.Advance(1); // Advance temporary reader past the potential marker (0x00)

         if (tempReaderForFlagCheck.TryRead(out byte flagCandidate)) // Successfully read the byte after marker
         {
            if (flagCandidate != 0x00) // This is a SegWit transaction (marker 0x00, flag non-zero)
            {
               isWitnessFormat = true;
               // Consume marker and flag from the main reader now that we've confirmed SegWit
               reader.Advance(1); // Consume marker (0x00)
               bip144Flag = reader.ReadByte(); // Consume and get the actual flag byte (e.g., 0x01)
            }
            else // Marker was 0x00, but flag was 0x00. Invalid per BIP144.
            {
               ThrowHelper.ThrowProtocolViolationException("Witness transaction found with marker 0x00 but flag 0x00, which is invalid according to BIP144.");
            }
         }
         // else: Stream ended after marker 0x00 before flag could be read.
         // This will be treated as a legacy transaction, and reader.ReadArray for inputs will likely fail if it expects more data.
         // Or, if it was just version + 0x00, ReadArray will read 0 inputs.
      }

      if (isWitnessFormat)
      {
         // SegWit Path
         tx.Inputs = reader.ReadArray(protocolVersion, _transactionInputSerializer);
         tx.Outputs = reader.ReadArray(protocolVersion, _transactionOutputSerializer);

         if ((bip144Flag & 0x01) != 0) // Check if the first bit (witness data presence) of the flag is set
         {
            for (int i = 0; i < tx.Inputs!.Length; i++)
            {
               tx.Inputs[i].ScriptWitness = reader.ReadWithSerializer(protocolVersion, _transactionWitnessSerializer);
            }
            // BIP144: "If the witness is empty, the witness structure MUST be omitted" (i.e. flag bit not set, or no witness section)
            // "but a transaction MUST be encoded with the new serialization format if at least one input has a non-empty scriptWitness."
            // So, if flag bit 0x01 is set, there MUST be witness data (tx.HasWitness() must be true).
            if (!tx.HasWitness())
            {
               ThrowHelper.ThrowProtocolViolationException("Superfluous witness record: witness flag set but no actual witness data found.");
            }
         }
         else if(tx.HasWitness()) // Should not happen: flag says no witness, but witness data somehow got populated.
         {
            // This might indicate an issue with HasWitness() or manual object manipulation.
            // For deserialization, if flag bit 0x01 is NOT set, we MUST NOT have witness data.
            // The loop above wouldn't have run, so ScriptWitness fields would be null. HasWitness() should be false.
            // If HasWitness() is true here, it's an inconsistency.
             ThrowHelper.ThrowProtocolViolationException("Inconsistent witness data: witness flag not set, but witness data present in object.");
         }

         // Check for any other unsupported flag bits.
         // BIP144: "All other flag bits are reserved for future use. Non-zero values for other flag bits are invalid." (Bitcoin Core might be more lenient)
         // For strictness, we can reject unknown flags.
         if ((bip144Flag & (~0x01)) != 0)
         {
            ThrowHelper.ThrowProtocolViolationException($"Unsupported witness flags set: {bip144Flag}. Only flag bit 0 (0x01) is understood.");
         }
      }
      else // Legacy Path or fall-through from failed SegWit detection (e.g. stream too short after marker)
      {
         // If `allowWitness` was false, or `markerCandidate` wasn't 0x00.
         // The `reader` is still at the original position if marker wasn't 0x00 (or if peeking failed).
         // `ReadArray` will read `tx_in_count` (which could be 0x00 for 0 inputs in legacy).
         tx.Inputs = reader.ReadArray(protocolVersion, _transactionInputSerializer);
         tx.Outputs = reader.ReadArray(protocolVersion, _transactionOutputSerializer);
      }

      tx.LockTime = reader.ReadUInt();
      return tx;
   }

   public int Serialize(Transaction tx, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
   {
      bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, false) ?? false;
      byte flags = 0;
      int size = 0;

      size += writer.WriteInt(tx.Version);

      // Consistency check.
      if (allowWitness)
      {
         // Check whether witnesses need to be serialized.
         if (tx.HasWitness())
         {
            flags |= 1;
         }
      }

      if (flags != 0)
      {
         // Use extended format in case witnesses are to be serialized.
         size += writer.WriteVarInt(0);
         size += writer.WriteByte(flags);
      }

      size += writer.WriteArray(tx.Inputs, protocolVersion, _transactionInputSerializer);
      size += writer.WriteArray(tx.Outputs, protocolVersion, _transactionOutputSerializer);

      if ((flags & 1) != 0)
      {
         if (tx.Inputs != null)
         {
            for (int i = 0; i < tx.Inputs.Length; i++)
            {
               size += writer.WriteWithSerializer(tx.Inputs[i].ScriptWitness!, protocolVersion, _transactionWitnessSerializer);
            }
         }
      }

      size += writer.WriteUInt(tx.LockTime);

      return size;
   }
}
