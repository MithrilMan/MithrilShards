using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Stratis;
using NBitcoin.BouncyCastle.Math;
using NBitcoin_Target = MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.NBitcoinTypes.NBitcoin_Target;
using MS_Target = MithrilShards.Chain.Bitcoin.DataTypes.Target;

namespace MithrilShards.Network.Benchmark.Benchmarks.Target
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser, PlainExporter]
   public class Target_Study
   {
      //uint actualTimespan = 0x1b0404cb;
      uint perfectCompactValue = 0x03123456;

      MS_Target t1;
      NBitcoin_Target nt1;

      ulong scalar = (ulong)TimeSpan.FromDays(14).TotalSeconds; //actualTimespan value of bitcoin

      [GlobalSetup]
      public void Setup()
      {
         t1 = new MS_Target(perfectCompactValue);
         nt1 = new NBitcoin_Target(perfectCompactValue);
      }

      [Benchmark]
      public MS_Target MulScalar_Target()
      {
         return t1 * scalar;
      }

      [Benchmark]
      public NBitcoin_Target MulScalar_NBitcoinTarget()
      {
         return new NBitcoin_Target(nt1
            .ToBigInteger()
            .Multiply(BigInteger.ValueOf((long)scalar))
            );
      }

      //[Benchmark]
      //public object Div_Target()
      //{
      //   return t1 / t1;
      //}

      //[Benchmark]
      //public object Div_NBitcoinTarget()
      //{
      //   return nt1 / nt1;
      //}



      internal static void Debug()
      {
         uint diff = 0x1b0404cb;

         var t1 = new MS_Target(diff);
         NBitcoin_Target nt1 = new NBitcoin_Target(diff);

         uint scalar = 20;

         var t1MulScalar = t1 * scalar;

         var bigInt = nt1.ToBigInteger();
         var nt1MulScalar = bigInt.Multiply(BigInteger.ValueOf((long)scalar));

         var bits = t1MulScalar.Bits();
         var nBits = new NBitcoin_Target(nt1MulScalar).ToUInt256();
      }


      internal static void TestResults()
      {
         var test = new Target_Study();
         test.Setup();

         var compact = test.t1.ToCompact();
         var nCompact = test.nt1.ToCompact();


         Console.WriteLine($"MulScalar_NBitcoinTarget {test.MulScalar_NBitcoinTarget().ToUInt256().ToString()}");
         Console.WriteLine($"MulScalar_Target {test.MulScalar_Target()}");

         var resultStrat = TestStrat(
            powTargetTimespanTicks: 12096000000000,
            actualTimespanTicks: 20554910000000,
            newTargetBigInt: "26959535291011309493156476344723991336010898738574164086137773096960",
            powLimitBigInt: "26959535291011309493156476344723991336010898738574164086137773096960"
            );

         var resultMS = TestMS(
            powTargetTimespanTicks: 12096000000000,
            actualTimespanTicks: 20554910000000,
            newTargetBigInt: "26959535291011309493156476344723991336010898738574164086137773096960",
            powLimitBigInt: "26959535291011309493156476344723991336010898738574164086137773096960"
            );
      }


      internal static NBitcoin_Target TestStrat(long powTargetTimespanTicks, long actualTimespanTicks, string newTargetBigInt, string powLimitBigInt)
      {
         TimeSpan powTargetTimespan = TimeSpan.FromTicks(powTargetTimespanTicks);
         TimeSpan actualTimespan = TimeSpan.FromTicks(actualTimespanTicks);
         NBitcoin_Target proofOfWorkLimit = new NBitcoin_Target(new BigInteger(powLimitBigInt));

         if (actualTimespan < TimeSpan.FromTicks(powTargetTimespan.Ticks / 4))
            actualTimespan = TimeSpan.FromTicks(powTargetTimespan.Ticks / 4);
         if (actualTimespan > TimeSpan.FromTicks(powTargetTimespan.Ticks * 4))
            actualTimespan = TimeSpan.FromTicks(powTargetTimespan.Ticks * 4);

         // Retarget.
         BigInteger newTarget = new BigInteger(newTargetBigInt);
         newTarget = newTarget.Multiply(BigInteger.ValueOf((long)actualTimespan.TotalSeconds));
         newTarget = newTarget.Divide(BigInteger.ValueOf((long)powTargetTimespan.TotalSeconds));

         var finalTarget = new NBitcoin_Target(newTarget);
         if (finalTarget > proofOfWorkLimit)
            finalTarget = proofOfWorkLimit;

         return finalTarget;
      }

      internal static MS_Target TestMS(long powTargetTimespanTicks, long actualTimespanTicks, string newTargetBigInt, string powLimitBigInt)
      {
         TimeSpan powTargetTimespan = TimeSpan.FromTicks(powTargetTimespanTicks);
         BigInteger newTarget = new BigInteger(newTargetBigInt);

         ulong actualTimespan = (ulong)Math.Clamp(
             value: TimeSpan.FromTicks(actualTimespanTicks).TotalSeconds,
             min: TimeSpan.FromTicks(powTargetTimespan.Ticks / 4).TotalSeconds,
             max: TimeSpan.FromTicks(powTargetTimespan.Ticks * 4).TotalSeconds
             );

         // retarget
         MS_Target powLimit = new MS_Target(new NBitcoin_Target(new BigInteger(powLimitBigInt)).ToUInt256().ToBytes());
         MS_Target bnNew = new MS_Target(new NBitcoin_Target(newTarget).ToUInt256().ToBytes());
         bnNew *= actualTimespan;
         bnNew /= (ulong)powTargetTimespan.TotalSeconds;

         if (bnNew > powLimit)
         {
            bnNew = powLimit;
         }

         return bnNew;
      }
   }
}