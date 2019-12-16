using System;

namespace MithrilShards.Core.Forge {
   public interface IForge : IDisposable {
      void Start();

      void ShutDown();
   }
}
