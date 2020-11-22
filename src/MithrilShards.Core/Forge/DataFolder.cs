using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Forge
{
   public class DataFolders : IDataFolders
   {
      public const string ROOT_FEATURE = "root";
      public string RootPath { get; }

      readonly Dictionary<string, string> _paths;

      public string this[string featureKey]
      {
         get
         {
            if (featureKey == null)
            {
               throw new ArgumentNullException(nameof(featureKey));
            }

            if (this._paths.TryGetValue(featureKey.ToLowerInvariant(), out string? path))
            {
               return path;
            }
            else
            {
               throw new KeyNotFoundException($"Cannot find data folder information for feature {featureKey}");
            }
         }
         set
         {
            if (featureKey == null)
            {
               throw new ArgumentNullException(nameof(featureKey));
            }

            if (value == null)
            {
               throw new ArgumentNullException(nameof(value));
            }

            this._paths[featureKey.ToLowerInvariant()] = value;
         }
      }

      public DataFolders(string rootPath)
      {
         this.RootPath = rootPath;
         this._paths = new Dictionary<string, string>();
         this[ROOT_FEATURE] = rootPath;
      }
   }
}