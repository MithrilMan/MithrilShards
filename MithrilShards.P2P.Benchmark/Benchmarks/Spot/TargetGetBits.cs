using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks.Target
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser, PlainExporter]
   public class TargetGetBits
   {
      uint perfectCompactValue = 0x03123456;

      MSTarget target;

      [GlobalSetup]
      public void Setup()
      {
         target = new MSTarget(perfectCompactValue);
      }

      [Benchmark]
      public int WithBitOperations()
      {
         return target.WithBitOperations();
      }

      [Benchmark]
      public int AsBitcoin()
      {
         return target.AsBitcoin();
      }

      [Benchmark]
      public int AsNBitcoin()
      {
         return target.AsNBitcoin();
      }



      public class MSTarget : MithrilShards.Chain.Bitcoin.DataTypes.Target
      {
         public MSTarget(uint value) : base(value)
         {
         }

         public int WithBitOperations()
         {
            const int bitsPerPart = sizeof(ulong) * 8;

            if (this.part4 != 0)
            {
               int zeroes = BitOperations.LeadingZeroCount(this.part4);
               if (zeroes > 0) return (bitsPerPart * 4) - zeroes;
            }

            if (this.part3 != 0)
            {
               int zeroes = BitOperations.LeadingZeroCount(this.part3);
               if (zeroes > 0) return (bitsPerPart * 3) - zeroes;
            }

            if (this.part2 != 0)
            {
               int zeroes = BitOperations.LeadingZeroCount(this.part2);
               if (zeroes > 0) return (bitsPerPart * 2) - zeroes;
            }

            if (this.part1 != 0)
            {
               int zeroes = BitOperations.LeadingZeroCount(this.part1);
               if (zeroes > 0) return bitsPerPart - zeroes;
            }

            return 0;
         }

         public int AsBitcoin()
         {
            ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref this.part1), 8);

            for (int pos = leftBytes.Length - 1; pos >= 0; pos--)
            {
               if (leftBytes[pos] != 0)
               {
                  for (int nbits = 31; nbits > 0; nbits--)
                  {
                     if ((leftBytes[pos] & 1U << nbits) != 0)
                        return 32 * pos + nbits + 1;
                  }
                  return 32 * pos + 1;
               }
            }
            return 0;
         }

         public int AsNBitcoin()
         {
            if (part4 != 0)
            {
               for (int nbits = 63; nbits > 0; nbits--)
               {
                  if ((part4 & 1UL << nbits) != 0)
                     return 64 * 3 + nbits + 1;
               }
               return 64 * 3 + 1;
            }
            if (part3 != 0)
            {
               for (int nbits = 63; nbits > 0; nbits--)
               {
                  if ((part3 & 1UL << nbits) != 0)
                     return 64 * 2 + nbits + 1;
               }
               return 64 * 2 + 1;
            }
            if (part2 != 0)
            {
               for (int nbits = 63; nbits > 0; nbits--)
               {
                  if ((part2 & 1UL << nbits) != 0)
                     return 64 * 1 + nbits + 1;
               }
               return 64 * 1 + 1;
            }
            if (part1 != 0)
            {
               for (int nbits = 63; nbits > 0; nbits--)
               {
                  if ((part1 & 1UL << nbits) != 0)
                     return 64 * 0 + nbits + 1;
               }
               return 64 * 0 + 1;
            }
            return 0;
         }
      }
   }
}