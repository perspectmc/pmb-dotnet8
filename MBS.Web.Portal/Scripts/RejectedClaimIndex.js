function RejectedClaimIndexPageLoad() {
    $('#rejectedclaim').dataTable({
        "stateSave": true,
        "stateDuration": -1,
        "lengthMenu": [[50, 100, 300, -1], [50, 100, 300, "All"]],
        "pageLength": 100,
        "searching": true,
        "order": [[1, "desc"]],
        "columns": [
            { "orderable": false },
            null,
            null,
            null,
            null,
            { "orderDataType": "dom-text", "type": "date-eu" },
            null,
            { "orderDataType": "dom-text", "type": "date-eu" },
            null,
            { "orderable": false },
            { "orderable": false },
            { "orderable": false },
            { "orderable": false }
        ],
        "drawCallback": function (settings) {
            $("input:checkbox[name=batch]").prop("checked", false);
            $("input:checkbox[name=all]").prop("checked", false);
        }
    });
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

function OpenDetail(id, mode) {
    mode = mode == "" ? "0" : "1";  //1 is specific, 0 is all      
    window.location.href = _applicationBaseUrl + '/ServiceRecord/Edit?mode=' + mode + "&id=" + id;
}

function DeleteServiceRecord(id) {
    var result = confirm("Are you sure you want to delete this rejected claim service record?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/ToIgnore',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response) {
                    var dataTable = $('#rejectedclaim').DataTable();
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

function ConvertToPaid(id) {
    var result = confirm("Are you sure you want to convert this rejected claim to paid claim?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/ConvertToPaid',
            cache: false,
            data: { 'id': id },
            async: true,
            success: function (response) {
                if (response == "True") {
                    var dataTable = $('#rejectedclaim').DataTable();
                    dataTable.row($("#" + id)).remove();
                    dataTable.draw();
                } else {
                    alert("There is an error when converting the service record, please try again!");
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when converting the service record, please try again!");
            }
        });
    }
}

//function GenerateClaimsIn() {
//    var checkBoxList = $("input:checked:not('#chkAll')");
//    if (checkBoxList.length > 0) {
//        var selectIdList = "";
//        var imagePath = _applicationBaseUrl + '/Content/images/ajax_loader_blue_256.gif';
//        $("#SubmissionResult").html("<div><img src='" + imagePath + "' style='margin-bottom:30px;'/><br/><span style='padding-left:20px;'>Generating submission for rejected claims, please wait...</span></div>");
//        $("#GenerateClaimsIn").modal('show');

//        $.each(checkBoxList, function () {
//            selectIdList += $(this).parent().parent().attr("id") + ",";
//        })

//        selectIdList = selectIdList.substr(0, selectIdList.length - 1);
//        if (selectIdList.length > 0) {
//            var errorContent = "<h4>There is an issue resetting the claims for resubmission! Please try again!</h4>" +
//                                "<button class='btn btn-default' type='button' onclick='GenerateClaimsIn();CancelPropagation(event);'>Retry</button>&nbsp;" +
//                                "<button class='btn btn-default' type='button' onclick='CloseModal();CancelPropagation(event);'>Close</button>";
//            $.ajax({
//                type: "POST",
//                url: _applicationBaseUrl + '/ServiceRecord/GenerateResubmission',
//                cache: false,
//                data: { "idList": selectIdList },
//                async: true,
//                success: function (response) {
//                    if (response == "success") {
//                        $.each(checkBoxList, function () {
//                            $(this).parent().parent().toggle(false);
//                        });
//                        $("#chkAll").removeAttr("checked");
//                        $("#SubmissionResult").empty();
//                        $("#GenerateClaimsIn").modal("hide");

//                        $.each(checkBoxList, function () {
//                            $("#rejectedclaim").DataTable().row($(this).parent().parent()).remove();
//                        })

//                        $("#rejectedclaim").DataTable().draw();
//                    } else {
//                        $("#SubmissionResult").html(errorContent);
//                    }
//                },
//                error: function (jqXHR, textStatus, errorThrown) {
//                    $("#SubmissionResult").html(errorContent);
//                }
//            });
//        }
//    } else {
//        alert("You must select at least one record for resubmission!");
//    }
//}

//function CloseModal() {
//    $("#GenerateClaimsIn").modal("hide");
//}

function batchResubmission() {
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
                data: { 'ids': ids },
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

function batchIgnore() {
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
