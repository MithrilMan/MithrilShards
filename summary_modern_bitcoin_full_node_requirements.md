# Summary of Modern Bitcoin Full Node Requirements

## 1. Introduction

The purpose of this document is to outline the necessary features and protocol changes required to upgrade an existing Bitcoin full node, currently based on protocol version 70015 (dating back to January 2017), to a modern, compliant full node. This summary will cover key P2P protocol enhancements, significant feature updates like Segregated Witness (SegWit) and Taproot, and other considerations crucial for robust participation in the current Bitcoin network.

## 2. Core P2P Protocol Version & Capabilities

### Protocol Version Negotiation
While the fundamental structure of the `version` message remains similar, modern Bitcoin nodes have evolved. Nodes may announce higher protocol versions (e.g., 70016 or newer specific to implementations like Bitcoin Core). More significantly, reliance has shifted towards using service bits to explicitly signal supported capabilities. The upgraded node must accurately reflect its feature set through these mechanisms rather than solely relying on an older protocol version number. Announcing a higher protocol version without implementing corresponding features can lead to network incompatibilities.

### Service Bits
Correctly setting and interpreting service bits is paramount for modern network interaction. Key service bits include:
*   **`NODE_NETWORK`**: Indicates the node is a full node capable of serving blockchain data. This remains fundamental.
*   **`NODE_WITNESS`**: Signals support for Segregated Witness (SegWit). This was introduced around protocol version 70012 and is critical. The existing node (70015) should already set this, but its full implications across the system must be ensured.
*   **`NODE_NETWORK_LIMITED`**: Indicates a pruned node. If the upgraded node supports pruning, this bit should be set.
*   **`NODE_COMPACT_FILTERS` (BIP157/BIP158)**: Signals support for serving compact block filters, allowing light clients to sync more efficiently and privately. This is a common feature in modern nodes.
*   Other service bits related to specific features (e.g., `NODE_P2P_V2` if BIP324 is implemented) should be considered based on the desired feature set of the upgraded node.

The node must not only announce its own capabilities correctly but also interpret the service bits of its peers to understand their supported features and interact with them appropriately.

## 3. Key P2P Message and Feature Updates

### `addrv2` and `sendaddrv2` (BIP155)
*   **Limitations of `addr` (v1):** The original `addr` message, used for peer address relay, has several limitations. It only supports IPv4, IPv6, and Tor v2 addresses (now deprecated). The fixed-size address field is inefficient for Tor v3 and other potential future address types.
*   **`addrv2` Message Format:** BIP155 introduces the `addrv2` message, which allows for variable-length network identifiers and addresses. This makes it extensible for different address types, including Tor v3 onion addresses and I2P. It includes a network ID field to specify the address type (e.g., IPv4, IPv6, Tor v3, I2P).
*   **`sendaddrv2` Message:** Peers signal their willingness to receive `addrv2` messages by sending a `sendaddrv2` message (an empty message). An upgraded node should send `sendaddrv2` to its peers during handshake and be prepared to receive and send `addrv2` messages if the peer also supports it. If a peer does not send `sendaddrv2`, the node should fall back to using the older `addr` message format for compatibility, but prioritize `addrv2` where available.

### `wtxidrelay` (BIP339)
*   **Purpose:** `wtxidrelay` allows peers to request transactions using their witness transaction ID (`wtxid`) instead of the traditional transaction ID (`txid`). It also enables peers to announce new transactions using `wtxid`s.
*   **Benefits for SegWit Nodes:** This is particularly beneficial for SegWit-enabled nodes. The `wtxid` covers all transaction data, including the witness, while the `txid` does not. By relaying `wtxid`s, nodes can avoid downloading witness data for transactions if they are not interested in it (e.g., for non-witness validation or if they already have the witness data). This can significantly reduce bandwidth, especially when combined with compact block relay. Peers signal support for `wtxidrelay` by sending a `sendwtxidrelay` message. The node should then use `inv` messages with `MSG_WTX` inventory types to announce transactions via their `wtxid`.

