﻿using System;
using System.Buffers;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class BlockLocatorBuilding
   {
      [Params(1_000, 100_000, 10_000_000)]
      public int top_height;

      [Benchmark]
      public Span<int> Log()
      {
         int itemsToAdd = this.top_height <= 10 ? (this.top_height + 1) : (10 + (int)Math.Ceiling(Math.Log2(this.top_height)));
         Span<int> indexes = new Span<int>(ArrayPool<int>.Shared.Rent(itemsToAdd), 0, itemsToAdd);

         int index = 0;
         int current = this.top_height;
         while (index < 10 && current > 0)
         {
            indexes[index++] = current--;
         }

         int step = 1;
         while (current > 0)
         {
            indexes[index++] = current;
            step *= 2;
            current -= step;
         }
         indexes[itemsToAdd - 1] = 0;

         return indexes;
      }

      [Benchmark]
      public List<int> Loop()
      {
         // Modify the step in the iteration.
         int step = 1;
         List<int> indexes = new List<int>();

         // Start at the top of the chain and work backwards.
         for (int index = this.top_height; index > 0; index -= step)
         {
            // Push top 10 indexes first, then back off exponentially.
            if (indexes.Count >= 10)
               step *= 2;

            indexes.Add(index);
         }

         //  Push the genesis block index.
         indexes.Add(0);
         return indexes;
      }
   }
}