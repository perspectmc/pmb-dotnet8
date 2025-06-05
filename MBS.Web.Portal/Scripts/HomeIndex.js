function HomePageIndex() { 
    $('#IntervalList').change(function () {
        $.each($(".loading_data"), function () {
            var cell = $(this);
            var imagePath = "<img src='" + _applicationBaseUrl + '/Content/images/icon_loading.gif' + "' />";            
            cell.html(imagePath);
        });

        $.ajax({
            async: true,
            type: "GET",
            cache: false,
            url: _applicationBaseUrl + '/Home/GetTotals?t=' + Math.random() + '&period=' + $('#IntervalList').val(),
            success: function (response) {
                if (response != null && response != undefined) {
                    $("#TotalUnSubmittedAmount").html(response.UnSubmitted.Amount);
                    $("#TotalSubmittedAmount").html(response.Submitted.Amount);
                    $("#TotalPendingAmount").html(response.Pending.Amount);
                    $("#TotalPaidAmount").html(response.Paid.Amount);
                    $("#TotalRejectedAmount").html(response.Rejected.Amount);
                    $("#TotalExpiringAmount").html(response.Expiring.Amount);
                    
                    $('.loading_data').formatCurrency();
                    $("#NumberOfUnSubmittedRecords").html(response.UnSubmitted.NumberOfRecords);
                    $("#NumberOfSubmittedRecords").html(response.Submitted.NumberOfRecords);
                    $("#NumberOfPendingRecords").html(response.Pending.NumberOfRecords);
                    $("#NumberOfPaidRecords").html(response.Paid.NumberOfRecords);
                    $("#NumberOfRejectedRecords").html(response.Rejected.NumberOfRecords);
                    $("#NumberOfExpiringRecords").html(response.Expiring.NumberOfRecords);
                } else {
                    alert("There is an error retrieving the total claims amount!");
                }
            },
            error: function (msg) {
                $.each($(".loading_data"), function () {
                    var imagePath = _applicationBaseUrl + '/Content/images/icon_cancel.png';
                    var cell = $(this);
                    cell.html("<img src='" + imagePath + "' style='width:16px;height:11px' />");
                });
            }
        });
    });
}

function ResubmitFax() {    
    var imagePath = _applicationBaseUrl + '/Content/images/ajax_loader_blue_256.gif';
    $("#SubmissionResult").html("<div><img src='" + imagePath + "' style='margin-bottom:30px;'/><br/><span style='padding-left:20px;'>Preparing WCB claims for re-submission, please wait...</span></div>");
    $("#fax_resubmission").modal('show');
    $.ajax({
        type: "POST",
        url: _applicationBaseUrl + '/ServiceRecord/ResubmitWCB',
        cache: false,
        async: true,
        success: function (response) {
            if (response != null && response != undefined) {
                var submissionResultContent = "";
               
                if (response.WCBSubmitted) {
                    submissionResultContent += "<div class='alert alert-info'>" + response.WCBMessage + "</div>";
                } else if (response.WCBMessage != null && response.WCBMessage != "") {
                    submissionResultContent += "<div class='alert alert-danger'>" + response.WCBMessage + "</div>";
                }

                $("#SubmissionResult").html(submissionResultContent);
                
            } else {
                $("#SubmissionResult").html("<div class='alert alert-danger'>There are no claims need to re-submitted!</div>");
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            $("#SubmissionResult").html("<div class='alert alert-danger'>There is an error re-submitting the claims. Error -> " + errorThrown + "</div>");
        }
    });    
}

function openSearchWithClaimLost() {
    window.location.href = _applicationBaseUrl + '/ServiceRecord/SearchClaimsBeta?showClaimsType=lost';
}

function openSearchWithClaimExpiring() {
    window.location.href = _applicationBaseUrl + '/ServiceRecord/SearchClaimsBeta?showClaimsType=expiring';
}