using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Example.Network
{
   /// <summary>
   /// The only reason to have this message serializer instead of using directly the <see cref="NetworkMessageSerializerBase"/> is because we want to expose our <see cref="IPeerContext"/>
   /// as a ExamplePeerContext and in some cases we want our serializer to have some other services injected or other common helper method in a common class
   /// </summary>
   /// <typeparam name="TMessageType">The type of the message type.</typeparam>
   /// <seealso cref="MithrilShards.Core.Network.Protocol.Serialization.NetworkMessageSerializerBase{TMessageType}" />
   public abstract class ExampleNetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType, ExamplePeerContext> where TMessageType : INetworkMessage, new()
   {

      protected void MethodOurSerializersMayNeed()
      {

      }
   }
}