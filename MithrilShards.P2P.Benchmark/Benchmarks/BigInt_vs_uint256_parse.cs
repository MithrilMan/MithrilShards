using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.P2P.Benchmark.Benchmarks {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RPlotExporter, RankColumn]
   public class BigInt_vs_uint256_parse {
      [Benchmark]
      public BigInteger BigIntegerParse() => BigInteger.Parse("0fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", System.Globalization.NumberStyles.HexNumber);

      [Benchmark]
      public uint256 uint256Parse() => uint256.Parse("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141");
   }
}
