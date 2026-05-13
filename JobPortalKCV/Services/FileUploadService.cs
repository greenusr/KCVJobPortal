using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using JobPortalKCV.Models;

namespace JobPortalKCV.Services
{
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string FilePath { get; set; }
        public string OriginalFileName { get; set; }
    }

    public static class FileUploadService
    {
        public const string NoFileSelected = "No file selected.";
        public const string InvalidFileFormat = "Invalid file format. Please upload a valid file.";
        public const string InvalidImageFormat = "Invalid image format. Please upload a valid image.";
        public const string FileTooLarge = "File size exceeds the allowed limit.";

        private static readonly Dictionary<string, string[]> CvMimeTypes = new Dictionary<string, string[]>
        {
            { ".pdf", new[] { "application/pdf" } },
            { ".doc", new[] { "application/msword", "application/octet-stream" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/octet-stream" } }
        };

        private static readonly Dictionary<string, string[]> ImageMimeTypes = new Dictionary<string, string[]>
        {
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } },
            { ".gif", new[] { "image/gif" } },
            { ".webp", new[] { "image/webp" } },
            { ".svg", new[] { "image/svg+xml", "application/svg+xml", "text/xml", "application/xml" } }
        };

        private static readonly Dictionary<string, string[]> ProfileAvatarMimeTypes = new Dictionary<string, string[]>
        {
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } },
            { ".svg", new[] { "image/svg+xml", "application/svg+xml", "text/xml", "application/xml" } }
        };

        public static FileUploadResult SaveCv(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/cv", "/uploads/cv", FilterAllowedTypes(CvMimeTypes, settings.allowed_cv_types), ToBytes(settings.max_cv_upload_size_mb));
            }
        }

        public static FileUploadResult SaveAvatar(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/avatars", "/uploads/avatars", FilterAllowedTypes(ImageMimeTypes, settings.allowed_image_types), ToBytes(settings.max_avatar_upload_size_mb), InvalidImageFormat);
            }
        }

        public static FileUploadResult SaveLogo(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/logos", "/uploads/logos", FilterAllowedTypes(ImageMimeTypes, settings.allowed_image_types), ToBytes(settings.max_logo_upload_size_mb), InvalidImageFormat);
            }
        }

        public static FileUploadResult SaveCompanyLogo(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/logos", "/uploads/logos", FilterAllowedTypes(ImageMimeTypes, settings.allowed_image_types), ToBytes(settings.max_logo_upload_size_mb), InvalidImageFormat);
            }
        }

        public static FileUploadResult SaveProfileAvatar(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/avatars", "/uploads/avatars", FilterAllowedTypes(ProfileAvatarMimeTypes, settings.allowed_image_types), ToBytes(settings.max_avatar_upload_size_mb), InvalidImageFormat);
            }
        }

        public static FileUploadResult SaveSystemImage(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            using (var data = new JobPortalDataContext())
            {
                var settings = SystemSettingsService.GetSettings(data);
                return Save(file, server, "~/uploads/system", "/uploads/system", FilterAllowedTypes(ProfileAvatarMimeTypes, settings.allowed_image_types), ToBytes(settings.max_logo_upload_size_mb), "Invalid file format.");
            }
        }

        private static FileUploadResult Save(
            HttpPostedFileBase file,
            HttpServerUtilityBase server,
            string virtualFolder,
            string publicFolder,
            Dictionary<string, string[]> allowedTypes,
            int maxBytes,
            string invalidFormatMessage = InvalidFileFormat)
        {
            if (file == null || file.ContentLength == 0)
                return Fail(NoFileSelected);

            if (file.ContentLength > maxBytes)
                return Fail(FileTooLarge);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedTypes.ContainsKey(extension))
                return Fail(invalidFormatMessage);

            var contentType = (file.ContentType ?? "").ToLowerInvariant();

            if (!allowedTypes[extension].Any(type => type.Equals(contentType, StringComparison.OrdinalIgnoreCase)))
                return Fail(invalidFormatMessage);

            var folder = server.MapPath(virtualFolder);
            Directory.CreateDirectory(folder);

            string fileName;
            string fullPath;

            do
            {
                fileName = Guid.NewGuid().ToString("N") + extension;
                fullPath = Path.Combine(folder, fileName);
            }
            while (File.Exists(fullPath));

            file.SaveAs(fullPath);

            return new FileUploadResult
            {
                Success = true,
                FilePath = publicFolder + "/" + fileName,
                OriginalFileName = Path.GetFileName(file.FileName)
            };
        }

        private static FileUploadResult Fail(string message)
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        private static Dictionary<string, string[]> FilterAllowedTypes(Dictionary<string, string[]> knownTypes, string configuredTypes)
        {
            var allowedExtensions = (configuredTypes ?? "")
                .Split(',')
                .Select(type => "." + type.Trim().TrimStart('.').ToLowerInvariant())
                .Where(type => type.Length > 1)
                .Distinct()
                .ToList();

            var filtered = knownTypes
                .Where(item => allowedExtensions.Contains(item.Key))
                .ToDictionary(item => item.Key, item => item.Value);

            return filtered.Any() ? filtered : knownTypes;
        }

        private static int ToBytes(int megabytes)
        {
            var safeMegabytes = SystemSettingsService.Clamp(megabytes, 1, 100, SystemSettingsService.DefaultUploadSizeMb);
            return safeMegabytes * 1024 * 1024;
        }
    }
}
