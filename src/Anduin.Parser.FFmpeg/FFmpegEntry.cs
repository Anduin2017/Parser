using Aiursoft.CSTools.Services;
using Microsoft.Extensions.Logging;

namespace Anduin.Parser.FFmpeg;

/// <summary>
/// This class is responsible for detecting hardware acceleration availability (e.g. QSV, NVENC),
/// enumerating video files from a given directory, and transcoding them using FFmpeg.
/// </summary>
public class FFmpegEntry(
    ILogger<FFmpegEntry> logger,
    FFmpegOptions options,
    CommandService commandService)
{
    private const long MbToBytes = 1024 * 1024;

    /// <summary>
    /// The main entry point to parse and transcode all video files under the specified directory.
    /// It detects hardware acceleration first, then iterates all video files, and processes each in turn.
    /// </summary>
    /// <param name="path">The directory path to enumerate video files.</param>
    /// <param name="shouldTakeAction">Whether to actually transcode or only do a dry run.</param>
    /// <param name="action">What to do with the original file once transcoded (delete, move to trash, or do nothing).</param>
    public async Task OnServiceStartedAsync(string path, bool shouldTakeAction, DuplicateAction action)
    {
        logger.LogTrace("Enumerating files under path: {Path}", path);

        // Detect available hardware encoders before processing videos
        var selectedEncoder = await DetectHardwareAccelerationAsync(options.UseGpu);
        logger.LogInformation("Hardware acceleration choice: {SelectedEncoder}", selectedEncoder);

        var videos = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(v =>
                v.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                v.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                v.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                v.EndsWith(".wmv", StringComparison.OrdinalIgnoreCase) ||
                v.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase));

        foreach (var file in videos)
        {
            logger.LogTrace("Parsing video file: {File}", file);
            await ProcessVideoAsync(file, shouldTakeAction, action, selectedEncoder);
        }
    }

    /// <summary>
    /// Processes a single video file to decide if it should be transcoded
    /// (based on size, codec, extension, etc.), and invokes ParseVideoAsync if needed.
    /// </summary>
    /// <param name="filePath">Path to the video file to be processed.</param>
    /// <param name="shouldTakeAction">Whether to actually perform transcoding (if false, it's a dry run).</param>
    /// <param name="action">Indicates how to handle the original file once transcoded.</param>
    /// <param name="selectedEncoder">The chosen hardware (or CPU) encoder to use for transcoding.</param>
    private async Task ProcessVideoAsync(string filePath, bool shouldTakeAction, DuplicateAction action,
        string selectedEncoder)
    {
        var folder = Path.GetDirectoryName(filePath) ?? throw new Exception($"{filePath} is invalid!");
        var baseFileInfo = await commandService.RunCommandAsync("ffmpeg", $@"-i ""{filePath}""", folder);
        var fileInfo = new FileInfo(filePath);

        if (ShouldParseVideo(baseFileInfo.error, fileInfo))
        {
            var newFileName = GetNewFileName(fileInfo);
            if (shouldTakeAction)
            {
                await ParseVideoAsync(filePath, newFileName, folder, selectedEncoder, options.Crf, action);
            }
            else
            {
                logger.LogInformation("{FilePath} Running in dry run mode. Skip parsing...", filePath);
            }
        }
    }

    /// <summary>
    /// Determines if the given video file should be transcoded, based on size, codec, format, etc.
    /// </summary>
    /// <param name="baseFileInfo">The output from 'ffmpeg -i' command, which may contain the codec info.</param>
    /// <param name="fileInfo">The FileInfo object representing the video file.</param>
    /// <returns>True if the video should be transcoded, false if it is considered compliant already.</returns>
    private bool ShouldParseVideo(string baseFileInfo, FileInfo fileInfo)
    {
        var largeEnough = fileInfo.Length > 20 * MbToBytes;
        var isNotHevc = !baseFileInfo.Contains("Video: hevc", StringComparison.OrdinalIgnoreCase);
        var isNotMp4 = !fileInfo.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);
        var containsPrivacyInfo = baseFileInfo.Contains("creation_time", StringComparison.OrdinalIgnoreCase);

        if (!largeEnough)
        {
            logger.LogInformation(
                "{FileInfoFullName} should not be parsed, because it's too small: {FileInfoLength}MB. Minimum size is 20MB",
                fileInfo.FullName, fileInfo.Length / MbToBytes);
        }
        else if (isNotHevc)
        {
            logger.LogInformation("{FileInfoFullName} should be parsed, because it is not HEVC!", fileInfo.FullName);
        }
        else if (isNotMp4)
        {
            logger.LogInformation("{FileInfoFullName} should be parsed, because it is not mp4!", fileInfo.FullName);
        }
        else if (containsPrivacyInfo)
        {
            logger.LogInformation("{FileInfoFullName} should be parsed, because it contains privacy info!",
                fileInfo.FullName);
        }
        else
        {
            logger.LogInformation("{FileInfoFullName} should not be parsed, because it is compliant!",
                fileInfo.FullName);
        }

        return largeEnough && (isNotHevc || isNotMp4 || containsPrivacyInfo);
    }

    /// <summary>
    /// Builds a new filename (with _265 suffix) for the transcoded output file.
    /// </summary>
    /// <param name="fileInfo">The original video file's FileInfo object.</param>
    /// <returns>A string path for the new (transcoded) file.</returns>
    private string GetNewFileName(FileInfo fileInfo)
    {
        var bareName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
        return Path.Combine(fileInfo.DirectoryName!, $"{bareName}_265.mp4");
    }

    /// <summary>
    /// Invokes FFmpeg to transcode a single video file according to the selected encoder and CRF.
    /// After successful transcoding, handles post-processing actions (delete original, move to trash, etc.).
    /// </summary>
    /// <param name="sourceFilePath">Path to the original video file.</param>
    /// <param name="targetFilePath">Path for the transcoded output file.</param>
    /// <param name="folder">The working directory to run FFmpeg in.</param>
    /// <param name="selectedEncoder">The chosen hardware/CPU encoder name.</param>
    /// <param name="crf">CRF value for x265 or relevant parameter for hardware encoders.</param>
    /// <param name="action">Indicates what to do with the original file after successful transcode.</param>
    private async Task ParseVideoAsync(
        string sourceFilePath,
        string targetFilePath,
        string folder,
        string selectedEncoder,
        int crf,
        DuplicateAction action)
    {
        logger.LogWarning("{SourceFilePath} parsing initialized! crf is {Crf}", sourceFilePath, crf);

        if (File.Exists(targetFilePath))
        {
            File.Delete(targetFilePath);
        }

        var (result, output, error) = await RunTranscodeCommandAsync(
            sourceFilePath, targetFilePath, folder, selectedEncoder, crf);

        if (result != 0)
        {
            throw new Exception(
                $"FFmpeg failed to parse the video: {sourceFilePath}. " +
                $"Output: {output}. Error: {error}");
        }

        var sourceFileInfo = new FileInfo(sourceFilePath);
        var targetFileInfo = new FileInfo(targetFilePath);
        logger.LogInformation(
            "New file size is {NewFileSize}, source file size is {SourceFileSize}. Saved {SavedFileSize}",
            ConvertFileSizeToMb(targetFileInfo.Length),
            ConvertFileSizeToMb(sourceFileInfo.Length),
            ConvertFileSizeToMb(sourceFileInfo.Length - targetFileInfo.Length));

        // If the transcode file exists and seems valid, handle the duplicate action
        if (targetFileInfo is { Exists: true, Length: > 1 * MbToBytes }
            && error.Contains("speed=", StringComparison.OrdinalIgnoreCase))
        {
            switch (action)
            {
                case DuplicateAction.Delete:
                    File.Delete(sourceFilePath);
                    logger.LogInformation("Deleted {SourceFilePath}.", sourceFilePath);
                    break;
                case DuplicateAction.MoveToTrash:
                    MoveToTrashAsync(sourceFilePath, folder);
                    logger.LogInformation("Moved {path} to .trash folder.", sourceFilePath);
                    break;
                case DuplicateAction.Nothing:
                    logger.LogInformation("No action taken for {SourceFilePath}.", sourceFilePath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }
        else
        {
            throw new Exception("After parsing, still couldn't locate the converted file: " + targetFilePath);
        }
    }

    /// <summary>
    /// Assembles FFmpeg command arguments based on the selected encoder and executes to transcode.
    /// Returns the FFmpeg process exit code, stdout, and stderr for further analysis.
    /// </summary>
    /// <param name="sourceFilePath">Original file path.</param>
    /// <param name="targetFilePath">Destination path for the transcode file.</param>
    /// <param name="folder">Working directory to run the FFmpeg command.</param>
    /// <param name="selectedEncoder">The hardware/CPU encoder identifier (e.g., IntelQSV, NvidiaNVENC, CPU, etc.).</param>
    /// <param name="crf">CRF value or comparable quality setting for hardware encoders.</param>
    /// <returns>Tuple of (exitCode, stdout, stderr)</returns>
    private async Task<(int result, string output, string error)> RunTranscodeCommandAsync(
        string sourceFilePath,
        string targetFilePath,
        string folder,
        string selectedEncoder,
        int crf)
    {
        // Different encoder commands based on selected hardware acceleration
        var encoderArgs = selectedEncoder switch
        {
            "AppleVideotoolbox" => $@"-codec:a copy -codec:v hevc_videotoolbox -b:v 5M -pix_fmt yuv420p",
            "IntelQSV" => $@"-codec:a copy -codec:v hevc_qsv -global_quality {crf}",
            "NvidiaNVENC" =>
                $@"-codec:a copy -codec:v hevc_nvenc -rc:v vbr -cq:v {crf} -rc-lookahead 10 -profile:v main10",
            "AMDAMF" => $@"-codec:a copy -codec:v hevc_amf -crf {crf}",
            _ => $@"-codec:a copy -codec:v libx265 -crf {crf}"
        };

        // We keep preset as an example, but you may adjust it for each encoder if necessary
        var ffmpegArgs = $@"-i ""{sourceFilePath}"" -preset slow {encoderArgs} ""{targetFilePath}""";

        logger.LogInformation("Running ffmpeg with arguments: {FfmpegArgs}", ffmpegArgs);
        var (result, output, error) = await commandService.RunCommandAsync(
            "ffmpeg",
            ffmpegArgs,
            folder,
            timeout: TimeSpan.FromMinutes(200));

        return (result, output, error);
    }

    /// <summary>
    /// Moves the source file to a hidden ".trash" folder, to avoid permanently deleting it.
    /// </summary>
    /// <param name="sourceFile">The original file path.</param>
    /// <param name="sourceFolder">The folder where the file currently resides.</param>
    private void MoveToTrashAsync(string sourceFile, string sourceFolder)
    {
        var trashFolder = Path.Combine(sourceFolder, ".trash");
        if (!Directory.Exists(trashFolder))
        {
            Directory.CreateDirectory(trashFolder);
        }

        var trashPath = Path.Combine(trashFolder, Path.GetFileName(sourceFile));
        while (File.Exists(trashPath))
        {
            trashPath = Path.Combine(
                trashFolder,
                $"{Guid.NewGuid()}{Path.GetExtension(sourceFile)}");
        }

        File.Move(sourceFile, trashPath);
    }

    /// <summary>
    /// Converts a file size in bytes to an MB string with 2 decimal places.
    /// </summary>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <returns>A string representing the size in MB, e.g. "12.34 MB".</returns>
    private static string ConvertFileSizeToMb(long fileSize)
    {
        var sizeInMb = (double)fileSize / (1024 * 1024);
        return sizeInMb.ToString("0.##") + " MB";
    }

    #region Hardware Acceleration Detection

    /// <summary>
    /// Detects the available hardware acceleration by listing the ffmpeg encoders,
    /// then attempts to verify Intel QSV if it appears. Fallback priority:
    /// Apple (videotoolbox) -> Intel (qsv) -> NVIDIA (nvenc) -> AMD (amf) -> CPU (libx265).
    /// </summary>
    /// <param name="userWantsGpu">Indicates if the user wants to use hardware acceleration at all.</param>
    /// <returns>The string identifier of the chosen encoder.</returns>
    private async Task<string> DetectHardwareAccelerationAsync(bool userWantsGpu)
    {
        if (!userWantsGpu)
        {
            logger.LogInformation("User explicitly disabled GPU. Will use CPU (libx265).");
            return "CPU";
        }

        // Retrieve the list of available encoders from ffmpeg
        var encoders = await RetrieveAvailableEncodersAsync();
        if (encoders.Count == 0)
        {
            logger.LogWarning("No encoders found or failed to retrieve encoders. Using CPU (libx265) as fallback.");
            return "CPU";
        }

        // Log all detected encoders
        logger.LogInformation("Detected Encoders: {Encoders}", string.Join(", ", encoders));

        // Priority selection: Apple -> Intel -> Nvidia -> AMD -> CPU
        if (encoders.Any(e => e.Contains("hevc_videotoolbox", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("Choosing Apple hardware acceleration: hevc_videotoolbox");
            return "AppleVideotoolbox";
        }

        if (encoders.Any(e => e.Contains("hevc_qsv", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("Detected Intel QSV in encoders list. Verifying usability...");
            var qsvOk = await IsQsvReallyAvailableAsync();
            if (qsvOk)
            {
                logger.LogInformation("Intel QSV is verified to be usable! Returning IntelQSV.");
                return "IntelQSV";
            }
            else
            {
                logger.LogWarning("Intel QSV was detected but is not usable. Fallback to next candidate...");
            }
        }

        if (encoders.Any(e => e.Contains("hevc_nvenc", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("Choosing NVIDIA hardware acceleration: hevc_nvenc");
            return "NvidiaNVENC";
        }

        if (encoders.Any(e => e.Contains("hevc_amf", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogInformation("Choosing AMD hardware acceleration: hevc_amf");
            return "AMDAMF";
        }

        logger.LogInformation("No preferred hardware acceleration found. Fallback to CPU (libx265).");
        return "CPU";
    }

    /// <summary>
    /// Lists all available ffmpeg encoders by running 'ffmpeg -encoders',
    /// and returns each line representing an encoder.
    /// </summary>
    /// <returns>A list of strings, each describing one encoder.</returns>
    private async Task<List<string>> RetrieveAvailableEncodersAsync()
    {
        var (exitCode, output, error) = await commandService.RunCommandAsync(
            "ffmpeg", "-encoders", Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(30));

        var encoders = new List<string>();
        if (exitCode != 0)
        {
            logger.LogError("Failed to retrieve encoders. ExitCode: {ExitCode}, Error: {Error}", exitCode, error);
            return encoders; // return empty, fallback will happen
        }

        // Each line might look like:
        // " V..... h264_nvenc           NVIDIA NVENC H.264 encoder (codec h264)"
        // We'll collect them into a list
        using var reader = new StringReader(output);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.Contains("encoders:") || string.IsNullOrWhiteSpace(line))
                continue;
            encoders.Add(line.Trim());
        }

        return encoders;
    }

    /// <summary>
    /// Performs a short test encode with ffmpeg to verify if Intel QSV is actually usable,
    /// rather than just being listed in the encoder list.
    /// We use a 1-second color pattern (lavfi) and encode it to null output.
    /// </summary>
    /// <returns>True if QSV encoding succeeds, false otherwise.</returns>
    private async Task<bool> IsQsvReallyAvailableAsync()
    {
        // We'll write output to a null muxer so we don't need a real file
        // Of course, you could also write to a temp file if you prefer.
        var testArgs = "-f lavfi -i color=c=black:size=128x128:duration=1:rate=30 " +
                       "-c:v hevc_qsv -f null -";

        var (exitCode, _, error) = await commandService.RunCommandAsync(
            "ffmpeg",
            testArgs,
            Directory.GetCurrentDirectory(),
            TimeSpan.FromSeconds(30));

        if (exitCode == 0)
        {
            logger.LogInformation("QSV test encode success. QSV is available.");
            return true;
        }
        else
        {
            logger.LogWarning("QSV test encode failed with exit code {Code}. Error: {Err}", exitCode, error);
            return false;
        }
    }

    #endregion
}
