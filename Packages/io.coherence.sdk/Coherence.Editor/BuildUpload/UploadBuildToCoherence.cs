// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Log;

    public class UploadBuildToCoherence
    {
        private static Logger logger = Log.GetLogger<UploadBuildToCoherence>();

        public static bool UploadWindowsBuild(string path)
        {
            return UploadBuildForPlatform(AvailablePlatforms.Windows, path);
        }

        public static bool UploadWebGlBuild(string path)
        {
            return UploadBuildForPlatform(AvailablePlatforms.WebGL, path);
        }

        public static bool UploadLinuxBuild(string path)
        {
            return UploadBuildForPlatform(AvailablePlatforms.Linux, path);
        }

        public static bool UploadMacOsBuild(string path)
        {
            return UploadBuildForPlatform(AvailablePlatforms.macOS, path);
        }

        private static bool UploadBuildForPlatform(AvailablePlatforms platform, string path)
        {
            var validator = GetValidator(platform);

            if (!validator.Validate(path))
            {
                var message = validator.GetInfoString();
                logger.Warning(Warning.EditorBuildUploadValidator, message);

                return false;
            }

            var uploader = GetUploader(platform);

            return uploader.Upload(platform, path);
        }

        private static BuildPathValidator GetValidator(AvailablePlatforms platform)
        {
            switch (platform)
            {
                case AvailablePlatforms.Windows:
                    return new WindowsPathValidator();
                case AvailablePlatforms.Linux:
                    return new LinuxPathValidator();
                case AvailablePlatforms.WebGL:
                    return new WebGLPathValidator();
                case AvailablePlatforms.macOS:
                    return new MacOSPathValidator();
            }

            return null;
        }

        private static BuildUploader GetUploader(AvailablePlatforms platform)
        {
            switch (platform)
            {
                case AvailablePlatforms.Linux:
                case AvailablePlatforms.Windows:
                    return new DefaultUploader();
                case AvailablePlatforms.WebGL:
                    return new WebGLUploader();
                case AvailablePlatforms.macOS:
                    return new MacOSUploader();
            }

            return null;
        }
    }
}
