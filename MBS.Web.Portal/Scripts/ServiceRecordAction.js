var myPremiumCodeList = "";
var myCareCodeList = "";
var myRNPExcludeCodeList = "";
var specialCodeList = [{ Code: "893A", Rate: 0.25 }, { Code: "894A", Rate: 0.4 }, { Code: "895A", Rate: 0.15 }];
var initialRunFlag = true;
var feeCodeList = []; //all code include WCB
var wcbFeeCodeList = []; //WCB Code only
var _isReadOnly = false;
var _codeNeedCalculatedUnit = ["580H", "585H", "918A"]; //copy time
var _nextFeeCodes = ["501H", "503H", "505H", "507H"];
var _codeNeedCalculatedUnit3 = ["540H", "545H"];
var _codeNeedToIncOneUnitGreaterThan7 = ["40J", "50J", "81A", "222A", "223A", "225A", "226A", "728A", "729A", "918A", "926A", "927A", "928A", "919A", "936A", "41B", "164B", "13C", "16C", "12E", "13E", "135I", "31J", "61J", "81J"];
var _codeFor60Minutes = [ "30J", "60J", "80J" ];
var _startupCodes = [ "500H", "502H", "504H", "506H" ];

var _versionNumber = "";

var _autoCompleteUnitCodeCtrlId = "";

