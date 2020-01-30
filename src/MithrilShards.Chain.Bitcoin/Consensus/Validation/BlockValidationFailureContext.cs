namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public enum BlockValidationFailureContext
   {
      /// <summary>
      /// Initial value. Block has not yet been rejected
      /// </summary>
      Unset,

      /// <summary>
      /// Invalid by consensus rules (excluding any below reasons)
      /// </summary>
      Consensus,

      /**
       * Invalid by a change to consensus rules more recent than SegWit.
       * Currently unused as there are no such consensus rule changes, and any download
       * sources realistically need to support SegWit in order to provide useful data,
       * so differentiating between always-invalid and invalid-by-pre-SegWit-soft-fork
       * is uninteresting.
       */
      RecentConsensusChange,

      /// <summary>
      /// Block was cached as being invalid and we didn't store the reason why.
      /// </summary>
      BlockCachedInvalid,

      /// <summary>
      /// Invalid proof of work or time too old.
      /// </summary>
      BlockInvalidHeader,

      /// <summary>
      /// The block's data didn't match the data committed to by the PoW.
      /// </summary>
      BlockMutated,

      /// <summary>
      /// We don't have the previous block the checked one is built on.
      /// </summary>
      BlockMissingPreviousHeader,

      /// <summary>
      /// A block this one builds on is invalid.
      /// </summary>
      BlockInvalidPreviousHeader,

      /// <summary>
      /// Block timestamp was > 2 hours in the future (or our clock is bad).
      /// </summary>
      BlockTimeFuture,

      /// <summary>
      /// the block failed to meet one of our checkpoints
      /// </summary>
      BlockCheckpoint,
   }
}
