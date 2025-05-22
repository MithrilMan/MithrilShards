# Roadmap for Bitcoin Full Node Implementation

## 1. Introduction

### Purpose
This document outlines a phased development path for updating the `MithrilShards.Chain.Bitcoin` project to a modern, compliant Bitcoin full node. The primary objective of this roadmap is to achieve full, from-scratch synchronization capability with the current Bitcoin network. Wallet functionality is explicitly out of scope for this iteration.

### Reference
This roadmap is built upon the requirements detailed in the "Summary of Modern Bitcoin Full Node Requirements" document and the architectural approaches proposed in the "Design for an Updated Bitcoin Full Node" document.

### Goal
The ultimate goal is to produce a functional full node that can reliably connect to the Bitcoin network, discover peers, download and validate the entire blockchain (headers and blocks) including SegWit and Taproot transactions, and maintain an up-to-date UTXO set.

## 2. Guiding Principles for Development

*   **Incremental Updates:** Features will be developed and integrated in logical, manageable blocks. Each phase will build upon the previous one, allowing for progressive enhancement and testing.
*   **Test-Driven Development (TDD) Approach:** Where feasible, unit tests will be written before or concurrently with feature implementation to ensure correctness from the ground up. Official Bitcoin test vectors will be utilized extensively.
*   **Integration Testing:** Regular and iterative testing against the Bitcoin testnet (and eventually signet or a private regtest environment) is crucial. This will help identify integration issues and ensure P2P compatibility.
*   **Code Reviews:** (Assuming a collaborative development environment) All significant changes should undergo peer review to maintain code quality, ensure adherence to the design, and catch potential issues early.
*   **Focus on Sync:** The highest priority will be given to features and fixes that are essential for discovering peers, establishing connections, downloading blockchain data (headers and blocks), and correctly validating all consensus rules up to the current chain tip.

## 3. Development Phases

### Phase 1: Core Protocol & Network Foundation Updates

