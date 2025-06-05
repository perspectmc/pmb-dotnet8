var _provinceList = new Array();
var _countryFieldName = "";
var _provinceFieldName = "";
var _countryProvinceMap = {};

function InitializeCountryProvince(countryFieldName, provinceFieldName, countryProvinceMap) {
    _countryFieldName = countryFieldName;
    _provinceFieldName = provinceFieldName;
    _countryProvinceMap = countryProvinceMap;

    var countryObj = $("#" + countryFieldName);

    $("#" + provinceFieldName + " > option").each(function () {
        _provinceList.push({ OptionText: this.text, OptionValue: this.value });
    });

    if (countryObj != null) {
        countryObj.change(function () {
            SetupProvinceOptions($(this), $("#" + provinceFieldName));
        });

        SetupProvinceOptions(countryObj, $("#" + provinceFieldName));
    }
}

function SetupProvinceOptions(countryObj, provObj) {
    var countryVal = countryObj.val();    
    var provVal = provObj.val();
    
    ClearProvinceOptions();
    
    if (countryVal != null && countryVal != "") {
        var map = GetMapObject(countryVal, _countryProvinceMap);

        if (map != null) {
            var fromValue = parseFloat(map.from);
            var toValue = parseFloat(map.to);
            var selectedValue = map.defaultProv;

            if (provVal != null) {
                if (provVal >= fromValue && provVal <= toValue) {
                    selectedValue = provVal;
                }
            }

            $.each(_provinceList, function (index, item) {
                var curValue = parseFloat(item.OptionValue);
                if (curValue >= fromValue && curValue <= toValue) {
                    var isSelected = "";

                    if (item.OptionValue == selectedValue) {
                        isSelected = "selected";
                    }

                    provObj.append('<option title="' + item.OptionText + '" value="' +
                    item.OptionValue + '" ' + isSelected + '>' + item.OptionText + '</option>');
                }
            });

            provObj.val(selectedValue);
        }
    }

    provObj.width(countryObj.width());
}

function ClearProvinceOptions() {
    $("#" + _provinceFieldName + " > option").each(function () {
        $(this).remove();
    });
}

function GetMapObject(mapValue, mapList) {
    var mapIndex = -1;

    $.each(mapList, function (index, item) {
        if (mapValue == item.country) {
            mapIndex = index;
        }
    });

    if (mapIndex > -1) {
        return mapList[mapIndex];
    }
    else {
        return null;
    }
}