var _countryPostalCodeMap = {};

function InitializePostalCodeMask(countryPCFieldName, postalCodeFieldName, countryPostalCodeMap) {    
    _countryPostalCodeMap = countryPostalCodeMap;

    var countryObj = $("#" + countryPCFieldName);
    if (countryObj != null) {
        countryObj.change(function () {
            //Reset the mask and the value
            var postalCodeObj = $("#" + postalCodeFieldName);
            postalCodeObj.unmask();
            postalCodeObj.val("");

            postalCodeObj.mask(GetPostalCodeMask($(this).val()));
            AddPostalCodeExample(postalCodeFieldName, GetPostalCodeEx($(this).val()));
        });

        $("#" + postalCodeFieldName).mask(GetPostalCodeMask(countryObj.val()));
        AddPostalCodeExample(postalCodeFieldName, GetPostalCodeEx(countryObj.val()));
    }
}

function GetPostalCodeMask(countryVal) {
    var mask = "";

    var mapObject = GetMapObject(countryVal, _countryPostalCodeMap);
    if (mapObject != null) {
        mask = mapObject.mask;
    }
    
    return mask;
}

function GetPostalCodeEx(countryVal) {
    var mask = "";

    var mapObject = GetMapObject(countryVal, _countryPostalCodeMap);
    if (mapObject != null) {
        mask = mapObject.example;
    }

    return mask;
}

function AddPostalCodeExample(postalCodeFieldName, maskExample) {
    var labelObj = $("label[for='" + postalCodeFieldName + "']");
    var label = labelObj.text();
    var index = label.indexOf(" (Ex: ");
    if (index > 0) {
        label = label.substring(0, index);
    }

    label += " (Ex: " + maskExample + ")";
    labelObj.text(label);
}