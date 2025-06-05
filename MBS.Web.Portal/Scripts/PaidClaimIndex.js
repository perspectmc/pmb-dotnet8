function PaidClaimIndexPageLoad(editButtonUrl) {

    $('#paidclaim').dataTable({
        "ajax": {
            url: _applicationBaseUrl + '/ServiceRecord/GetPaidClaims',
            type: 'POST',
        },        
        "serverSide": true,
        "processing": true,
        "stateSave": true,
        "stateDuration": -1,
        "lengthMenu": [[50, 100, 300, -1], [50, 100, 300, "All"]],
        "pageLength": 100,
        "searching": true,
        "order": [[1, "desc"]],
        "columns": [
             {
                 "targets": 0,
                 "data": null,
                 "orderable": false,
                 "width": "10px",
                 "autowidth": false,
                 "render": function (data, type, row, meta) {
                     return '<input type="checkbox" name="batch" data_id="' + data.ServiceRecordId + '" />';
                 }
            },
            { "data": 'ClaimNumber' },
            { "data": 'HospitalNumber' },
            { "data": 'LastName' },
            { "data": 'FirstName', 'orderable': false },
            { "data": 'ServiceDateString', "orderDataType": "dom-text", "type": "date-eu" },
            { "data": 'PmtAppDateString', "orderDataType": "dom-text", "type": "date-eu" },
            { "data": 'ClaimAmountString', 'orderable': false },
            { "data": 'PaidAmountString', 'orderable': false },
            { "data": 'VarianceString' },
            { "data": 'CPSClaimNumber', 'orderable': false },
            { "data": 'FeeCodesString', 'orderable': false, },
            { "data": 'ExplainCodesString', 'orderable': false },            
            {
                "data": 'ServiceRecordId',
                "orderable": false,
                "render": function (data, type, row, meta) {
                    if (type === 'display' && data != null) {
                        return '<div class="control_buttons clearfix" style="display:none;"><button class="btn btn-xs control_buttons" onclick="ViewDetail(\'' + data + '\');CancelPropagation(event);"><img src="' +
                                editButtonUrl + '" title="View" /><span>View</span></button>' +
                                '<button class="btn btn-xs" style="padding-left: 10px;" onclick="ResubmitServiceRecord(\'' + data + '\');CancelPropagation(event);" ><img src="' + editButtonUrl + '" title="Edit & Resubmit" /><span>Edit & Resubmit</span></button></div>';

                    }

                    return data;
                }
            }
        ],
        "drawCallback": function (settings) {
            $("input:checkbox[name=batch]").prop("checked", false);
            $("input:checkbox[name=all]").prop("checked", false);
            $(".checkbox_row_header").width(10);
        }
    });

    var table = $('#paidclaim').DataTable();
    table.on('draw', function () {
        $("tr[role='row']").on("mouseover", function () {
            $(this).find('.control_buttons').show();
        });

        $("tr[role='row']").on("mouseout", function () {
            $(this).find('.control_buttons').hide();
        });
    });
}

function ViewDetail(id) {
    window.location.href = _applicationBaseUrl + "/ServiceRecord/Edit?id=" + id;
}

function batchResubmission() {
    var count = 0;
    var ids = "";
    $("input:checkbox[name=batch]:checked").each(function () {
        ids += $(this).attr("data_id") + ",";
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