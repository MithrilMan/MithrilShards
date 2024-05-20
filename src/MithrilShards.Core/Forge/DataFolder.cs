using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Forge;

public class DataFolders : IDataFolders
{
   public const string ROOT_FEATURE = "root";
   public string RootPath { get; }

   readonly Dictionary<string, string> _paths;

   public string this[string featureKey]
   {
      get
      {
         ArgumentNullException.ThrowIfNull(featureKey);

         if (_paths.TryGetValue(featureKey.ToLowerInvariant(), out string? path))
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
         ArgumentNullException.ThrowIfNull(featureKey);
         ArgumentNullException.ThrowIfNull(value);

         _paths[featureKey.ToLowerInvariant()] = value;
      }
   }

   public DataFolders(string rootPath)
   {
      RootPath = rootPath;
      _paths = [];
      this[ROOT_FEATURE] = rootPath;
   }
}
