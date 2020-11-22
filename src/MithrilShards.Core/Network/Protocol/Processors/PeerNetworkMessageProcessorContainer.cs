using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
         internal readonly Func<object, object[], object> handler;
         public ProcessorHandler(INetworkMessageProcessor processor, Func<object, object[], object> handler)
         {
            this.processor = processor;
            this.handler = handler;
         }
         internal ValueTask<bool> InvokeAsync(INetworkMessage message, CancellationToken cancellation)
         {
            return (ValueTask<bool>)handler.Invoke(processor, new object[] { message, cancellation });
         }
      }

      /// <summary>
      /// The mapping between MessageType and which processor instance is able to handle the request.
      /// </summary>
      private readonly Dictionary<Type, List<ProcessorHandler>> _mapping = new Dictionary<Type, List<ProcessorHandler>>();

      public PeerNetworkMessageProcessorContainer(IEnumerable<INetworkMessageProcessor> processors)
      {
         ConfigureMapping(processors);
      }

      private void ConfigureMapping(IEnumerable<INetworkMessageProcessor> processors)
      {
         Type refType = typeof(INetworkMessageHandler<>);
         foreach (INetworkMessageProcessor processor in processors)
         {
            // skip processors that aren't enabled
            if (!processor.Enabled) continue;

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

               if (!_mapping.TryGetValue(handledMessageType, out List<ProcessorHandler>? handlers))
               {
                  handlers = new List<ProcessorHandler>();
                  _mapping[handledMessageType] = handlers;
               }

               handlers.Add(new ProcessorHandler(processor, CreateLambdaWrapper(method)));
            }
         }
      }


      /// <summary>
      /// Processes the message using mapped message handlers.
      /// </summary>
      /// <param name="message">The message.</param>
      /// <returns><see langword="true"/> if message has been processed, <see langword="false"/> otherwise.</returns>
      public async ValueTask<bool> ProcessMessageAsync(INetworkMessage message, CancellationToken cancellation)
      {
         if (!_mapping.TryGetValue(message.GetType(), out List<ProcessorHandler>? handlers)) return false;

         for (int i = 0; i < handlers.Count; i++)
         {
            ProcessorHandler handler = handlers[i];
            if (handler.processor.CanReceiveMessages)
            {
               // when an handler return false, mean it doesn't want other handlers to continue parsing the message
               if (await handler.InvokeAsync(message, cancellation).ConfigureAwait(false)) break;
            }
         }

         return true;
      }


      /// <summary>
      /// Improve performance over a straight <see cref="MethodInfo.Invoke"/>, creating a compiled lambda expression that
      /// allow to have a call on a generic function that internally performs needed cast to invoke the proper open delegate.
      /// </summary>
      /// <param name="method">The method.</param>
      /// <remarks>Of course this method is intended to generate the wrapper that should cached and invoked in place
      /// of the wrapped MethodInfo. If the use case doesn't allow reuse, don't use this method.</remarks>
      /// <returns></returns>
      private static Func<object, object[], object> CreateLambdaWrapper(MethodInfo method)
      {
         ParameterExpression instance = Expression.Parameter(typeof(object), "target");
         ParameterExpression arguments = Expression.Parameter(typeof(object[]), "arguments");

         MethodCallExpression call = Expression.Call(
            Expression.Convert(instance, method.DeclaringType),
            method,
            method.GetParameters()
               .Select((parameter, index) => Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(index)), parameter.ParameterType))
               .ToArray());

         return Expression
            .Lambda<Func<object, object[], object>>(Expression.Convert(call, typeof(object)), instance, arguments)
            .Compile();
      }
   }
}
