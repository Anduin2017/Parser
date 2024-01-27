using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CSTools.Tools;
using Anduin.Parser.FFmpeg;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Anduin.Parser.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly SingleCommandApp<FFmpegHandler> _program = new SingleCommandApp<FFmpegHandler>()
        .WithDefaultOption(CommonOptionsProvider.PathOptions);
    private readonly string _testVideo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "test_video.mp4");

    [TestMethod]
    public async Task InvokeHelp()
    {
        var result = await _program.TestRunAsync(new[] { "--help" });
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeVersion()
    {
        var result = await _program.TestRunAsync(new[] { "--version" });
        Assert.AreEqual(0, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeUnknown()
    {
        var result = await _program.TestRunAsync(new[] { "--wtf" });
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeWithoutArg()
    {
        var result = await _program.TestRunAsync(Array.Empty<string>());
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeFFmpegWithoutArg()
    {
        var result = await _program.TestRunAsync(new[] { "ffmpeg" });
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
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
        var result = await _program.TestRunAsync(new[]
        {
            "--path",
            tempFolder
        });

        // Assert
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
        var result = await _program.TestRunAsync(new[]
        {
            "--path",
            tempFolder,
            "-g"
        });

        // Assert
        Assert.AreEqual(0, result.ProgramReturn);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "test-video_265.mp4")));
        Assert.IsFalse(File.Exists(tempFile));

        // Clean
        FolderDeleter.DeleteByForce(tempFolder);
    }
}