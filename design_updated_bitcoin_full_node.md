# Design for an Updated Bitcoin Full Node

## 1. Introduction

### Purpose
This document outlines the proposed architectural and code-level changes required to upgrade the `MithrilShards.Chain.Bitcoin` project. The goal is to transform the existing implementation into a modern Bitcoin full node that complies with current network consensus rules and peer-to-peer (P2P) protocols.

### Reference
This design is based on the requirements and features detailed in the "Summary of Modern Bitcoin Full Node Requirements" document.

## 2. Core Philosophy

*   **Modularity:** The upgrade will adhere to the existing modular design principles of the Mithril Shards framework. New features and protocol enhancements will be implemented as distinct components or "shards" where appropriate, or integrated into existing shards in a clean and maintainable way.
*   **Leverage Existing Libraries:** For complex cryptographic operations, particularly Schnorr signatures (BIP340) and potentially aspects of SegWit validation (BIP141, BIP143, BIP144) and Taproot (BIP341, BIP342), the project will prioritize the use of well-tested, community-trusted C# Bitcoin libraries such as NBitcoin or Secp256k1.NET. If suitable libraries are unavailable or their integration proves problematic, the design will specify the need for new or updated internal cryptographic components, acknowledging the significant effort and risk associated with custom cryptographic implementations.
*   **Testability:** All changes and new components will be designed with testability as a core principle. This includes extensive unit tests for individual functions and classes, integration tests for component interactions, and the use of standard Bitcoin test vectors.

## 3. Proposed Changes by Component/Feature

The following sections detail the proposed changes to the `MithrilShards.Chain.Bitcoin` codebase, referencing specific files and areas where modifications are anticipated.

### Protocol Version & Capabilities
*   **Files Affected:** `KnownVersion.cs`, `NodeImplementation.cs`, `BitcoinShard.cs`, `ForgeBuilderExtensions.cs`, `HandshakeProcessor.cs`, `VersionMessage.cs`.
*   **Changes:**
    *   **`KnownVersion.cs`:**
        *   Add a new constant, e.g., `CurrentHighestSupportedVersion = 70016;` (or higher if more recent stable protocol versions are targeted for specific features beyond this scope). This will represent the protocol version the node announces.
        *   Consider adding constants for specific feature-related protocol versions if needed for conditional logic (e.g., `P2P_V2_VERSION` if BIP324 were to be implemented).
    *   **`NodeImplementation.cs` (instantiated in `ForgeBuilderExtensions.cs`):**
        *   The `protocolVersion` parameter passed to the `NodeImplementation` constructor should be updated to `KnownVersion.CurrentHighestSupportedVersion`.
        *   The `minimumSupportedVersion` should be evaluated. While 70015 is the baseline, a modern node might consider a higher minimum (e.g., post-SegWit activation, 70013) to reduce complexity of supporting very old peers, or keep it flexible based on specific shard requirements.
    *   **`HandshakeProcessor.cs` (`CreateVersionMessage` method):**
        *   **`VersionMessage.Version`:** Set this to `KnownVersion.CurrentHighestSupportedVersion`.
        *   **`VersionMessage.Services`:**
            *   Ensure `NODE_WITNESS` is set (should already be for 70015).
            *   Evaluate and add `NODE_NETWORK_LIMITED` if pruning is supported or planned.
            *   Consider adding `NODE_COMPACT_FILTERS` (BIP157/158) if this feature is to be implemented.
            *   Dynamically add other service bits based on implemented features (e.g., `NODE_P2P_V2` if BIP324 is implemented).
    *   **Peer Version and Service Handling:**
        *   Review logic in `HandshakeProcessor.cs` and other relevant areas that check peer versions and their advertised services. Ensure the node interacts correctly based on mutually supported features. For example, not attempting to send `addrv2` to a peer that hasn't signaled support.

