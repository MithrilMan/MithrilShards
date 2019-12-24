Raw bitcoin messages:

Magic 0xD9B4BEF9 (F9 BE B4 D9)

### version
```
Message Header:
 F9 BE B4 D9                                                                   - Main network magic bytes
 76 65 72 73 69 6F 6E 00 00 00 00 00                                           - "version" command
 64 00 00 00                                                                   - Payload is 100 bytes long
 35 8d 49 32                                                                   - payload checksum (little endian)

Version message:
 62 EA 00 00                                                                   - 60002 (protocol version 60002)
 01 00 00 00 00 00 00 00                                                       - 1 (NODE_NETWORK services)
 11 B2 D0 50 00 00 00 00                                                       - Tue Dec 18 10:12:33 PST 2012
 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF 00 00 00 00 00 00 - Recipient address info - see Network Address
 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF 00 00 00 00 00 00 - Sender address info - see Network Address
 3B 2E B3 5D 8C E6 17 65                                                       - Node ID
 0F 2F 53 61 74 6F 73 68 69 3A 30 2E 37 2E 32 2F                               - "/Satoshi:0.7.2/" sub-version string (string is 15 bytes long)
 C0 3E 03 00                                                                   - Last block sending node has is block #212672
```
F9 BE B4 D9 76 65 72 73 69 6F 6E 00 00 00 00 00 64 00 00 00 35 8d 49 32 62 EA 00 00 01 00 00 00 00 00 00  00 11 B2 D0 50 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF 00 00 00 00 00 00 3B 2E B3 5D 8C E6 17 65 0F 2F 53 61 74 6F 73 68 69 3A 30 2E 37 2E 32 2F C0 3E 03 00