global using Microsoft.Playwright;
global using Microsoft.Playwright.NUnit;
global using NUnit.Framework;
global using PlaywrightMasterToolkit.Base;
global using PlaywrightMasterToolkit.Config;
global using PlaywrightMasterToolkit.Helpers;
global using PlaywrightMasterToolkit.Models;
global using PlaywrightMasterToolkit.Utils;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)]
