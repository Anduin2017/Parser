using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.CSTools.Tools;
using Anduin.Parser.Photos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Anduin.Parser.Tests;

[TestClass]
public class PhotoCompressionIntegrationTests
{
    private readonly SingleCommandApp<CompressPhotosHandler> _program = new SingleCommandApp<CompressPhotosHandler>()
        .WithDefaultOption(CommonOptionsProvider.PathOptions);
    private readonly string _testPhotosPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "big-photos");

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
    public async Task InvokeCompressWithoutArg()
    {
        var result = await _program.TestRunAsync(["compress-photos"]);
        Assert.AreEqual(1, result.ProgramReturn);
    }

    [TestMethod]
    public async Task InvokeCompressWithTemp()
    {
        // Prepare
        var tempFolder = Path.Combine(Path.GetTempPath(), $"Parser-UT-{Guid.NewGuid()}");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        // Copy all files from _testPhotosPath to tempFolder
        foreach (var file in Directory.EnumerateFiles(_testPhotosPath))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(tempFolder, fileName));
        }
        
        // Run
        // Delete original photos
        var result = await _program.TestRunAsync([
            "--path",
            tempFolder,
            "-del"
        ]);

        // Assert
        Assert.AreEqual(0, result.ProgramReturn);

        // p1 should be compressed and deleted.
        Assert.IsFalse(File.Exists(Path.Combine(tempFolder, "p1.jpg")));
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "p1_comp.jpg")));
        Assert.IsTrue(new FileInfo(Path.Combine(tempFolder, "p1_comp.jpg")).Length < 3 * 1024 * 1024);
        
        // Clean
        FolderDeleter.DeleteByForce(tempFolder);
    }
}