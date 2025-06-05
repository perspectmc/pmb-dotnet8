function ChangeLockStatus(userId) {
    var button = $("#" + userId + " .control_buttons");
    var action = "lock";

    if (button.hasClass("unlock")) {
        action = "unlock";
    }

    var result = confirm("Are you sure you want to " + action + " this user account?");

    if (result == true) {
        ToggleStatusAsync(_applicationBaseUrl + '/UserManagement/ChangeLockStatus', { 'userId': userId }, function () {
            var userTable = $('#users').DataTable();
            var img = button.find("img");
            var span = button.find("span");
            if (action == "lock") {
                button.removeClass("lock");
                button.addClass("unlock");
                img.attr("src", _applicationBaseUrl + '/Content/images/icon_okay.png');
                span.html("Unlock?");
            } else {
                button.removeClass("unlock");
                button.addClass("lock");
                img.attr("src", _applicationBaseUrl + '/Content/images/icon_lock.png');
                span.html("Lock?");
            }

            userTable.cell(button.parent()).data(button.parent().html()).draw();
        }, function () {
            alert("There is an error when " + action + " the user account. Please try again or contact support.");
        });
    }

    return false;
}

function OpenDetail(id) {
    window.location.href = _applicationBaseUrl + '/UserManagement/ResetPassword?id=' + id;
}

function SendBulkEmail() {
    var message = "";
    $("#errorMessage").html(message);

    var subject = $.trim($("#emailSubject").val());
    if (subject.length == 0) {
        message += "Email Subject cannot be empty!<br>";
    }

    var content = $.trim($("#emailContent").val());

    if (content.length == 0) {
        message += "Email content cannot be empty!<br>";
    }

    var checkBoxes = $(".enable_user_checkbox:checked");
    if (checkBoxes.length == 0) {
        message += "You must select at least one user to send email!<br>";
    }

    if (message.length > 0) {
        $("#errorMessage").html(message);
        return;
    }

    var emailAddresses = [];
    $.each(checkBoxes, function (key, value) {
        var id = $(value).parent().parent().attr("id");
        var email = $.trim($("#email_" + id).text());

        var model = new NewEmailBulkModel(subject, content, email);

        var sendResult = $("#sendResult");
        $.ajax({
            async: false,
            type: "POST",
            dataType: "json",
            contentType: "application/json",
            cache: false,
            url: _applicationBaseUrl + '/UserManagement/SendBulkEmail',
            data: JSON.stringify(model),
            beforeSend: function () {
            },
            success: function (response) {
                var color = "red";
                if (response != null && response != undefined && response === true) {
                    color = "#0CEEA7";
                }

                sendResult.append("<span style='color:" + color + ";width:25%;'>" + email + "</span><br>");
            },
            error: function (jqXHR, textStatus, errorThrown) {
            }
        });
    })
}

function NewEmailBulkModel(subject, content, email) {
    var self = this;
    self.MessageSubject = subject;
    self.MessageBody = content;
    self.UserEmail = email;
}

function ImpersonateUser(userId) {

    $.ajax({
        url: _applicationBaseUrl + '/UserManagement/UserImpersonation',
        data: "targetId=" + userId,
        cache: true,
        success: function (data) {
            if (data) {
                window.location.href = _applicationBaseUrl + "/Home";
            } else {
                alert("There is an error performing the action!");
            }
        },
        error: function (data) {
            alert("There is an error performing the action!")
        }
    });
}