﻿namespace BricksAndHearts.ViewModels;

public class ContactUsViewModel
{
    public List<(string question, string answer)> FAQs = new List<(string question, string answer)>
    {
        ("As a tenant, what references do I need to provide to move into a property?",
            "References will depend on the Landlord’s criteria. These references can be emailed to the Landlord with the support of a Change Ahead volunteer."),
        ("What is the minimum tenancy agreement on any accommodation?", "Minimum of 6 months"),
        ("Does the landlord have to be a member of the Change Ahead Housing Charter to list a property?",
            "Yes, the Landlord must be an approved member of the Change Ahead Housing Charter to ensure the safety of their properties meet the Government’s published social housing white paper -The Charter for Social Housing Residents."),
        ("As a landlord, how do I apply for the Change Ahead Charter?",
            "Email housing@changeahead.org.uk with Reference<> “Bricks & Hearts Membership”"),
        ("Is the platform free to use for the tenant?",
            "Yes, the platform is free for anyone looking for long term accommodation."),
        ("As a tenant, when will I be notified about date and time of property viewings?",
            "A Change Ahead volunteer from the Housing Team will contact you with regards to viewing appointments. They will support you in all communications with the landlord up until move in date."),
        ("What happens after a tenant moves in but needs support?",
            "A Change Ahead volunteer will check in to see how we can support the tenant in employment, training and education opportunities, financial and health advice."),
        ("Once the tenant and landlord have been matched, does the property listing disappear?", "Yes"),
        ("As a landlord, how do I allocate the space to a tenant?",
            "This will be matched by a Change Ahead volunteer and will be waiting for your approval."),
        ("As a user of the App what measures are in place to ensure there is trust and safety along the process?",
            "Landlords are only permitted to list their properties on the platform because they have committed to the Change Ahead Housing Charter. They have agreed to be accountable for ensuring the safety of their properties and empowering tenants to have a voice. The Change Ahead volunteers have Disclosure and Barring Service certification to ensure they are clear from criminal records.")
    };
}