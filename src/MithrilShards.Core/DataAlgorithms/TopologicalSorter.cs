using System.Collections.Generic;
using System.Linq;

namespace MithrilShards.Core.DataAlgorithms
{
   /// <summary>
   /// Topological Sorter algorithm.
   /// </summary>
   /// <typeparam name="TItem">The type of the item.</typeparam>
   public class TopologicalSorter<TItem> where TItem : notnull
   {
      private class Relations
      {
         public int Dependencies = 0;
         public HashSet<TItem> Dependents = new HashSet<TItem>();
      }

      private Dictionary<TItem, Relations> map = new Dictionary<TItem, Relations>();


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
         foreach (TItem dependency in dependencies)
         {
            // do not add eventual dependency to itself
            if (dependency.Equals(item)) continue;

            if (!map.ContainsKey(dependency))
            {
               map.Add(dependency, new Relations());
            }

            var dependents = map[dependency].Dependents;

            if (!map.ContainsKey(item))
            {
               map.Add(item, new Relations());
            }

            if (dependents.Add(item))
            {
               map[item].Dependencies++;
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
         Dictionary<TItem, TopologicalSorter<TItem>.Relations> map = this.map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

         // first add all nodes
         List<(TItem item, int level)> sorted = map.Where(item => item.Value.Dependencies == 0).Select(kvp => (item: kvp.Key, level: 0)).ToList();

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
            .Select(kvp => kvp.Key).ToList();

         return (sorted, cycled);
      }

      public void Clear()
      {
         map.Clear();
      }
   }
}
