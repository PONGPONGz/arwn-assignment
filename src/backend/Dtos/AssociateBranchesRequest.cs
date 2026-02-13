namespace ClinicPos.Api.Dtos;

public class AssociateBranchesRequest
{
    public List<Guid> BranchIds { get; set; } = [];
}
