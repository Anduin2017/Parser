using System.CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Anduin.Parser.Core.Framework;
using Anduin.Parser.FFmpeg;

namespace Parser.Tests;

[TestClass]
public class IntegrationTests
{
    private readonly RootCommand _program;
    private readonly string _testVideo;

    public IntegrationTests()
    {
        var descriptionAttribute = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

        this._testVideo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "test_video.mp4");
        this._program = new RootCommand(descriptionAttribute ?? "Unknown usage.")
            .AddGlobalOptions()
            .AddPlugins(
                new FFmpegPlugin()
            );
    }

    [TestMethod]
    public async Task InvokeHelp()
    {
        var result = await _program.InvokeAsync(new[] { "--help" });
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task InvokeVersion()
    {
        var result = await _program.InvokeAsync(new[] { "--version" });
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task InvokeUnknown()
    {
        var result = await _program.InvokeAsync(new[] { "--wtf" });
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task InvokeWithoutArg()
    {
        var result = await _program.InvokeAsync(Array.Empty<string>());
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public async Task InvokeFFmpegWithoutArg()
    {
        var result = await _program.InvokeAsync(new[] { "ffmpeg" });
        Assert.AreEqual(1, result);
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
        var result = await _program.InvokeAsync(new[]
        {
            "ffmpeg",
            "--path",
            tempFolder
        });

        // Assert
        Assert.AreEqual(0, result);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "test-video_265.mp4")));
        Assert.IsFalse(File.Exists(tempFile));

        // Clean
        Directory.Delete(tempFolder, true);
    }
}