function ServiceRecordActionPageLoad(isReadOnly, preimumCodeList, careCodeList, rnpExcludeCodeList, versionNumber) {
    _isReadOnly = isReadOnly;
    myPremiumCodeList = preimumCodeList;
    myCareCodeList = careCodeList;
    myRNPExcludeCodeList = rnpExcludeCodeList;

    _versionNumber = versionNumber;
        
    $.each($(".explainCode"), function () {
        var obj = $(this);
        var code = obj.text();
        if (code != null && code != undefined) {
            obj.tooltip({ html: true, placement: "top" });
        }   
    });

    $.each($(".feeCodeLabel"), function () {        
        $(this).tooltip({ html: false, placement: "top" });
    });

    $.each($(".diagCodeLabel"), function () {
        $(this).tooltip({ html: false, placement: "top" });
    });

    $.each($(":text"), function () {
        $(this).keypress(function (e) {
            if (e.which == 13) {
                e.preventDefault();
            }
        });
    });
    
    SetRequiredField("Record_PatientLastName");
    SetRequiredField("Record_PatientFirstName");
    SetRequiredField("BirthDateString");
    SetRequiredField("Record_Sex");
    SetRequiredField("ServiceDateString");

    $("#PremiumCodeLink").tooltip({ html: true, placement: 'right' });

    $("#HospitalNumberLink").tooltip({ html: true, placement: 'left' });
    $("#SpecialCircumstanceLink").tooltip({ html: true, placement: 'left' });
    $("#RunCodeLink").tooltip({ html: true, placement: 'left' });

    $("#CalculationStartNoteLink").tooltip({ html: true, placement: 'left' });
    $("#CalculationEndNoteLink").tooltip({ html: true, placement: 'left' });

    $('input[type=radio][name="Record.ClaimType"]').change(function ()
    {
        if ($('[name="Record.ClaimType"]:radio:checked').val() == 1) {
            $("#wordCounter").val(425);
        } else {
            $("#wordCounter").val(770);
        }

        $("#Record_Comment").keyup();
    })

    $("#Record_ClaimType").change();

    $("#Record_Comment").keyup(function () {
        var claimType = $('[name="Record.ClaimType"]:radio:checked').val(); //MSB - 0, WCB - 1
        var myLimit = claimType == 1 ? 425 : 770;
        var commentBox = $(this).val();
        var count = myLimit - commentBox.length;
        if (count < 0) {
            $(this).val(commentBox.substr(0, myLimit));
            count = 0;
        }
            
        $("#wordCounter").val(count);
    });

    $("#Record_Comment").keyup();

    $("#Record_Notes").keyup(function () {
        var myLimit = 1000;
        var commentBox = $(this).val();
        var count = myLimit - commentBox.length;
        if (count < 0) {
            $(this).val(commentBox.substr(0, myLimit));
            count = 0;
        }

        $("#wordCounterNotes").val(count);
    });

    $("#Record_Notes").keyup();

    $("#Record_HospitalNumber").keyup(function () {
        ValidationHSN();
    });

    $("#Record_HospitalNumber").autocomplete({
        focus: function (event, ui) {
            //$(this).val(ui.item.value.myHospitalNumber);
            return false;
        },
        source: function (request, response) {
            $.ajax({
                url: _applicationBaseUrl + '/ServiceRecord/GetPatientListUsingHSN',
                data: "prefix=" + request.term,
                cache: true,
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.myHospitalNumber + " (" + item.myProvince + ") - " + item.myLastName + ", " + item.myFirstName + " - " + item.myBirthDate + " - " + item.mySex,
                            value: item
                        }
                    }));
                }
            });
        },
        select: function (event, ui) {
            $(this).val(ui.item.value.myLastName);
            $("#Record_PatientLastName").val(ui.item.value.myLastName);
            $("#Record_PatientFirstName").val(ui.item.value.myFirstName);
            $("#BirthDateString").val(ui.item.value.myBirthDate);
            $("#Record_Sex").val(ui.item.value.mySex);
            $("#Record_Province").val(ui.item.value.myProvince);
            $("#Record_HospitalNumber").val(ui.item.value.myHospitalNumber);

            if (ui.item.value.myReferringDocNumber != "" && ui.item.value.myReferringDocNumber != undefined && ui.item.value.myReferringDocNumber != null) {
                $("#Record_ReferringDoctorNumber").val(ui.item.value.myReferringDocNumber);
                RunReferringDocNameRetrieval();
            } else {
                $("#Record_ReferringDoctorNumber").val("");
                $("#DoctorName").val("");
            }

            $("#ErrorHSN").toggle(false);
            ValidationHSN();
            return false;
        }
    });
  
    $("#Record_Province").change(function () {
        $("#Record_HospitalNumber").keyup();
    });

    $("#Record_ServiceLocation").change(function () {
        for (var i = 1; i < 8; i++) {
            ProcessCode(i);        
        }
    });

    //$("#ServiceStartTimeString").keyup(function () {
    //    ServiceTime_Changed();
    //});

    //$("#ServiceEndTimeString").keyup(function () {
    //    ServiceTime_Changed();
    //});

    var imageURL = _applicationBaseUrl + "/Content/images/calendar.gif";    
    var yearRange = (new Date().getFullYear() - 120) + ":" + new Date().getFullYear();
    $("#BirthDateString").datepicker({
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        yearRange: yearRange,
        dateFormat: 'mmy',
        maxDate: '0',
        showOn: "button",
        buttonImage: imageURL,
        buttonImageOnly: true,
        buttonText: "Select date"
    });

    $("#ServiceDateString").datepicker({
        dateFormat: 'ddmmy',
        maxDate: '0',
        showOn: "button",
        buttonImage: imageURL,
        buttonImageOnly: true,
        buttonText: "Select date"
    });

    $("#DischargeDateString").datepicker({
        dateFormat: 'ddmmy',
        showOn: "button",
        buttonImage: imageURL,
        buttonImageOnly: true,
        buttonText: "Select date"
    });

    $("#Record_PatientLastName").autocomplete({
        focus: function (event, ui) {
            //$(this).val(ui.item.value.myLastName);
            return false;
        },
        source: function (request, response) {            
            $.ajax({
                url: _applicationBaseUrl + '/ServiceRecord/GetPatientList',
                data: "prefix=" + request.term,
                cache: true,
                success: function (data) {                    
                    response($.map(data, function (item) {
                        return {
                            label: item.myLastName + ", " + item.myFirstName + " - " + item.myHospitalNumber + " - " + item.myBirthDate+ " - " + item.mySex,
                            value: item
                        }
                    }));
                }
            });
        },
        select: function (event, ui) {
            $(this).val(ui.item.value.myLastName);
            $("#Record_PatientFirstName").val(ui.item.value.myFirstName);
            $("#BirthDateString").val(ui.item.value.myBirthDate);
            $("#Record_Sex").val(ui.item.value.mySex);
            $("#Record_Province").val(ui.item.value.myProvince);
            $("#Record_HospitalNumber").val(ui.item.value.myHospitalNumber);

            if (ui.item.value.myReferringDocNumber != "" && ui.item.value.myReferringDocNumber != undefined && ui.item.value.myReferringDocNumber != null) {
                $("#Record_ReferringDoctorNumber").val(ui.item.value.myReferringDocNumber);
                RunReferringDocNameRetrieval();
            } else {
                $("#Record_ReferringDoctorNumber").val("");
                $("#DoctorName").val("");
            }

            $("#ErrorHSN").toggle(false);
            return false;
        }
    });

    $("#DoctorName").autocomplete({
        focus: function (event, ui) {
            $(this).val(ui.item.label);
            return false;
        },
        source: function (request, response) {
            $.ajax({
                url: _applicationBaseUrl + '/ServiceCode/GetRefDocNameList',
                data: "prefix=" + request.term,
                cache: true,
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.Value,
                            value: item.Key
                        }
                    }));
                }
            });
        },
        select: function (event, ui) {
            $(this).val(ui.item.label);
            $("#Record_ReferringDoctorNumber").val(ui.item.value);
            return false;
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        let txt = String(item.label).replace(new RegExp(this.term, "gi"), "<span style='color:blue;font-weight: bold;'>$&</span>");
        return $("<li style='width:600px'></li>")
            .data("ui-autocomplete-item", item)
            .append("<a>" + txt + "</a>")
            .appendTo(ul);
    };

    $("#Record_ReferringDoctorNumber").autocomplete({
        focus: function (event, ui) {
            $(this).val(ui.item.label);
            return false;
        },
        source: function (request, response) {
            $.ajax({
                url: _applicationBaseUrl + '/ServiceCode/GetRefDocCodeList',
                data: "prefix=" + request.term,
                cache: true,
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.Key + " " + item.Value,
                            value: item.Key
                        }
                    }));
                }
            });
        },
        select: function (event, ui) {
            var index = ui.item.label.indexOf(" ");
            $(this).val(ui.item.value);
            $("#DoctorName").val(ui.item.label.substr(index + 1));
            return false;
        }
    }).data("ui-autocomplete")._renderItem = function (ul, item) {
        let txt = String(item.label).replace(new RegExp(this.term, "gi"), "<span style='color:blue;font-weight: bold;'>$&</span>");
        return $("<li style='width:500px'></li>")
            .data("ui-autocomplete-item", item)
            .append("<a>" + txt + "</a>")
            .appendTo(ul);
    };
    
    RunReferringDocNameRetrieval();

    //$("#PremiumCode").keyup(function () {
    //    for (var i = 1; i < 8; i++) {
    //        ProcessCode(i + "");
    //    }
    //});
    
    var unitCodesToGetModel = "";
    var diagCodesToGetDesc = "";

    //Setup Each Unit Record fields Initialization
    for (var i = 1; i < 8; i++) {
        
        //Deal with Diag Code Field Initialization
        $(".diagcodeauto" + i).autocomplete({
            focus: function (event, ui) {
                $(this).val(ui.item.value);

                var diagCodeDescription = "No description available.";

                var diagCodeDescriptionToolTip = $(this).parent().find('.diagCodeLabel');

                var indexOf = ui.item.label.indexOf(" - ");
                if (diagCodeDescriptionToolTip != null && indexOf > 0) {
                    diagCodeDescription = ui.item.label.substr(indexOf + 3);
                }

                diagCodeDescriptionToolTip.attr("title", diagCodeDescription);
                diagCodeDescriptionToolTip.tooltip('fixTitle');

                return false;
            },
            source: function (request, response) {
                $.ajax({
                    url: _applicationBaseUrl + '/ServiceCode/GetICDCodeList',
                    data: "prefix=" + request.term,
                    cache: true,
                    success: function (data) {
                        response($.map(data, function (item) {
                            return {
                                label: item.Key + " - " + item.Value,
                                value: item.Key
                            }
                        }));
                    }
                });
            },
            select: function (event, ui) {
                $(this).val(ui.item.value);
                $("#LastSelectedDiagCode").val(ui.item.value);

                PopulateBelowDiagCode(GetIndex($(this).attr("id")), ui.item.value, ui.item.label);

                return false;
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            let txt = String(item.label).replace(new RegExp(this.term, "gi"), "<span style='color:blue;font-weight: bold;'>$&</span>");
            return $("<li style='width:350px'></li>")
                .data("ui-autocomplete-item", item)
                .append("<a>" + txt + "</a>")
                .appendTo(ul);
        };

        var diagCode = $("#DiagCode" + i).val();
        if (diagCode != "" && diagCode != undefined) {
            diagCodesToGetDesc += diagCode + ",";
        }

        //Deal with Unit fields Initialization
        $("#UnitPremiumCode" + i + "").change(function () {
            ProcessCode(GetIndex($(this).attr("id")));
        });
       
        $("#UnitAmount" + i).focus(function () {
            $(this).blur();
        });

        $("#UnitCode" + i).autocomplete({
            minLength: 2,
            delay: 200,
            focus: function (event, ui) {
                //$(this).val(ui.item.key.replace(" - WCB", ""));
                return false;
            },
            source: function (request, response) {
                var claimType = $('[name="Record.ClaimType"]:radio:checked').val(); //MSB - 0, WCB - 1
                _autoCompleteUnitCodeCtrlId = $(this)[0].element[0].id;

                var termToSearch = request.term.toUpperCase();
                var feeCodeObject = feeCodeList.find(el => el.key == termToSearch);
                
                if (feeCodeObject != null && feeCodeObject != undefined) {
                    $("#" + _autoCompleteUnitCodeCtrlId).autocomplete("close");
                    $("#" + _autoCompleteUnitCodeCtrlId).val(feeCodeObject.key.replace(" - WCB", ""));
                    ItemSelected($("#" + _autoCompleteUnitCodeCtrlId), feeCodeObject);
                    
                } else {
                    $.ajax({
                        async: true,
                        url: _applicationBaseUrl + '/ServiceCode/GetUnitCodeFee',
                        data: "prefix=" + request.term + "&claimType=" + claimType,
                        cache: true,
                        success: function (data) {
                            if (data != null && data.length == 1) {
                                var item = data[0];
                                $("#" + _autoCompleteUnitCodeCtrlId).val(item.key.replace(" - WCB", ""));
                                ItemSelected($("#" + _autoCompleteUnitCodeCtrlId), item);
                            } else {
                                response(data);
                            }
                        }
                    });
                }
            },
            select: function (event, ui) {
                $(this).val(ui.item.key.replace(" - WCB", ""));

                if (ui != null && ui != undefined) {
                    ItemSelected($(this), ui.item);
                }

                return false;
            }
        }).data("ui-autocomplete")._renderItem = function (ul, item) {
            let txt = String(item.label).replace(new RegExp(this.term, "gi"), "<span style='color:blue;font-weight: bold;'>$&</span>");
            return $("<li style='width:350px'></li>")
                .data("ui-autocomplete-item", item)
                .append("<a>" + txt + "</a>")
                .appendTo(ul);
        };

        $("#UnitCode" + i).keyup(function () {
            ProcessCode(GetIndex($(this).attr("id")));
        });

        $("#UnitNumber" + i).keyup(function () {
            ProcessCode(GetIndex($(this).attr("id")));
        });

        $("#UnitStartTime" + i).keyup(function () {
            UnitTime_Changed(GetIndex($(this).attr("id")));
        });

        $("#UnitEndTime" + i).keyup(function () {
            UnitTime_Changed(GetIndex($(this).attr("id")));
        });

        //Get Unit Code
        var unitCodeNumber = $("#UnitCode" + i).val();
        if (unitCodeNumber != null && unitCodeNumber != undefined && unitCodeNumber != "") {
            var specialCode = GetSpecialCode(unitCodeNumber);
            if (specialCode != null) {
                $("#UnitNumber" + i).val(1);
                $("#UnitNumber" + i).attr("disabled", "disabled");
            } else {
                unitCodesToGetModel += unitCodeNumber + ",";                
            }
        }

        $("#SpecialCircumstanceIndicator" + i).change(function () {
            CheckSpecialCircumstanceWithUnitCode(GetIndex($(this).attr("id")));
        });

        $("#RecordClaimType" + i + "").change(function () {
            CascadeRecordClaimType(GetIndex($(this).attr("id")), "RecordClaimTypeChange");
        });
    }
    
    //Request the description of the Diag Code
    if (diagCodesToGetDesc != "") {
        $.ajax({
            async: true,
            type: "GET",
            cache: true,
            url: _applicationBaseUrl + '/ServiceCode/GetICDCodeDescriptionList',
            data: "diagcodelist=" + diagCodesToGetDesc,

            success: function (data) {
                if (data != undefined && data.length > 0) {
                    for (var i = 1; i < 8; i++) {
                        var diagCode = $("#DiagCode" + i).val().toUpperCase();
                        var diagCodeObject = data.find(el => el.myCode == diagCode);
                        if (diagCodeObject != null) {
                            var diagCodeDescriptionToolTip = $(".diagcodeauto" + i).parent().find('.diagCodeLabel');
                            diagCodeDescriptionToolTip.attr("title", diagCodeObject.myDescription);
                            diagCodeDescriptionToolTip.tooltip('fixTitle');
                        }
                    }
                }

                return false;
            },
            error: function (msg) {
                alert(msg.status + " " + msg.statusText);
            }
        });
    }

    //Request Fee Model for the list of code
    if (unitCodesToGetModel != "") {
        $.ajax({
            async: true,
            type: "GET",
            cache: true,
            url: _applicationBaseUrl + '/ServiceCode/GetUnitCodeList',
            data: "unitcodelist=" + $("#MostUsedCodes").val() + "," + _codeNeedCalculatedUnit.join(",") + "," + _nextFeeCodes.join(",") + "," + _codeNeedCalculatedUnit3.join(",") + "," + unitCodesToGetModel,
            success: function (data) {
                if (data != undefined && data.length > 0) {
                    $.map(data, function (item) {
                        feeCodeList.push(item);
                    })

                    for (var i = 1; i < 8; i++) {
                        var unitCodeCtrl = $("#UnitCode" + i);
                        var unitCodeNumber = unitCodeCtrl.val();
                        if (unitCodeNumber != null && unitCodeNumber != undefined && unitCodeNumber != "") {
                            unitCodeNumber = unitCodeNumber.toUpperCase()
                            var wcbFeeCode = unitCodeNumber + " - WCB";
                            var feeCodeObject = data.find(el => el.key == unitCodeNumber || el.key == wcbFeeCode);
                            if (feeCodeObject != null && feeCodeObject != undefined) {

                                var label = $("label[for='UnitAmount" + i + "']");
                                var nextElement = label.children("span")[0];
                                if (nextElement != null && nextElement != undefined && nextElement.tagName.toLowerCase() == "span") {
                                    nextElement.remove();
                                }

                                var myFeeCodeAmount = parseFloat(feeCodeObject.value);
                                if (myFeeCodeAmount == 0) {

                                    label.append("<span style='color:red'> *</span>");

                                    //Need to open Unit Amount field
                                    $("#UnitAmount" + i).unbind("focus");

                                    $("#UnitAmount" + i).addClass("manualentry");

                                    $("#UnitAmount" + i).change(function () {
                                        var myEnterAmount = parseFloat($(this).val());
                                        if (isNaN(myEnterAmount)) {
                                            myEnterAmount = 0;
                                        }

                                        $(this).val(TrueRound(myEnterAmount, 2).toFixed(2));

                                        var id = $(this).attr("id");

                                        CalPremiumAmount(id.substr(id.length - 1), $(this).val());
                                        CalSpecialTotal();
                                        CalTotalAmount();
                                        NeedToShowDischargeDate();
                                    });

                                    $("#UnitNumber" + i).focus(function () {
                                        $(this).blur();
                                    });

                                } else {
                                    $("#UnitAmount" + i).unbind("change");

                                    $("#UnitAmount" + i).removeClass("manualentry");

                                    $("#UnitAmount" + i).focus(function () {
                                        $(this).blur();
                                    });

                                    $("#UnitNumber" + i).unbind("focus");
                                }

                                if ($(".manualentry").length != null && $(".manualentry").length != undefined) {
                                    var commentLabel = $("label[for='CommentLabel']");
                                    var nextElement = commentLabel.children("span")[0];
                                    if ($(".manualentry").length == 0) {
                                        if (nextElement != null && nextElement != undefined) {
                                            nextElement.remove();
                                        }
                                    } else {
                                        if (nextElement == null || nextElement == undefined || nextElement.tagName.toLowerCase() != "span") {
                                            commentLabel.append("<span style='color:red'> *</span>");
                                        }
                                    }
                                }

                                var myUnitTotalAmount = 0;
                                var myAmountCtrl = GetAmountControl(i);
                                var myUnitNumber = parseInt(Trim($("#UnitNumber" + i).val()));

                                if (myFeeCodeAmount == 0) {
                                    myUnitTotalAmount = parseFloat(myAmountCtrl.val());
                                } else {
                                    myUnitTotalAmount = myFeeCodeAmount * myUnitNumber;
                                }

                                if (myUnitTotalAmount > 9999.99) {
                                    myUnitTotalAmount = 9999.99;
                                }

                                if (!_isReadOnly) {
                                    myAmountCtrl.val(TrueRound(myUnitTotalAmount, 2).toFixed(2));
                                    CalPremiumAmount(i, myUnitTotalAmount);
                                } else if (_isReadOnly) {
                                    myUnitTotalAmount = parseFloat(myAmountCtrl.val());
                                    myAmountCtrl.val(TrueRound(myUnitTotalAmount, 2).toFixed(2));
                                    CalPremiumAmount(i, myUnitTotalAmount);
                                }

                                var unitStartTimeLabel = RemoveLabelRequiredElement("UnitStartTime", i);
                                var unitEndTimeLabel = RemoveLabelRequiredElement("UnitEndTime", i);

                                if (feeCodeObject.requiredUnitTime) {
                                    unitStartTimeLabel.append("<span style='color:red'> *</span>");
                                    unitEndTimeLabel.append("<span style='color:red'> *</span>");
                                }

                                if (feeCodeObject.requiredReferDoc) {
                                    $("#UnitCode" + i).addClass("refdocrequired");
                                }

                                var feeCodeDescriptionToolTip = unitCodeCtrl.parent().find('.feeCodeLabel');
                                var feeCodeDescription = "No description available.";
                                var unitTimeRequired = false;
                                if (feeCodeObject != null && feeCodeObject != undefined) {
                                    var feeCodeAndDescription = feeCodeObject.label;
                                    if (feeCodeAndDescription.indexOf(" - WCB") == -1) {
                                        var separatorIndex = feeCodeAndDescription.indexOf(" - ");
                                        feeCodeDescription = feeCodeAndDescription.substr(separatorIndex +3);
                                    }
                                }

                                feeCodeDescriptionToolTip.attr("title", feeCodeDescription);
                                feeCodeDescriptionToolTip.tooltip('fixTitle');
                            }
                        }
                    }

                    InitialRun();
                }
            },
            error: function (msg) {
                alert(msg.status + " " + msg.statusText);
            }
        });
    } else {
        $.ajax({
            async: true,
            type: "GET",
            cache: true,
            url: _applicationBaseUrl + '/ServiceCode/GetUnitCodeList',
            data: "unitcodelist=" + $("#MostUsedCodes").val() + "," + _codeNeedCalculatedUnit.join(",") + "," + _nextFeeCodes.join(",") + "," + _codeNeedCalculatedUnit3.join(","),
            success: function (data) {
                if (data != undefined && data.length > 0) {
                    $.map(data, function (item) {
                        feeCodeList.push(item);
                    })
                }

                InitialRun();
            },
            error: function (msg) {
                alert(msg.status + " " + msg.statusText);
            }
        });
    }

    if (isReadOnly) {        
        $(":input").attr("disabled", "disabled");         

        $(".cancelButton").removeAttr("disabled");

        $(".paidResubmitButton").removeAttr("disabled");

        $("input[name='__RequestVerificationToken']").removeAttr("disabled");
        
        $("#Record_ServiceRecordId").removeAttr("disabled");
        $("#Record_ClaimStatus").removeAttr("disabled");

        $("#ButtonUsedToSubmit").removeAttr("disabled");

        $(".paidAmountToolTip").show();

        $(".paidAmountToolTip").tooltip({ html: false, placement: "top" });
    }
}

function InitialRun() {
    initialRunFlag = true;

    //ServiceTime_Changed();
    
    CalTotalAmount();
    CalSpecialTotal();
    NeedToShowDischargeDate();
    SetRefDocRequiredOrNot();

    initialRunFlag = false;
}

function PopulateBelowDiagCode(currentIndex, diagCode, diagCodeLabel)
{
    for (var i = currentIndex; i < 8; i++) {
        $("#DiagCode" + i).val(diagCode);

        var diagCodeDescription = "No description available.";

        var diagCodeDescriptionToolTip = $("#DiagCode" + i).parent().find('.diagCodeLabel');

        var indexOf = diagCodeLabel.indexOf(" - ");
        if (diagCodeDescriptionToolTip != null && indexOf > 0) {
            diagCodeDescription = diagCodeLabel.substr(indexOf + 3);
        }

        diagCodeDescriptionToolTip.attr("title", diagCodeDescription);
        diagCodeDescriptionToolTip.tooltip('fixTitle');
    }
}

function RunReferringDocNameRetrieval()
{
    if ($("#Record_ReferringDoctorNumber").val() != "" && $("#Record_ReferringDoctorNumber").val() != undefined) { 
        var code = $("#Record_ReferringDoctorNumber").val().toLowerCase();
        $.ajax({
            async: true,
            url: _applicationBaseUrl + '/ServiceCode/GetRefDocCodeList',
            data: "prefix=" + $("#Record_ReferringDoctorNumber").val(),
            cache: true,
            success: function (data) {
                if (data != null && data != undefined && data.length > 0) {
                    for (var i = 0; i < data.length; i++) {
                        var item = data[i];
                        if (item != null && item != undefined && item.Key.toLowerCase() == code) {
                            $("#DoctorName").val(item.Value);
                            return;
                        }
                    }
                }
            }
        });
    }    
}

function ValidationHSN() {
    var provValue = $("#Record_Province").val();
    var IsValid = false;

    var checkHealthNumber = Trim($("#Record_HospitalNumber").val());

    if (provValue == "SK") {
        if (IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 9) {
            var healthNumber = checkHealthNumber.substring(0, 8);
            var myNumber = parseInt(healthNumber)

            if (!isNaN(myNumber)) {
                var myRemainder = GetModulus11Remainder(healthNumber);
                if (myRemainder > 0)
                    myRemainder = 11 - myRemainder;

                //Append Check Digit
                healthNumber = healthNumber + myRemainder;
                myRemainder = GetModulus11Remainder(healthNumber);

                if (myRemainder == 0) {
                    if (healthNumber == checkHealthNumber) {
                        IsValid = true;
                    }
                }
            }
        }
    } else if (provValue == "BC" || provValue == "ON" || provValue == "NS") {
        if (IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 10) {
            IsValid = true;
        }
    } else if (provValue == "AB" || provValue == "MB" || provValue == "NB" || provValue == "YT") {
        if (IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 9) {
            IsValid = true;
        }
    } else if (provValue == "PE") {
        if (IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 8) {
            IsValid = true;
        }
    } else if (provValue == "NL") {
        if (IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 12) {
            IsValid = true;
        }
    } else if (provValue == "NT") {
        var leadChar = checkHealthNumber.substring(0, 1);
        var restHealthNumber = checkHealthNumber.substr(1);
        if ((leadChar == "D" || leadChar == "H" || leadChar == "M" || leadChar == "N" || leadChar == "T") && IsWholeNumber(restHealthNumber) && checkHealthNumber.length == 8) {
            IsValid = true;
        }
    } else if (provValue == "NU") {
        var leadChar = checkHealthNumber.substring(0, 1);
        var restHealthNumber = checkHealthNumber.substr(1);
        if (leadChar == "1" && IsWholeNumber(checkHealthNumber) && checkHealthNumber.length == 9) {
            IsValid = true;
        }
    } else if (provValue == "QC") {
        IsValid = false;
    }

    if (!IsValid)
        $("#ErrorHSN").toggle(true);
    else
        $("#ErrorHSN").toggle(false);
}

function GetIndex(value) {
    return value.substr(value.length - 1);
}

function DisableEnterKey(e) {
    var key;
    if (window.event)
        key = window.event.keyCode; //IE
    else
        key = e.which; //firefox      

    return (key != 13);
}

function ItemSelected(source, item) {
    var index = GetIndex($(source).attr('id'));

    var findItem = GetFeeCodeObject(item.key);
    if (findItem == null) {
        feeCodeList.push(item);
    }

    ProcessCode(index);

    return;
}

function RemoveLabelRequiredElement(forTag, index) {
    var label = $("label[for='" + forTag + index + "']");
    var nextElement = label.children("span")[0];
    if (nextElement != null && nextElement != undefined && nextElement.tagName.toLowerCase() == "span") {
        nextElement.remove();
    }

    return label;
}

function GetNextFeeCode(myControlId, myCurrentFeeCode) {
    var myControlIndex = parseInt(myControlId);
    myCurrentFeeCode = myCurrentFeeCode.toUpperCase();

    if (!isNaN(myControlIndex)) {
        if (myControlIndex < 8) {
            myControlIndex++;

            var myFeeCtrl = GetFeeControl(myControlIndex);
            var myUnitNumbeCtrl = GetUnitControl(myControlIndex);
            var myNextCode = "";

            if (myCurrentFeeCode == "500H") {
                myNextCode = "501H";
            }
            else if (myCurrentFeeCode == "502H") {
                myNextCode = "503H";
            }
            else if (myCurrentFeeCode == "504H") {
                myNextCode = "505H";
            }
            else if (myCurrentFeeCode == "506H") {
                myNextCode = "507H";
            }

            if (myNextCode.length > 0) {
                if (!IsWholeNumber(myUnitNumbeCtrl.val())) {
                    var calculatedUnit = $("#TotalUnits").val();
                    if (calculatedUnit != null && calculatedUnit != undefined && calculatedUnit != "" && calculatedUnit != "0") {
                        myUnitNumbeCtrl.val(calculatedUnit);
                        if (calculatedUnit > 1) {
                            var myStartTime = $("#ServiceStartTimeString").val();
                            var myEndTime = $("#ServiceEndTimeString").val();
                            $("#UnitStartTime" + myControlIndex).val(myStartTime);
                            $("#UnitEndTime" + myControlIndex).val(myEndTime);
                        }

                    } else {
                        myUnitNumbeCtrl.val(1);
                    }
                }

                myFeeCtrl.val(myNextCode);
                ProcessCode(myControlIndex);
            }
        }
    }

}

function NeedToShowDischargeDate() {
    var myCode = "";
    for (i = 1; i < 8; i++) {
        myCode = $("#UnitCode" + i).val();
        if (myCode != null && myCode != "" && myCareCodeList != null && myCareCodeList != undefined && myCareCodeList.length != 0) {
            if (myCareCodeList.includes(myCode.toUpperCase())) {
                $("#DischargeDateDiv").toggle(true);
                return;
            }
        }
    }

    $("#DischargeDateDiv").toggle(false);
}

function CalTotalAmount() {
    var myAmount;
    var myPremiumAmount;
    var myTotalAmount = 0;
    var i;

    for (i = 1; i < 8; i++) {

        myAmount = parseFloat($("#UnitAmount" + i).val());
        myPremiumAmount = parseFloat($("#UnitPremiumFeeCode" + i).val());

        if (!isNaN(myAmount)) {
            myTotalAmount += myAmount;
        }

        if (!isNaN(myPremiumAmount)) {
            myTotalAmount += myPremiumAmount;
        }
    }

    if (myTotalAmount > 99999.99)
        myTotalAmount = 99999.99;

    $("#TotalAmount").val(TrueRound(myTotalAmount, 2).toFixed(2));
}

function SetRefDocRequiredOrNot() {
    RemoveLabelRequiredElement("Record_ReferringDoctorNumber", "");
    RemoveLabelRequiredElement("DoctorName", "");
   
    if ($(".refdocrequired").length > 0) {
        $("label[for='Record_ReferringDoctorNumber']").append("<span style='color:red'> *</span>");
        $("label[for='DoctorName']").append("<span style='color:red'> *</span>");
    }
}

function CalPremiumAmount(index, myAmount) {
    var myPremiumCtrl = GetPremiumFeeControl(index);
    var myFeeCodeCtrl = GetFeeControl(index);
    var myPremiumCodeCtrl = GetUnitPremiumControl(index);
    var myPremiumCode = myPremiumCodeCtrl.val();
    var myServiceLocationCode = $("#Record_ServiceLocation").val().toUpperCase();

    var myRate = 0.0;

    myPremiumCode = myPremiumCode.toLowerCase();

    var myUnitCode = myFeeCodeCtrl.val().toUpperCase();

    if (myCareCodeList.includes(myUnitCode)) {
        myRate = 0.0;
    } else {
        if (!myPremiumCodeList.includes(myUnitCode)) {
            if (myPremiumCode == "k")
                myRate = 1;
            else if (myPremiumCode == "b")
                myRate = 0.5;
            else if (myPremiumCode == "f")
                myRate = 0.1;
        }               
    }

    if (myRNPExcludeCodeList.includes(myUnitCode)) {
        myRate += 0.0;
    } else if (myServiceLocationCode == 'X') {
        myRate += 0.15;
    }
    
    myAmount = myAmount * myRate;

    myPremiumCtrl.val(TrueRound(myAmount, 2).toFixed(2));
}

function TrueRound(value, digits) {
    return ((Math.round((value * Math.pow(10, digits)).toFixed(digits - 1)) / Math.pow(10, digits)).toFixed(digits)) * 1;
}

function GetUnitControl(index) {
    return $("#UnitNumber" + index);
}

function GetAmountControl(index) {
    return $("#UnitAmount" + index);
}

function GetPremiumFeeControl(index) {
    return $("#UnitPremiumFeeCode" + index);
}

function GetFeeControl(index) {
    return $("#UnitCode" + index);
}

function GetStartTimeControl(index) {
    return $("#UnitStartTime" + index);

}

function GetEndTimeControl(index) {
    return $("#UnitEndTime" + index);
}

function GetUnitPremiumControl(index) {
    return $("#UnitPremiumCode" + index);
}

function GetSpecialCircumstanceControl(index) {
    return $("#SpecialCircumstanceIndicator" + index);
}

function GetRecordClaimTypeControl(index) {
    return $("#RecordClaimType" + index);
}

function CheckSpecialCircumstanceWithUnitCode(index) {
    var mySpecCirCtrl = GetSpecialCircumstanceControl(index);
    var myFeeCodeCtrl = GetFeeControl(index);
    var myFeeCode = Trim(myFeeCodeCtrl.val().toUpperCase());
    var feeCodeObject = GetFeeCodeObject(myFeeCode);

    var feeDeterminant = "";
    if (feeCodeObject != null && feeCodeObject != undefined) {
        feeDeterminant = feeCodeObject.feeDeterminant
    }

    if (mySpecCirCtrl.val() != "" && mySpecCirCtrl.val() != undefined && mySpecCirCtrl.val() != "TA" && feeDeterminant != "W" && feeDeterminant != "X") {      
        mySpecCirCtrl.css("border", "1px solid red");
    } else {
        mySpecCirCtrl.css("border", "");
    }
}

function CascadeRecordClaimType(index, changeType) {
    var myRecordClaimTypeCtrl = GetRecordClaimTypeControl(index);
    var myRecordClaimType = myRecordClaimTypeCtrl.val();

    if (changeType == "RecordClaimTypeChange") {
        for (i = 1; i < 8; i++) {
            if (i != index) {
                var myFeeCodeCtrl = GetFeeControl(i);
                var myFeeCode = Trim(myFeeCodeCtrl.val().toUpperCase());

                if (myFeeCode != null && myFeeCode != "") {
                    var targetRecordClaimType = GetRecordClaimTypeControl(i);
                    targetRecordClaimType.val(myRecordClaimType);
                }
            }
        }
    } else if (changeType == "UsePreviousLine") {
        var previousIndex = index - 1;
        if (previousIndex > 0) {
            var myPreviousRecordClaimType = GetRecordClaimTypeControl(previousIndex).val();
            myRecordClaimTypeCtrl.val(myPreviousRecordClaimType);
        }
    }
}

function ProcessCode(index) {
    var myFeeCodeCtrl = GetFeeControl(index);
    var myFeeCode = Trim(myFeeCodeCtrl.val().toUpperCase());
    myFeeCodeCtrl.val(myFeeCode);

    var feeCodeObject = GetFeeCodeObject(myFeeCode);

    var myAmountCtrl = GetAmountControl(index);
    var myPremiumFee = GetPremiumFeeControl(index);

    var myUnitNumbeCtrl = GetUnitControl(index);
    var myUnitNumber = parseInt(Trim(myUnitNumbeCtrl.val()));
    
    var myStartTimeControl = GetStartTimeControl(index);
    var myEndTimeControl = GetEndTimeControl(index);
    var myPremiumCode = GetUnitPremiumControl(index);

    var myRecordClaimType = GetRecordClaimTypeControl(index);

    var feeCodeDescriptionToolTip = myFeeCodeCtrl.parent().find('.feeCodeLabel');
    var feeCodeDescription = "No description available.";

    var unitTimeRequired = false;
    var refDocRequired = false;

    if (feeCodeObject != null && feeCodeObject != undefined) {
        var feeCodeAndDescription = feeCodeObject.label;
        if (feeCodeAndDescription.indexOf(" - WCB") == -1) {
            var separatorIndex = feeCodeAndDescription.indexOf(" - ");
            feeCodeDescription = feeCodeAndDescription.substr(separatorIndex + 3);
        }

        unitTimeRequired = feeCodeObject.requiredUnitTime;
        refDocRequired = feeCodeObject.requiredReferDoc;
    }

    feeCodeDescriptionToolTip.attr("title", feeCodeDescription);
    feeCodeDescriptionToolTip.tooltip('fixTitle');

    if (myFeeCode.length >= 2 && feeCodeObject != null) {

        if ($("#PremiumCode").val() != "" && $("#PremiumCode").val() != undefined && myPremiumCode.val() == "" || myPremiumCode.val() == undefined) {
            var isWeekend = false;
            var jsDate = $('#ServiceDateString').datepicker('getDate');
            if (jsDate !== null) { // if any date selected in datepicker
                jsDate instanceof Date; // -> true
                var dayOfWeek = jsDate.getDay();
                isWeekend = (dayOfWeek === 6) || (dayOfWeek === 0);
            }

            if (isWeekend && !myPremiumCodeList.includes(myFeeCode)) {
                myPremiumCode.val("B");
            } else {
                myPremiumCode.val($("#PremiumCode").val());
            }
        }

        var unitStartTimeLabel = RemoveLabelRequiredElement("UnitStartTime", index);
        var unitEndTimeLabel = RemoveLabelRequiredElement("UnitEndTime", index);
        myFeeCodeCtrl.removeClass("refdocrequired");
        
        if (ValidateSpecialCode(index)) {
            if (feeCodeObject != undefined) {                
                var specialCodeItem = GetSpecialCode(myFeeCode);
                if (specialCodeItem != null) {
                    myUnitNumbeCtrl.val(1);
                    myUnitNumbeCtrl.attr("disabled", "disabled");
                } else {
                    if (!_isReadOnly) {                    
                        myUnitNumbeCtrl.removeAttr("disabled");
                    }

                    var myFeeCodeAmount = parseFloat(feeCodeObject.value);

                    if (!isNaN(myFeeCodeAmount)) {
                        if (_codeNeedCalculatedUnit.includes(myFeeCode) || _nextFeeCodes.includes(myFeeCode) || _codeNeedCalculatedUnit3.includes(myFeeCode)) {
                            if (_nextFeeCodes.includes(myFeeCode)) {
                                var myPreviousStartUpResult = GetPreviousStartUpLine(index, myFeeCode); //Start up line - Previous
                                if (myPreviousStartUpResult.PreviousLineIndex > -1) {
                                    var myPreviousStartUpPremCtrl = GetUnitPremiumControl(myPreviousStartUpResult.PreviousLineIndex);
                                    myPreviousStartUpPremCtrl.val(myPremiumCode.val());

                                    var myPreviousUnitAmount = GetAmountControl(myPreviousStartUpResult.PreviousLineIndex);
                                    CalPremiumAmount(myPreviousStartUpResult.PreviousLineIndex, myPreviousUnitAmount.val());
                                }
                            }

                            var myPreviousLineResult = GetPreviousHLines(index); //here
                            if (myPreviousLineResult.PreviousLineIndex > -1) {
                                var myPreviousLineIndex = myPreviousLineResult.PreviousLineIndex;
                                var myPreviousStartTime = GetStartTimeControl(myPreviousLineIndex).val();
                                var myPreviousEndTime = GetEndTimeControl(myPreviousLineIndex).val();
                                if (myPreviousStartTime.length == 4 && myPreviousEndTime.length == 4) {
                                    var isTimeSpanOverNextDay = IsTimeSpanOverNextDay(myPreviousStartTime, myPreviousEndTime);

                                    if (myFeeCode == "540H") {
                                        if (isTimeSpanOverNextDay) {
                                            if (!IsSourceGreaterThanTarget(myPreviousStartTime, "1700")) {
                                                var myUnitResult = GetServiceTimeUnint("1700", "0000");
                                                if (myUnitResult.UnitNumber > 0) {
                                                    myUnitNumber = myUnitResult.UnitNumber;
                                                    myUnitNumbeCtrl.val(myUnitNumber);

                                                    myStartTimeControl.val("1700");
                                                    myEndTimeControl.val("0000");
                                                }
                                            }
                                        } else { //Check to see if it is before 1700 and end in the next morning
                                            if (!IsSourceGreaterThanTarget(myPreviousStartTime, "1700") && IsSourceGreaterThanTarget(myPreviousStartTime, "0700")) {
                                                var myUnitResult = GetServiceTimeUnint("1700", myPreviousEndTime);
                                                if (myUnitResult.UnitNumber > 0) {
                                                    myUnitNumber = myUnitResult.UnitNumber;
                                                    myUnitNumbeCtrl.val(myUnitNumber);

                                                    myStartTimeControl.val("1700");
                                                    myEndTimeControl.val(myPreviousEndTime);
                                                }
                                            }
                                        }
                                    } else if (myFeeCode == "545H") {
                                        if (isTimeSpanOverNextDay) {
                                            var myUnitResult = GetServiceTimeUnint("0000", myPreviousEndTime);
                                            if (myUnitResult.UnitNumber > 0) {
                                                myUnitNumber = myUnitResult.UnitNumber;
                                                myUnitNumbeCtrl.val(myUnitNumber);

                                                myStartTimeControl.val("0000");
                                                myEndTimeControl.val(myPreviousEndTime);
                                            }
                                        }
                                    } else if (myFeeCode != myPreviousLineResult.PreviousFeeCode) {
                                        myStartTimeControl.val(myPreviousStartTime);
                                        myEndTimeControl.val(myPreviousEndTime);

                                        myUnitNumber = GetUnitControl(myPreviousLineIndex).val();
                                        myUnitNumbeCtrl.val(myUnitNumber);
                                    }
                                }
                            } else {
                                if (!myCareCodeList.includes(myFeeCode) && !myPremiumCodeList.includes(myFeeCode)) {
                                    var myPreviousLinePremiumCode = GetPreviousLinePremiumCode(index);
                                    if (myPreviousLinePremiumCode != "") {
                                        var myPreviousStartTime = GetStartTimeControl(index).val();
                                        var myPreviousEndTime = GetEndTimeControl(index).val();
                                        if (myPreviousStartTime.length < 4 || myPreviousEndTime.length < 4) {
                                            myPremiumCode.val(myPreviousLinePremiumCode);
                                        }
                                    }
                                }
                            }
                        } else {
                            if (!myCareCodeList.includes(myFeeCode) && !myPremiumCodeList.includes(myFeeCode)) {
                                var myPreviousLinePremiumCode = GetPreviousLinePremiumCode(index);
                                if (myPreviousLinePremiumCode != "") {
                                    var myPreviousStartTime = GetStartTimeControl(index).val();
                                    var myPreviousEndTime = GetEndTimeControl(index).val();
                                    if (myPreviousStartTime.length < 4 || myPreviousEndTime.length < 4) {
                                        myPremiumCode.val(myPreviousLinePremiumCode);
                                    }
                                }
                            }
                        }

                        if (isNaN(myUnitNumber) || myUnitNumber == 0 || _startupCodes.includes(myFeeCode)) {
                            myUnitNumber = 1;
                            myUnitNumbeCtrl.val(1);
                        }

                        var unitAmountLabel = RemoveLabelRequiredElement("UnitAmount", index);

                        if (myFeeCodeAmount == 0) {
                            unitAmountLabel.append("<span style='color:red'> *</span>");

                            //Need to open Unit Amount field
                            myAmountCtrl.unbind("focus");

                            myAmountCtrl.addClass("manualentry");

                            myAmountCtrl.change(function () {
                                console.log("myAmountCtrl On Change");

                                var id = $(this).attr("id");
                                var index = id.substr(id.length - 1);

                                var myEnterAmount = parseFloat($(this).val());
                                if (isNaN(myEnterAmount)) {
                                    myEnterAmount = 0;
                                }

                                $(this).val(TrueRound(myEnterAmount, 2).toFixed(2));

                                CalPremiumAmount(index, $(this).val());
                                CalSpecialTotal();
                                CalTotalAmount();
                                NeedToShowDischargeDate();
                            });

                            myUnitNumbeCtrl.focus(function () {
                                $(this).blur();
                            });
                        } else {
                            myAmountCtrl.unbind("change");

                            myAmountCtrl.removeClass("manualentry");

                            myAmountCtrl.focus(function () {
                                $(this).blur();
                            });

                            myUnitNumbeCtrl.unbind("focus");
                        }

                        if ($(".manualentry").length != null && $(".manualentry").length != undefined) {
                            var commentLabel = $("label[for='CommentLabel']");
                            var nextElement = commentLabel.children("span")[0];
                            if ($(".manualentry").length == 0) {
                                if (nextElement != null && nextElement != undefined) {
                                    nextElement.remove();
                                }
                            } else {
                                if (nextElement == null || nextElement == undefined || nextElement.tagName.toLowerCase() != "span") {
                                    commentLabel.append("<span style='color:red'> *</span>");
                                }
                            }
                        }

                        var myUnitTotalAmount = 0;

                        if (myFeeCodeAmount == 0) {
                            myUnitTotalAmount = parseFloat(myFeeCodeAmount);
                        } else {
                            myUnitTotalAmount = myFeeCodeAmount * myUnitNumber;
                        }

                        if (myUnitTotalAmount > 9999.99) {
                            myUnitTotalAmount = 9999.99;
                        }

                        if (!_isReadOnly) {
                            myAmountCtrl.val(TrueRound(myUnitTotalAmount, 2).toFixed(2));
                            CalPremiumAmount(index, myUnitTotalAmount);
                        } else if (_isReadOnly) {
                            myUnitTotalAmount = parseFloat(myAmountCtrl.val());
                            myAmountCtrl.val(TrueRound(myUnitTotalAmount, 2).toFixed(2));
                            CalPremiumAmount(index, myUnitTotalAmount);
                        }

                        if (specialCodeItem == null && !initialRunFlag) {
                            GetNextFeeCode(index, feeCodeObject.key);
                        }

                        if (unitTimeRequired) {
                            unitStartTimeLabel.append("<span style='color:red'> *</span>");
                            unitEndTimeLabel.append("<span style='color:red'> *</span>");
                        }

                        if (refDocRequired) {
                            myFeeCodeCtrl.addClass("refdocrequired");
                        }

                    } else {
                        if (isNaN(myUnitNumber)) {
                            myUnitNumbeCtrl.val(0);
                        }

                        myAmountCtrl.val(0);
                        myPremiumFee.val(0);
                    }
                }

                CheckSpecialCircumstanceWithUnitCode(index);
                CascadeRecordClaimType(index, "UsePreviousLine");
            } else {
                if (isNaN(myUnitNumber)) {
                    myUnitNumbeCtrl.val(0);
                }

                myAmountCtrl.val(0);
                myPremiumFee.val(0);

                myRecordClaimType.val("");
            }
        }
    } else if (myFeeCode.length == 0) {
        ClearFields(index);
    }

    CalSpecialTotal();
    CalTotalAmount();
    NeedToShowDischargeDate();    
    SetRefDocRequiredOrNot();
}

function GetFeeCodeAmount(feeCode) {
    var obj = GetFeeCodeObject(feeCode);
    if (obj != undefined) {    
        return obj.value + "";
    }

    return "";
}

function GetFeeCodeObject(feeCode) {
    feeCode = feeCode.toUpperCase();
    var wcbFeeCode = feeCode + " - WCB";
    return feeCodeList.find(el => el.key == feeCode || el.key == wcbFeeCode);
}

function GetPreviousStartUpLine(currentIndex, myCurrentfeeCode) {
    var myPreviousCode = "";
    if (myCurrentfeeCode == "501H") {
        myPreviousCode = "500H";
    }
    else if (myCurrentfeeCode == "503H") {
        myPreviousCode = "502H";
    }
    else if (myCurrentfeeCode == "505H") {
        myPreviousCode = "504H";
    }
    else if (myCurrentfeeCode == "507H") {
        myPreviousCode = "506H";
    }
    
    var myResult = { PreviousLineIndex: -1, PreviousFeeCode: "" };

    for (var i = currentIndex - 1; i > 0; i--) {
        var myUnitCodeCtrl = GetFeeControl(i);
        if (myUnitCodeCtrl != null && myUnitCodeCtrl != undefined) {
            var myUnitCode = myUnitCodeCtrl.val();
            if (myUnitCode != null && myUnitCode != undefined && myUnitCode != "") {
                if (myPreviousCode == myUnitCode.toUpperCase()) {
                    myResult.PreviousLineIndex = i;
                    myResult.PreviousFeeCode = myUnitCode;
                    return myResult;
                }
            }
        }
    }

    return myResult;
}

function GetPreviousHLines(currentIndex) {
    var myResult = { PreviousLineIndex: -1, PreviousFeeCode: "" };

    for (var i = 0; i < currentIndex; i++) {
        var myUnitCodeCtrl = GetFeeControl(i);
        if (myUnitCodeCtrl != null && myUnitCodeCtrl != undefined) {
            var myUnitCode = myUnitCodeCtrl.val();
            if (myUnitCode != null && myUnitCode != undefined && myUnitCode != "") {
                if (_nextFeeCodes.includes(myUnitCode.toUpperCase())) {
                    myResult.PreviousLineIndex = i;
                    myResult.PreviousFeeCode = myUnitCode;
                    return myResult;
                }
            }
        }
    }

    return myResult;
}

function GetPreviousLinePremiumCode(currentIndex) {
    for (var i = currentIndex - 1; i > 0; i--) {
        var myUnitCodeCtrl = GetFeeControl(i);
        if (myUnitCodeCtrl != null && myUnitCodeCtrl != undefined) {
            var myUnitCode = myUnitCodeCtrl.val();
            if (myUnitCode != null && myUnitCode != undefined && myUnitCode != "") {
                if (!myCareCodeList.includes(myUnitCode) && !myPremiumCodeList.includes(myUnitCode)) { //Not Care Code and Premium Code
                    var unitPremCodeCtrl = GetUnitPremiumControl(i);
                    if (unitPremCodeCtrl != null && unitPremCodeCtrl != undefined) {
                        var unitPremCode = unitPremCodeCtrl.val();
                        if (unitPremCode != null && unitPremCode != undefined && unitPremCode != "") {
                            return unitPremCode;
                        }
                    }
                }
            }
        }
    }
        
    return "";
}

function UnitTime_Changed(index) {   
    ValidateUnitTime("UnitStartTime" + index, true);
    ValidateUnitTime("UnitEndTime" + index, true);
    var startTimeUnit = $("#UnitStartTime" + index).val();
    var endTimeUnit = $("#UnitEndTime" + index).val();

    var myUnitResult = GetServiceTimeUnint(startTimeUnit, endTimeUnit, GetFeeControl(index).val());

    if (!myUnitResult.EmptyValue) {
        if (myUnitResult.UnitNumber == 0) {
            myUnitResult.UnitNumber = 1;
        }

        var unitNumberToSet = myUnitResult.UnitNumber;
        $("#UnitPremiumCode" + index).val(myUnitResult.PremiumCode);
        
        var feeCode = GetFeeControl(index).val();        
        if (feeCode != "" && feeCode != null && feeCode != undefined) {
            if (_startupCodes.includes(feeCode)) {
                unitNumberToSet = 1;
                $("#UnitStartTime" + index).val("");
                $("#UnitEndTime" + index).val("");                
            } else {
                var feeControlObject = GetFeeCodeObject(feeCode);
                if (feeControlObject != null && feeControlObject != undefined && parseFloat(feeControlObject.value) == 0) {
                    unitNumberToSet = 1;
                }
            }
        }

        $("#UnitNumber" + index).val(unitNumberToSet);

        ProcessCode(index);

        if (_startupCodes.concat(feeCode)) {
            var myNextCode = "";

            if (feeCode == "500H") {
                myNextCode = "501H";
            }
            else if (feeCode == "502H") {
                myNextCode = "503H";
            }
            else if (feeCode == "504H") {
                myNextCode = "505H";
            }
            else if (feeCode == "506H") {
                myNextCode = "507H";
            }
            
            if (parseInt(index) < 7) {
                for (var i = parseInt(index) + 1; i < 8; i++) {
                    var myUnitCodeCtrl = GetFeeControl(i);
                    if (myUnitCodeCtrl != null && myUnitCodeCtrl != undefined) {
                        var myUnitCode = myUnitCodeCtrl.val();
                        if (myUnitCode != null && myUnitCode != undefined && myUnitCode != "") {
                            if (myUnitCode.toUpperCase() == myNextCode) {
                                $("#UnitNumber" + i).val(myUnitResult.UnitNumber);
                                $("#UnitStartTime" + i).val(startTimeUnit);
                                $("#UnitEndTime" + i).val(endTimeUnit);
                                $("#UnitPremiumCode" + i).val(myUnitResult.PremiumCode);

                                ProcessCode(i);

                                break;
                            }
                        }
                    }
                }
            }
        }

    }
}

function IsTimeSpanOverNextDay(myStartTime, myEndTime)
{
    var spanOverNextDay = false;
    if (myStartTime.length == 4 && myEndTime.length == 4) {
        var myStartTimeValue = parseInt(myStartTime);
        var myEndTimeValue = parseInt(myEndTime);

        if (!isNaN(myStartTimeValue) && !isNaN(myEndTimeValue) && myStartTimeValue < 2400 && myEndTimeValue < 2400) {
            var myStartHour = myStartTime.substring(0, 2);
            var myStartMin = myStartTime.substring(2);
            var myEndHour = myEndTime.substring(0, 2);
            var myEndMin = myEndTime.substring(2);

            if (!isNaN(myStartHour) && !isNaN(myStartMin) && !isNaN(myEndHour) && !isNaN(myEndMin)) {
                if (myStartHour >= 0 && myStartHour < 24 &&
                    myEndHour >= 0 && myEndHour < 24 &&
                    myStartMin >= 0 && myStartMin < 60 &&
                    myEndMin >= 0 && myEndMin < 60) {
                    var myServiceTime = new Date();
                    var myStartDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myStartHour, myStartMin, 0, 0);
                    var myEndDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myEndHour, myEndMin, 0, 0);

                    if (myStartDate > myEndDate) { //This indicate the Start Time is in the previous date
                        spanOverNextDay = true;
                    }
                }
            }
        }
    }

    return spanOverNextDay;
}

