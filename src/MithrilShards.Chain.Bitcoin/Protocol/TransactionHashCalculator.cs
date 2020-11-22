﻿using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class TransactionHashCalculator : ITransactionHashCalculator
   {
      private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;

      public TransactionHashCalculator(IProtocolTypeSerializer<Transaction> transactionSerializer)
      {
         _transactionSerializer = transactionSerializer;
      }

      public UInt256 ComputeHash(Transaction transaction, int protocolVersion)
      {
         var buffer = new ArrayBufferWriter<byte>();
         _transactionSerializer.Serialize(transaction,
                                              protocolVersion,
                                              buffer,
                                              new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false)));

         return HashGenerator.DoubleSha256AsUInt256(buffer.WrittenSpan);
      }

      public UInt256 ComputeWitnessHash(Transaction transaction, int protocolVersion)
      {
         var buffer = new ArrayBufferWriter<byte>();
         _transactionSerializer.Serialize(transaction,
                                              protocolVersion,
                                              buffer,
                                              new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, transaction.HasWitness())));

         return HashGenerator.DoubleSha256AsUInt256(buffer.WrittenSpan);

         throw new NotImplementedException();
      }
   }
}