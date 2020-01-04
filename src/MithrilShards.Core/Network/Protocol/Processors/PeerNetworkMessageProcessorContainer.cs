using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MithrilShards.Core.Network.Protocol.Processors
{
   /// <summary>
   /// Build an internal lookup of processors that has to be invoked for a specific message.
   /// </summary>
   public class PeerNetworkMessageProcessorContainer
   {
      internal class ProcessorHandler
      {
         internal readonly INetworkMessageProcessor processor;
         internal readonly MethodInfo handler;
         public ProcessorHandler(INetworkMessageProcessor processor, MethodInfo handler)
         {
            this.processor = processor;
            this.handler = handler;
         }
         internal void Invoke(INetworkMessage message, CancellationToken cancellation)
         {
            this.handler.Invoke(this.processor, new object[] { message, cancellation });
         }
      }

      /// <summary>
      /// The mapping between MessageType and which processor instance is able to handle the request.
      /// </summary>
      private readonly Dictionary<Type, List<ProcessorHandler>> mapping = new Dictionary<Type, List<ProcessorHandler>>();

      public PeerNetworkMessageProcessorContainer(IEnumerable<INetworkMessageProcessor> processors)
      {
         this.ConfigureMapping(processors);
      }

      private void ConfigureMapping(IEnumerable<INetworkMessageProcessor> processors)
      {
         Type refType = typeof(INetworkMessageHandler<>);
         foreach (INetworkMessageProcessor processor in processors)
         {
            Type processorType = processor.GetType();

            IEnumerable<Type> handledMessageTypes = processorType.GetInterfaces()
               .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == refType)
               .Select(i => i.GetGenericArguments().First());

            foreach (Type handledMessageType in handledMessageTypes)
            {
               Type concreteMessageHandlerType = refType.MakeGenericType(handledMessageType);

               MethodInfo method = processorType.GetInterfaceMap(concreteMessageHandlerType).TargetMethods
                  //we are just interested to cache the method ProcessMessageAsync
                  .Where(method => method.Name == nameof(INetworkMessageHandler<INetworkMessage>.ProcessMessageAsync))
                  .First();

               if (!this.mapping.TryGetValue(handledMessageType, out List<ProcessorHandler> handlers))
               {
                  handlers = new List<ProcessorHandler>();
                  this.mapping[handledMessageType] = handlers;
               }

               handlers.Add(new ProcessorHandler(processor, method));
            }
         }
      }


      /// <summary>
      /// Processes the message using mapped message handlers.
      /// </summary>
      /// <param name="message">The message.</param>
      /// <returns><see langword="true"/> if message has been processed, <see langword="false"/> otherwise.</returns>
      public bool ProcessMessage(INetworkMessage message, CancellationToken cancellation)
      {
         if (!this.mapping.TryGetValue(message.GetType(), out List<ProcessorHandler> handlers)) return false;

         for (int i = 0; i < handlers.Count; i++)
         {
            handlers[i].Invoke(message, cancellation);
         }

         return true;
      }
   }
}