### `feefilter` (BIP133)
*   **Purpose:** The `feefilter` message allows a node to inform its peers about the minimum fee rate (satoshis per kilobyte) of transactions it is willing to relay. Peers receiving this message should not send `inv` or `tx` messages for transactions below this fee rate.
*   **Modern Relevance:** Although introduced in protocol version 70013 and thus expected to be present in a 70015-based node, it's crucial to confirm its full and correct implementation and usage. This includes honoring received `feefilter` messages and potentially sending its own `feefilter` based on its mempool policy. This helps reduce bandwidth spent on low-fee transactions that the node would likely not relay anyway.

### Removal of `reject` message (BIP61 context)
*   **Deprecation:** The `reject` message (BIP61), previously used to inform peers why a transaction or block was rejected, was deprecated and its use strongly discouraged. Bitcoin Core removed sending `reject` messages in version 0.18.0 (March 2019) and later versions also removed processing of incoming `reject` messages.
*   **Modern Handling:** Modern nodes should *not* send `reject` messages. Relying on receiving `reject` messages from peers to determine misbehavior or invalid transactions is no longer a viable strategy. Instead, nodes should disconnect and potentially ban peers that send invalid data. Robust internal logging and debugging mechanisms are essential for identifying reasons for transaction/block invalidity, rather than relying on peer-provided `reject` reasons which could be misleading or used in DoS attacks.

## 4. Segregated Witness (SegWit) (BIP141, BIP143, BIP144)

While the existing node (protocol version 70015) is "witness aware" (indicated by the `NODE_WITNESS` service bit and `VersionMessage` field `nVersion >= 70012`), upgrading to a modern node requires ensuring *full, correct, and efficient* processing of SegWit data throughout the entire system. This goes beyond simple announcement.

*   **Transaction Deserialization and Serialization:** The node must be able to correctly parse transactions that include witness data (SegWit marker, flag, witness program, and witness stack) and serialize them, including for transmission to peers.
*   **Block Processing & Witness Commitment (BIP141):**
    *   Blocks containing SegWit transactions include a witness commitment in the coinbase transaction's `witness` field. This commitment is a hash of all `wtxid`s in the block.
    *   The block header's commitment structure (e.g., a witness reserved value in `scriptSig` of coinbase) must be correctly validated against this witness commitment.
*   **Transaction Validation & Sighash Algorithms (BIP143):**
    *   SegWit introduced new sighash algorithms for transaction inputs spending native SegWit outputs (P2WPKH, P2WSH). The node must implement these algorithms correctly to validate signatures for SegWit transactions. This is critical for chain security.
*   **`wtxid` Calculation and Usage:** The node must be able_to calculate the Witness Transaction ID (`wtxid`) for all transactions (both SegWit and legacy) and use it where appropriate (e.g., in `wtxidrelay`, block commitment). The `txid` remains the primary identifier for non-witness transactions and for general transaction indexing for legacy systems, but `wtxid` is crucial for SegWit-related operations.
*   **Inventory Types:** The node must support and correctly process new inventory types related to SegWit:
    *   `MSG_WITNESS_TX`: Requesting a transaction with witness data.
    *   `MSG_WITNESS_BLOCK`: Requesting a full block with witness data.
    *   `MSG_CMPCT_BLOCK` (Compact Blocks, BIP152): When used in conjunction with SegWit, compact blocks must also correctly handle witness data, typically by sending transaction `wtxid`s and allowing peers to request missing transactions with witness if needed.

## 5. Taproot (BIP340, BIP341, BIP342)

Taproot, activated in November 2021, represents a major upgrade to Bitcoin, enhancing privacy, efficiency, and scriptability. Full support for Taproot is essential for a modern node.

### Overview
Taproot introduces Pay-to-Taproot (P2TR) outputs, which can be spent either via a "key path" (using a Schnorr signature) or one of many "script paths" (using a Merkleized Abstract Syntax Tree - MAST). This makes complex scripts indistinguishable from simple key spends on-chain, improving privacy.

### Schnorr Signatures (BIP340)
*   **New Signature Scheme:** Taproot introduces Schnorr signatures as the standard for key path spends and for signatures within Tapscript. Schnorr signatures offer several advantages over ECDSA, including linearity (enabling aggregation) and smaller size for some use cases.
*   **Implications for Transaction Validation:** The node must be able_to parse and validate Schnorr signatures according to BIP340 rules. This includes new sighash flags and message construction for signing.

