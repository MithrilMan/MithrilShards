using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class MethodInfo_vs_Delegate
   {
      interface IMessage { }
      class Message1 : IMessage { }
      class Message2 : IMessage { }
      interface IFoo<T> where T : IMessage
      {
         ValueTask<bool> ProcessMessageAsync(T message, CancellationToken cancellation);
      }

      class Foo : IFoo<Message1>, IFoo<Message2>
      {
         public ValueTask<bool> ProcessMessageAsync(Message1 message, CancellationToken cancellation)
         {
            return new ValueTask<bool>(message != null);
         }

         public ValueTask<bool> ProcessMessageAsync(Message2 message, CancellationToken cancellation)
         {
            return new ValueTask<bool>(message != null);
         }
      }

      private Foo _classInstance;
      private MethodInfo _method;
      private Delegate _computedDelegate;
      private Func<object, object[], object> _lambdaWrapper;
      private readonly Message1 _messageInstance = new Message1();
      private readonly Message2 _messageInstance2 = new Message2();



      [GlobalSetup]
      public void Setup()
      {
         _classInstance = new Foo();

         Type refType = typeof(IFoo<>);
         Type processorType = typeof(Foo);

         IEnumerable<Type> handledMessageTypes = processorType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == refType)
            .Select(i => i.GetGenericArguments().First());

         Type handledMessageType = _messageInstance.GetType();
         Type concreteMessageHandlerType = refType.MakeGenericType(handledMessageType);

         MethodInfo method = processorType.GetInterfaceMap(concreteMessageHandlerType).TargetMethods
            //we are just interested to cache the method ProcessMessageAsync
            .Where(method => method.Name == nameof(IFoo<IMessage>.ProcessMessageAsync))
            .First();

         Type delegateType = Expression.GetFuncType(handledMessageType, typeof(CancellationToken), typeof(ValueTask<bool>));
         _method = method;
         _computedDelegate = Delegate.CreateDelegate(delegateType, _classInstance, method);

         _lambdaWrapper = createWrapperFunc(_method);
      }


      [Benchmark]
      public object DirectCall()
      {
         return _classInstance.ProcessMessageAsync(_messageInstance, default);
      }

      [Benchmark]
      public object UsingMethodInfo()
      {
         return _method.Invoke(_classInstance, new object[] { _messageInstance, default });
      }

      [Benchmark]
      public void UsingDelegate()
      {
         _computedDelegate.DynamicInvoke(_messageInstance, default);
      }

      [Benchmark]
      public void UsingDelegateMethod()
      {
         _computedDelegate.Method.Invoke(_classInstance, new object[] { _messageInstance, default });
      }

      [Benchmark]
      public void UsingLambdaWrapper()
      {
         _lambdaWrapper.Invoke(_classInstance, new object[] { _messageInstance, (CancellationToken)default });
      }

      private static Func<object, object[], object> createWrapperFunc(MethodInfo method)
      {
         ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
         ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

         MethodCallExpression call = Expression.Call(
            Expression.Convert(instanceParameter, method.DeclaringType),
            method,
            method.GetParameters()
               .Select((parameter, index) => Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)), parameter.ParameterType))
               .Cast<Expression>()
               .ToArray());

         var lambda = Expression.Lambda<Func<object, object[], object>>(Expression.Convert(call, typeof(object)), instanceParameter, argumentsParameter);

         return lambda.Compile();
      }
   }
}