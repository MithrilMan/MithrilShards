namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// A "reason" why a transaction was invalid, suitable for determining whether the
   /// provider of the transaction should be banned/ignored/disconnected/etc.
   /// </summary>
   public enum TransactionValidationStateResults
   {
      /// <summary>
      /// Initial value. Tx has not yet been rejected.
      /// </summary>
      Unset = 0,

      /// <summary>
      /// Invalid by consensus rules.
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
      /// Didn't meet our local policy rules.
      /// </summary>
      NotStandard,

      /// <summary>
      /// Transaction was missing some of its inputs.
      /// </summary>
      MissingInputs,

      /// <summary>
      /// Transaction spends a coinbase too early, or violates locktime/sequence locks.
      /// </summary>
      PrematureSpend,

      /// <summary>
      /// Transaction might be missing a witness, have a witness prior to SegWit activation,
      /// or witness may have been malleated (which includes non-standard witnesses).
      /// </summary>
      WitnessMutated,

      /// <summary>
      /// Tx already in mempool or conflicts with a tx in the chain (if it conflicts with another tx in mempool,
      /// we use <see cref="MempoolPolicy"/> as it failed to reach the RBF threshold)
      /// Currently this is only used if the transaction already exists in the mempool or on chain.
      /// </summary>
      Conflict,

      /// <summary>
      /// Violated mempool's fee/size/descendant/RBF/etc limits.
      /// </summary>
      MempoolPolicy,
   }
}
