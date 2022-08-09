function setCurrentTab(inputTab){
    currentTab = inputTab
    localStorage.setItem("storedCurrentTab", currentTab);
}

function setValue(elementId, value) {
    document.getElementById(elementId).value = value;
}

function checkIfSelectedValueIsTarget(target){
    if ($("#availabilitySelect :selected").val() == target) {
        $("#availableFromInput").show();
    } else {
        $("#availableFromInput").hide();
    }
}

