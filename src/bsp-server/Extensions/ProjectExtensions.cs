using Microsoft.Build.Evaluation;

public static class ProjectExtensions
{
    private const string PACKAGE_REFERENCE_TAG = "PackageReference";

    public static bool IsTestProject(this Project project)
    {
        return project.AllEvaluatedItems.Any(item => 
            item.ItemType.Equals(PACKAGE_REFERENCE_TAG, StringComparison.OrdinalIgnoreCase) &&
            item.EvaluatedInclude.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsRunnableProject(this Project project)
    {
        var outputType = project.GetProperty("OutputType");
        return outputType?.EvaluatedValue.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? true;
    }
}