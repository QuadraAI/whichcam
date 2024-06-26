﻿using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

using WhichCam.Model;

namespace WhichCam;

public static class InfosExtractor
{
    public static readonly string[] validImageFormats =
    [
        ".jpg", ".png", ".gif", ".tiff", ".cr2", ".nef", ".arw", ".dng", ".raf",
        ".rw2", ".erf", ".nrw", ".crw", ".3fr", ".sr2", ".k25", ".kc2", ".mef",
        ".cs1", ".orf", ".mos", ".kdc", ".cr3", ".ari", ".srf", ".srw", ".j6i",
        ".fff", ".mrw", ".x3f", ".mdc", ".rwl", ".pef", ".iiq", ".cxi", ".nksc",
    ];

    public static List<PictureInformationsModel> RetrieveInformation(DirectoryInfo targetDirectory)
    {
        var outputInformation = new List<PictureInformationsModel>();
        var picturesPaths = targetDirectory.GetFiles()
            .Where(f => validImageFormats.Contains(f.Extension.ToLower()))
            .Select(f => f.FullName);
        foreach (var path in picturesPaths)
        {
            try
            {
                using var stream = File.OpenRead(path);
                var directories = ImageMetadataReader.ReadMetadata(stream);
                var cameraInformation = GetCameraInformation(directories);
                if (cameraInformation is null)
                {
                    continue;
                }
                outputInformation.Add(new PictureInformationsModel()
                {
                    Path = path,
                    Maker = cameraInformation.Maker,
                    Model = cameraInformation.Model,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
        return outputInformation;
    }

    private static CameraInformations? GetCameraInformation(IReadOnlyList<MetadataExtractor.Directory>? directories)
    {
        if (directories is null || directories.Count == 0)
        {
            return null;
        }
        var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (ifd0Directory is not null)
        {
            var maker = ifd0Directory.GetDescription(ExifDirectoryBase.TagMake);
            var model = ifd0Directory.GetDescription(ExifDirectoryBase.TagModel);

            return new CameraInformations() { Maker = maker, Model = model };
        }
        return null;
    }

    public static void OrderPictures(List<PictureInformationsModel> infos, DirectoryInfo baseDir, string orderBy)
    {
        foreach (var info in infos)
        {
            DirectoryInfo outputDir;
            if (orderBy is "maker" && info.Maker is not null)
            {
                outputDir = new (Path.Combine(baseDir.FullName, info.Maker.ToLower()));
            }
            else if (orderBy is "model" && info.Model is not null)
            {
                outputDir = new (Path.Combine(baseDir.FullName, info.Model.ToLower()));
            }
            else
            {
                continue;
            }
            try
            {
                if (outputDir.Exists is false)
                {
                    outputDir.Create();
                }
                File.Copy(info.Path, outputDir.FullName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
