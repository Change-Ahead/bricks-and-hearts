function setCurrentTab(inputTab) {
    currentTab = inputTab
    localStorage.setItem("storedCurrentTab", currentTab);
}

function setValue(elementId, value) {
    document.getElementById(elementId).value = value;
}

function checkIfSelectedValueIsTarget(target, id) {
    if ($("#" + id + " :selected").val() == target) {
        $("#ifTrue").show();
        $("#ifFalse").hide();
    } else {
        $("#ifTrue").hide();
        $("#ifFalse").show();
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
        $("#target").val("CB2 1LA");
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