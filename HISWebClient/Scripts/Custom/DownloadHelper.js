﻿   $().ready(function() {
        var intervalId;
        var i = 0;
        intervalId = setInterval(function () {
            i++;
            $.post("/Home/Progress", function (progress) {
                //if (progress >= 1000) {
                //    updateMonitor(taskId, "Completed");
                //    clearInterval(intervalId);
                //} else {
                updateMonitor(status, progress, i);
                //}
            });

        }, 1000);

       
        function updateMonitor(status, progress, i) {
            if (i > 1)
            {
                clearInterval(intervalId);
                //var url = "/Home/downloadFile"
                //$.ajax({
                //    type: "Post",
                //    url: url,
                //    type: "text",
                //    success: function (data) {
                //        alert(data)
                //        window.location = s;
                //    },
                //    error: function()
                //    {
                //        alert("FAIL")
                //    }

                //});


                //var s = $.post("/Home/downloadFile",null,"text");
                window.location = "/downloadtest.csv";
                //$('#monitor').html("done");
               
            }
            else
            {
                $('#monitor').html(progress);
            }
        }
    })