namespace MithrilShards.Example.Protocol.Types;

public class PongFancyResponse
{
   /// <summary>
   /// The nonce received from the ping request.
   /// </summary>
   public ulong Nonce { get; set; }

   public string? Quote { get; set; }
}
