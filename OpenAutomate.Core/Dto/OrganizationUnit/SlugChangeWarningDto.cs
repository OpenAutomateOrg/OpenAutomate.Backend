namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class SlugChangeWarningDto
    {
        public string CurrentName { get; set; }
        public string CurrentSlug { get; set; }
        public string NewName { get; set; }
        public string NewSlug { get; set; }
        public string[] PotentialImpacts { get; set; }
        public bool RequiresConfirmation { get; set; } = true;
    }
} 