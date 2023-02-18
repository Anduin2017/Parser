﻿using Aiursoft.Parser.Core.Abstracts;
using Aiursoft.Parser.FFmpeg.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Parser.FFmpeg;

public class StartUp : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<CommandService>();
    }
}