var msbSubmissionResult = null;
var wcbSubmissionResult = null;

function OpenDetail(id) {
    window.location.href = _applicationBaseUrl + '/ServiceRecord/Edit?id=' + id;
}

function DeleteServiceRecord(id) {
    var result = confirm("Are you sure you want to delete this service record?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/Delete',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response) {
                    var dataTable = $('#notsubmitted').DataTable();
                    dataTable.row($("#" + id)).remove();
                    dataTable.draw();

                    var total = 0;
                    $.each($(".claimAmount"), function () {
                        var text = $.trim($(this).text()).replace("$", "");
                        var amount = parseFloat(text);
                        total += amount;
                    });

                    $("#totalAmount").html("Total Amount: $" + TrueRound(total, 2).toFixed(2));

                } else {
                    alert("There is an error when deleting the service record, please try again!");
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when deleting the service record, please try again!");
            }
        });
    }
}

function TrueRound(value, digits) {
    return ((Math.round((value * Math.pow(10, digits)).toFixed(digits - 1)) / Math.pow(10, digits)).toFixed(digits)) * 1;
}

function SubmitReport() {
    if (!$('#submission').hasClass('in')) {
        msbSubmissionResult = null;
        wcbSubmissionResult = null;

        var imagePath = _applicationBaseUrl + '/Content/images/ajax_loader_blue_256.gif';
        $("#SubmissionResult").html("<div><img src='" + imagePath + "' style='margin-bottom:30px;'/><br/><span style='padding-left:20px;'>Preparing claimin for submission, please wait...</span></div>");
        $("#submission").modal('show');

        $(".submit_button").attr("disabled", "disabled");

        SubmitMSB();
    }
}

function PopupResponse() {
    try
    {    
        if ($("#hiddenPopupMessage").hasClass("isPDF")) {
            var isSafari = navigator.vendor && navigator.vendor.indexOf('Apple') > -1 &&
               navigator.userAgent &&
               navigator.userAgent.indexOf('CriOS') == -1 &&
               navigator.userAgent.indexOf('FxiOS') == -1;

            if (isSafari) {
                var byteCharacters = atob($("#hiddenPopupMessage").html());
                var byteNumbers = new Array(byteCharacters.length);
                for (var i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }
                var byteArray = new Uint8Array(byteNumbers);
                var file = new Blob([byteArray], { type: 'application/pdf;base64' });
                this.pdfUrl = (window.URL || window.webkitURL).createObjectURL(file);
                //If browser is Safari, use a Reader to display the PDF in the previously opened window
                var reader = new FileReader();
                reader.onloadend = function (e) {
                    var win = window.open();
                    win.document.write('<iframe src="' + String(reader.result) + '" frameborder="0" style="border:0; top:0px; left:0px; bottom:0px; right:0px; width:100%; height:100%;" allowfullscreen></iframe>')
                    win.document.close();
                };
                reader.readAsDataURL(file);
            } else {

                var objbuilder = '';
                objbuilder += ('<object type="application/pdf" width="100%" height="100%" data = "data:application/pdf;base64,');
                objbuilder += ($("#hiddenPopupMessage").html());
                objbuilder += ('"  class="internal">');
                objbuilder += ('<embed src="data:application/pdf;base64,');
                objbuilder += ($("#hiddenPopupMessage").html());
                objbuilder += ('" type="application/pdf"  />');
                objbuilder += ('</object>');

                var mywindow = window.open("about:blank", "_blank", "toolbar=yes, scrollbars=yes, resizable=yes, top=200, left=300, width=1000, height=600");
                mywindow.document.write('<html><head>');

                mywindow.document.write('<title>MSB - Validation Report</title>');
                mywindow.document.write('</head>');
                mywindow.document.write('<body>');
                mywindow.document.write('<h1>' + $("#hiddenPopupMessage").data("fileName") + '</h1>');
                mywindow.document.write(objbuilder);
                mywindow.document.write('</body>');

                mywindow.document.write('</html>');
                mywindow.document.close();
            }
        } else {
            var mywindow = window.open("about:blank", "_blank", "toolbar=yes, scrollbars=yes, resizable=yes, top=200, left=300, width=1000, height=600");
            mywindow.document.write('<html><head>');
            mywindow.document.write('<title>MSB - Submission Result</title>');
            mywindow.document.write('</head><body>');
            mywindow.document.write('<p>' + $("#hiddenPopupMessage").html() + '</p>');
            mywindow.document.write('</body>');
            mywindow.document.write('</html>');
            mywindow.document.close();
        }
    }
    catch (ex)
    {
    }

    return true;
}

