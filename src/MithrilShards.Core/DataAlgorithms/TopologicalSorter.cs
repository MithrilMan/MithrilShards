using System.Collections.Generic;
using System.Linq;

namespace MithrilShards.Core.DataAlgorithms;

/// <summary>
/// Topological Sorter algorithm.
/// </summary>
/// <typeparam name="TItem">The type of the item.</typeparam>
public class TopologicalSorter<TItem> where TItem : notnull
{
   private class Relations
   {
      public int Dependencies = 0;
      public HashSet<TItem> Dependents = [];
   }

   private readonly Dictionary<TItem, Relations> _map = [];


   /// <summary>
   /// Adds the specified item with its dependency references.
   /// </summary>
   /// <param name="item">The item.</param>
   /// <param name="dependencies">The dependencies.</param>
   public void Add(TItem item, params TItem[] dependencies)
   {
      Add(item, dependencies.AsEnumerable());
   }

   /// <summary>
   /// Adds the specified item with its dependency references.
   /// </summary>
   /// <param name="item">The item.</param>
   /// <param name="dependencies">The dependencies.</param>
   public void Add(TItem item, IEnumerable<TItem> dependencies)
   {
      if (!dependencies.Any() && !_map.ContainsKey(item))
      {
         _map.Add(item, new Relations());
         return;
      }

      foreach (TItem dependency in dependencies)
      {
         // do not add eventual dependency to itself
         if (dependency.Equals(item)) continue;

         if (!_map.TryGetValue(dependency, out TopologicalSorter<TItem>.Relations? dependencyDependecies))
         {
            dependencyDependecies = new Relations();
            _map.Add(dependency, dependencyDependecies);
         }

         HashSet<TItem>? dependents = dependencyDependecies.Dependents;

         if (!_map.TryGetValue(item, out TopologicalSorter<TItem>.Relations? itemDependencies))
         {
            itemDependencies = new Relations();
            _map.Add(item, itemDependencies);
         }

         if (dependents.Add(item))
         {
            itemDependencies.Dependencies++;
         }
      }
   }

   /// <summary>
   /// Sorts data topologically, returning sorting items with a level indicating the dependency depth, useful if we want to apply
   /// some more fine grained sorting later.
   /// if cycled is not null, the graph isn't a DAG graph and circular dependency happens.
   /// </summary>
   /// <returns></returns>
   public (IEnumerable<(TItem item, int level)> sorted, IEnumerable<TItem> cycled) Sort()
   {
      var map = _map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

      // first add all nodes
      var sorted = map.Where(item => item.Value.Dependencies == 0).Select(kvp => (item: kvp.Key, level: 0)).ToList();

      // then iteratively add items with less dependency
      for (int idx = 0; idx < sorted.Count; idx++)
      {
         (TItem item, int level) current = sorted[idx];

         sorted.AddRange(
            map[current.item]
               .Dependents
               .Where(item => --map[item].Dependencies == 0)
               .Select(item => (item: item, level: current.level + 1))
            );
      }

      var cycled = map
         .Where(kvp => kvp.Value.Dependencies != 0)
         .Select(kvp => kvp.Key)
         .ToList();

      return (sorted, cycled);
   }

   public void Clear()
   {
      _map.Clear();
   }
}
