var _pageNumberToBeLoad = -1;
var _pageNumberUsedInTimeOut = -1;
var _isAdmin = false;

function SearchClaimPageLoad(editButtonUrl, isAdmin) {
    _isAdmin = isAdmin;
    var myDataTableOption = {
        pageOption: [[50, 100, 300, -1], [50, 100, 300, "All"]],
        columnOption: [
            {
                "targets": 0,
                "data": null,
                "orderable": false,
                "width": "10px",
                "autowidth": false,
                "render": function (data, type, row, meta) {
                    if (type === 'display' && data != null) {
                        if (data.PaidOrRejected == "Pending" || data.PaidOrRejected == "Rejected" || data.PaidOrRejected == "Submitted") {
                            return '<input type="checkbox" name="batch" data_id="' + data.ServiceRecordId + '" />';
                        } else {
                            return '';
                        }
                    }

                    return data;
                }
            },
            { "data": 'ClaimNumber' },
            { "data": 'HospitalNumber' },
            { "data": 'LastName' },
            { "data": 'FirstName' },
            { "data": 'ServiceDateString', "orderDataType": "dom-text", "type": "date-eu" },
            { "data": 'SubmissionDateString', "orderDataType": "dom-text", "type": "date-eu"  },
            { "data": 'VarianceString' },
            { "data": 'PaidOrRejected' },
            { "data": 'CPSClaimNumber', 'orderable': false, },
            { "data": 'FeeCodesString', 'orderable': false, },
            { "data": 'ExplainCodesString', 'orderable': false, },
            {
                "data": 'ServiceRecordId',
                "orderable": false,
                "render": function (data, type, row, meta) {
                    if (type === 'display' && data != null) {
                        return '<div class="control_buttons clearfix" style="display:none;"><button class="btn btn-xs control_buttons" onclick="ViewDetail(\'' + data + '\');CancelPropagation(event);"><img src="' + editButtonUrl +
                            '" title="View" /><span>View</span></button></div>';
                    }

                    return data;
                }
            }
        ],
        orderOption: [[1, "desc"]]
    };

    if (isAdmin) {
        myDataTableOption = {
            pageOption: [50, 100, 300, 500],
            columnOption: [
                {
                    "targets": 0,
                    "data": null,
                    "orderable": false,
                    "width": "10px",
                    "autowidth": false,
                    "render": function (data, type, row, meta) {
                        if (type === 'display' && data != null) {
                            return '<input type="checkbox" name="batch" data_id="' + data.ServiceRecordId + '" />';
                        }

                        return data;
                    }
                },
                {
                    "data": 'UserName',
                    "orderable": false
                },
                { "data": 'ClaimNumber' },
                { "data": 'HospitalNumber' },
                { "data": 'LastName' },
                { "data": 'FirstName' },
                { "data": 'ServiceDateString', "orderDataType": "dom-text", "type": "date-eu" },
                { "data": 'SubmissionDateString', "orderDataType": "dom-text", "type": "date-eu" },
                { "data": 'VarianceString' },
                { "data": 'PaidOrRejected' },
                { "data": 'CPSClaimNumber', 'orderable': false, },
                { "data": 'FeeCodesString', 'orderable': false, },
                { "data": 'ExplainCodesString', 'orderable': false, },
                {
                    "data": 'ServiceRecordId',
                    "orderable": false,
                    "render": function (data, type, row, meta) {
                        if (type === 'display' && data != null) {
                            return '<div class="control_buttons clearfix" style="display:none;"><button class="btn btn-xs control_buttons" onclick="ViewDetail(\'' + data + '\');CancelPropagation(event);"><img src="' + editButtonUrl +
                                '" title="View" /><span>View</span></button></div>';
                        }

                        return data;
                    }
                }
            ],
            orderOption: [[2, "desc"]]
        };
    }

    $('#searchClaims').dataTable({
        "ajax": {
            url: _applicationBaseUrl + '/ServiceRecord/PerformSearchClaims',
            type: 'POST',
            data: function (d) {
                d.SearchClaimNumber = $("#SearchClaimNumber").val();
                d.SearchExplainCode = $("#SearchExplainCode").val();
                d.SearchLastName = $("#SearchLastName").val();
                d.SearchHSN = $("#SearchHSN").val();
                d.SearchClaimTypeList = $("#SearchClaimTypeList").val();
                d.SearchServiceStartDateString = $("#SearchServiceStartDateString").val();
                d.SearchServiceEndDateString = $("#SearchServiceEndDateString").val();
                d.SearchSubmissionStartDateString = $("#SearchSubmissionStartDateString").val();
                d.SearchSubmissionEndDateString = $("#SearchSubmissionEndDateString").val();
            }
        },
        "stateSave": true,
        "stateDuration": -1,
        "serverSide": true,
        "processing": true,
        "lengthMenu": myDataTableOption.pageOption,
        "pageLength": 100,
        "searching": false,
        "order": myDataTableOption.orderOption,
        "columns": myDataTableOption.columnOption,
	    "drawCallback": function (settings) {
	        $("input:checkbox[name=batch]").prop( "checked", false );
	        $("input:checkbox[name=all]").prop("checked", false);
	        $(".checkbox_row_header").width(10);
	    }
    });

    var table = $('#searchClaims').DataTable();

    table.on('draw', function () {
        $("tr[role='row']").on("mouseover", function () {
            $(this).find('.control_buttons').show();
        });

        $("tr[role='row']").on("mouseout", function () {
            $(this).find('.control_buttons').hide();
        });

        if (!_isAdmin) {
            var pageState = {
                "orderInfo": table.order(),
                "pageInfo": table.page.info(),
                "searchclaimnumber": $("#SearchClaimNumber").val(),
                "searchExplainCode": $("#SearchExplainCode").val(),
                "searchlastname": $("#SearchLastName").val(),
                "searchhsn": $("#SearchHSN").val(),
                "searchclaimtypelist": $("#SearchClaimTypeList").val(),
                "searchservicestartdatestring": $("#SearchServiceStartDateString").val(),
                "searchserviceenddatestring": $("#SearchServiceEndDateString").val(),
                "searchsubmissionstartdatestring": $("#SearchSubmissionStartDateString").val(),
                "searchsubmissionenddatestring": $("#SearchSubmissionEndDateString").val()
            };

            sessionStorage.pageState = JSON.stringify(pageState);

            if (_pageNumberToBeLoad > -1) {
                _pageNumberUsedInTimeOut = _pageNumberToBeLoad;

                setTimeout(() => {
                    if (_pageNumberUsedInTimeOut > -1) {
                        $('#searchClaims').DataTable().page(_pageNumberUsedInTimeOut).draw(false);
                        _pageNumberUsedInTimeOut - 1;
                    }
                }, 50);

                _pageNumberToBeLoad = -1;
            }
        }
    });

    if (sessionStorage.pageState && !_isAdmin) {
        var pageState = JSON.parse(sessionStorage.pageState);

        table.page.len(pageState.pageInfo.length);
        table.order(pageState.orderInfo[0]);

        $("#SearchClaimNumber").val(pageState.searchclaimnumber);
        $("#SearchExplainCode").val(pageState.searchExplainCode);
        $("#SearchLastName").val(pageState.searchlastname);
        $("#SearchHSN").val(pageState.searchhsn);
        $("#SearchClaimTypeList").val(pageState.searchclaimtypelist);
        $("#SearchServiceStartDateString").val(pageState.searchservicestartdatestring);
        $("#SearchServiceEndDateString").val(pageState.searchserviceenddatestring);
        $("#SearchSubmissionStartDateString").val(pageState.searchsubmissionstartdatestring);
        $("#SearchSubmissionEndDateString").val(pageState.searchsubmissionenddatestring);

        table.draw();

        _pageNumberToBeLoad = pageState.pageInfo.page;
    }

    $("#SearchServiceStartDateString").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0',
        onSelect: function (dateText) {
            ServiceDatePickerOnSelect();
        }
    });


    $("#SearchServiceEndDateString").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0',
        onSelect: function (dateText) {
            ServiceDatePickerOnSelect();
        }
    });

    $("#SearchSubmissionStartDateString").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0',
        onSelect: function (dateText) {
            SubmissionDatePickerOnSelect();
        }
    });


    $("#SearchSubmissionEndDateString").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0',
        onSelect: function (dateText) {
            SubmissionDatePickerOnSelect();
        }
    });

    $("#SearchClaimTypeList").change(function () {
        PerformSearch();
    });
}

