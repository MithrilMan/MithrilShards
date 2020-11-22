using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.P2P.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class ParseNibble
   {

      char _data;

      [GlobalSetup]
      public void Setup()
      {
         _data = "0123456789ABCDEF"[(char)new Random().Next(0, 15)];
      }



      [Benchmark]
      public int ParseNibble_A() => ParseNibble1(_data);

      [Benchmark]
      public int ParseNibble_B() => ParseNibble2(_data);

      [Benchmark]
      public int ParseNibble_C() => ParseNibble3(_data);

      private static int ParseNibble1(char c)
      {
         if (c >= '0' && c <= '9')
         {
            return c - '0';
         }
         c = (char)(c & ~0x20);
         if (c >= 'A' && c <= 'F')
         {
            return c - ('A' - 10);
         }
         throw new ArgumentException("Invalid nibble: " + c);
      }

      private static int ParseNibble2(char c)
      {
         switch (c)
         {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
               return c - '0';
            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
               return c - ('a' - 10);
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
               return c - ('A' - 10);
            default:
               throw new ArgumentException("Invalid nibble: " + c);
         }
      }

      private static int ParseNibble3(char c)
      {
         if (c >= '0' && c <= '9')
         {
            return c - '0';
         }
         else if (c >= 'a' && c <= 'f')
         {
            return c - ('a' - 10);
         }
         else if (c >= 'A' && c <= 'F')
         {
            return c - ('A' - 10);
         }
         else
         {
            throw new ArgumentException("Invalid nibble: " + c);
         }
      }
   }
}
