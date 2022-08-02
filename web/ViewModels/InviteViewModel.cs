namespace BricksAndHearts.ViewModels;

public class InviteViewModel
{
    public string? InviteLinkToAccept { get; set; } = null;

    public InviteViewModel(string? inviteLinkToAccept = null)
    {
        InviteLinkToAccept = inviteLinkToAccept;
    }
}