function ServiceDatePickerOnSelect() {
    if ($("#SearchServiceStartDateString").val() != "" && $("#SearchServiceStartDateString").val() != undefined &&
        $("#SearchServiceEndDateString").val() != "" && $("#SearchServiceEndDateString").val() != undefined) {
        PerformSearch();
    }
}

function SubmissionDatePickerOnSelect() {
    if ($("#SearchSubmissionStartDateString").val() != "" && $("#SearchSubmissionStartDateString").val() != undefined &&
        $("#SearchSubmissionEndDateString").val() != "" && $("#SearchSubmissionEndDateString").val() != undefined) {
        PerformSearch();
    }
}

function ViewDetail(id) {
    window.location.href = _applicationBaseUrl + "/ServiceRecord/Edit?id=" + id;
}

function GenerateServiceReportDate(days) {
    var endDate = new Date();
    var startDate = new Date();
    if (days == '7day') {
        startDate.setDate(endDate.getDate() - 7);
    } else if (days == '1month') {
        startDate.setMonth(endDate.getMonth() - 1);
    } else if (days == '3month') {
        startDate.setMonth(endDate.getMonth() - 3);
    } else if (days == '12month') {
        startDate.setMonth(endDate.getMonth() - 12);
    } else if (days == 'previousyear') {
        var previousYear = startDate.getFullYear() - 1;
        startDate = new Date(previousYear, 0, 1);
        endDate = new Date(previousYear, 11, 31);

    } else if (days == 'yeartodate') {
        startDate.setMonth(0);
        startDate.setDate(1);
    }

    $("#SearchServiceStartDateString").datepicker('setDate', startDate);
    $("#SearchServiceEndDateString").datepicker('setDate', endDate);

    PerformSearch();
}

