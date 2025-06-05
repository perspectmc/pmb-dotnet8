
function GotoDetail(id) {
    window.location.href = _applicationBaseUrl + "/ServiceRecord?id=" + id;
}

function DownloadReport(id, type) {
    if (id != "") {
        

        if (type == 1 || type == 2) {
            var iframe = document.createElement("iframe");

            if (type == 1) {
                iframe.src = _applicationBaseUrl + '/ClaimsIn/DownloadClaimsIn?id=' + id;
            } else {
                iframe.src = _applicationBaseUrl + '/ClaimsIn/DownloadRTFReport?id=' + id;
            }

            // This makes the IFRAME invisible to the user.
            iframe.style.display = "none";

            // Add the IFRAME to the page.  This will trigger
            //   a request to GenerateFile now.
            document.body.appendChild(iframe);        
        } else if (type == 3) {
            window.open(_applicationBaseUrl + '/ClaimsIn/Submitted?id=' + id);
        }                       
    } else {
        alert("Invalid parameters");
    }
}