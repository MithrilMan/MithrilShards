using System;
using System.Numerics;
using System.Security.Cryptography;
using BenchmarkDotNet.Running;
using MithrilShards.P2P.Benchmark.Benchmarks;

namespace MithrilShards.P2P.Benchmark {
   class Program {


      static void Main(string[] args) {
         //NBitcoin.curve

         //var curve = ECCurve.CreateFromValue("1.3.132.0.10");

         BigInteger N = BigInteger.Parse("0fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", System.Globalization.NumberStyles.HexNumber);

         Console.WriteLine(N);

         //BenchmarkRunner.Run<BigInt_vs_uint256_parse>();
         BenchmarkRunner.Run<MagicNumberFinder>();
      }
   }
}
