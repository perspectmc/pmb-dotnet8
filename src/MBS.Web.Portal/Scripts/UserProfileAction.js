var _iconOkay = "";
var _iconCancel = "";
function UserProfilePageLoad(iconOkay, iconCancel) {
    _iconOkay = iconOkay;
    _iconCancel = iconCancel;

    SetMaxLength("DoctorNumber", 4);
    SetMaxLength("DoctorName", 80);
    SetMaxLength("ClinicNumber", 3);
    SetMaxLength("GroupNumber", 3);
    SetMaxLength("GroupUserKey", 10);

    SetMaxLength("PhoneNumber", 12);
    SetMaxLength("DiagnosticCode", 3);
    SetMaxLength("Street", 50);
    SetMaxLength("City", 50);
    SetMaxLength("PostalCode", 6);

    SetRequiredField("DoctorNumber");
    SetRequiredField("PhoneNumber");
    SetRequiredField("DoctorName");
    SetRequiredField("ClinicNumber");
    SetRequiredField("GroupNumber");

    SetRequiredField("DiagnosticCode");
    SetRequiredField("Street");
    SetRequiredField("City");

    $("label[for='PostalCode']").append(" (S4S9S9)");
    SetRequiredField("PostalCode");

    $("#PostalCode").mask("?a9a9a9");
    $("#PhoneNumber").mask("?999-999-9999");
}

function CheckConnection() {
    var groupNumber = $("#GroupNumber").val();
    var groupUserKey = $("#GroupUserKey").val();

    if (groupNumber.length != 3 || groupUserKey.length < 8) {
        alert("Group Number and Group User Key are not in the correct!");
    } else {
        $.ajax({
            async: true,
            type: "GET",
            data: "groupNumber=" + groupNumber + "&groupUserKey=" + groupUserKey,
            url: _applicationBaseUrl + '/Account/IsMSBGroupValid',
            success: function (data) {
                if (data) {
                    $("#checkresult").attr("src", _iconOkay);
                } else {
                    $("#checkresult").attr("src", _iconCancel);
                }
            },
            error: function (msg) {
                alert(msg);
            }
        });
    }
}