function IsSourceGreaterThanTarget(myStartTime, myEndTime) { //myStartTime is the source, myEndTime is the target
    var sourceGreater = false;
    if (myStartTime.length == 4 && myEndTime.length == 4) {
        var myStartTimeValue = parseInt(myStartTime);
        var myEndTimeValue = parseInt(myEndTime);

        if (!isNaN(myStartTimeValue) && !isNaN(myEndTimeValue) && myStartTimeValue < 2400 && myEndTimeValue < 2400) {
            var myStartHour = myStartTime.substring(0, 2);
            var myStartMin = myStartTime.substring(2);
            var myEndHour = myEndTime.substring(0, 2);
            var myEndMin = myEndTime.substring(2);

            if (!isNaN(myStartHour) && !isNaN(myStartMin) && !isNaN(myEndHour) && !isNaN(myEndMin)) {
                if (myStartHour >= 0 && myStartHour < 24 &&
                    myEndHour >= 0 && myEndHour < 24 &&
                    myStartMin >= 0 && myStartMin < 60 &&
                    myEndMin >= 0 && myEndMin < 60) {
                    var myServiceTime = new Date();
                    var myStartDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myStartHour, myStartMin, 0, 0);
                    var myEndDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myEndHour, myEndMin, 0, 0);

                    sourceGreater = myStartDate >= myEndDate;
                }
            }
        }
    }

    return sourceGreater;
}

