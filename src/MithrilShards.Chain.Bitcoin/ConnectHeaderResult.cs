using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Chain.Bitcoin
{
   public enum ConnectHeaderResult
   {
      /// <summary>
      /// Connected successfully
      /// </summary>
      Connected = 1,


      /// <summary>
      /// The header is already current tip.
      /// No operation has been performed.
      /// </summary>
      SameTip = 2,



      /// <summary>
      /// The header was already in the chain.
      /// Chain has been rewound.
      /// </summary>
      Rewinded = 4,


      /// <summary>
      /// Genesis header has been set as tip.
      /// The headers chain has been resetted to genesis.
      /// </summary>
      ResettedToGenesis = 5,


      /// <summary>
      /// The new tip previous header is missing, cannot connect the new tip.
      /// </summary>
      MissingPreviousHeader = 6
   }
}
