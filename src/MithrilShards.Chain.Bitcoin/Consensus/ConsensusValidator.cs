using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusValidator : IConsensusValidator
   {
      /// <summary>
      /// The logger
      /// </summary>
      readonly ILogger<ConsensusValidator> logger;

      /// <summary>
      /// The event bus
      /// </summary>
      readonly IEventBus eventBus;

      /// <summary>
      /// The known header validation rules.
      /// </summary>
      readonly IEnumerable<IHeaderValidationRule> headerValidationRules;
      readonly IConsensusParameters consensusParameters;
      readonly HeadersTree headersTree;
      readonly IHeaderValidationContextFactory headerValidationContextFactory;

      private readonly object headerValidationLock = new object();
      private readonly object validationLock = new object();

      public ConsensusValidator(ILogger<ConsensusValidator> logger,
                                IEventBus eventBus,
                                IEnumerable<IHeaderValidationRule> headerValidationRules,
                                IConsensusParameters consensusParameters,
                                HeadersTree headersTree,
                                IHeaderValidationContextFactory headerValidationContextFactory)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.headerValidationRules = headerValidationRules;
         this.consensusParameters = consensusParameters;
         this.headersTree = headersTree;
         this.headerValidationContextFactory = headerValidationContextFactory;

         this.VerifyValidationRules(this.headerValidationRules);
      }

      /// <summary>
      /// Verifies that all registered validation rules have all dependent rules registered too and order rules based on their dependency graph.
      /// </summary>
      /// <typeparam name="TValidationRule">The type of the validation rules.</typeparam>
      /// <param name="rules">The rules to verify.</param>
      private void VerifyValidationRules<TValidationRule>(IEnumerable<TValidationRule> rules)
      {
         Type validationRulesType = typeof(TValidationRule);

         using IDisposable logScope = this.logger.BeginScope("Verifying validation rules for {ValidationRuleType}", validationRulesType.Name);
         foreach (TValidationRule rule in rules)
         {
            Type ruleType = rule!.GetType();
            foreach (Type requiredRule in this.GetRequiredRules(ruleType))
            {
               if (!validationRulesType.IsAssignableFrom(requiredRule))
               {
                  throw new ArgumentException($"{nameof(ruleType)} must implement {validationRulesType.Name}.");
               }

               if (!rules.Any(rule => requiredRule.IsAssignableFrom(requiredRule)))
               {
                  throw new ArgumentException($"{nameof(ruleType)} requires '{requiredRule.Name}' but the rule (or a subclass of that rule) is not registered.");
               }
            }
         }
      }

      /// <summary>
      /// Gets the required rules defined using <see cref="RequiresRuleAttribute" /> for the rule of <paramref name="ruleType" /> type.
      /// </summary>
      /// <param name="ruleType">Type of the rule that need to get required rules.</param>
      /// <returns></returns>
      private List<Type> GetRequiredRules(Type ruleType)
      {
         return ruleType.GetCustomAttributes(typeof(RequiresRuleAttribute), true)
            .Select(req => ((RequiresRuleAttribute)req).RequiredRuleType)
            .ToList();
      }



      public bool ProcessNewBlockHeaders(BlockHeader[] headers, out BlockValidationState state, [MaybeNullWhen(false)] out HeaderNode lastProcessedHeader)
      {
         lastProcessedHeader = null!;
         state = null!;

         lock (this.validationLock)
         {
            foreach (BlockHeader header in headers)
            {
               bool accepted = this.AcceptBlockHeader(header, out state, out lastProcessedHeader);
               this.CheckBlockIndex();

               if (!accepted)
               {
                  lastProcessedHeader = null!;
                  return false;
               }

               // lastProcessedHeader = header;
            }
         }

         //if (NotifyHeaderTip())
         //{
         //   if (::ChainstateActive().IsInitialBlockDownload() && ppindex && *ppindex)
         //   {
         //      LogPrintf("Synchronizing blockheaders, height: %d (~%.2f%%)\n", (*ppindex)->nHeight, 100.0 / ((*ppindex)->nHeight + (GetAdjustedTime() - (*ppindex)->GetBlockTime()) / Params().GetConsensus().nPowTargetSpacing) * (*ppindex)->nHeight);
         //   }
         //}

         return true;
      }



      /// <summary>
      /// Validates the header performing checks for every <see cref="IHeaderValidationRule"/> known rule.
      /// </summary>
      /// <param name="header">The header to validate.</param>
      /// <param name="validationState">The resulting state of the validation.</param>
      /// <returns>
      /// <see langword="true"/> if the validation succeed, <see langword="false"/> otherwise and the reason of the fault
      /// can be found in <paramref name="validationState"/>.
      /// </returns>
      private bool AcceptBlockHeader(BlockHeader header, out BlockValidationState validationState, [MaybeNullWhen(false)] out HeaderNode lastProcessedHeader)
      {
         UInt256 headerHash = header.Hash!;
         validationState = new BlockValidationState();
         lastProcessedHeader = null!;

         lock (this.headerValidationLock)
         {
            //don't validate genesis header
            if (headerHash != this.consensusParameters.Genesis)
            {
               // check if the tip we want to set is already into our chain
               if (this.headersTree.TryGetNode(headerHash, false, out HeaderNode? tipNode))
               {
                  if (tipNode.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
                  {
                     validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "duplicate", "block marked as invalid");
                     return false;
                  }

                  this.logger.LogDebug("The header we want to accept is already in our headers chain.");
                  return true;
               }

               if (header.PreviousBlockHash == null)
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "prev-hash-null", "previous hash null, allowed only on genesis block");
                  return false;
               }

               IHeaderValidationContext context = this.headerValidationContextFactory.Create(header);

               foreach (IHeaderValidationRule rule in this.headerValidationRules)
               {
                  if (!rule.Check(context, ref validationState))
                  {
                     this.logger.LogDebug("Header validation failed: {HeaderValidationState}", validationState.ToString());
                     return false;
                  }
               }
            }

            //lastProcessedHeader = AddToBlockIndex(block);

            lastProcessedHeader = this.headersTree.Add(header);

            return true;
         }
      }

      public void CheckBlockIndex()
      {
         // not sure it's needed
      }
   }
}
