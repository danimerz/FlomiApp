namespace FlomiApp.Security;

public static class Roles
{
    public const string Admin         = "Admin";
    public const string FahrzeugAdmin = "FahrzeugAdmin";
    public const string MoebelAdmin   = "MoebelAdmin";
    public const string BereichsAdmin = "BereichsAdmin";

    // Kombinations-Strings für [Authorize(Roles = ...)]
    public const string AdminOrFahrzeug  = "Admin,FahrzeugAdmin";
    public const string AdminOrMoebel    = "Admin,MoebelAdmin";
    public const string AdminOrBereichs  = "Admin,BereichsAdmin";
    public const string AnyAdmin         = "Admin,FahrzeugAdmin,MoebelAdmin,BereichsAdmin";
}
