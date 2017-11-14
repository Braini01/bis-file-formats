# ArmA File Format Library
This project provides developers with libraries that enable them to read common files that are mainly used by [Bohemia Interactive][1] for their games of the ArmA series, like rapified configs (config.bin), textures (\*.paa,\*.pac), skeletal animations (\*.rtm), game file container (\*.pbo) etc.

## The Idea, Goal and Vision
The basic idea is to create a central and public code base for those ArmA file formats, that is easy to use and integrate into a project. Ideally, this project would become the one stop shop for every developer working with those file formats. Such efforts have not had much success in the past and with this project some of the common reasons for this are tried to be avoided (see Features).

## Features

### Modularity
By providing small packages you can keep your project slick and dont need to include huge libraries with stuff you do not care about.

### Portability
The libraries are created using the [.NET Standard][2]. This makes it possible to use the libraries in most .NET applications and or libraries regardless of it being a .NET Core, .NET Framework, Mono, UWP or Xamarin application or library. With .NET Core being available on most platforms a good degree of platform-independence is achieved. You also can choose from a [lot of programming languages][3] to write a .NET application, like C#, F#, C++/CLI, VB.NET and many more.

### Nuget (Not implemented yet)
By providing all libs as NuGet packages, the integration of a library into your project becomes a piece of cake.

## Current Project State
The code you can find here is basically a little reorganized dump of some of the code that I created over the years researching those file formats. It currently basically enables you to read most files. It probably is often missing important accessors or functions that would be useful for a public API and I hope by putting this as an open source project that a lot of people will contribute to make this code base useful. So I highly encourage everyone to post some PRs to make this a great API.



[1]: https://www.bistudio.com/
[2]: https://github.com/dotnet/standard
[3]: https://en.wikipedia.org/wiki/List_of_CLI_languages
