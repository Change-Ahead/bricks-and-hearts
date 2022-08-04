function setCurrentTab(inputTab){
    currentTab = inputTab
    localStorage.setItem("storedCurrentTab", currentTab);
}

function setValue(elementId, value) {
    document.getElementById(elementId).value = value;
}

function checkIfSelectedValueIsTarget(){
    if ($("#Select :selected").val() == "true")
    {
        $("#ifTrue").show();
        $("#ifFalse").hide();
    }
    else
    {
        $("#ifTrue").hide();
        $("#ifFalse").show();
    }
}

