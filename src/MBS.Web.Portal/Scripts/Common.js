function CancelPropagation(event) {
    if (event.stopPropagation) {
        event.stopPropagation();   // W3C model
    } else {
        event.cancelBubble = true; // IE model
    }
}

function ShowBootstrapModal(modal_id, url, isCache) {
    var modal_obj = $("#" + modal_id);
    
    modal_obj.find(".modal-body").empty();
    modal_obj.modal("show");

    $.ajax({
        cache: isCache,
        url: url,
        beforeSend: function (xhr) {
            var imagePath = "<img src='" + _applicationBaseUrl + '/Content/images/icon_loading.gif' + "' />";
            modal_obj.find(".modal-body").append(imagePath);
        },
        success: function (data) {            
            modal_obj.find(".modal-body").empty();
            modal_obj.find(".modal-body").html(data);
        }
    });
}

function ResizeSortable(e, control) {
    control.children().each(function () {
        $(this).width($(this).width());
    });

    return control;
}

function SetRequiredField(fieldName) {
    var label = $("label[for='" + fieldName + "']")
    label.append("<span style='color:red'> *</span>");
}

function SetMaxLength(fieldName, maxLength) {
    $("#" + fieldName).attr("maxlength", maxLength);
}

function MaskPhoneNumberFields(fieldNames) {
    $.each(fieldNames, function (index, item) {
        $("#" + item).mask("999-999-9999");
    });
}

function ToggleStatusAsync(endPointUrl, data, successCallBack, errorCallBack) {
    $.ajax({
        type: "POST",
        url: endPointUrl,
        cache: false,
        data: data,
        async: true,
        success: function (response) {
            if (response.success) {
                successCallBack();
            } else {
                errorCallBack();
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            errorCallBack();
        }
    });
}

function objToString(obj) {
    var str = '';
    for (var p in obj) {
        if (obj.hasOwnProperty(p)) {
            str += p + '::' + obj[p] + '\n';
        }
    }
    return str;
}

function ChangePasswordPageLoad() {
    SetRequiredField("NewPassword");
    SetRequiredField("OldPassword");
    SetRequiredField("ConfirmPassword");

    $("#PasswordRequirement").tooltip({ html: true, placement: 'left' });
}

function UserRegisterPageLoad() {
    SetRequiredField("Username");
    SetRequiredField("Email");
    SetRequiredField("Password");

    $("#PasswordRequirement").tooltip({ html: true, placement: 'left' });
}

function AdminResetPasswordPageLoad() {
    SetRequiredField("NewPassword");
    $("#PasswordRequirement").tooltip({ html: true, placement: 'left' });
}