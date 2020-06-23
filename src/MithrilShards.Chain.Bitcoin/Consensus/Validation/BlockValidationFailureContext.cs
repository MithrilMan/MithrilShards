namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public static class BlockValidationFailureContext
   {
      /// <summary>
      /// Initial value. Block has not yet been rejected
      /// </summary>
      public const int Unset = 0;

      /// <summary>
      /// Invalid by consensus rules (excluding any below reasons)
      /// </summary>
      public const int Consensus = 1;

      /**
       * Invalid by a change to consensus rules more recent than SegWit.
       * Currently unused as there are no such consensus rule changes, and any download
       * sources realistically need to support SegWit in order to provide useful data,
       * so differentiating between always-invalid and invalid-by-pre-SegWit-soft-fork
       * is uninteresting.
       */
      public const int RecentConsensusChange = 2;

      /// <summary>
      /// Block was cached as being invalid and we didn't store the reason why.
      /// </summary>
      public const int BlockCachedInvalid = 3;

      /// <summary>
      /// Invalid proof of work or time too old.
      /// </summary>
      public const int BlockInvalidHeader = 4;

      /// <summary>
      /// The block's data didn't match the data committed to by the PoW.
      /// </summary>
      public const int BlockMutated = 5;

      /// <summary>
      /// We don't have the previous block the checked one is built on.
      /// </summary>
      public const int BlockMissingPreviousHeader = 6;

      /// <summary>
      /// A block this one builds on is invalid.
      /// </summary>
      public const int BlockInvalidPreviousHeader = 7;

      /// <summary>
      /// Block timestamp was > 2 hours in the future (or our clock is bad).
      /// </summary>
      public const int BlockTimeFuture = 8;

      /// <summary>
      /// the block failed to meet one of our checkpoints
      /// </summary>
      public const int BlockCheckpoint = 9;
   }
}
