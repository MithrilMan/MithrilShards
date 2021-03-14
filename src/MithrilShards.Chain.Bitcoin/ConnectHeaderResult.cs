namespace MithrilShards.Chain.Bitcoin
{
   public enum ConnectHeaderResult
   {
      /// <summary>
      /// Connected successfully
      /// </summary>
      Connected,


      /// <summary>
      /// The header is already current tip.
      /// No operation has been performed.
      /// </summary>
      SameTip,



      /// <summary>
      /// The header was already in the chain.
      /// Chain has been rewound.
      /// </summary>
      Rewinded,


      /// <summary>
      /// Genesis header has been set as tip.
      /// The headers chain has been resetted to genesis.
      /// </summary>
      ResettedToGenesis,


      /// <summary>
      /// The new tip previous header is missing, cannot connect the new tip.
      /// </summary>
      MissingPreviousHeader,


      /// <summary>
      /// The header is invalid.
      /// A <see cref="MithrilShards.Chain.Bitcoin.Consensus.Validation.BlockValidationState"/> should contain more detail about the invalid data.
      /// </summary>
      Invalid
   }
}
