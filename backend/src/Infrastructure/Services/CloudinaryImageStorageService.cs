using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace InventoryApp.Infrastructure.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private const string Folder = "inventoryapp/productos";

    public CloudinaryImageStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = Folder,
            Transformation = new Transformation().Width(800).Height(800).Crop("limit").Quality("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error is not null)
            throw new InvalidOperationException($"Error subiendo imagen a Cloudinary: {result.Error.Message}");

        return (result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }
}
