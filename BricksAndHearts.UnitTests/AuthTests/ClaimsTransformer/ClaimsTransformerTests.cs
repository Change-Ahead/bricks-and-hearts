using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.AuthTests.ClaimsTransformer;

public class ClaimsTransformerTests
{
    [Fact]
    public void TransformAsync_OnUserWithNoLandlordId_DoesntAddLandlordRole()
    {
        
    }
    
    [Fact]
    public void TransformAsync_OnUserWithLandlordId_AddsLandlordRole()
    {

    }
}