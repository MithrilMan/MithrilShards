using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace MithrilShards.Network.Benchmark
{
   class Program
   {

      public class MyConfig : ManualConfig
      {
         public MyConfig()
         {
            //this.Add(CsvMeasurementsExporter.Default);
            //this.Add(RPlotExporter.Default);
            //this.Add(Job.Default
            //   .With(new GcMode()
            //   {
            //      Force = false // tell BenchmarkDotNet not to force GC collections after every iteration
            //   }));
         }
      }

#pragma warning disable CA1823
      private const string JitTieredCompilation = "COMPLUS_TieredCompilation";
#pragma warning restore CA1823

      static void Main(string[] args)
      {
         // to disable tiered compilation, either set environment variable COMPLUS_TieredCompilation to 0 or uncomment this configuration
         //IConfig config = DefaultConfig.Instance
         //   .StopOnFirstError(true)
         //   .With(Job.Default
         //      //.With(CoreRuntime.Core31)
         //      .WithEnvironmentVariable(JitTieredCompilation, "0")
         //   );
         //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

         BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

         //for debug
         //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
      }
   }
}
