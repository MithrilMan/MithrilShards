namespace MithrilShards.Core.Forge;

public interface IDataFolders
{
   string RootPath { get; }

   /// <summary>
   /// Gets or sets the path corresponding to the specified feature key.
   /// </summary>
   /// <param name="featureKey">The feature key corresponding to the get or set path.</param>
   /// <returns>The path corresponding to the specified feature key, <see langword="null"/> if the <paramref name="featureKey"/> is unknown.</returns>
   public string this[string featureKey] { get; set; }
}
