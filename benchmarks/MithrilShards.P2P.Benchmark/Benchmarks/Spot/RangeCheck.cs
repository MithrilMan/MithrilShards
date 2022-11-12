using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks;

[SimpleJob(RuntimeMoniker.Net70)]
[RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
public class RangeCheck
{

   [GlobalSetup]
   public void Setup()
   {
   }


   [Benchmark]
   public void ArrayRange()
   {
      byte[] arr = new byte[10];
      Span<byte> span1 = arr;

      byte[] arr2 = new byte[20];

      arr2[..10].CopyTo(span1);
   }

   [Benchmark]
   public void AsSpanRange()
   {
      byte[] arr = new byte[10];
      Span<byte> span1 = arr;

      byte[] arr2 = new byte[20];

      arr2.AsSpan(..10).CopyTo(span1);
   }

   [Benchmark]
   public void AsSpanRange2()
   {
      byte[] arr = new byte[10];
      Span<byte> span1 = arr;

      byte[] arr2 = new byte[20];

      arr2.AsSpan()[..10].CopyTo(span1);
   }

   [Benchmark]
   public void AsSpanRange3()
   {
      byte[] arr = new byte[10];
      Span<byte> span1 = arr;

      byte[] arr2 = new byte[20];

      ((Span<byte>)arr2)[..10].CopyTo(span1);
   }

   [Benchmark]
   public void ExplicitSlice()
   {
      byte[] arr = new byte[10];
      Span<byte> span1 = arr;

      byte[] arr2 = new byte[20];
      var span2 = new Span<byte>(arr2, 0, 10);

      span2.CopyTo(span1);
   }
}
