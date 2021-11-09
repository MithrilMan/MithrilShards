namespace MithrilShards.WebApi;

/// <summary>
/// Placeholder to define known core Web API areas.
/// This class may be extended to add more const for 3rd party areas.
/// </summary>
public abstract class WebApiArea
{
   /// <summary>
   /// The default API area where common actions will be available.
   /// </summary>
   public const string AREA_API = "api";

   /// <summary>
   /// The area where Dev controllers should be placed.
   /// Dev controllers are controllers useful during debug that can expose internal details
   /// or are risky to be used in a public environment
   /// They may be risky to execute by an end user that doesn't have good technical details
   /// knowledge about the application.
   /// </summary>
   public const string AREA_DEV = "dev";
}
