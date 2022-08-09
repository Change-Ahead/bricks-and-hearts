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

