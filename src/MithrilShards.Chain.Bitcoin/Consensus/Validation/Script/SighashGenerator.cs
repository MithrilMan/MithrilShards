using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Protocol; // For KnownVersion
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes; // For UInt256
using MithrilShards.Core.Network.Protocol.Serialization; // For IProtocolTypeSerializer, Hashing
using MithrilShards.Chain.Bitcoin.Crypto; // For HashUtils

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   [Flags]
   public enum SigHashType : uint
   {
      All = 0x00000001,
      None = 0x00000002,
      Single = 0x00000003,
      AnyoneCanPay = 0x00000080,
      // Taproot specific (values are distinct from legacy modifiers but overlap with base types if not careful)
      // For Taproot, sighash byte is 0 for SIGHASH_ALL_TAPROOT, or one of the legacy values.
      // SIGHASH_ALL_TAPROOT is effectively the default when sighash byte is 0x00.
      // Other values like 0x01, 0x02, 0x03, 0x81, 0x82, 0x83 are SIGHASH_ALL, SIGHASH_NONE, SIGHASH_SINGLE
      // with optional SIGHASH_ANYONECANPAY equivalent behavior for Taproot.
   }

   // Constants for Taproot sighash types as defined in BIP341
   // These are the values of the sighash byte if present, or 0x00 for default SIGHASH_ALL_TAPROOT
   public static class TaprootSigHash
   {
      public const byte DEFAULT = 0x00; // Equivalent to SIGHASH_ALL_TAPROOT
      public const byte ALL = 0x01;
      public const byte NONE = 0x02;
      public const byte SINGLE = 0x03;
      public const byte ANYONECANPAY = 0x80; // This is a flag, ORed with one of the above
      public const byte ALL_ANYONECANPAY = ALL | ANYONECANPAY; // 0x81
      public const byte NONE_ANYONECANPAY = NONE | ANYONECANPAY; // 0x82
      public const byte SINGLE_ANYONECANPAY = SINGLE | ANYONECANPAY; // 0x83
   }


   public static class SighashGenerator
   {
      public static byte[]? CalculateLegacySignatureHash(
          Transaction txToSign,
          int inputIndexToSign,
          byte[] subScriptBytes,
          byte sighashTypeRaw,
          IProtocolTypeSerializer<Transaction> transactionSerializer)
      {
         if (inputIndexToSign < 0 || inputIndexToSign >= txToSign.Inputs!.Length) return null;

         SigHashType nHashType = (SigHashType)sighashTypeRaw;
         Transaction txCopy = CreateTransactionCopyForSighash(txToSign);

         if ((nHashType & SigHashType.AnyoneCanPay) != 0)
         {
            txCopy.Inputs = new TransactionInput[] { txCopy.Inputs![inputIndexToSign] };
            txCopy.Inputs[0].SignatureScript = subScriptBytes;
         }
         else
         {
            for (int i = 0; i < txCopy.Inputs!.Length; i++) txCopy.Inputs[i].SignatureScript = Array.Empty<byte>();
            txCopy.Inputs[inputIndexToSign].SignatureScript = subScriptBytes;
         }

         SigHashType baseType = nHashType & (SigHashType)0x1f;
         if (baseType == SigHashType.None)
         {
            txCopy.Outputs = Array.Empty<TransactionOutput>();
            if ((nHashType & SigHashType.AnyoneCanPay) == 0)
            {
               for (int i = 0; i < txCopy.Inputs!.Length; i++)
                  if (i != inputIndexToSign) txCopy.Inputs[i].Sequence = 0;
            }
         }
         else if (baseType == SigHashType.Single)
         {
            if (inputIndexToSign >= txCopy.Outputs!.Length)
               return HashUtils.Hash256(new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            var newOutputs = new TransactionOutput[inputIndexToSign + 1];
            for (int i = 0; i < inputIndexToSign; i++)
               newOutputs[i] = new TransactionOutput { Value = -1, ScriptPubKey = Array.Empty<byte>() };
            newOutputs[inputIndexToSign] = txCopy.Outputs[inputIndexToSign];
            txCopy.Outputs = newOutputs;

            if ((nHashType & SigHashType.AnyoneCanPay) == 0)
            {
               for (int i = 0; i < txCopy.Inputs!.Length; i++)
                  if (i != inputIndexToSign) txCopy.Inputs[i].Sequence = 0;
            }
         }

         var writer = new ArrayBufferWriter<byte>();
         transactionSerializer.Serialize(txCopy, KnownVersion.CurrentVersion, writer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false)));
         var sighashTypeBytes = new byte[4];
         BitConverter.GetBytes((uint)nHashType).CopyTo(sighashTypeBytes, 0);
         if (!BitConverter.IsLittleEndian) Array.Reverse(sighashTypeBytes);
         writer.Write(sighashTypeBytes);
         return HashUtils.Hash256(writer.WrittenSpan.ToArray());
      }

      private static Transaction CreateTransactionCopyForSighash(Transaction original)
      {
         var copy = new Transaction { Version = original.Version, LockTime = original.LockTime };
         copy.Inputs = original.Inputs?.Select(i => new TransactionInput { PreviousOutput = i.PreviousOutput, SignatureScript = i.SignatureScript, Sequence = i.Sequence }).ToArray() ?? Array.Empty<TransactionInput>();
         copy.Outputs = original.Outputs?.Select(o => new TransactionOutput { Value = o.Value, ScriptPubKey = o.ScriptPubKey }).ToArray() ?? Array.Empty<TransactionOutput>();
         return copy;
      }

      public static byte[]? CalculateWitnessSignatureHash(
          Transaction txToSign, int inputIndexToSign, byte[] witnessScriptBytes, long amount, byte sighashTypeRaw)
      {
         if (inputIndexToSign < 0 || inputIndexToSign >= txToSign.Inputs!.Length) return null;
         SigHashType nHashType = (SigHashType)sighashTypeRaw;
         SigHashType baseType = nHashType & (SigHashType)0x1f;

         byte[] nVersionBytes = BitConverter.GetBytes(txToSign.Version);
         if (!BitConverter.IsLittleEndian) Array.Reverse(nVersionBytes);

         byte[] hashPrevouts = ((nHashType & SigHashType.AnyoneCanPay) == 0) ? ComputeHashPrevouts(txToSign) : UInt256.Zero.GetBytes();
         byte[] hashSequence = ((nHashType & SigHashType.AnyoneCanPay) == 0 && baseType != SigHashType.Single && baseType != SigHashType.None) ? ComputeHashSequence(txToSign) : UInt256.Zero.GetBytes();

         TransactionInput currentInput = txToSign.Inputs[inputIndexToSign];
         byte[] outpointBytes = new byte[36];
         currentInput.PreviousOutput!.Hash!.GetBytes().CopyTo(outpointBytes, 0);
         BitConverter.GetBytes(currentInput.PreviousOutput.Index).CopyTo(outpointBytes, 32);
         if (!BitConverter.IsLittleEndian) Array.Reverse(outpointBytes, 32, 4);

         byte[] scriptCodeBytes;
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms))
         { WriteCompactSize(writer, (ulong)witnessScriptBytes.Length); writer.Write(witnessScriptBytes); scriptCodeBytes = ms.ToArray(); }

         byte[] amountBytes = BitConverter.GetBytes(amount);
         if (!BitConverter.IsLittleEndian) Array.Reverse(amountBytes);
         byte[] nSequenceBytes = BitConverter.GetBytes(currentInput.Sequence);
         if (!BitConverter.IsLittleEndian) Array.Reverse(nSequenceBytes);

         byte[] hashOutputs;
         if (baseType != SigHashType.Single && baseType != SigHashType.None) hashOutputs = ComputeHashOutputs(txToSign);
         else if (baseType == SigHashType.Single && inputIndexToSign < txToSign.Outputs!.Length) hashOutputs = ComputeHashOutputs(txToSign, inputIndexToSign);
         else hashOutputs = UInt256.Zero.GetBytes();

         byte[] nLockTimeBytes = BitConverter.GetBytes(txToSign.LockTime);
         if (!BitConverter.IsLittleEndian) Array.Reverse(nLockTimeBytes);
         byte[] sighashTypeFinalBytes = BitConverter.GetBytes((uint)sighashTypeRaw); // sighashTypeRaw is just one byte, but BIP143 specifies it as uint32
         if (!BitConverter.IsLittleEndian) Array.Reverse(sighashTypeFinalBytes);

         using (var stream = new MemoryStream())
         {
            stream.Write(nVersionBytes); stream.Write(hashPrevouts); stream.Write(hashSequence);
            stream.Write(outpointBytes); stream.Write(scriptCodeBytes); stream.Write(amountBytes);
            stream.Write(nSequenceBytes); stream.Write(hashOutputs); stream.Write(nLockTimeBytes);
            stream.Write(sighashTypeFinalBytes);
            return HashUtils.Hash256(stream.ToArray());
         }
      }

      private static byte[] ComputeHashPrevouts(Transaction tx) { /* As before */
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var input in tx.Inputs!) {
               writer.Write(input.PreviousOutput!.Hash!.GetBytes());
               byte[] idxBytes = BitConverter.GetBytes(input.PreviousOutput.Index); if(!BitConverter.IsLittleEndian) Array.Reverse(idxBytes); writer.Write(idxBytes);
            } return HashUtils.Hash256(ms.ToArray()); } }
      private static byte[] ComputeHashSequence(Transaction tx) { /* As before */
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var input in tx.Inputs!) {
               byte[] seqBytes = BitConverter.GetBytes(input.Sequence); if(!BitConverter.IsLittleEndian) Array.Reverse(seqBytes); writer.Write(seqBytes);
            } return HashUtils.Hash256(ms.ToArray()); } }
      private static byte[] ComputeHashOutputs(Transaction tx, int? singleOutputIndex = null) { /* As before */
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            if (singleOutputIndex.HasValue) {
               if (singleOutputIndex.Value < tx.Outputs!.Length) SerializeOutput(writer, tx.Outputs[singleOutputIndex.Value]);
            } else {
               foreach (var output in tx.Outputs!) SerializeOutput(writer, output);
            } return HashUtils.Hash256(ms.ToArray()); } }
      private static void SerializeOutput(BinaryWriter writer, TransactionOutput output) { /* As before */
         byte[] valBytes = BitConverter.GetBytes(output.Value); if(!BitConverter.IsLittleEndian) Array.Reverse(valBytes); writer.Write(valBytes);
         WriteCompactSize(writer, (ulong)output.ScriptPubKey!.Length); writer.Write(output.ScriptPubKey); }
      private static void WriteCompactSize(BinaryWriter writer, ulong value) { /* As before */
         if (value < 0xFD) writer.Write((byte)value);
         else if (value <= 0xFFFF) { writer.Write((byte)0xFD); byte[] v = BitConverter.GetBytes((ushort)value); if(!BitConverter.IsLittleEndian) Array.Reverse(v); writer.Write(v); }
         else if (value <= 0xFFFFFFFF) { writer.Write((byte)0xFE); byte[] v = BitConverter.GetBytes((uint)value); if(!BitConverter.IsLittleEndian) Array.Reverse(v); writer.Write(v); }
         else { writer.Write((byte)0xFF); byte[] v = BitConverter.GetBytes(value); if(!BitConverter.IsLittleEndian) Array.Reverse(v); writer.Write(v); } }


      // --- Taproot Sighash (BIP341) ---
      private static byte[] ComputeTaprootHashPrevoutsAll(Transaction txToSign)
      {
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var input in txToSign.Inputs!) {
               writer.Write(input.PreviousOutput!.Hash!.GetBytes());
               byte[] idxBytes = BitConverter.GetBytes(input.PreviousOutput.Index); if(!BitConverter.IsLittleEndian) Array.Reverse(idxBytes); writer.Write(idxBytes);
            } return HashUtils.Hash256(ms.ToArray()); }
      }
      private static byte[] ComputeTaprootHashAmountsAll(TransactionOutput[] spentOutputs)
      {
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var utxo in spentOutputs) {
               byte[] valBytes = BitConverter.GetBytes(utxo.Value); if(!BitConverter.IsLittleEndian) Array.Reverse(valBytes); writer.Write(valBytes);
            } return HashUtils.Hash256(ms.ToArray()); }
      }
      private static byte[] ComputeTaprootHashScriptPubKeysAll(TransactionOutput[] spentOutputs)
      {
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var utxo in spentOutputs) {
               WriteTaprootCompactSize(writer, (ulong)utxo.ScriptPubKey!.Length); writer.Write(utxo.ScriptPubKey);
            } return HashUtils.Hash256(ms.ToArray()); }
      }
      private static byte[] ComputeTaprootHashSequenceAll(Transaction txToSign)
      {
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var input in txToSign.Inputs!) {
               byte[] seqBytes = BitConverter.GetBytes(input.Sequence); if(!BitConverter.IsLittleEndian) Array.Reverse(seqBytes); writer.Write(seqBytes);
            } return HashUtils.Hash256(ms.ToArray()); }
      }
      private static byte[] ComputeTaprootHashOutputsAll(Transaction txToSign)
      {
         using (var ms = new MemoryStream()) using (var writer = new BinaryWriter(ms)) {
            foreach (var output in txToSign.Outputs!) SerializeTransactionOutputBip341(writer, output);
            return HashUtils.Hash256(ms.ToArray()); }
      }
      private static void SerializeTransactionOutputBip341(BinaryWriter writer, TransactionOutput output)
      {
         byte[] valBytes = BitConverter.GetBytes(output.Value); if(!BitConverter.IsLittleEndian) Array.Reverse(valBytes); writer.Write(valBytes);
         WriteTaprootCompactSize(writer, (ulong)output.ScriptPubKey!.Length); writer.Write(output.ScriptPubKey);
      }
      private static void WriteTaprootCompactSize(BinaryWriter writer, ulong value) => WriteCompactSize(writer, value); // Reuse existing for now


      public static byte[]? CalculateTaprootSignatureHash(
          Transaction txToSign,
          int inputIndexToSign,
          TransactionOutput[] spentOutputs,
          byte sighashTypeRaw,
          byte extFlags = 0, // Default 0 for key-path or basic script-path
          byte[]? tapLeafHash = null,
          byte[]? annex = null,
          IProtocolTypeSerializer<TransactionInput>? txInputSerializer = null, // Not typically used for BIP341 sighash components
          IProtocolTypeSerializer<TransactionOutput>? txOutputSerializer = null // Not typically used for BIP341 sighash components
          )
      {
         if (inputIndexToSign < 0 || inputIndexToSign >= txToSign.Inputs!.Length) return null;
         if (spentOutputs == null || spentOutputs.Length != txToSign.Inputs.Length) return null;

         byte outputType = (byte)(sighashTypeRaw & 0x03); // SIGHASH_ALL_TAPROOT, NONE, SINGLE
         bool anyoneCanPay = (sighashTypeRaw & TaprootSigHash.ANYONECANPAY) != 0;

         using (var ms = new MemoryStream(512)) // Initial capacity
         {
            // Common prefix data for all sighash types
            ms.WriteByte(0x00); // EPOCH
            ms.WriteByte(sighashTypeRaw);

            byte[] temp4Bytes = new byte[4];
            BitConverter.GetBytes(txToSign.Version).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);
            BitConverter.GetBytes(txToSign.LockTime).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);

            if (!anyoneCanPay)
            {
               ms.Write(ComputeTaprootHashPrevoutsAll(txToSign), 0, 32);
               ms.Write(ComputeTaprootHashAmountsAll(spentOutputs), 0, 32);
               ms.Write(ComputeTaprootHashScriptPubKeysAll(spentOutputs), 0, 32);
               ms.Write(ComputeTaprootHashSequenceAll(txToSign), 0, 32);
            }

            if (outputType != TaprootSigHash.NONE && outputType != TaprootSigHash.SINGLE) // i.e., SIGHASH_ALL or SIGHASH_ALL_TAPROOT (0x00)
            {
               ms.Write(ComputeTaprootHashOutputsAll(txToSign), 0, 32);
            }

            // Data specific to the input being signed
            byte spendType = (byte)(extFlags << 1);
            if (tapLeafHash != null) spendType |= 0x01; // Script path
            ms.WriteByte(spendType);

            if (anyoneCanPay)
            {
               // outpoint
               ms.Write(txToSign.Inputs[inputIndexToSign].PreviousOutput!.Hash!.GetBytes(), 0, 32);
               BitConverter.GetBytes(txToSign.Inputs[inputIndexToSign].PreviousOutput!.Index).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);
               // amount
               byte[] temp8Bytes = new byte[8];
               BitConverter.GetBytes(spentOutputs[inputIndexToSign].Value).CopyTo(temp8Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp8Bytes); ms.Write(temp8Bytes, 0, 8);
               // scriptPubKey
               using (var spkMs = new MemoryStream()) using (var spkWriter = new BinaryWriter(spkMs)) { WriteTaprootCompactSize(spkWriter, (ulong)spentOutputs[inputIndexToSign].ScriptPubKey!.Length); spkWriter.Write(spentOutputs[inputIndexToSign].ScriptPubKey!); ms.Write(spkMs.ToArray(), 0, (int)spkMs.Length); }
               // nSequence
               BitConverter.GetBytes(txToSign.Inputs[inputIndexToSign].Sequence).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);
            }
            else
            {
               BitConverter.GetBytes((uint)inputIndexToSign).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);
            }

            if (annex != null && annex.Length > 0)
            {
               using (var annexMs = new MemoryStream()) using (var annexWriter = new BinaryWriter(annexMs)) { WriteTaprootCompactSize(annexWriter, (ulong)annex.Length); annexWriter.Write(annex); ms.Write(HashUtils.Hash256(annexMs.ToArray()), 0, 32); }
            }

            // Data specific to SIGHASH_SINGLE
            if (outputType == TaprootSigHash.SINGLE)
            {
               if (inputIndexToSign < txToSign.Outputs!.Length)
               {
                  using (var outMs = new MemoryStream()) using (var outWriter = new BinaryWriter(outMs)) { SerializeTransactionOutputBip341(outWriter, txToSign.Outputs[inputIndexToSign]); ms.Write(HashUtils.Hash256(outMs.ToArray()), 0, 32); }
               }
               else
               {
                  ms.Write(UInt256.Zero.GetBytes(), 0, 32); // Output index out of bounds
               }
            }

            // Data specific to a script path spend (ext_flag = 0 or 1)
            if (tapLeafHash != null)
            {
               ms.Write(tapLeafHash, 0, 32);
               ms.WriteByte(extFlags); // key_version for Tapscript, but for sighash ext_flags is used. For key path, ext_flags is 0.
                                       // Here, extFlags is passed in. For key path, tapLeafHash is null, so this block isn't entered.
                                       // For script path, tapLeafHash is non-null. ext_flags for script path is 0, (or 1 for future extensions).
                                       // codeseparator_pos is always 0xFFFFFFFF for sighash.
               BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(temp4Bytes, 0); if (!BitConverter.IsLittleEndian) Array.Reverse(temp4Bytes); ms.Write(temp4Bytes, 0, 4);
            }

            return HashUtils.TaggedHash("TapSighash", ms.ToArray());
         }
      }
   }
}
>>>>>>> REPLACE