### Address Relay (`addrv2`/`sendaddrv2` - BIP155)
*   **Files/Components Affected:**
    *   New Messages: `Addrv2Message.cs`, `SendAddrv2Message.cs` (in `MithrilShards.Chain.Bitcoin/Protocol/Messages/Network/`)
    *   New Serializers: `Addrv2MessageSerializer.cs`, `SendAddrv2MessageSerializer.cs` (in `MithrilShards.Chain.Bitcoin/Protocol/Serialization/Serializers/Messages/Network/`)
    *   Processors: `HandshakeProcessor.cs`, `AddressProcessor.cs` (or a new `AddressRelayProcessor.cs`)
    *   Data Structures: `PeerAddressBook.cs` (in `MithrilShards.Chain.Bitcoin/Network/`)
*   **Changes:**
    *   **Messages:**
        *   **`SendAddrv2Message.cs`:** An empty message used to signal `addrv2` support. Command: `sendaddrv2`.
        *   **`Addrv2Message.cs`:** Contains a list of `NetworkAddressTimestampV2` objects. Command: `addrv2`.
            *   `NetworkAddressTimestampV2` will need to support variable-length addresses and network IDs as per BIP155.
    *   **Serializers:**
        *   Implement serializers for `SendAddrv2Message` (empty payload) and `Addrv2Message` (handling the new address format).
    *   **Processors:**
        *   **`HandshakeProcessor.cs`:**
            *   After successfully sending `VerackMessage` and receiving the peer's `VerackMessage`, send `SendAddrv2Message` to the peer.
            *   When receiving a `SendAddrv2Message` from a peer, set a flag/property on the `PeerContext` (e.g., `PeerContext.SupportsAddrV2 = true;`).
        *   **`AddressProcessor.cs` (or equivalent):**
            *   **Receiving `Addrv2Message`:** If a peer sends `Addrv2Message`, parse it and update the `PeerAddressBook`.
            *   **Sending Addresses:** When relaying addresses to a peer:
                *   If `PeerContext.SupportsAddrV2` is true, construct and send `Addrv2Message`.
                *   Otherwise, fall back to sending the legacy `AddrMessage`.
            *   Prioritize `addrv2` when available.
    *   **Data Structures:**
        *   **`PeerAddressBook.cs`:** Modify or extend to store addresses received via `Addrv2Message`, including their network ID and potentially longer address data. This might involve a new internal representation or adapting the existing `PeerAddress` structure.

### Transaction Relay (`wtxidrelay` - BIP339)
*   **Files/Components Affected:** `HandshakeProcessor.cs`, `PeerContext.cs`, `InventoryVector.cs` (or `InvMessage.cs`), `GetDataMessage.cs`, relevant transaction processing logic.
*   **Changes:**
    *   **`HandshakeProcessor.cs` / `PeerContext.cs`:**
        *   The node should send a `SendWtxidRelayMessage` (empty message, command `sendwtxidrelay`) after `verack` to signal its own support for `wtxidrelay`.
        *   Handle incoming `WtxidRelayMessage` (empty message, command `wtxidrelay` - note: BIP339 defines `wtxidrelay` as the message from peer to signal preference, and the node itself would send `sendwtxidrelay` to signal capability. The summary document had this slightly mixed up, so clarifying here based on BIP339 text). On receipt of `wtxidrelay` from a peer, set `PeerContext.PreferWtxidRelay = true;`.
    *   **Inventory Announcements (`InvMessage`):**
        *   BIP339 states: "Nodes which have negotiated wtxidrelay SHOULD announce all their transactions using type `MSG_TX`." This means `inv` messages continue to use `MSG_TX`.
    *   **Transaction Requests (`GetDataMessage`):**
        *   If `PeerContext.PreferWtxidRelay` is true for a peer, that peer MAY request transactions using the inventory type `MSG_WTX` (value `5`) in their `GetDataMessage`.
        *   The node's logic for handling `GetDataMessage` must:
            *   Recognize `MSG_WTX` inventory type.
            *   If `MSG_WTX` is requested for a transaction, respond with the transaction including its full witness data (serialized as per BIP144).
            *   If `MSG_TX` is requested, respond with the transaction without witness data (or as traditionally serialized).
    *   **Announcing to `wtxidrelay` Peers:**
        *   While `inv` still uses `MSG_TX`, the node should be prepared for a `wtxidrelay`-enabled peer to immediately follow up with a `getdata` for `MSG_WTX`. This means the node should have the `wtxid` readily available for transactions it announces.
    *   **Internal `wtxid` Handling:**
        *   Ensure `wtxid` is calculated and potentially cached for all transactions in the mempool to efficiently respond to `MSG_WTX` requests.

