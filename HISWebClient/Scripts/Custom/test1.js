$(document).ready(function () {

    var table = $('#example').DataTable({

        "createdRow": function (row, data, index) {

            if (data[5].replace(/[\$,]/g, '') * 1 > 250000) {

                var s = $('td', row).eq(5).html();
                var d = $('td', row).eq(4).html();
                $('td', row).eq(5).html("<a href='#' id=" + d + " <span class='glyphicon glyphicon glyphicon-download-alt' aria-hidden='true'></span> </a>");
                $('td', row).eq(5).click(function () { test(d); return false; });
            }

        }

        //"columnDefs": [ {

        //    "targets": -1,

        //    "data": null,

        //    "defaultContent": "<a href='test(" + data[0] + ")' <span class='glyphicon glyphicon glyphicon-download-alt' aria-hidden='true'></span></a>"

        //} ]

    });
    $('#example tbody').on('click', 'button', function () {

        var data = table.row($(this).parents('tr')).data();

        alert(data[0] + "'s salary is: " + data[5]);

    });

    function test(d) {
        alert(d)
    }
});