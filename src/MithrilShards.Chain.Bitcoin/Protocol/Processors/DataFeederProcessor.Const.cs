using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

public partial class DataFeederProcessor
{
   /// <summary>
   /// Maximum number of block hashes allowed in the BlockLocator.
   /// <seealso href="https://lists.linuxfoundation.org/pipermail/bitcoin-dev/2018-August/016285.html"/>
   /// <seealso href="https://github.com/bitcoin/bitcoin/pull/13907" />
   /// </summary>
   private const int MAX_LOCATOR_SIZE = 101;
}
