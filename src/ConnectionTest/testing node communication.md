# Testing node communication

In order to test the Forge implementation, I used the handy [bitcoin testnet in a box](https://github.com/freewil/bitcoin-testnet-box)	

I used the docker implementation:

`docker pull freewil/bitcoin-testnet-box`

then I modified the suggested docker run arguments, in order to open the node port, this way, to run the image with an reachable testnet node, run the image as

`docker run -t -i -p 19000:19000 -p 19001:19001 -p 19011:19011 freewil/bitcoin-testnet-box`

after that, in the tty console, write `make start` to start the node and this way you can connect to the testnet box using the endpoint `127.0.0.1:19000`

If everything is going as expected and you ran the Forge within bitcoin-testnet network, you should see something similar to this

![img](https://cdn.discordapp.com/attachments/662122241190002699/662122563270475776/unknown.png)

At the time of the screenshot (2nd of January 2020) only the handshake implementation was ready, this is why of warnings following the successful handshake.