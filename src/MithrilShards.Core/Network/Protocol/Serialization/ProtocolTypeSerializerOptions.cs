using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MithrilShards.Core.Network.Protocol.Serialization;

public class ProtocolTypeSerializerOptions
{
   private readonly Dictionary<string, object> _options = new();

   public bool HasOption(string key)
   {
      return _options.ContainsKey(key);
   }

   public ProtocolTypeSerializerOptions(params (string Key, object Value)[] values)
   {
      if (values != null)
      {
         foreach ((string Key, object Value) item in values)
         {
            _options.Add(item.Key, item.Value);
         }
      }
   }


   /// <summary>
   /// Gets the desired option, if available, or the default value.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="key">The key.</param>
   /// <param name="defaultValue">The default value.</param>
   /// <returns></returns>
   [return: MaybeNull]
   public T Get<T>(string key, T? defaultValue = default)
   {
      if (!_options.TryGetValue(key, out object? value)) return defaultValue;

      return (T)value;
   }

   /// <summary>
   /// Sets the specified key, overriding previous value if already set.
   /// </summary>
   /// <param name="key">The key.</param>
   /// <param name="value">The value.</param>
   public ProtocolTypeSerializerOptions Set(string key, object value)
   {
      _options.Add(key, value);
      return this;
   }
}
