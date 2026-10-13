namespace AssetManagement.UI.Helpers;

public static class DisplayFormat
{
    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd");

    // The API stores times in UTC but sends them without a zone; show them in local time
    public static string DateTime(DateTime? value) =>
        value.HasValue
            ? System.DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "";
}