function GetServiceTimeUnint(myStartTime, myEndTime, myUnitCode) {
    var myResult = { UnitNumber: 0, PremiumCode: "2", EmptyValue: true };

    var isWeekend = false;
    var jsDate = $('#ServiceDateString').datepicker('getDate');
    if (jsDate !== null) { // if any date selected in datepicker
        jsDate instanceof Date; // -> true
        var dayOfWeek = jsDate.getDay();
        isWeekend = (dayOfWeek === 6) || (dayOfWeek === 0);
    }

    if (myStartTime.length == 4 && myEndTime.length == 4) {
        myResult.EmptyValue = false;
        var myStartTimeValue = parseInt(myStartTime);
        var myEndTimeValue = parseInt(myEndTime);

        if (!isNaN(myStartTimeValue) && !isNaN(myEndTimeValue) && myStartTimeValue < 2400 && myEndTimeValue < 2400) {
            var myStartHour = myStartTime.substring(0, 2);
            var myStartMin = myStartTime.substring(2);
            var myEndHour = myEndTime.substring(0, 2);
            var myEndMin = myEndTime.substring(2);

            if (!isNaN(myStartHour) && !isNaN(myStartMin) && !isNaN(myEndHour) && !isNaN(myEndMin)) {
                if (myStartHour >= 0 && myStartHour < 24 &&
                    myEndHour >= 0 && myEndHour < 24 &&
                    myStartMin >= 0 && myStartMin < 60 &&
                    myEndMin >= 0 && myEndMin < 60) {
                    var myServiceTime = new Date();
                    var myStartDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myStartHour, myStartMin, 0, 0);
                    var myEndDate = new Date(myServiceTime.getFullYear(), myServiceTime.getMonth(), myServiceTime.getDay(), myEndHour, myEndMin, 0, 0);

                    var myInterval = 0;
                    if (myEndDate > myStartDate) {
                        myInterval = (myEndDate - myStartDate) / (1000 * 60);
                    }
                    else if (myEndDate < myStartDate) {
                        myInterval = (24 * 60) - (myStartDate - myEndDate) / (1000 * 60);
                    }
                    else {
                        return;
                    }

                    if (_codeFor60Minutes.includes(myUnitCode)) {
                        myResult.UnitNumber = parseInt(myInterval / 60);
                    } else {
                        myResult.UnitNumber = parseInt(myInterval / 15);
                        var myRemainder = myInterval % 15;
                        if (myRemainder > 0) {
                            if (_codeNeedToIncOneUnitGreaterThan7.includes(myUnitCode) && myRemainder > 7) {
                                myResult.UnitNumber++;                        
                            } else if (!_codeNeedToIncOneUnitGreaterThan7.includes(myUnitCode)) {
                                myResult.UnitNumber++;
                            }
                        }
                    }
                }
               
                if (!myPremiumCodeList.includes(myUnitCode)) {
                    if (myStartHour >= 17 || (isWeekend && myStartHour >= 7)) {
                        myResult.PremiumCode = "B";
                    } else if (myStartHour >= 0 && myStartHour < 7) {
                        myResult.PremiumCode = "K";
                    }
                }
            }
        }
    }

    return myResult;
}