### `reject` Message Deprecation (BIP61 context)
*   **Files Affected:** All code locations that currently send `RejectMessage`. Transaction and block validation processors.
*   **Changes:**
    *   **Remove Sending `RejectMessage`:**
        *   Search for all instances of `networkMessageBroker.EnqueueMessageAsync(peerContext, new RejectMessage(...))`.
        *   Replace these instances with:
            *   Detailed internal logging of the rejection reason (invalid transaction, block, etc.).
            *   Incrementing a peer misbehavior score on the `PeerContext`.
            *   If the misbehavior score exceeds a threshold, disconnect the peer and potentially ban them for a period.
    *   **Remove Reliance on Receiving `RejectMessage`:**
        *   Review if any logic relies on information from received `RejectMessage`s. This is unlikely to be a primary decision driver but should be checked.
        *   Focus on proactive validation and handling of invalid data directly, rather than reacting to `reject` messages.

### Segregated Witness (SegWit) Full Integration (BIP141, BIP143, BIP144)
*   **Files/Components Affected:** `Transaction.cs`, `TransactionInput.cs`, `TransactionOutput.cs`, `TransactionWitness.cs` (new or existing), `TransactionSerializer.cs`, `Block.cs`, `BlockHeader.cs`, `BlockSerializer.cs`, `BlockValidator.cs`, transaction validation logic, signature verification logic.
*   **Changes:**
    *   **Data Structures:**
        *   **`Transaction.cs`:**
            *   Ensure it has a `TransactionWitness Witness { get; set; }` property (or similar structure like `List<TransactionInputWitness>`).
            *   Add methods `GetHash()` (traditional txid) and `GetWitnessHash()` (`wtxid`).
        *   **`TransactionInput.cs`:**
            *   Needs a `ScriptWitness WitnessScript { get; set; }` property (e.g., `new ScriptWitness(List<byte[]> pushes)`).
        *   **`Block.cs`:**
            *   Ensure transactions within the block can contain witness data.
        *   **`BlockHeader.cs`:**
            *   The witness commitment needs to be stored or calculated. BIP141 specifies the commitment is stored in the coinbase transaction's witness. The block header itself doesn't change structure for this, but validation requires checking this commitment.
    *   **Serialization (`TransactionSerializer.cs`, `BlockSerializer.cs`):**
        *   Update `TransactionSerializer` to correctly parse and serialize transactions according to BIP144 (marker, flag, witness data).
        *   Ensure `BlockSerializer` correctly handles blocks containing SegWit transactions.
    *   **Validation:**
        *   **`wtxid` Calculation:** Implement correct `wtxid` calculation (double SHA256 of transaction with marker, flag, and witness data).
        *   **Witness Commitment (BIP141):**
            *   In `BlockValidator.cs`, after validating all transactions:
                *   Calculate the witness Merkle root from all `wtxid`s in the block.
                *   Extract the witness commitment from the coinbase transaction's witness data.
                *   Verify that the commitment matches the calculated witness Merkle root.
        *   **Signature Validation (BIP143):**
            *   Update signature verification logic (e.g., in a `TransactionSignatureChecker` class).
            *   When validating inputs spending P2WPKH or P2WSH outputs, use the new sighash algorithm defined in BIP143. This is a critical and complex part. Leverage of NBitcoin's `Transaction.Sign` and `Verify` methods, which handle SegWit sighashes, is highly recommended.
        *   **Script Validation:** For P2WSH, the script in the witness is executed, similar to P2SH.
    *   **Test Vectors:** Extensively use BIP141, BIP143, and BIP144 test vectors.

