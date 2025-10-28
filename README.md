# Parser

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/anduin/parser/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.com/anduin/parser/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/anduin/parser/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.com/anduin/parser/badges/master/coverage.svg)](https://gitlab.aiursoft.com/anduin/parser/-/pipelines)
[![NuGet version](https://img.shields.io/nuget/v/Anduin.Parser.svg)](https://www.nuget.org/packages/Anduin.Parser/)
[![ManHours](https://manhours.aiursoft.cn/r/gitlab.aiursoft.com/anduin/parser.svg)](https://gitlab.aiursoft.com/anduin/parser/-/commits/master?ref_type=heads)

A small project helps me to parse and save my videos.

## Install

Requirements:

1. [.NET 9 SDK](http://dot.net/)

Run the following command to install this tool:

```bash
dotnet tool install --global Anduin.Parser
```

## Usage

After getting the binary, run it directly in the terminal.

```bash
$ parser
Required command was not provided.
Option '--path' is required.

Description:
  A cli tool project helps to re-encode and save all videos under a path.

Usage:
  parser [command] [options]

Options:
  -g, --gpu                     Use NVIDIA GPU to speed up parsing. Only if you have an NVIDIA GPU attached. [default:
                                False]
  -c, --crf <crf>               The range of the CRF scale is 0-51, where 0 is lossless (for 8 bit only, for 10 bit use
                                -qp 0), 20 is the default, and 51 is worst quality possible. [default: 20]
  -p, --path <path> (REQUIRED)  Path of the videos to be parsed.
  -d, --dry-run                 Preview changes without actually making them
  -v, --verbose                 Show detailed log
  --version                     Show version information
  -?, -h, --help                Show help and usage information

Commands:
  ffmpeg  The command to convert all video files to HEVC using FFmpeg.
```

It will fetch all videos under that folder, and try to re-encode it with ffmpeg.