function SubmitMSB() {
    $.ajax({
        type: "POST",
        url: _applicationBaseUrl + '/ServiceRecord/SubmitMSB',
        cache: false,
        async: true,
        success: function (response) {
            if (response != null && response != undefined) {
                msbSubmissionResult = response;
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            msbSubmissionResult = { MSBSubmitted: false, MSBMessage: "There is an error submitting the claims. Error -> " + errorThrown };
        },
        complete: function () {
            SubmitWCB();
        }
    });
}

function SubmitWCB() {
    $.ajax({
        type: "POST",
        url: _applicationBaseUrl + '/ServiceRecord/SubmitWCB',
        cache: false,
        async: true,
        success: function (response) {
            if (response != null && response != undefined) {
                wcbSubmissionResult = response;
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            wcbSubmissionResult = { WCBSubmitted: false, WCBMessage: "There is an error submitting the claims. Error -> " + errorThrown };
        },
        complete: function () {
            DisplaySubmissionResult();
        }
    });
}

function DisplaySubmissionResult() {
    if (msbSubmissionResult != null && wcbSubmissionResult != null) {
        var submissionResultContent = "";
        if (msbSubmissionResult.MSBSubmitted) {
            submissionResultContent = "<div class='alert alert-info'><span style='font-size:medium'>MSB Submission Result</span><br>MSB claims had submitted to the MSB API successfully.<br>" +
                                        "A new window is opened to show the submitted claims from MSB, please print it out for your reference.<br>" +
                                        "If a new window does not popup, please click <a href='#' onclick='PopupResponse(); return false;' style='color:black;'>here</a> to open the window and allow popup for this site!</div>";
            $("#hiddenPopupMessage").html(msbSubmissionResult.MSBMessage);

            if (msbSubmissionResult.MSBIsPDFContent) {
                $("#hiddenPopupMessage").addClass("isPDF");
                $("#hiddenPopupMessage").data("fileName",  msbSubmissionResult.MSBValidationReportPDFFileName);
            }

            PopupResponse();
        } else {
            if (msbSubmissionResult.MSBRejected) {
                submissionResultContent = "<div class='alert alert-danger'><span style='font-size:medium'>MSB Submission Result</span><br>MSB claims had been rejected by MSB.<br>" +
                                        "A new window is opened to show the rejected reasons from MSB, please print it out for your reference.<br>" +
                                        "If a new window does not popup, please click <a href='#' onclick='PopupResponse(); return false;' style='color:black;'>here</a> to open the window and allow popup for this site!</div>";

                $("#hiddenPopupMessage").html(msbSubmissionResult.MSBMessage);

                if (msbSubmissionResult.MSBIsPDFContent) {
                    $("#hiddenPopupMessage").addClass("isPDF");
                    $("#hiddenPopupMessage").data("fileName", msbSubmissionResult.MSBValidationReportPDFFileName);
                }

                PopupResponse();
            } else {
                if (msbSubmissionResult.MSBServerError) {
                    submissionResultContent += "<div class='alert alert-warning'><span style='font-size:medium'>MSB Submission Result</span><br>" + msbSubmissionResult.MSBMessage + "</div>";

                } else {
                    submissionResultContent += "<div class='alert alert-danger'><span style='font-size:medium'>MSB Submission Result</span><br>" + msbSubmissionResult.MSBMessage + "</div>";
                }
            }
        }

        if (wcbSubmissionResult.WCBSubmitted) {
            submissionResultContent += "<div class='alert alert-info'><span style='font-size:medium'>WCB Submission Result</span><br>WCB claims had submitted to our Fax Provider queue and will fax to WCB shortly.</div>";
        } else if (wcbSubmissionResult.WCBMessage != null && wcbSubmissionResult.WCBMessage !== "") {
            submissionResultContent += "<div class='alert alert-danger'><span style='font-size:medium'>WCB Submission Result</span><br>" + wcbSubmissionResult.WCBMessage + "</div>";
        }

        $("#SubmissionResult").html(submissionResultContent);

        if (msbSubmissionResult.MSBSubmitted && wcbSubmissionResult.WCBSubmitted) {
            $("#notsubmitted").DataTable().clear();
            $("#notsubmitted").DataTable().draw();

            $(".submit_button").removeAttr("disabled");
        } else {
            var dataTable = $("#notsubmitted").DataTable()
            if (msbSubmissionResult.MSBSubmitted || msbSubmissionResult.MSBServerError) {
                $.each($(".msbclaim"), function () {
                    var row = dataTable.row($(this));
                    row.remove();
                });
            }

            if (wcbSubmissionResult.WCBSubmitted) {
                $.each($(".wcbclaim"), function () {
                    var row = dataTable.row($(this));
                    row.remove();
                });
            } else {
                //WCB flag is false, but some of them still submitted, remove the submitted one
                if (wcbSubmissionResult.WCBSubmittedIds != "" && wcbSubmissionResult.WCBSubmittedIds != null) {
                    var ids = wcbSubmissionResult.WCBSubmittedIds.split(",");
                    for (var i = 0; i < ids.length; i++) {
                        if (ids[i] != "") {
                            var row = dataTable.row($("#" + ids[i]));
                            row.remove();
                        }
                    }
                }
            }

            $("#notsubmitted").DataTable().draw();
            $(".submit_button").removeAttr("disabled");
        }
    }   
}