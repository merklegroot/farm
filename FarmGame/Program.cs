namespace FarmGame;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "migrate-assets")
        {
            Raylib_cs.Raylib.InitWindow(1, 1, "migrate");
            try
            {
                DefinedAssetStore.MigrateLegacyJsonAssets();
            }
            finally
            {
                Raylib_cs.Raylib.CloseWindow();
            }

            return;
        }

        new Game().Run();
    }
}
