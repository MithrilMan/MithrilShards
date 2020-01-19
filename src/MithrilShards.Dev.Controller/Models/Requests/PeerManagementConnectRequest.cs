using System.ComponentModel.DataAnnotations;

namespace MithrilShards.Dev.Controller.Models.Requests
{
   public class PeerManagementConnectRequest
   {
      [Required]
      public string EndPoint { get; set; } = string.Empty;
   }
}
