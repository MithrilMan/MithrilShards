using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.Forge.MithrilShards;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Manage the forge server, that allow incoming and outcoming connections.
   /// </summary>
   /// <seealso cref="System.IDisposable" />
   public interface IForgeServer : IMithrilShard {
   }
}
