
//Old and/or retired code retained for reference purposes...



<!-- BC - TEST - Add an area for the download manager... -->
<div style="position:absolute; top: 12em; left: 1em;">
    <div style="position:relative; display: inline; float: left">
        <div id="downloadManager" style="position:relative; display:none; float: left; background-color: white">
            <div class="container">
                <div class="row">
                    <!-- word-wrap: break-word - Allow long words to break and wrap to next line.
                         white-space:pre-wrap - Preserve white space in browser.  Wrap text when necessary and on line breaks. -->
                    <table id="tblDownloadManager" class="table table-bordered table-hover" style="word-wrap: break-word; white-space:pre-wrap;">
                        <caption style="font-weight:bold; font-size:2em;">Download Manager</caption>
                        <thead>
                            <tr style="height: 3em;" class="active">
                                <th class="col-sm-1 text-center" style="vertical-align: middle;">Request ID</th>
                                <th class="col-sm-2 text-center" style="vertical-align: middle;">Current Status</th>
                                <th class="col-sm-3 text-center" style="vertical-align: middle;">Time Series URI</th>
                                <th class="col-sm-1 text-center" style="vertical-align: middle;"><input type="button" id="btnDownloadAll" value="Download All" /></th>
                                <th class="col-sm-2 text-center" style="vertical-align: middle;"><label><input type="checkbox" id="chkbxAutoDownload" />&nbsp;Auto Download?</label></th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                        <tfoot>
                            @*<tr>
                                    <td>Footer text goes here</td>
                                </tr>*@
                        </tfoot>
                    </table>
                </div>
            </div>
        </div>
        <div class="glyphicon glyphicon-hand-right expander2" title="Click to open the Download Manager..." style="font-size: 3em; float:left; margin-left: 0.5em;"></div>
    </div>
</div>


//show/hide download manager...
$('.expander2').on('click', function () {

    var effect = 'slide';
    var options = { 'direction': 'left' };
    var duration = 500; //milliseconds
    var complete = function () {
        var item = $('.expander2');

        if ($('#downloadManager').is(':visible')) {
            //alert("Visible!!");
            item.removeClass('glyphicon-hand-right');
            item.addClass('glyphicon-hand-left');

            item.attr('title', 'Click to close the Download Manager...');
        }
        else {
            //alert("NOT Visible!!");
            item.removeClass('glyphicon-hand-left');
            item.addClass('glyphicon-hand-right');

            item.attr('title', 'Click to open the Download Manager...');
        }
    };

    $('#downloadManager').toggle(effect, options, duration, complete);
});
