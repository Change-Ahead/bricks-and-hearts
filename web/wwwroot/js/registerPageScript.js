let currentTab = 0;

// Get the input field
let inputs, currentInput

inputs = document.getElementsByTagName('input');
// Execute a function when the user presses a key on the keyboard
window.addEventListener("keydown", function (event) {
    // If the user presses the "Enter" key on the keyboard
    if (event.key === "Enter") {
        // Cancel the default action, if needed
        event.preventDefault();
        // Deselects all boxes
        for (currentInput = 0; currentInput < inputs.length; currentInput++) {
            inputs[currentInput].blur();
        }
        // Trigger the button element with a click
        document.getElementById("nextBtn").click();
    }
});
if(localStorage.getItem("storedCurrentTab")!=null){
    currentTab = parseInt(localStorage.getItem("storedCurrentTab"));
    localStorage.setItem("storedCurrentTab", "0");
}
showTab(currentTab);

function showTab(currentTab) {
    document.getElementById("nextBtn").blur();
    document.getElementById("prevBtn").blur();
    let tabList = document.getElementsByClassName("registerTab");
    tabList[currentTab].style.display = "block";
    if (currentTab === 0) {
        document.getElementById("prevBtn").style.display = "none";
    } else {
        document.getElementById("prevBtn").style.display = "inline";
    }
    if (currentTab === (tabList.length - 1)) {
        document.getElementById("nextBtn").innerHTML = "Submit";
        populateReviewTab();
    } else {
        document.getElementById("nextBtn").innerHTML = "Next";
    }
    fixStepIndicator(currentTab);
}

function nextPrev(tabChange) {
    let tabList = document.getElementsByClassName("registerTab");
    // Exit the function if any field in the current tab is invalid:
    if (tabChange === 1 && !validateForm()) return false;
    // Hide the current tab:
    tabList[currentTab].style.display = "none";
    // Increase or decrease the current tab by 1:
    currentTab = currentTab + tabChange;
    // if you have reached the end of the form... :
    if (currentTab >= tabList.length) {
        //...the form gets submitted:
        document.getElementById("landlordEntry").submit();
        return false;
    }
    // Otherwise, display the correct tab:
    showTab(currentTab);
}

function validateForm() {
    // This function deals with validation of the form fields
    let tabList, tabInputList, tabInputIterator, valid = true;
    tabList = document.getElementsByClassName("registerTab");
    tabInputList = tabList[currentTab].querySelectorAll("input, select");
    // A loop that checks every input field in the current tab:
    for (tabInputIterator = 0; tabInputIterator < tabInputList.length; tabInputIterator++) {
        // Validation checks for the form
        let currentField = tabInputList[tabInputIterator]
        if ((currentField.name === "MembershipId") && ((tabInputList[tabInputIterator-1]).value === "false"))
        {
           currentField.className = "form-control empty";
        }
        if ((currentField.value === "" && currentField.className!=="form-control empty")
            || ((currentField.name === "MembershipId") && (currentField.value === "") && ((tabInputList[tabInputIterator-1]).value === "true"))
            || (currentField.type === "email" && (currentField.value.indexOf("@") === -1))
            || (currentField.type === "tel" && !Number.isInteger(Number(currentField.value.replace(/\+/g,"")))))
        {
            // add an "invalid" class to the field:
            tabInputList[tabInputIterator].className += " invalid";
            tabInputList[tabInputIterator].value = ""
            // and set the current valid status to false:
            valid = false;
        }
    }
    // If the valid status is true, mark the step as finished and valid:
    if (valid) {
        document.getElementsByClassName("step")[currentTab].className += " finish";
    }
    return valid;
}

function fixStepIndicator(currentStep) {
    // This function removes the "active" class of all steps...
    let stepList = document.getElementsByClassName("step");
    for (let stepIterator = 0; stepIterator < stepList.length; stepIterator++) {
        stepList[stepIterator].classList.remove("active");
    }
    //... and adds the "active" class to the current step:
    stepList[currentStep].className += " active";
}

function populateReviewTab() {
    $("#TitleReview").text($("#Title").val());
    $("#FirstNameReview").text($("#FirstName").val());
    $("#LastNameReview").text($("#LastName").val());

    let address = $("#Address_AddressLine1").val() + ",\n";
    const adr2 = $("#Address_AddressLine2").val();
    const adr3 = $("#Address_AddressLine3").val();
    address += adr2 !== "" ? adr2 + ",\n" : "";
    address += adr3 !== "" ? adr3 + ",\n" : "";
    address += $("#Address_TownOrCity").val() + ",\n";
    address += $("#Address_County").val() + ",\n";
    address += $("#Address_Postcode").val();
    $("#AddressReview").text(address);

    $("#EmailReview").text($("#Email").val());
    $("#PhoneReview").text($("#Phone").val());

    $("#LandlordTypeReview").text($("#LandlordType").val());
    const forProfit = $("#IsLandlordForProfit").val();
    $("#IsLandlordForProfitReview").text(forProfit ? "For profit" : "Not for profit");
    $("#CompanyNameReview").text($("#CompanyName").val());

    const charterStatus = $("#Select").val();
    $("#CharterStatusReview").text(charterStatus ? "Already approved" : "Not yet approved");
    $("#MembershipIdReview").text($("#MembershipId").val());
}