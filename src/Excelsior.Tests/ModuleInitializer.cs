public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
        VerifierSettings.DontScrubDateTimes();
        VerifierSettings.DontScrubGuids();
        // Pin the fallback paper size. Excelsior writes no pageSetup/@paperSize, so Morph otherwise
        // takes the machine's region: A4 here, Letter on the North-American CI agents. That renders
        // the same workbook at a different page size and mismatches every png snapshot.
        VerifyOpenXml.UseLetterPageSize = false;
        VerifierSettings.InitializePlugins();
    }
}
