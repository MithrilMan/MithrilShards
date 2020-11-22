﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class DiscardThrow
   {
      object _data;

      [GlobalSetup]
      public void Setup()
      {
         _data = new object();
      }


      [Benchmark]
      public void WithDiscard() => WithDiscard(_data);

      public static void WithDiscard(object data)
      {
         _ = data ?? throw new Exception();
      }

      [Benchmark]
      public void WithIf() => WithIf(_data);

      public static void WithIf(object data)
      {
         if (data == null)
         {
            throw new Exception();
         }
      }
   }
}
