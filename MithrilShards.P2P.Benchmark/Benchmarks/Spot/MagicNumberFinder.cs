using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class MagicNumberFinder
   {
      readonly byte[] magicNumberBytes = BitConverter.GetBytes(0x0709110B);
      readonly int magicNumber = 0x0709110B;

      private ReadOnlySequence<byte> input;

      [Params(100, 10000)]
      //[Params(1_000_000)]
      public int PacketSize;

      //0 = at the beginning, 1 = at the end
      [Params(0f, 0.2f, 0.5f, 1f)]
      //[Params(1f)]
      public float MagicPacketRelativePosition;

      [GlobalSetup]
      public void Setup()
      {
         byte[] data = new byte[this.PacketSize];
         new Random().NextBytes(data);

         //ensure magic packet is at the right position, replace every occurrence of the first magic packet in the string with something else
         for (int i = 0; i < data.Length; i++)
         {
            byte b = data[i];
            if (b == this.magicNumberBytes[0])
            {
               data[i] = (byte)'\0';
            }
         }

         // insert magic packet at the right position
         int position = (int)(data.Length * this.MagicPacketRelativePosition);
         position = Math.Min(position, data.Length - (this.magicNumberBytes.Length + 1));
         for (int i = 0; i < this.magicNumberBytes.Length; i++)
         {
            data[position + i] = this.magicNumberBytes[i];
         }

         this.input = new ReadOnlySequence<byte>(data);
      }


      [Benchmark]
      public bool FindWithTryAdvanceTo()
      {
         var reader = new SequenceReader<byte>(this.input);
         return this.FindWithTryAdvanceTo(ref reader);
      }

      [Benchmark]
      public bool FindWithForLoop()
      {
         var reader = new SequenceReader<byte>(this.input);
         return this.FindWithForLoop(ref reader);
      }




      private bool FindWithTryAdvanceTo(ref SequenceReader<byte> reader)
      {
         // advance to the first byte of the magic number.
         while (reader.TryAdvanceTo(this.magicNumberBytes[0], advancePastDelimiter: false))
         {
            if (reader.TryReadLittleEndian(out int magicRead))
            {
               if (magicRead == this.magicNumber)
               {
                  return true;
               }
               else
               {
                  reader.Rewind(3);
               }
            }
            else
            {
               return false;
            }
         }

         // didn't found the first magic byte so can advance up to the end
         reader.Advance(reader.Remaining);
         return false;
      }

      private bool FindWithForLoop(ref SequenceReader<byte> reader)
      {
         for (int i = 0; i < this.magicNumberBytes.Length; i++)
         {
            byte expectedByte = this.magicNumberBytes[i];

            if (reader.TryRead(out byte receivedByte))
            {
               if (expectedByte != receivedByte)
               {
                  // If we did not receive the next byte we expected
                  // we either received the first byte of the magic value
                  // or not. If yes, we set index to 0 here, which is then
                  // incremented in for loop to 1 and we thus continue
                  // with the second byte. Otherwise, we set index to -1
                  // here, which means that after the loop incrementation,
                  // we will start from first byte of magic.
                  i = receivedByte == this.magicNumberBytes[0] ? 0 : -1;
               }
            }
            else
            {
               //nothing left to read
               // in case there are partial matches for the magic packet, don't consider them as consumed
               // so they will be examined again next iteration when hopefully the full magic number will be present
               reader.Rewind(i);

               return false;
            }
         }
         return true;
      }
   }
}
