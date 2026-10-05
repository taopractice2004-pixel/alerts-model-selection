namespace AlertService.Common.Constants;

public static class AlertConstants
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int SearchMaxLength = 200;
    public const string SortByCreatedDate = "createdDate";
    public const string SortBySeverity = "severity";
    public const string SortByTitle = "title";
    public const string SortDirectionAsc = "asc";
    public const string SortDirectionDesc = "desc";
    public const string SortByPattern = "^(?i)(createdDate|severity|title)$";
    public const string SortDirectionPattern = "^(?i)(asc|desc)$";
    public const string NonWhitespacePattern = @"^[\s\S]*\S[\s\S]*$";
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int TagMinLength = 1;
    public const int TagMaxLength = 30;
    public const int MaxTagsPerAlert = 10;
}
