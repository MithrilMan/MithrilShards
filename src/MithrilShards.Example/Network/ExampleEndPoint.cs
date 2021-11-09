using System.Net;

namespace MithrilShards.Example.Network;

/// <summary>
/// Suppose you need to attach some information about the destination endpoint you want to connect to.
/// You can extend IPEndPoint without having to re-implement much other components.
/// Note: in order to connect using this EndPoint, you need to implement your custom <see cref="Core.Network.Client.ConnectorBase"/>
/// and you may want to remove default <see cref="Core.Network.Client.RequiredConnection"/> like we did in this example.
/// </summary>
/// <seealso cref="System.Net.IPEndPoint" />
public class ExampleEndPoint : IPEndPoint
{
   /// <summary>
   /// Extra information I need to connect to a peer (e.g. it could be an encryption key to be used to exchange informations).
   /// </summary>
   public string MyExtraInformation { get; }

   public ExampleEndPoint(IPAddress address, int port, string myExtraInformation) : base(address, port)
   {
      MyExtraInformation = myExtraInformation;
   }
}