*   **Objective:** Update basic P2P communication to modern standards and lay the groundwork for handling SegWit and Taproot data structures.
*   **Tasks:**
    *   **1.1: Update Protocol Version Handling:**
        *   Define `KnownVersion.CurrentHighestSupportedVersion` (e.g., `70016`). Research the latest stable protocol version used by major clients like Bitcoin Core for non-experimental features to determine the most appropriate value.
        *   Update `NodeImplementation.cs` instantiation (in `ForgeBuilderExtensions.cs`) to use this new highest version for announcements.
        *   Set `NodeImplementation.minimumSupportedVersion` to at least `70012` (post-BIP37 `filterload`/`filteradd`/`filterclear`, BIP130 `sendheaders`, BIP133 `feefilter`). Protocol version `70013` (BIP133 `feefilter`) is also a good candidate.
        *   In `HandshakeProcessor.cs`, ensure the outgoing `VersionMessage` uses the new highest version and accurately reflects initial service bits (`NODE_NETWORK` is fundamental. `NODE_WITNESS` must be set. `NODE_NETWORK_LIMITED` should be set if pruning is or will be supported by default).
        *   **Files:** `MithrilShards.Chain.Bitcoin/Protocol/Types/KnownVersion.cs`, `MithrilShards.Chain.Bitcoin/Network/NodeImplementation.cs`, `MithrilShards.Chain.Bitcoin/ForgeBuilderExtensions.cs`, `MithrilShards.Chain.Bitcoin/Protocol/Processors/HandshakeProcessor.cs`, `MithrilShards.Chain.Bitcoin/Protocol/Messages/Data/VersionMessage.cs`.
    *   **1.2: Implement `addrv2` and `sendaddrv2` (BIP155):**
        *   Create `Addrv2Message.cs` and `SendAddrv2Message.cs` in `MithrilShards.Chain.Bitcoin/Protocol/Messages/Network/`.
        *   Implement `Addrv2MessageSerializer.cs` and `SendAddrv2MessageSerializer.cs` in `MithrilShards.Chain.Bitcoin/Protocol/Serialization/Serializers/Messages/Network/`.
        *   Modify `HandshakeProcessor.cs` to send `SendAddrv2Message` after successful `verack` and to process incoming `SendAddrv2Message` from peers (e.g., setting `PeerContext.SupportsAddrV2 = true`).
        *   Update `AddressProcessor.cs` (or equivalent, e.g., `AddressManagerShard`) to:
            *   Receive and parse `Addrv2Message`.
            *   When relaying addresses, construct and send `Addrv2Message` if the peer supports it; otherwise, fall back to legacy `AddrMessage`.
        *   Update `PeerAddressBook.cs` (or similar data structure in `MithrilShards.Chain.Bitcoin/Network/`) to correctly store and manage addresses received via `addrv2`, including their network ID and variable length.
        *   **Files:** New message/serializer classes, `HandshakeProcessor.cs`, `AddressProcessor.cs` (or equivalent), `PeerAddressBook.cs` (or equivalent).
    *   **1.3: Implement `wtxidrelay` (BIP339) Signaling:**
        *   In `HandshakeProcessor.cs`:
            *   Send a `SendWtxidRelayMessage` (empty message, command `sendwtxidrelay`) after `verack` to signal the node's capability.
            *   Handle incoming `WtxidRelayMessage` (empty message, command `wtxidrelay`) from a peer by setting a flag on the `PeerContext` (e.g., `PeerContext.PreferWtxidRelay = true`).
        *   **Note:** Full handling of `MSG_WTX` in `getdata` requests is deferred to Phase 3. This task focuses on the handshake aspect.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Protocol/Processors/HandshakeProcessor.cs`, `MithrilShards.Chain.Bitcoin/Network/PeerContext.cs`. (May require creating new message classes `SendWtxidRelayMessage.cs` and `WtxidRelayMessage.cs` and their serializers if they don't exist).
    *   **1.4: `reject` Message Handling Update (BIP61 Context):**
        *   Audit the entire codebase for any instances where `RejectMessage` is constructed and sent.
        *   Remove all outgoing `RejectMessage` calls.
        *   Replace their functionality with:
            *   Detailed internal logging of the rejection reason.
            *   Incrementing a peer misbehavior score on the `PeerContext`.
            *   Disconnecting and potentially banning the peer if the score exceeds a defined threshold.
        *   Review if any logic depends on *receiving* `RejectMessage`; this should also be phased out in favor of direct validation and consequence management.
        *   **Files:** All protocol processors (e.g., `BlockProcessor.cs`, `TransactionProcessor.cs`), validation services, and any other components that currently send `reject` messages.
    *   **1.5: Basic SegWit Structure Verification (BIP141, BIP144):**
        *   In `MithrilShards.Chain.Bitcoin/Protocol/Types/`:
            *   Verify `Transaction.cs` can represent witness data (e.g., a `TransactionWitness` property).
            *   Verify `Block.cs` can store transactions with witness data.
            *   Verify `BlockHeader.cs` and related logic can access the witness commitment (which is part of the coinbase transaction's witness).
        *   Review `TransactionSerializer.cs` and `BlockSerializer.cs` to ensure they can correctly serialize and deserialize transactions and blocks that include witness data as per BIP144. This is about structural parsing, not full validation yet.
        *   **Files:** `Transaction.cs`, `Block.cs`, `BlockHeader.cs`, `TransactionWitness.cs` (if it exists or needs creation), `TransactionSerializer.cs`, `BlockSerializer.cs`.
    *   **1.6: Unit Tests for Phase 1 Components:**
        *   Develop unit tests for new messages (`Addrv2Message`, `SendAddrv2Message`, etc.) and their serializers.
        *   Test updated handshake logic.
        *   Test address processing and storage for `addrv2`.
        *   Test basic SegWit structure parsing.

### Phase 2: Full SegWit Validation and Initial Taproot Data Handling

*   **Objective:** Achieve full SegWit validation capability and prepare the node to parse and store Taproot-related data structures, even if full Taproot validation is not yet complete.
*   **Tasks:**
    *   **2.1: Full SegWit Validation Logic (BIP141, BIP143):**
        *   Implement the new signature validation logic for SegWit inputs (P2WPKH, P2WSH) as per BIP143. This will likely involve integrating or writing new sighash calculation methods. Leveraging NBitcoin's capabilities here is strongly advised.
        *   Implement the validation of the witness commitment in the block's coinbase transaction against the `wtxid`s of transactions in the block (BIP141).
        *   Integrate these validation rules thoroughly into `BlockValidator.cs` (or its equivalent validation pipeline) and any transaction-specific validation logic.
        *   Utilize official SegWit BIP test vectors for comprehensive testing.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Consensus/Validation/` (e.g., `BlockValidator.cs`, `TransactionValidator.cs`, new rule classes), potentially cryptography helper classes for sighashes if not fully covered by an external library.
    *   **2.2: Schnorr Signature Verification Stubs/Placeholders (BIP340):**
        *   Evaluate and select a C# library for BIP340 Schnorr signatures (e.g., NBitcoin, Secp256k1.NET, BouncyCastle). Prioritize libraries with good community support and proven correctness.
        *   Integrate the chosen library. Create wrapper functions or helper classes in `MithrilShards.Chain.Bitcoin/Cryptography/` if needed to abstract its usage.
        *   In the transaction validation logic where Taproot key-path spends would be validated, add placeholder calls to Schnorr signature verification. These might initially return `true` or log that verification is pending, to be fully implemented in Phase 3. This allows the overall structure to be built.
        *   **Files:** New or existing cryptography helper classes, `MithrilShards.Chain.Bitcoin/Consensus/Validation/TransactionValidator.cs`.
    *   **2.3: P2TR Output Parsing and Taproot Witness Structure (BIP341):**
        *   Update `TransactionOutput.cs` and associated script parsing utilities (e.g., `Script.cs`, `ScriptReader.cs`) to correctly identify Pay-to-Taproot (P2TR) outputs (script pattern: `OP_1 <32-byte x-only public key>`). Add methods like `IsP2TR()`.
        *   Ensure P2TR outputs can be correctly serialized, deserialized, and stored (e.g., in the UTXO set).
        *   Define structures in `TransactionWitness.cs` or `TransactionInput.cs` to hold Taproot witness data, including control blocks for script path spends.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Protocol/Types/TransactionOutput.cs`, `MithrilShards.Chain.Bitcoin/Protocol/Types/Script.cs`, script utility classes, `MithrilShards.Chain.Bitcoin/Protocol/Types/TransactionWitness.cs`.
    *   **2.4: Unit and Integration Tests for Phase 2 Components:**
        *   Extensive testing of SegWit validation using official BIP141 and BIP143 test vectors.
        *   Tests for P2TR output parsing and serialization.
        *   Initial integration tests on testnet to ensure blocks with SegWit transactions are processed correctly.

### Phase 3: Taproot Validation and Network Integration

*   **Objective:** Implement full Taproot (both key-path and script-path) validation and ensure the node correctly processes Taproot transactions and blocks as received from the network.
*   **Tasks:**
    *   **3.1: Full Schnorr Signature Verification (BIP340):**
        *   Complete the implementation and integration of Schnorr signature verification using the chosen library.
        *   Ensure this logic is correctly applied when validating Taproot key-path spends, using the appropriate sighash flags (e.g., `SIGHASH_ALL_TAPROOT`).
        *   Utilize BIP340 test vectors.
        *   **Files:** Cryptography helper classes, `MithrilShards.Chain.Bitcoin/Consensus/Validation/TransactionValidator.cs`.
    *   **3.2: Taproot Key-Path Spend Validation (BIP341):**
        *   Implement the full validation logic for key-path spends of P2TR outputs. This involves verifying the Schnorr signature against the Taproot output key.
        *   Integrate into `TransactionValidator.cs`.
        *   Utilize BIP341 test vectors for key-path spends.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Consensus/Validation/TransactionValidator.cs`.
    *   **3.3: Tapscript Validation (BIP341, BIP342):**
        *   Implement parsing of Taproot witness data for script-path spends, including the control block (to derive the tapleaf hash and verify the Merkle path) and the Tapscript itself.
        *   Update the script execution engine (`ScriptInterpreter.cs` or equivalent) to support Tapscript semantics as defined in BIP342. This includes:
            *   `OP_CHECKSIG`/`OP_CHECKSIGVERIFY` expecting Schnorr signatures.
            *   Implementing `OP_CHECKSIGADD`.
            *   Handling new `OP_SUCCESSx` opcodes.
            *   Other changes to opcode behavior and resource limits.
        *   This is a significant and complex task requiring meticulous attention to detail and thorough testing against BIP342 test vectors.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Consensus/Script/ScriptInterpreter.cs` (or equivalent), new classes for control block parsing and Tapscript validation rules within `MithrilShards.Chain.Bitcoin/Consensus/Validation/`.
    *   **3.4: Update Block Validation for Taproot:**
        *   Ensure `BlockValidator.cs` correctly orchestrates the validation of blocks containing Taproot transactions, correctly invoking P2TR output validation for both key-path and script-path spends.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Consensus/Validation/BlockValidator.cs`.
    *   **3.5: `getdata` Handling for `MSG_WTX` (BIP339):**
        *   Fully implement the logic in `DataFeederProcessor.cs` (or the component responsible for handling `getdata` messages).
        *   If a peer (for whom `PeerContext.PreferWtxidRelay` is true) requests a transaction using inventory type `MSG_WTX`, the node must respond with the full transaction data including the witness, serialized according to BIP144.
        *   Ensure `wtxid`s are readily available for transactions in the mempool or recently confirmed blocks to facilitate these responses.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Protocol/Processors/DataFeederProcessor.cs` (or equivalent), mempool management classes.
    *   **3.6: Unit and Integration Tests for Taproot (using official BIP test vectors):**
        *   Comprehensive tests for Schnorr signature validation (BIP340).
        *   Tests for key-path spend validation (BIP341).
        *   Extensive tests for Tapscript execution, covering all new/changed opcodes and rules (BIP342).
        *   Tests for control block parsing and validation.
        *   Integration tests on testnet with blocks containing Taproot transactions.

### Phase 4: Synchronization, Robustness & Testing

*   **Objective:** Ensure the node can reliably synchronize with the Bitcoin network (testnet first, then mainnet observation) from scratch and operate robustly under normal network conditions.
*   **Tasks:**
    *   **4.1: Chain Synchronization Testing (Testnet):**
        *   Initiate a full blockchain synchronization from scratch on the Bitcoin testnet.
        *   Monitor progress, identify and debug any issues related to:
            *   Peer discovery and connection management.
            *   Header and block download logic.
            *   Chain assembly and reorganization (`ChainState.cs` or equivalent).
            *   Correct validation of all historical blocks (including pre-SegWit, SegWit, and Taproot eras).
            *   UTXO set management.
        *   Watch for memory leaks, excessive CPU/disk usage, or deadlocks.
    *   **4.2: Peer Management Review and Enhancement:**
        *   Review existing peer scoring, banning logic, and connection slot management (`ConnectionManager.cs` or equivalent).
        *   Enhance these mechanisms based on observations from testnet sync and modern best practices to improve resilience against unreliable or misbehaving peers.
        *   **Files:** `MithrilShards.Chain.Bitcoin/Network/ConnectionManager.cs`, peer management and scoring components.
    *   **4.3: Performance Profiling and Optimization:**
        *   Once basic sync is working, use profiling tools to identify performance bottlenecks, particularly in:
            *   Script validation (especially complex SegWit or Tapscripts).
            *   Signature verification (ECDSA and Schnorr).
            *   Database/UTXO set access.
            *   Message serialization/deserialization.
        *   Implement targeted optimizations for critical code paths without sacrificing correctness.
    *   **4.4: Comprehensive Logging Review:**
        *   Review all logging statements across the updated components.
        *   Ensure logs are clear, informative, and provide sufficient diagnostic information for troubleshooting.
        *   Adjust log levels to avoid excessive verbosity in normal operation while allowing for detailed debugging when needed.
    *   **4.5: (Optional) Implement other minor P2P enhancements if critical for stable sync:**
        *   Depending on findings during testnet sync, minor P2P features or adjustments not initially prioritized might become necessary for stable operation (e.g., more nuanced handling of `feefilter` if not already robust, or specific DoS protections).
    *   **4.6: Final Mainnet Sync Test (Observation Mode):**
        *   After achieving stability and good performance on testnet, attempt a full sync on the Bitcoin mainnet.
        *   Initially, run the node in an "observation" or "listen-only" mode if possible (e.g., not advertising its address widely, limiting outgoing connections) to minimize risk.
        *   The primary goal is to ensure it can correctly parse, validate, and process all mainnet blocks and transactions up to the current tip.

## 4. Post-Roadmap (Future Work - Out of Scope for this Initial Upgrade)

Once the node can reliably sync and validate the chain as a modern full node, further enhancements can be considered. These are explicitly out of scope for the current roadmap aimed at achieving core sync functionality:

*   **Wallet Functionality:** Implementing private key management, transaction creation, signing (including for SegWit and Taproot outputs), and balance tracking.
*   **Advanced P2P Features:**
    *   **Erlay (BIP330):** For more efficient transaction relay.
    *   **Compact Block Filters (BIP157/BIP158):** To support light clients.
    *   **P2P Encryption (BIP324):** For enhanced privacy of P2P communication.
*   **Advanced DoS Mitigation:** Implementing more sophisticated DoS detection and prevention mechanisms beyond standard peer management.
*   **Pruning:** Implementing logic to prune old block data to save disk space.
*   **Mempool Policy Enhancements:** Refining mempool acceptance and eviction logic to align more closely with Bitcoin Core's policies or specific project needs.

This roadmap provides a structured approach to a complex upgrade. Flexibility will be required, and priorities may shift based on challenges encountered during development and testing. Consistent testing and adherence to Bitcoin's consensus rules are paramount throughout the process.
