function setCurrentTab(inputTab){
    currentTab = inputTab
    localStorage.setItem("storedCurrentTab", currentTab);
}

function setValue(elementId, value) {
    document.getElementById(elementId).value = value;
}

function checkIfSelectedValueIsTarget(target){
    if ($("#Select :selected").val() == target) {
        $("#ifTrue").show();
        $("#ifFalse").hide();
    } else {
        $("#ifTrue").hide();
        $("#ifFalse").show();
    }
}

function copyLinkToClipboard(linkToCopy){
    // Copy the text passed into the function plus the rest of the url
    let fullLink = window.location.origin+"/invite/"+linkToCopy
    navigator.clipboard.writeText(fullLink);
    
    // Change the button styling to show the link has been copied
    document.getElementById("copyBtn").innerHTML = "<i class='bi bi-check'></i> invite link copied"
    document.getElementById("copyBtn").className = "btn btn-success my-3"
}