function ValidateUnitTime(myUnitTimeId, isUnit) {
    var ctrl = $("#" + myUnitTimeId);
    if (ctrl != null && ctrl != undefined) {
        if (ctrl.val().length < 4 && ctrl.val().length > 0) {
            if (!isUnit) {
                $("#Error" + myUnitTimeId).toggle(true);
            } else {
                ctrl.css('color', 'red');
            }
        } else {
            if (!isUnit) {
                $("#Error" + myUnitTimeId).toggle(false);
            } else {
                ctrl.css('color', 'black');
            }
        }
    }
}

function GetModulus11Remainder(myValue) {
    var myNumber = 0;
    for (i = 0, j = 9; i < myValue.length; i++, j--)
        myNumber = myNumber + (j * parseInt(myValue.charAt(i)));

    myNumber = myNumber % 11;

    return myNumber;
}

function IsWholeNumber(val) {
    return String(val).search(/^\s*\d+\s*$/) != -1
}

function IsNumeric(input) {
    return (input - 0) == input && input.length > 0;
}

function IsDate(val) {
    return val.search(/^(0[1-9]|[12][0-9]|3[01])\/(0[1-9]|1[012])\/(19|20)\d{2}$/)
}

function Trim(stringToTrim) {
    if (stringToTrim != undefined) {
        return stringToTrim.replace(/^\s+|\s+$/g, "");
    } else {
        return "";
    }
}

