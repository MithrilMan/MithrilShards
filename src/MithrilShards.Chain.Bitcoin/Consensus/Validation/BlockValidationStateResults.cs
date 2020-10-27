namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   ///  A "reason" why a block was invalid, suitable for determining whether the provider of the block should be banned/ignored/disconnected/etc.
   ///  These are much more granular than the rejection codes, which may be more useful for some other use-cases.
   /// </summary>
   public enum BlockValidationStateResults
   {
      /// <summary>
      /// Initial value. Block has not yet been rejected
      /// </summary>
      Unset = 0,

      /// <summary>
      /// Invalid by consensus rules (excluding any below reasons)
      /// </summary>
      Consensus = 1,

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
      CachedInvalid,

      /// <summary>
      /// Invalid proof of work or time too old.
      /// </summary>
      InvalidHeader,

      /// <summary>
      /// The block's data didn't match the data committed to by the PoW.
      /// </summary>
      Mutated,

      /// <summary>
      /// We don't have the previous block the checked one is built on.
      /// </summary>
      MissingPreviousHeader,

      /// <summary>
      /// A block this one builds on is invalid.
      /// </summary>
      InvalidPreviousHeader,

      /// <summary>
      /// Block timestamp was > 2 hours in the future (or our clock is bad).
      /// </summary>
      TimeFuture,

      /// <summary>
      /// the block failed to meet one of our checkpoints
      /// </summary>
      Checkpoint,
   }
}
