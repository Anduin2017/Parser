using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CSTools.Tools;
using Anduin.Parser.FFmpeg;

[assembly:DoNotParallelize]

namespace Anduin.Parser.Tests;

[TestClass]
public class FFmpegIntegrationTests
{
    private readonly SingleCommandApp<FFmpegHandler> _program = new SingleCommandApp<FFmpegHandler>()
        .WithDefaultOption(CommonOptionsProvider.PathOptions);
    private readonly string _testVideo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "test_video.mp4");

    [TestMethod]
    public async Task InvokeHelp()
    {
        var result = await _program.TestRunAsync(["--help"]);
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeVersion()
    {
        var result = await _program.TestRunAsync(["--version"]);
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeUnknown()
    {
        var result = await _program.TestRunAsync(["--wtf"]);
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeWithoutArg()
    {
        var result = await _program.TestRunAsync([]);
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeFFmpegWithoutArg()
    {
        var result = await _program.TestRunAsync(["ffmpeg"]);
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    [Ignore]
    public async Task InvokeFFmpegWithTemp()
    {
        // Prepare
        var tempFolder = Path.Combine(Path.GetTempPath(), $"Parser-UT-{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempFolder, "test-video.mp4");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        File.Copy(_testVideo, tempFile);

        // Run
        var result = await _program.TestRunAsync([
            "--path",
            tempFolder
        ]);

        // Assert
        if (result.ProgramReturn != 0)
        {
            Console.WriteLine(result.StdErr);
            Console.WriteLine(result.StdOut);
        }
        Assert.AreEqual(0, result.ProgramReturn);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "test-video_265.mp4")));
        Assert.IsFalse(File.Exists(tempFile));

        // Clean
        FolderDeleter.DeleteByForce(tempFolder);
    }

    // Ignore the next test:
    [TestMethod]
    [Ignore]
    public async Task InvokeFFmpegWithGpu()
    {
        // Prepare
        var tempFolder = Path.Combine(Path.GetTempPath(), $"Parser-UT-{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempFolder, "test-video.mp4");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        File.Copy(_testVideo, tempFile);

        // Run
        var result = await _program.TestRunAsync([
            "--path",
            tempFolder,
            "-g"
        ]);

        // Assert
        if (result.ProgramReturn != 0)
        {
            Console.WriteLine(result.StdErr);
            Console.WriteLine(result.StdOut);
        }
        Assert.AreEqual(0, result.ProgramReturn);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "test-video_265.mp4")));
        Assert.IsFalse(File.Exists(tempFile));

        // Clean
        FolderDeleter.DeleteByForce(tempFolder);
    }
}
