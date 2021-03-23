---
title: Testing node communication
description: Mithril Shards bitcoin implementation, testing node
---

--8<-- "refs.txt"

# Testing node communication

In order to test the Forge implementation, I used the handy [bitcoin testnet in a box](https://github.com/freewil/bitcoin-testnet-box){:target="_blank"}	

I used the docker implementation:

`docker pull freewil/bitcoin-testnet-box`

then I modified the suggested docker run arguments, in order to open the node port, this way, to run the image with an reachable testnet node, run the image as

`docker run -t -i -p 19000:19000 -p 19001:19001 -p 19011:19011 freewil/bitcoin-testnet-box`

after that, in the tty console, write `make start` to start the node and this way you can connect to the testnet box using the endpoint `127.0.0.1:19000`

If everything is going as expected and you ran the Forge within bitcoin-testnet network, you should see something similar to this

![img](https://cdn.discordapp.com/attachments/662122241190002699/662122563270475776/unknown.png)

At the time of the screenshot (2nd of January 2020) only the handshake implementation was ready, this is why of warnings following the successful handshake.



## Troubleshooting

##### System.Net.Sockets.SocketException (10013)

In case you receive this error while trying to open some port for listening, the reason may be your OS is excluding some port ranges for some reason.
To check if the port is reserved, you can use the command

`netsh interface ipv4 show excludedportrange tcp`

you'll see a list of port ranges that may include the port you are trying to open.
To fix that your best bet is to just change the port you want to use in your configuration file, otherwise you need to understand why a specific port range is being reserved and eventually change it.

You can delete the excludedportrange if you want and you know what you are doing, using commands like

`netsh int ipv4 delete excludedportrange protocol=tcp startport=45000 numberofports=100`

and add new ones with 
`netsh int ipv4 add excludedportrange protocol=tcp startport=45000 numberofports=100`