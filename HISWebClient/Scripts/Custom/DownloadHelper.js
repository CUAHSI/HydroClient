   $().ready(function() {
        var intervalId;
        var i = 0;
        intervalId = setInterval(function () {
            i++;
            $.post("/Home/Progress", function (progress) {
                updateMonitor(status, progress, i);
            });

        }, 1000);

       
        function updateMonitor(status, progress, i) {
            if (i > 1)
            {
                clearInterval(intervalId);
                window.location = "/downloadtest.csv";               
            }
            else
            {
                $('#monitor').html(progress);
            }
        }
    })