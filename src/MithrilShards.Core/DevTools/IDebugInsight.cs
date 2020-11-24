namespace MithrilShards.Core.DevTools
{
   /// <summary>
   /// Some useful methods to obtain information about component internals
   /// </summary>
   public interface IDebugInsight
   {
      /// <summary>
      /// Generic method to return an object filled with useful information to check its current status.
      /// </summary>
      /// <returns></returns>
      object GetInsight();
   }
}