### Taproot Outputs (P2TR - Pay-to-Taproot) (BIP341)
*   **New Output Type:** P2TR outputs are defined by the script `OP_1 <32-byte public key>`, where the public key is a "Taproot output key" (usually an X-only pubkey).
*   **Key Path Spending:** The default and most efficient way to spend a P2TR output. It requires a valid Schnorr signature from the private key corresponding to the Taproot output key.
*   **Script Path Spending:** If the conditions for key path spending cannot be met, or if more complex logic is desired, P2TR outputs can be spent by revealing a specific script from a (hidden) Merkle tree of scripts, along with a control block and inputs satisfying that script. The Taproot output key commits to this Merkle root.

### Tapscript (BIP342)
*   **Scripting Enhancements:** Tapscript defines the semantics for scripts used in script path spends of P2TR outputs. It introduces several changes compared to legacy Script:
    *   Signature opcodes (`OP_CHECKSIG`, `OP_CHECKSIGVERIFY`) now expect Schnorr signatures.
    *   New opcodes like `OP_CHECKSIGADD` for batch verification.
    *   Removal of signature hashing from the script interpreter (sighash is now fixed).
    *   Changes to resource limits and opcode behavior (e.g., `OP_SUCCESSx` opcodes).
*   **MAST-like Structures:** Tapscript enables efficient MAST, where only the executed script path is revealed on-chain.
*   **Validation Rules:** The node must implement all new Tapscript validation rules, including parsing control blocks, Merkle proofs for script paths, and executing Tapscripts correctly.

### P2P Implications for Taproot
*   **No New P2P Messages:** Taproot itself does not introduce new P2P messages.
*   **Existing Message Compatibility:** Transactions with P2TR outputs and inputs spending them are relayed using existing `tx` messages. Blocks containing Taproot transactions are relayed using existing `block` messages.
*   **Parsing and Validation:** The core requirement is that the node's transaction and block validation logic must be fully Taproot-aware. This includes:
    *   Recognizing and validating P2TR output scripts.
    *   Validating inputs spending P2TR outputs, whether via key path (Schnorr signature) or script path (Tapscript execution, control block, Merkle proof).
    *   Ensuring blocks containing Taproot transactions adhere to all consensus rules.

## 6. Other Considerations for Modern Nodes

### Network Robustness
*   **DoS Resistance:** Over the years, numerous improvements have been made to Bitcoin Core and other node implementations to enhance resistance against various Denial-of-Service (DoS) attacks. This includes stricter validation rules, better resource management for peer connections, and improved detection of misbehaving peers. While a deep dive is beyond this summary, the upgraded node should incorporate modern best practices for peer management and data validation to protect itself and the network.
*   **Peer Management:** Modern nodes often have more sophisticated logic for selecting peers, managing connection slots, and handling peer scoring to prioritize good peers and quickly disconnect malicious or unhelpful ones.

### Chain Sync
*   **Efficient Sync:** A modern node must be able to efficiently and reliably synchronize the entire blockchain from scratch (Initial Block Download - IBD) or from a recent state. This involves optimized block fetching, validation, and disk I/O. While not a direct P2P message change since 70015, the overall sync process has seen many performance improvements in node implementations (e.g., parallel validation, optimized database writes). The upgraded node should aim for competitive sync performance.
*   **AssumeUTXO (Bitcoin Core specific, but relevant concept):** While not a protocol rule, initiatives like AssumeUTXO in Bitcoin Core aim to drastically speed up initial sync for new nodes by allowing them to start from a recent, validated UTXO set snapshot. Awareness of such developments is useful for long-term node strategy.

## 7. Conclusion

This summary outlines the key P2P protocol versions, message updates, and major feature enhancements like SegWit and Taproot that are essential for upgrading an existing Bitcoin full node (from a 70015 baseline) to a modern, secure, and compliant participant in the Bitcoin network. Addressing these points will form the foundation for the design and development work required for the update, ensuring the node can correctly validate all current consensus rules, interact efficiently with contemporary peers, and contribute to the overall health and security of the Bitcoin ecosystem.
