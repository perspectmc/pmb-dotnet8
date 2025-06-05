function ClaimsInReportPageLoad(isFilled) {    
    if (!isFilled) {
        $("#GenerateReport").attr("disabled", "disabled");
        $("#GenerateReport").tooltip();

        $("#GeneratePDFReport").attr("disabled", "disabled");
        $("#GeneratePDFReport").tooltip();
    }

    SetRequiredField("ReportType");
    SetRequiredField("ServiceStartDate");
    SetRequiredField("ServiceEndDate");

    $("#ServiceStartDate").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0'
    });

    $("#ServiceEndDate").datepicker({
        dateFormat: 'dd/mm/yy',
        showButtonPanel: true,
        changeMonth: true,
        changeYear: true,
        maxDate: '0'
    });
}

function DownloadReport(type) {
    if (CheckValidReportDates()) {
        var startDate = $("#ServiceStartDate").val();
        var endDate = $("#ServiceEndDate").val();
        var reportType = $("#ReportType").val();

        if (startDate != undefined && endDate != undefined && reportType != undefined) {            
            if (type == 1) {
                var iframe = document.createElement("iframe");
                iframe.src = _applicationBaseUrl + '/ClaimsReportRTFDownload/' + startDate.replaceAll("/", "-") + "/" + endDate.replaceAll("/", "-") + "/" + reportType;

                // This makes the IFRAME invisible to the user.
                iframe.style.display = "none";

                // Add the IFRAME to the page.  This will trigger
                //   a request to GenerateFile now.
                document.body.appendChild(iframe);
            } else if (type == 2) {
                window.open(_applicationBaseUrl + '/ClaimsReportPDFDownload/' + startDate.replaceAll("/", "-") + "/" + endDate.replaceAll("/", "-") + "/" + reportType);
            }
            
        } else {
            alert("Invalid parameters");
        }
    }
}

function CheckValidReportDates() {
    var startDate = $("#ServiceStartDate").val();
    var endDate = $("#ServiceEndDate").val();
    if (startDate != null && endDate != null) {
        if (!CheckDateFormat(startDate)) {
            alert("Please enter a correct start date of service.");
            return false;
        }
        else if (!CheckDateFormat(endDate)) {
            alert("Please enter a correct end date of service.");
            return false;
        }

        var myStartDateObj = GetDateObj(startDate);
        var myEndDateObj = GetDateObj(endDate);

        if (myStartDateObj > myEndDateObj) {
            alert("The end date must be greater than or equal to the start date.");
            return false;
        }

        return true;
    }

    return false;
}

function CheckDateFormat(myDate) {
    var regExp = /^([1-9]|0[1-9]|[12][0-9]|3[01])\/([1-9]|0[1-9]|1[012])\/(20\d{2})$/;
    return myDate.match(regExp);
}

function GetDateObj(myDateString) {
    var mySplit = myDateString.split("/");
    var myDay = parseInt(mySplit[0]);
    var myMonth = parseInt(mySplit[1]);
    var myYear = parseInt(mySplit[2]);

    var myDateObj = new Date();
    myDateObj.setFullYear(myYear, myMonth - 1, myDay);
    return myDateObj;
}

function GenerateReportDate(days) {
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

    $("#ServiceStartDate").datepicker('setDate', startDate);
    $("#ServiceEndDate").datepicker('setDate', endDate);

    SearchResult();
}

function SearchResult() {
    if (CheckValidReportDates()) {
        submitForm();
    }
}

function submitForm() {
    $("#claimsReportForm").submit();
}