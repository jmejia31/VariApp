using Microsoft.Extensions.Configuration;

namespace InventoryApp.Infrastructure.Services;

internal static class CloudinaryFolderResolver
{
    public static string Resolve(IConfiguration configuration, string baseFolder)
    {
        var environmentPrefix = configuration["Cloudinary:EnvironmentPrefix"]?
            .Trim()
            .Trim('/');

        return string.IsNullOrWhiteSpace(environmentPrefix)
            ? baseFolder
            : $"{environmentPrefix}/{baseFolder}";
    }
}
