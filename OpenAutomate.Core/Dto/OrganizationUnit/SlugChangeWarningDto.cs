namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class SlugChangeWarningDto
    {
        public string CurrentSlug { get; set; }
        public string ProposedSlug { get; set; }
        public bool IsChangeSignificant { get; set; }
        public string[] PotentialImpacts { get; set; }
        public bool RequiresConfirmation { get; set; }
    }
} 