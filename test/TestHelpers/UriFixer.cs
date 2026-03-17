namespace test;

public static class UriFixer
{
    public static Uri WithFileSchema(string filePath)
    {
        // workaround for "file://" schema being not serialized: https://github.com/dotnet/runtime/issues/90140
        return new Uri($"file://{filePath}", UriKind.Absolute);
    }
}