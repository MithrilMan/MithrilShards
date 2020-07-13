﻿using System.Buffers;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public class ProtocolTypeSerializerOptions
   {
      private Dictionary<string, object> options = new Dictionary<string, object>();

      public bool HasOption(string key)
      {
         return this.options.ContainsKey(key);
      }

      public ProtocolTypeSerializerOptions(params (string Key, object Value)[] values)
      {
         if (values != null)
         {
            foreach (var item in values)
            {
               this.options.Add(item.Key, item.Value);
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
      public T Get<T>(string key, T defaultValue = default)
      {
         if (!this.options.TryGetValue(key, out object? value)) return defaultValue;

         return (T)value;
      }

      /// <summary>
      /// Sets the specified key, overriding previous value if already set.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <param name="value">The value.</param>
      public ProtocolTypeSerializerOptions Set(string key, object value)
      {
         this.options.Add(key, value);
         return this;
      }
   }
}