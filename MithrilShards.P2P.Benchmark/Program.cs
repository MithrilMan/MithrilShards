using System;
using System.Numerics;
using System.Security.Cryptography;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using MithrilShards.Core.Crypto;
using MithrilShards.Network.Benchmark.Benchmarks;

namespace MithrilShards.Network.Benchmark {
   class Program {

      public class Config : ManualConfig {
         public Config() {
            //this.Add(CsvMeasurementsExporter.Default);
            //this.Add(RPlotExporter.Default);
         }
      }

      static void Main(string[] args) {
         //NBitcoin.curve

         //var curve = ECCurve.CreateFromValue("1.3.132.0.10");
         //BigInteger N = BigInteger.Parse("0fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", System.Globalization.NumberStyles.HexNumber);

         byte[] data = new byte[1000];
         new Random().NextBytes(data);
         Console.WriteLine(NBitcoin.Crypto.Hashes.SHA256(data));
         Console.WriteLine(NBitcoin.Crypto.Hashes.Hash256(data));
         Console.WriteLine(new NBitcoin.uint256(HashGenerator.Sha256(data).ToArray()));
         Console.WriteLine(new NBitcoin.uint256(HashGenerator.DoubleSha256(data).ToArray()));

         //BenchmarkRunner.Run<BigInt_vs_uint256_parse>();
         //BenchmarkRunner.Run<MagicNumberFinder>();
         BenchmarkRunner.Run<Benchmarks.SHA256>();
      }
   }
}