function LTrim(stringToTrim) {
    if (stringToTrim != undefined) {
        return stringToTrim.replace(/^\s+/, "");
    } else {
        return "";
    }
}

function RTrim(stringToTrim) {
    if (stringToTrim != undefined) {
        return stringToTrim.replace(/\s+$/, "");
    } else {
        return "";
    }
}

function GetSpecialCode(feeCode) {
    for (var i = 0; i < specialCodeList.length; i++) {
        var item = specialCodeList[i];
        if (item.Code.toUpperCase() == feeCode.toUpperCase()) {
            return item;
        }
    }

    return null;
}

function GetAllTotalAmount(index) {    
    var myTotalAmount = 0;
    var myRate = 0;
    var myUnitCode = $("#UnitCode" + index).val();
    
    var specialCode = GetSpecialCode(myUnitCode);
    if (specialCode != null) {
        myRate = specialCode.Rate;       
        for (var i = 1; i < 8; i++) {
            if (i != index) {
                var unitCode = $("#UnitCode" + i).val().toUpperCase();
                if (!myPremiumCodeList.includes(unitCode) && !IsWCBCode(unitCode)) {
                    var myAmount = parseFloat($("#UnitAmount" + i).val());
                    if (!isNaN(myAmount)) {
                        myTotalAmount += myAmount;
                    }
                }
            }
        }
    }
        
    return myTotalAmount * myRate;
}

