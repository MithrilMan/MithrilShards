# Needs

When we get headers from peers, in order to validate the chain we need to fetch blocks.

We may face problems implementing a naive block downloader:

- If we download blocks everytime a new header is validated, we may waste resource validating blocks of forks that are far behind the tip.
- A bad peer could send us a long chain of technically valid headers but when we try later to validate its blocks, they could be invalid: this would cause the node to waste lot of resources to validate blocks that will turn out to be wrong.
- We must ask blocks only to peers that prove us they have such block (otherwise we may ban them while they are instead on a better chain and we'll sit on a fork)



A way to achieve a fair block distribution is to ask blocks to peer that proved us to have required blocks and with a good score, in order to maximixe the throughput and the chance to succeed.

The score system may take into consideration the ping time (obtained by ping messages) and can eventually track download speed of blocks to have a better understanding of the peer performance.

Note that a sheer byte/sec indicator isn't enough because the problem may be our network, so the peer speeds must be compared between them in order to diminish the impact of the node connection.

## Possible implementation

During header sync, peers check if they are in a position to ask for block downloads, judging by their position respect the node tip and their communicated best peer header.
Following bitcoin core implementation logic, a peer may trigger a block download when:

- its best known header has more chainwork than our current chain tip