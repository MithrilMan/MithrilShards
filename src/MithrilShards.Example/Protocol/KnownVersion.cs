namespace MithrilShards.Example.Protocol
{
   /// <summary>
   /// An handy class to manage known version that may alter the serialization and functionality of the shard.
   /// </summary>
   public sealed class KnownVersion
   {
      /// <summary>
      /// Fake version where we added feature X
      /// </summary>
      public const int V1 = 1;

      /// <summary>
      /// Fake version where we added feature Y
      /// </summary>
      public const int V2 = 2;

      /// <summary>
      /// Fake version where we added feature Z
      /// </summary>
      public const int V3 = 3;

      public static int CurrentVersion => V3;
   }
}