### Taproot (BIP340, BIP341, BIP342)
*   **Files/Components Affected:** Cryptography libraries/classes, `TransactionOutput.cs`, `TransactionInput.cs`, script handling classes (`Script.cs`, `ScriptInterpreter.cs`), `TransactionValidator.cs`, `BlockValidator.cs`.
*   **Changes:**
    *   **Cryptography (BIP340 - Schnorr Signatures):**
        *   **Library Integration:** Integrate a C# library that provides Schnorr signature verification (and potentially signing, for wallet features later) compatible with BIP340 (e.g., Secp256k1.NET, NBitcoin). This includes functions for public key parsing (x-only keys), signature parsing, and verification.
        *   **Wrapper Class:** Create a `SchnorrValidator` or similar utility class that wraps the chosen library's functions for ease of use within the MithrilShards codebase.
    *   **Data Structures:**
        *   **`TransactionOutput.cs`:**
            *   Modify `ScriptPubKey` handling to recognize P2TR outputs: `OP_1 <32-byte x-only public key>`. Add a property like `IsP2TR()`.
        *   **`TransactionInput.cs` / `TransactionWitness.cs`:**
            *   Taproot spends occur in the witness. The `scriptSig` must be empty for P2TR spends.
            *   The witness stack will contain:
                *   For key path spends: a single Schnorr signature.
                *   For script path spends: the script itself, a control block, and other witness items required by the script.
            *   Need structures to represent control blocks and easily access witness components for Taproot.
    *   **Validation (BIP341, BIP342):**
        *   **`TransactionValidator.cs` / `BlockValidator.cs`:**
            *   **Output Recognition:** Identify P2TR outputs during validation.
            *   **Input Spending Rules:**
                *   If an input spends a P2TR output:
                    *   Determine if it's a key path or script path spend based on the witness stack size and content of the annex (if present).
                    *   **Key Path Spend Validation (BIP341):**
                        *   Use the integrated Schnorr signature library to verify the signature against the Taproot output key using the appropriate sighash method (BIP341 specifies a new default sighash, `SIGHASH_ALL_TAPROOT`).
                    *   **Script Path Spend Validation (BIP341 & BIP342 - Tapscript):**
                        *   Parse the control block to extract the tapleaf hash and internal public key.
                        *   Verify the Merkle proof implicit in the control block against the P2TR output key.
                        *   Deserialize the revealed script (Tapscript).
                        *   Execute the Tapscript using the remaining witness items as input. The script interpreter needs to be updated for Tapscript semantics:
                            *   `OP_CHECKSIG`/`OP_CHECKSIGVERIFY` now expect Schnorr signatures.
                            *   Implement `OP_CHECKSIGADD`.
                            *   Handle new rules for `OP_SUCCESSx`.
                            *   Other changes as per BIP342.
        *   **Sighash Implementation:** Ensure all Taproot-specific sighash flags and algorithms (BIP341) are correctly implemented or available via the chosen crypto library.
    *   **Relay Logic:**
        *   No new P2P messages for Taproot. Existing `tx` and `block` messages will carry Taproot data. The core work is ensuring the node's internal validation logic is Taproot-aware.

## 4. Testing Strategy

*   **Unit Tests:**
    *   Each new network message (`Addrv2Message`, `SendAddrv2Message`, etc.) and its serializer.
    *   `wtxid` calculation.
    *   SegWit: Witness serialization/deserialization, witness commitment validation, BIP143 sighash generation and verification.
    *   Taproot: Schnorr signature verification (using test vectors from BIP340), P2TR output recognition, Tapscript execution for various opcodes (BIP342 test vectors), control block parsing and validation.
*   **Integration Tests:**
    *   Handshake sequence including `sendaddrv2` and `wtxidrelay` exchange.
    *   Address relay: sending `AddrMessage` vs `Addrv2Message` based on peer capability.
    *   Transaction relay: requesting transactions via `MSG_WTX` vs `MSG_TX`.
    *   Full block validation with SegWit and Taproot transactions.
*   **Test Vectors:**
    *   Actively use official BIP test vectors for SegWit (BIP141, BIP143, BIP144) and Taproot (BIP340, BIP341, BIP342).
    *   Reference Bitcoin Core's test files for additional scenarios.
