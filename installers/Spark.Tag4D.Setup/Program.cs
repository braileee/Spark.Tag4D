using Spark.Tag4D.Setup;

try
{
    SetupOptions options = SetupOptions.Parse(args);
    string msi = InstallerBuilder.Create(options).Build();
    Console.WriteLine($"Installer created: {msi}");
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
