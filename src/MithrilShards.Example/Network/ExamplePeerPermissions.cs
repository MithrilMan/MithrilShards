namespace MithrilShards.Example.Network
{
   public class ExamplePeerPermissions
   {
      public const int NONE = 0;

      /// <summary>
      /// Can read.
      /// </summary>
      public const int CAN_READ = 1 << 1;

      /// <summary>
      /// Can write.
      /// </summary>
      public const int CAN_WRITE = 1 << 2;

      /// <summary>
      /// Can Update.
      /// </summary>
      public const int CAN_UPDATE = 1 << 3;

      /// <summary>
      /// Can Delete.
      /// </summary>
      public const int CAN_DELETE = 1 << 4;

      public int Permissions { get; private set; } = NONE;

      public void Add(int permission)
      {
         this.Permissions |= permission;
      }

      public void Remove(int permission)
      {
         this.Permissions &= ~permission;
      }

      public bool Has(int permission)
      {
         return (this.Permissions & permission) == permission;
      }

      public void Reset()
      {
         this.Permissions = NONE;
      }
   }
}