*   **Testnet Sync:**
    *   Perform a full sync on the Bitcoin testnet (or a signet) to ensure P2P compatibility, correct handling of diverse real-world transactions and blocks, and overall stability. This is a crucial step to catch issues not covered by synthetic tests.

## 5. Affected Modules (Summary List)

*   `MithrilShards.Chain.Bitcoin/Protocol/Messages/Network/` (new messages: `Addrv2Message.cs`, `SendAddrv2Message.cs`, `SendWtxidRelayMessage.cs`, `WtxidRelayMessage.cs`)
*   `MithrilShards.Chain.Bitcoin/Protocol/Messages/Data/` (updates to `VersionMessage.cs`, `InventoryVector.cs`, `GetDataMessage.cs`)
*   `MithrilShards.Chain.Bitcoin/Protocol/Serialization/Serializers/Messages/Network/` (new serializers for network messages)
*   `MithrilShards.Chain.Bitcoin/Protocol/Serialization/Serializers/Messages/Data/` (updates for existing message serializers)
*   `MithrilShards.Chain.Bitcoin/Protocol/Processors/` (`HandshakeProcessor.cs`, `AddressProcessor.cs`, potentially new processors like `TransactionRelayProcessor.cs`)
*   `MithrilShards.Chain.Bitcoin/Consensus/Validation/` (major updates to `BlockValidator.cs`, `TransactionValidator.cs`, new script validation rules for Tapscript)
*   `MithrilShards.Chain.Bitcoin/Protocol/Types/` (`Transaction.cs`, `TransactionInput.cs`, `TransactionOutput.cs`, `Block.cs`, `BlockHeader.cs`, new `TransactionWitness.cs`, `ControlBlock.cs`)
*   `MithrilShards.Chain.Bitcoin/Network/` (`PeerAddressBook.cs`, `NodeImplementation.cs`, `PeerContext.cs`)
*   `MithrilShards.Chain.Bitcoin/` (`BitcoinShard.cs`, `ForgeBuilderExtensions.cs` for service registration and setup)
*   `MithrilShards.Chain.Bitcoin/Cryptography/` (new module or classes for Schnorr signature handling, potentially wrapping an external library)
*   `MithrilShards.Chain.Bitcoin/Utils/` (potentially new helper utilities for Taproot or SegWit)

## 6. Open Questions/Future Considerations

*   **Schnorr Signature Library:**
    *   **Evaluation:** Conduct a thorough evaluation of available C# libraries (NBitcoin, Secp256k1.NET, BouncyCastle) for BIP340 Schnorr signature support. Key criteria: correctness (BIP340 compliance), performance, maintainability, licensing, and ease of integration.
    *   **Fallback Plan:** If no suitable library is found, implementing Schnorr signatures internally is a high-risk, high-effort task that would require significant cryptographic expertise and rigorous testing. This should be a last resort.
*   **Tapscript Engine:**
    *   Building a fully compliant and secure Tapscript interpreter (BIP342) is a complex task. Leveraging existing script evaluation logic from established libraries (if possible and adaptable) or meticulous implementation based on BIP specifications and Bitcoin Core's reference implementation will be critical.
*   **Performance Optimizations:**
    *   Post-upgrade, profile the node under load (especially during IBD and when processing blocks with many SegWit/Taproot transactions) to identify bottlenecks in the new validation or serialization logic.
*   **Advanced P2P Features (Future Scope):**
    *   **Erlay (BIP330):** Consider for future enhancements to transaction relay efficiency.
    *   **Compact Block Filters (BIP157/BIP158):** Could be added for improved light client support.
    *   **P2P Encryption (BIP324):** For enhanced privacy on the P2P network.
*   **DoS Protections:**
    *   Continuously review and incorporate modern DoS protection mechanisms and peer management strategies from Bitcoin Core as the network evolves.
*   **Configuration:**
    *   Expose new features (e.g., enabling/disabling wtxidrelay if it's optional for self, though generally nodes should support it) via the shard's configuration system.

This design document provides a roadmap for the development effort. Each major feature (SegWit, Taproot, `addrv2`, etc.) will likely be broken down into smaller, manageable tasks during implementation.
