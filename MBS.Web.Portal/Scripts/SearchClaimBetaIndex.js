var _pageNumberToBeLoad = -1;
var _pageNumberUsedInTimeOut = -1;
var _isAdmin = false;
var _sessionStorage = true;

function SearchClaimPageLoad(editButtonUrl, isAdmin, showClaimsType) {
    _isAdmin = isAdmin;
    
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

    if ($("#SearchHSN").val() != "" && $("#SearchHSN").val() != null && $("#SearchHSN").val() != undefined) {
        $("#SearchHSN").prop("readonly", true);
        _sessionStorage = false;
    }
       
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
                        if (data.Status == "Pending" || data.Status == "Rejected" || data.Status == "Submitted") {
                            return '<input type="checkbox" name="batch" data_id="' + data.ServiceRecordId + '" />';
                        } else {
                            return '';
                        }
                    }

                    return data;
                }
            },
            { "data": 'ServiceDateString', "orderDataType": "dom-text", "type": "date-eu" },
            { "data": 'SubmissionDateString', "orderDataType": "dom-text", "type": "date-eu" },
            { "data": 'RunCode' },
            { "data": 'LastName' },
            { "data": 'FirstName' },
            {
                "data": 'HospitalNumber',
                "render": function (data, type, row, meta) {
                    if (type === 'display' && data != null) {
                        return '<a href="javascript:void(0);" onclick="openSearchWindow(\'' + data + '\');">' + data + '</a>';
                    }

                    return data;
                }
            },
            { "data": 'UnitCode' },
            { "data": 'UnitNumber' },
            { "data": 'DiagCode' },
            { "data": 'ClaimNumber' },
            { "data": 'CPSClaimNumber' },
            { "data": 'ExplainCode' },
            { "data": 'ExplainCode2' },
            { "data": 'ExplainCode3' },
            { "data": 'ClaimAmountString' },
            { "data": 'PaidAmountString' },
            { "data": 'VarianceString' },
            { "data": 'Status' }            
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
                { "data": 'ServiceDateString', "orderDataType": "dom-text", "type": "date-eu" },
                { "data": 'SubmissionDateString', "orderDataType": "dom-text", "type": "date-eu" },
                { "data": 'RunCode' },
                { "data": 'LastName' },
                { "data": 'FirstName' },
                {
                    "data": 'HospitalNumber',
                    "render": function (data, type, row, meta) {
                        if (type === 'display' && data != null) {
                            return '<a href="javascript:void(0);" onclick="openSearchWindow(\'' + data + '\');">' + data + '</a>';
                        }

                        return data;
                    }
                },
                { "data": 'UnitCode' },
                { "data": 'UnitNumber' },
                { "data": 'DiagCode' },
                { "data": 'ClaimNumber' },
                { "data": 'CPSClaimNumber' },
                { "data": 'ExplainCode' },
                { "data": 'ExplainCode2' },
                { "data": 'ExplainCode3' },
                { "data": 'ClaimAmountString' },
                { "data": 'PaidAmountString' },
                { "data": 'VarianceString' },
                { "data": 'Status' }               
            ],
            orderOption: [[2, "desc"]]
        };
    }

    $('#searchClaims').dataTable({
        "ajax": {
            url: _applicationBaseUrl + '/ServiceRecord/PerformSearchClaimsBeta',
            type: 'POST',
            data: function (d) {
                d.SearchClaimNumber = $("#SearchClaimNumber").val();
                d.SearchCPSClaimNumber = $("#SearchCPSClaimNumber").val();
                d.SearchUnitCode = $("#SearchUnitCode").val();
                d.SearchDiagCode = $("#SearchDiagCode").val();
                d.SearchExplainCode = $("#SearchExplainCode").val();
                d.SearchLastName = $("#SearchLastName").val();
                d.SearchFirstName = $("#SearchFirstName").val();
                d.SearchClinicName = $("#SearchClinicName").val();
                d.SearchHSN = $("#SearchHSN").val();
                d.SearchClaimTypeList = $("#SearchClaimTypeList").val();
                d.SearchServiceStartDateString = $("#SearchServiceStartDateString").val();
                d.SearchServiceEndDateString = $("#SearchServiceEndDateString").val();
                d.SearchSubmissionStartDateString = $("#SearchSubmissionStartDateString").val();
                d.SearchSubmissionEndDateString = $("#SearchSubmissionEndDateString").val();
                d.SearchUnsubmitted = $("#SearchUnsubmitted").prop('checked');
                d.SearchSubmitted = $("#SearchSubmitted").prop('checked');
                d.SearchPending = $("#SearchPending").prop('checked');
                d.SearchPaid = $("#SearchPaid").prop('checked');
                d.SearchRejected = $("#SearchRejected").prop('checked');
                d.SearchDeleted = $("#SearchDeleted").prop('checked');
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

        if (!_isAdmin && _sessionStorage) {
            var pageState = {
                "orderInfo": table.order(),
                "pageInfo": table.page.info(),
                "searchclaimnumber": $("#SearchClaimNumber").val(),
                "searchcpsclaimnumber": $("#SearchCPSClaimNumber").val(),
                "searchunitcode": $("#SearchUnitCode").val(),
                "searchdiagcode": $("#SearchDiagCode").val(),
                "searchexplaincode": $("#SearchExplainCode").val(),
                "searchlastname": $("#SearchLastName").val(),
                "searchfirstname": $("#SearchFirstName").val(),
                "searchclinicname": $("#SearchClinicName").val(),
                "searchhsn": $("#SearchHSN").val(),
                "searchclaimtypelist": $("#SearchClaimTypeList").val(),
                "searchservicestartdatestring": $("#SearchServiceStartDateString").val(),
                "searchserviceenddatestring": $("#SearchServiceEndDateString").val(),
                "searchsubmissionstartdatestring": $("#SearchSubmissionStartDateString").val(),
                "searchsubmissionenddatestring": $("#SearchSubmissionEndDateString").val(),
                "searchunsubmitted": $("#SearchUnsubmitted").prop('checked'),
                "searchsubmitted": $("#SearchSubmitted").prop('checked'),
                "searchpending": $("#SearchPending").prop('checked'),
                "searchpaid": $("#SearchPaid").prop('checked'),
                "searchrejected": $("#SearchRejected").prop('checked'),
                "searchdeleted": $("#SearchDeleted").prop('checked')
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

    if (sessionStorage.pageState && !_isAdmin && _sessionStorage) {
        var pageState = JSON.parse(sessionStorage.pageState);

        table.page.len(pageState.pageInfo.length);
        table.order(pageState.orderInfo[0]);

        $("#SearchClaimNumber").val(pageState.searchclaimnumber);
        $("#SearchCPSClaimNumber").val(pageState.searchcpsclaimnumber);
        $("#SearchUnitCode").val(pageState.searchunitcode);
        $("#SearchDiagCode").val(pageState.searchdiagcode);
        $("#SearchExplainCode").val(pageState.searchexplaincode);
        $("#SearchLastName").val(pageState.searchlastname);
        $("#SearchFirstName").val(pageState.searchfirstname);
        $("#SearchClinicName").val(pageState.searchclinicname);

        if (pageState.searchhsn != "" && pageState.searchhsn != null && pageState.hsn != undefined) {
            if (pageState.searchhsn != $("#SearchHSN").val()) {
                $("#SearchHSN").val(pageState.searchhsn);
            }
        }
        
        $("#SearchClaimTypeList").val(pageState.searchclaimtypelist);
        $("#SearchServiceStartDateString").val(pageState.searchservicestartdatestring);
        $("#SearchServiceEndDateString").val(pageState.searchserviceenddatestring);
        $("#SearchSubmissionStartDateString").val(pageState.searchsubmissionstartdatestring);
        $("#SearchSubmissionEndDateString").val(pageState.searchsubmissionenddatestring);

        $("#SearchUnsubmitted").prop('checked', pageState.searchunsubmitted);
        $("#SearchSubmitted").prop('checked', pageState.searchsubmitted);
        $("#SearchPending").prop('checked', pageState.searchpending);
        $("#SearchPaid").prop('checked', pageState.searchpaid);
        $("#SearchRejected").prop('checked', pageState.searchrejected);
        $("#SearchDeleted").prop('checked', pageState.searchdeleted);

        table.draw();

        _pageNumberToBeLoad = pageState.pageInfo.page;
    } else {
        $("#SearchUnsubmitted").prop('checked', true);
        $("#SearchSubmitted").prop('checked', true);
        $("#SearchPending").prop('checked', true);
        $("#SearchPaid").prop('checked', true);
        $("#SearchRejected").prop('checked', true);
    }



    $('input').keypress(function (e) {
        if (e.which == 13) {
            PerformSearch();
        }
    });

    $(document).contextmenu({
        delegate: ".dataTable td",
        menu: [
          { title: "View / Edit", cmd: "view" },
          { title: "Resubmit", cmd: "resubmit" },
          { title: "Delete", cmd: "delete" }
        ],
        select: function (event, ui) {
            var id = ui.target.parent().attr("id");
            switch (ui.cmd) {
                case "view":
                    window.location.href = _applicationBaseUrl + '/ServiceRecord/Edit?id=' + id;
                    break;
                case "resubmit":
                    resubmitClaim(id);
                    break;
                case "delete":
                    deleteClaim(id);
                    break;
            }
        },
        beforeOpen: function (event, ui) {
            var $menu = ui.menu,
                $target = ui.target,
                extraData = ui.extraData;
            ui.menu.zIndex(9999);
        }
    });

    $('.searchclaimtype').change(function () {
        PerformSearch();
    });

    if (showClaimsType == "lost") {
        $("#SearchPaid").prop("checked", false);

        var endDate = new Date();
        endDate.setMonth(endDate.getMonth() - 6);

        $("#SearchServiceStartDateString").datepicker('setDate', "");
        $("#SearchServiceEndDateString").datepicker('setDate', endDate);

        PerformSearch();
    } else if (showClaimsType == "expiring") {
        $("#SearchPaid").prop("checked", false);

        var startDate = new Date();
        startDate.setMonth(startDate.getMonth() - 4);

        var endDate = new Date();
        endDate.setMonth(endDate.getMonth() - 6);

        $("#SearchServiceStartDateString").datepicker('setDate', endDate);

        $("#SearchServiceEndDateString").datepicker('setDate', startDate);

        PerformSearch();
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

function CheckAllClaims() {
    $("#SearchUnsubmitted").prop("checked", true);
    $("#SearchSubmitted").prop("checked", true);
    $("#SearchPending").prop("checked", true);
    $("#SearchPaid").prop("checked", true);
    $("#SearchRejected").prop("checked", true);

    PerformSearch();
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
    $("#SearchCPSClaimNumber").val("");
    $("#SearchUnitCode").val("");
    $("#SearchDiagCode").val("");
    $("#SearchLastName").val("");
    $("#SearchFirstName").val("");
    $("#SearchClinicName").val("");
    $("#SearchExplainCode").val("");
    $("#SearchHSN").val("");
    $("#SearchClaimTypeList").val("-1");
    $("#SearchServiceStartDateString").val("");
    $("#SearchServiceEndDateString").val("");
    $("#SearchSubmissionStartDateString").val("");
    $("#SearchSubmissionEndDateString").val("");

    $("#SearchUnsubmitted").prop("checked", false);
    $("#SearchSubmitted").prop("checked", false);
    $("#SearchPending").prop("checked", false);
    $("#SearchPaid").prop("checked", false);
    $("#SearchRejected").prop("checked", false);
    $("#SearchDeleted").prop("checked", false);
    
    $('#searchClaims').DataTable().draw();
}

function resubmitClaim(id) {
    var result = confirm("Are you sure you want to resubmit this claim?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/BatchResubmission',
            cache: false,
            data: { 'ids': id },
            async: true,
            success: function (response) {
                if (response) {
                    window.location.reload(true);
                } else {
                    alert("There is an error when resubmitting this claim, please try again!");
                    window.location.reload(true);
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when resubmitting this claim, please try again!");
                window.location.reload(true);
            }
        });
    }
}

function deleteClaim(id) {
    var result = confirm("Are you sure you want to delete this claim?");
    if (result) {
        $.ajax({
            type: "POST",
            url: _applicationBaseUrl + '/ServiceRecord/BatchIgnore',
            cache: false,
            data: { 'ids': id },
            async: true,
            success: function (response) {
                if (response) {
                    window.location.reload(true);
                } else {
                    alert("There is an error when deleting this claim, please try again!");
                    window.location.reload(true);
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert("There is an error when deleting this claim, please try again!");
                window.location.reload(true);
            }
        });
    }
}


function batchResubmission() {
    var ids = [];
    $("input:checkbox[name=batch]:checked").each(function () {
        ids.push($(this).parent().parent().attr("id"));
    });

    var distinctIds = $.unique(ids);

    if (distinctIds.length == 0) {
        alert("You must select at least one claim to use the batch function.");
    } else {
        var result = confirm("You selected " + distinctIds.length + " claims. Are you sure you want to resubmit them?");
        if (result) {
            $.ajax({
                type: "POST",
                url: _applicationBaseUrl + '/ServiceRecord/BatchResubmission',
                cache: false,
                data: { 'ids': distinctIds.join(",") },
                async: true,
                success: function (response) {
                    if (response) {
                        window.location.reload(true);
                    } else {
                        alert("There is an error when resubmitting the selected claims, please try again!");
                        window.location.reload(true);
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    alert("There is an error when resubmitting the selected claims, please try again!");
                    window.location.reload(true);
                }
            });
        }
    }
}

function batchIgnore() {
    var ids = [];
    $("input:checkbox[name=batch]:checked").each(function () {
        ids.push($(this).parent().parent().attr("id"));
    });

    var distinctIds = $.unique(ids);

    if (distinctIds.length == 0) {
        alert("You must select at least one claim to use the batch function.");
    } else {
        var result = confirm("You selected " + distinctIds.length + " records. Are you sure you want to delete them?");
        if (result) {
            $.ajax({
                type: "POST",
                url: _applicationBaseUrl + '/ServiceRecord/BatchIgnore',
                cache: false,
                data: { 'ids': distinctIds.join(",") },
                async: true,
                success: function (response) {
                    if (response) {
                        window.location.reload(true);
                    } else {
                        alert("There is an error when deleting the selected claims, please try again!");
                        window.location.reload(true);
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    alert("There is an error when deleting the selected claims, please try again!");
                    window.location.reload(true);
                }
            });
        }
    }
}