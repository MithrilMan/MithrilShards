﻿using System;
using System.Buffers;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class HexEncoder
   {
      const string HexValues = "0123456789ABCDEF";
      private byte[] value;


      [GlobalSetup]
      public void Setup()
      {
         this.value = new Core.DataTypes.UInt256("0123456789abcdef0123456789ABCDEF0123456789abcdef0123456789ABCDEF").GetBytes().ToArray();
      }

      [Benchmark]
      public object ConvertAsMithril() => this.ConvertAsMithril(this.value);

      private object ConvertAsMithril(byte[] value)
      {
         return Core.Encoding.HexEncoder.ToHexString(value);
      }

      [Benchmark]
      public string ConvertReverseAsMithril () => this.ConvertReverseAsMithril(this.value);

      private string ConvertReverseAsMithril(byte[] value)
      {
         return Core.Encoding.HexEncoder.ToHexString(value);
      }

      [Benchmark]
      public object ConvertAsNEO() => ConvertAsNEO(this.value);

      [Benchmark]
      public object ConvertAsNBitcoin() => ConvertAsNBitcoin(this.value, false);

      private static string ConvertAsNEO(byte[] value)
      {
         StringBuilder sb = new StringBuilder();
         foreach (byte b in value)
            sb.AppendFormat("{0:x2}", b);
         return sb.ToString();
      }

      private static readonly string[] HexTbl = Enumerable.Range(0, 256).Select(v => v.ToString("x2")).ToArray();
      private static string ConvertAsNBitcoin(byte[] data, bool Space)
      {
         int spaces = (Space ? Math.Max((data.Length - 1), 0) : 0);

         void CreateHexString(Span<char> s, (int offset, int count, byte[] data) state)
         {
            int pos = 0;
            for (int i = state.offset; i < state.offset + state.count; i++)
            {
               if (Space && i != 0)
                  s[pos++] = ' ';
               string c = HexTbl[state.data[i]];
               s[pos++] = c[0];
               s[pos++] = c[1];
            }
         }

         return string.Create(2 * data.Length + spaces, (0, data.Length, data), CreateHexString);
      }
   }
}
