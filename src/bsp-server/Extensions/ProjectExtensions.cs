using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

public static class ProjectExtensions
{
    private const string PACKAGE_REFERENCE_TAG = "PackageReference";

    public static bool IsTestProject(this Project project)
    {
        return project.AllEvaluatedItems.Any(item => 
            item.ItemType.Equals(PACKAGE_REFERENCE_TAG, StringComparison.OrdinalIgnoreCase) &&
            item.EvaluatedInclude.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsTestProject(this ProjectInstance project)
    {
        return project.Items.Any(item => 
            item.ItemType.Equals(PACKAGE_REFERENCE_TAG, StringComparison.OrdinalIgnoreCase) &&
            item.EvaluatedInclude.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsRunnableProject(this Project project)
    {
        var outputType = project.GetProperty("OutputType");
        return outputType?.EvaluatedValue.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? true;
    }

    public static bool IsLibraryProject(this Project project)
    {
        var outputType = project.GetProperty("OutputType");
        return outputType?.EvaluatedValue.Equals("Library", StringComparison.OrdinalIgnoreCase) ?? true;
    }

    public static IEnumerable<string> GetProjectReferences(this Project project)
    {
        var items = project.GetItems("ProjectReference");

        return items.Select(x =>
            Path.GetFullPath(Path.Combine(project.DirectoryPath, x.EvaluatedInclude)));
    }
}