function GenerateSubmissionReportDate(days) {
    var endDate = new Date();
    var startDate = new Date();
    if (days == '4weeks') {
    } else if (days == 'over4weeks') {
        endDate.setDate(endDate.getDate() - 28);
        startDate = "";
    } else if (days == 'over8weeks') {
        endDate.setDate(endDate.getDate() - 56);
        startDate = "";
    } else if (days == 'over12weeks') {
        endDate.setDate(endDate.getDate() - 84);
        startDate = "";
    } else if (days == 'yeartodate') {
        startDate.setMonth(0);
        startDate.setDate(1);
    }

    $("#SearchSubmissionStartDateString").datepicker('setDate', startDate);
    $("#SearchSubmissionEndDateString").datepicker('setDate', endDate);

    PerformSearch();
}

function PerformSearch() {
    var table = $('#searchClaims').DataTable();
    table.draw();
}

function ClearSearch() {
    $("#SearchClaimNumber").val("");
    $("#SearchLastName").val("");
    $("#SearchExplainCode").val("");
    $("#SearchHSN").val("");
    $("#SearchClaimTypeList").val("2");
    $("#SearchServiceStartDateString").val("");
    $("#SearchServiceEndDateString").val("");
    $("#SearchSubmissionStartDateString").val("");
    $("#SearchSubmissionEndDateString").val("");

    $('#searchClaims').DataTable().draw();
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

function batchIgnore() {
    var count = 0;
    var ids = "";
    $("input:checkbox[name=batch]:checked").each(function () {
        ids += $(this).attr("data_id") + ",";
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