using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   //[RPlotExporter, CsvMeasurementsExporter]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class BigInt_vs_uint256_parse {
      [Params(1000)]
      public int N;

      [Benchmark]
      public void BigIntegerParse() {
         for (int i = 0; i < this.N; i++) {
            BigInteger.Parse("0fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", System.Globalization.NumberStyles.HexNumber);
         }
      }

      [Benchmark]
      public void uint256Parse() {
         for (int i = 0; i < this.N; i++) {
            _ = uint256.Parse("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141");
         }
      }
   }
}
