function setCurrentTab(inputTab) {
    currentTab = inputTab
    localStorage.setItem("storedCurrentTab", currentTab);
}

function setValue(elementId, value) {
    document.getElementById(elementId).value = value;
}

function displayIfSelectedValueIsTarget(target, id, idToShow, idToHide = null) {
    if ($("#" + id + " :selected").val() == target) {
        $("#" + idToShow).show();
        $("#" + idToHide).hide();
    } else {
        $("#" + idToShow).hide();
        $("#" + idToHide).show();
    }
}

function makeRequiredIfSelectedValueIsTarget(target, id, idToRequire) {
    if ($("#" + id + " :selected").val() == target) {
        $("#" + idToRequire).prop('required', true);
    } else {
        $("#" + idToRequire).removeAttr('required');
    }
}

function insertDefaultPostcodeIfNotSortByLocation(id) {
    if ($("#" + id + " :selected").val() == "Location") {
        $("#target").val("");
    } else {
        $("#target").val("HA1 2EY");
    }
}

function clearMembershipIdIfFalse() {
    if ($("#Select :selected").val() == "false") {
        $("#MembershipId").val(null);
    }
}

function copyLinkToClipboard(linkToCopy) {
    // Copy the text passed into the function plus the rest of the url
    const fullLink = window.location.origin + "/invite/" + linkToCopy
    navigator.clipboard.writeText(fullLink)
        .then(() => {
            // Change the button styling to show the link has been copied
            const copyBtn = document.getElementById("copyBtn");
            copyBtn.innerHTML = "<i class='bi bi-check'></i> invite link copied"
            copyBtn.className = "btn btn-success my-3"
        });
}

function hidePlaceholder(imageId) {
    $(imageId + "Placeholder").hide();
    $(imageId).show();
}