function IsWCBCode(unitCode) {
    unitCode = unitCode.toUpperCase();
    var code = wcbFeeCodeList.find(el => el.key == unitCode);    
    return code != undefined;
}

function CalSpecialTotal() {
    for (var i = 1; i < 8; i++) {
        var myUnitCode = $("#UnitCode" + i).val();        
        var specialItem = GetSpecialCode(myUnitCode);
        if (specialItem != null) {
            var myFeeCodeAmount = GetAllTotalAmount(i);

            if (myFeeCodeAmount > 9999.99)
                myFeeCodeAmount = 9999.99;

            $("#UnitAmount" + i).val(TrueRound(myFeeCodeAmount, 2).toFixed(2));
            
            CalPremiumAmount(i, myFeeCodeAmount);
            return;
        }        
    }
}

function ValidateSpecialCode(index) {
    if (!initialRunFlag) {
        var counter = 0;
        for (var i = 1; i < 8; i++) {
            var myUnitCode = $("#UnitCode" + i).val();
            var specialCode = GetSpecialCode(myUnitCode);
            if (specialCode != null) {
                counter++;
            }
        }
        
        if (counter > 1) {
            ClearFields(index);
            alert("You cannot have more than one record that contain 893A or 894A or 895A!");
            return false;
        }
    }

    return true;
}

function ClearFields(index) {
    $("#UnitCode" + index).val("");
    $("#UnitNumber" + index).val("");
    $("#UnitAmount" + index).val("");
    $("#UnitPremiumFeeCode" + index).val("");
    $("#UnitPremiumCode" + index).val("");
    $("#UnitStartTime" + index).val("");
    $("#UnitEndTime" + index).val("");

    $("#SpecialCircumstanceIndicator" + index).val("");

    $("#UnitCode" + index).removeClass("refdocrequired");

    $("#RecordClaimType" + index).val("");
}

function PadLeft(value, length) {
    if (value.length < length) {
        value = String(value);
        length = length - value.length;
        return ('0'.repeat(length) + value)
    } 

    return value;
}

function compare(a, b) {
    if (a.orderKey < b.orderKey) {
        return -1;
    }
    if (a.orderKey > b.orderKey) {
        return 1;
    }
    return 0;
}