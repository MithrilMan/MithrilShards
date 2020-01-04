using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using static MithrilShards.Network.Benchmark.Program;

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

      private Foo classInstance;
      private MethodInfo method;
      //private Func<IMessage, CancellationToken, ValueTask<bool>> computedDelegate;
      private Delegate computedDelegate;
      private Func<object, object[], object> lambdaWrapper;
      private readonly Message1 messageInstance = new Message1();



      [GlobalSetup]
      public void Setup()
      {
         this.classInstance = new Foo();

         Type refType = typeof(IFoo<>);
         Type processorType = typeof(Foo);

         IEnumerable<Type> handledMessageTypes = processorType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == refType)
            .Select(i => i.GetGenericArguments().First());

         Type handledMessageType = this.messageInstance.GetType();
         Type concreteMessageHandlerType = refType.MakeGenericType(handledMessageType);

         MethodInfo method = processorType.GetInterfaceMap(concreteMessageHandlerType).TargetMethods
            //we are just interested to cache the method ProcessMessageAsync
            .Where(method => method.Name == nameof(IFoo<IMessage>.ProcessMessageAsync))
            .First();

         Type delegateType = Expression.GetFuncType(handledMessageType, typeof(CancellationToken), typeof(ValueTask<bool>));
         this.method = method;
         //this.computedDelegate = (Func<IMessage, CancellationToken, ValueTask<bool>>)Delegate.CreateDelegate(delegateType, this.classInstance, method);
         this.computedDelegate = Delegate.CreateDelegate(delegateType, this.classInstance, method);

         this.lambdaWrapper = createWrapperFunc(this.method);
      }


      [Benchmark]
      public object DirectCall()
      {
         //this.computedDelegate(null, default);
         return this.classInstance.ProcessMessageAsync(this.messageInstance, default);
      }

      [Benchmark]
      public object UsingMethodInfo()
      {
         return this.method.Invoke(this.classInstance, new object[] { this.messageInstance, default });
      }

      [Benchmark]
      public void UsingDelegate()
      {
         //this.computedDelegate(null, default);
         this.computedDelegate.DynamicInvoke(this.messageInstance, default);
      }

      [Benchmark]
      public void UsingDelegateMethod()
      {
         //this.computedDelegate(null, default);
         this.computedDelegate.Method.Invoke(this.classInstance, new object[] { this.messageInstance, default });
      }

      [Benchmark]
      public void UsingLambdaWrapper()
      {
         this.lambdaWrapper.Invoke(this.classInstance, new object[] { this.messageInstance, (CancellationToken)default });
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