namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Transaction
{
   /// <summary>
   /// Holds settings/flags that control script validation behavior.
   /// This allows enabling or disabling certain consensus rules (e.g., P2SH, SegWit)
   /// based on when they were activated on the network.
   /// </summary>
   public class ScriptSettings
   {
      /// <summary>
      /// If true, Pay-to-Script-Hash (BIP16) validation logic is enabled.
      /// This should be true for blocks and transactions after P2SH activation.
      /// </summary>
      public bool IsP2shEnabled { get; }

      /// <summary>
      /// If true, Segregated Witness (BIP141, BIP143, BIP144) validation logic is enabled.
      /// This includes P2WPKH and P2WSH.
      /// (This flag will be used more extensively in later SegWit validation tasks).
      /// </summary>
      public bool IsWitnessEnabled { get; }

      // Add more flags as needed, e.g., for specific opcode enabling/disabling
      // based on soft forks (DER_SIG, STRICTENC, MINIMALDATA, etc.)

      /// <summary>
      /// If true, Taproot (BIP341, BIP342) validation logic is enabled.
      /// </summary>
      public bool IsTaprootEnabled { get; }


      public ScriptSettings(bool isP2shEnabled, bool isWitnessEnabled, bool isTaprootEnabled = false) // Default Taproot to false for explicit enabling
      {
         IsP2shEnabled = isP2shEnabled;
         IsWitnessEnabled = isWitnessEnabled; // Witness should generally be true if Taproot is true
         IsTaprootEnabled = isTaprootEnabled;

         if (IsTaprootEnabled && !IsWitnessEnabled)
         {
            // Taproot relies on witness structures. This state is inconsistent.
            // Consider throwing an ArgumentException or automatically enabling IsWitnessEnabled.
            // For simplicity, assume caller sets IsWitnessEnabled = true if IsTaprootEnabled = true.
            // Or, enforce it:
            // if (isTaprootEnabled) IsWitnessEnabled = true;
         }
      }

      /// <summary>
      /// Gets default settings, typically for validating new, unconfirmed transactions
      /// where modern rules apply (P2SH, SegWit v0, Taproot).
      /// </summary>
      public static ScriptSettings Unrestricted { get; } = new ScriptSettings(isP2shEnabled: true, isWitnessEnabled: true, isTaprootEnabled: true);

      /// <summary>
      /// Gets settings for validating scripts before P2SH and SegWit activation.
      /// </summary>
      public static ScriptSettings PreP2SH { get; } = new ScriptSettings(isP2shEnabled: false, isWitnessEnabled: false, isTaprootEnabled: false);

      /// <summary>
      /// Gets settings for validating scripts after P2SH and SegWit v0 activation, but before Taproot.
      /// </summary>
      public static ScriptSettings PreTaproot_WithSegWit { get; } = new ScriptSettings(isP2shEnabled: true, isWitnessEnabled: true, isTaprootEnabled: false);

      // Potentially add methods or constructors to derive settings from IConsensusParameters
      // or block height for historical validation.
   }
}
