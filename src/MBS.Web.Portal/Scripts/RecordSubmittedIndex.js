function DeleteServiceRecord(id) {
    var result = confirm("Are you sure you want to delete this service record?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/ToIgnore',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response) {
                    var dataTable = $('#unitrecords').DataTable();
                    dataTable.row($("#" + id)).remove();
                    dataTable.draw();                    
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

function openSearchWindow(hsn) {
    const url = new URL(window.location);
    var hostName = url.hostname;
    var portNumber = url.port;

    var path = "";
    if (hostName == "localhost") {
        path = "http://localhost:" + portNumber + "/ServiceRecord/SearchClaimsBeta?hsn=" + hsn;
    } else {
        path = "https://" + hostName + "/app/ServiceRecord/SearchClaimsBeta?hsn=" + hsn;
    }

    window.open(path, "_blank", "toolbar=yes,scrollbars=yes,resizable=yes,top=50,left=50,width=1600,height=1000");
}

function ActivateServiceRecord(id) {
    var result = confirm("Are you sure you want to validate this service record?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/Activate',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response) {
                    var dataTable = $('#unitrecords').DataTable();
                    var statusCell = $("#" + id + " .wcbfaxstatus");
                    statusCell.html("");

                    dataTable.row($("#" + id)).remove();
                    dataTable.draw();
                } else {
                    alert("There is an error when setting the service record to be validated, please try again!");
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when setting the service record to be validated, please try again!");
            }
        });
    }
}

function ViewFaxReceipt(serviceRecordId) {
    var imagePath = _applicationBaseUrl + '/Content/images/ajax_loader_blue_256.gif';
    $("#FaxReceiptDetail").html("<div><img src='" + imagePath + "' style='margin-bottom:30px;'/><br/><span style='padding-left:20px;'>Contacting our Fax provider to get the fax receipt, please wait...</span></div>");
    $("#viewFaxReceipt").modal('show');

    $.ajax({
        type: "POST",
        url: _applicationBaseUrl + '/ServiceRecord/GetFaxReceipt',
        data: { 'id': serviceRecordId },
        cache: false,
        async: true,
        success: function (response) {
            if (response != null && response != undefined) {
                var message = "";

                message += "<div><b>Submission Time:</b> " + response.SubmissionTime + "</div>";
                message += "<div><b>Completion Time:</b> " + response.CompletionTime + "</div>";
                message += "<div><b>Pages Sent:</b> " + response.PageSent + "</div>";
                message += "<div><b>Fax Status:</b> " + response.FaxStatus + "</div>";

                $("#FaxReceiptDetail").html(message);
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            $("#FaxReceiptDetail").html("<p>There is an error retrieve the fax receipt -> " + errorThrown);
        },
        complete: function () {

        }
    });
}

function ResubmitServiceRecord(id) {
    var result = confirm("Are you sure you want to resubmit this service record?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/Resubmit',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response) {
                    window.location.href = _applicationBaseUrl + "/ServiceRecord/Edit?id=" + id;
                } else {
                    alert("There is an error when resubmitting the service record, please try again!");
                    window.location.reload(true);
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when resubmitting the service record, please try again!");
                window.location.reload(true);
            }
        });
    }
}

function batchResubmission()
{
    var count = 0;
    var ids = "";
    $("input:checkbox[name=batch]:checked").each(function () {
        ids += $(this).parent().parent().attr("id") + ",";
        count++;
    });
    
    if (count == 0) {
        alert("You must select at least one row to use the batch function.");
    } else {        
        var result = confirm("You selected " + count + " records. Are you sure you want to resubmit them?");
        if (result) {
            $.ajax({
                type: "POST",
                url: _applicationBaseUrl + '/ServiceRecord/BatchResubmission',
                cache: false,
                data: { 'ids':  ids },
                async: true,
                success: function (response) {
                    if (response) {
                        window.location.reload(true);
                    } else {
                        alert("There is an error when resubmitting the selected service records, please try again!");
                        window.location.reload(true);
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    alert("There is an error when resubmitting the selected service records, please try again!");
                    window.location.reload(true);
                }
            });
        }
    }
}

function batchIgnore()
{
    var count = 0;
    var ids = "";
    $("input:checkbox[name=batch]:checked").each(function () {
        ids += $(this).parent().parent().attr("id") + ",";
        count++;
    });

    if (count == 0) {
        alert("You must select at least one row to use the batch function.");
    } else {
        var result = confirm("You selected " + count + " records. Are you sure you want to delete them?");
        if (result) {
            $.ajax({
                type: "POST",
                url: _applicationBaseUrl + '/ServiceRecord/BatchIgnore',
                cache: false,
                data: { 'ids': ids },
                async: true,
                success: function (response) {
                    if (response) {
                        window.location.reload(true);
                    } else {
                        alert("There is an error when deleting the selected service records, please try again!");
                        window.location.reload(true);
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    alert("There is an error when deleting the selected service records, please try again!");
                    window.location.reload(true);
                }
            });
        }
    }
}
