using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types
{
   public class TransactionSerializer : IProtocolTypeSerializer<Transaction>
   {
      readonly IProtocolTypeSerializer<TransactionInput> transactionInputSerializer;
      readonly IProtocolTypeSerializer<TransactionOutput> transactionOutputSerializer;
      readonly IProtocolTypeSerializer<TransactionWitness> transactionWitnessSerializer;

      public TransactionSerializer(IProtocolTypeSerializer<TransactionInput> transactionInputSerializer,
                                   IProtocolTypeSerializer<TransactionOutput> transactionOutputSerializer,
                                   IProtocolTypeSerializer<TransactionWitness> transactionWitnessSerializer)
      {
         this.transactionInputSerializer = transactionInputSerializer;
         this.transactionOutputSerializer = transactionOutputSerializer;
         this.transactionWitnessSerializer = transactionWitnessSerializer;
      }

      public Transaction Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
      {
         bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, false) ?? false;
         byte flags = 0;

         var tx = new Transaction { Version = reader.ReadInt() };

         /// Try to read the inputs. In case the dummy byte is, this will be read as an empty list of transaction inputs.
         TransactionInput[] inputs = reader.ReadArray(protocolVersion, this.transactionInputSerializer);

         if (inputs.Length == 0)
         {
            //we don't expect an empty transaction inputs list, so we treat this as having witness data
            flags = reader.ReadByte();
            if (flags != 0 && allowWitness)
            {
               tx.Inputs = reader.ReadArray(protocolVersion, this.transactionInputSerializer);
               tx.Outputs = reader.ReadArray(protocolVersion, this.transactionOutputSerializer);
            }
         }
         else
         {
            // otherwise we read valid inputs, now we have to read outputs
            tx.Outputs = reader.ReadArray(protocolVersion, this.transactionOutputSerializer);
         }

         if ((flags & 1) != 0 && allowWitness)
         {
            /* The witness flag is present, and we support witnesses. */
            flags ^= 1;

            for (int i = 0; i < tx.Inputs!.Length; i++)
            {
               tx.Inputs[i].ScriptWitness = reader.ReadWithSerializer(protocolVersion, transactionWitnessSerializer);
            }

            if (!tx.HasWitness())
            {
               // It's illegal to encode witnesses when all witness stacks are empty.
               ThrowHelper.ThrowNotSupportedException("Superfluous witness record");
            }
         }

         if (flags != 0)
         {
            /* Unknown flag in the serialization */
            ThrowHelper.ThrowNotSupportedException("Unknown transaction optional data");
         }

         tx.LockTime = reader.ReadUInt();

         return tx;
      }

      public int Serialize(Transaction tx, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
      {
         bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, false) ?? false;
         byte flags = 0;
         int size = 0;

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

         size += writer.WriteArray(tx.Inputs, protocolVersion, this.transactionInputSerializer);
         size += writer.WriteArray(tx.Outputs, protocolVersion, this.transactionOutputSerializer);

         if ((flags & 1) != 0)
         {
            if (tx.Inputs != null)
            {
               for (int i = 0; i < tx.Inputs.Length; i++)
               {
                  size += writer.WriteWithSerializer(tx.Inputs[i].ScriptWitness!, protocolVersion, this.transactionWitnessSerializer);
               }
            }
         }

         size += writer.WriteUInt(tx.LockTime);

         return size;
      }
   }
}
