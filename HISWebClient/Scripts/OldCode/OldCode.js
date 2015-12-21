
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


    <!-- Modal dialog for Download Manager... -->
        <div class="modal fade" id="dmModal" tabindex="-1" role="dialog" aria-labelledby="dmModalLabel" aria-hidden="true" style="z-index: 2000000000000000;">
            <div class="modal-dialog modal-dialog-full" role="document">
                <div class="modal-content modal-content-full">
                    <div class="modal-header">
                        <button type="button" class="close" data-dismiss="modal" ariahidden="true">&times;</button>
                        <label style="float:right; margin-right: 2em"><input type="checkbox" id="chkbxAutoDownload" checked="checked" />&nbsp;Auto Download Archives?</label>
                        <h3 class="modal-title" id="myModalLabel">Download Manager</h3>
                    </div>
                    <div class="modal-body modal-body-full row">

                        <table id="tblDownloadManagerHeaders" class="table">
                            <thead>
                                <tr>
                                    <th class="text-center" style="display: none; vertical-align: middle;">Request ID</th>
                                    <th class="text-center" style="width: 15%; vertical-align: middle;">
                                        <label class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">Task Id</label>
                                    </th>
                                    <th class="text-center" style="width: 25%; vertical-align: middle;">
                                        <label class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">Processing Status</label>
                                    </th>
                                    <th class="text-center" style="width: 40%; vertical-align: middle;">
                                        <label class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">Time Series Archive</label>
                                    </th>
                                    <th class="text-center" style="width: 20%; vertical-align: middle !important;">
                                        <input type="button" class="btn btn-primary text-center" id="btnDownloadAll" style="font-size: 1em; margin: 0 auto;" value="Download All Archives" />
                                    </th>
                                </tr>
                            </thead>
                        </table>
                        <div class="div-table-content">
                            <!--BCC - Try a dynamically sized font here...
                                 Source: http://stackoverflow.com/questions/14537611/bootstrap-responsive-text-size
                            -->
                            @*<table id="tblDownloadManager" class="table table-hover table-fixed" style="font-size: 1.5vmin; margin: 0 auto;">*@
                            @*<table id="tblDownloadManager" class="table table-hover table-fixed" style="font-size: 2.0vmin;">*@
                            <table id="tblDownloadManager" class="table table-hover" style="font-size: 2.0vmin;">
                                <tbody></tbody>
                                <tfoot>
                                    <tr>
                                        @*<td>Footer text goes here</td>*@
                                    </tr>
                                </tfoot>
                            </table>
                        </div>
                    </div>
                    <div class="modal-footer">
                        @*<p>This is the modal footer!!</p>*@
                    </div>
                </div>
            </div>
        </div>


            <!-- Modal dialog for Data Manager... -->
    <div class="modal fade" id="datamgrModal" tabindex="-1" role="dialog" aria-labelledby="datamgrModalLabel" aria-hidden="true" style="z-index: 2000000000000000;">
        <div class="modal-dialog modal-dialog-full" role="document">
            <div class="modal-content modal-content-full">
                <div class="modal-header">
                    <button type="button" class="close" data-dismiss="modal" ariahidden="true">&times;</button>
                    <a href="" id="seriesModal-DM" data-toggle="modal" data-target="#dmModal" style="float: right; margin-right: 2em;"><span class="glyphicon glyphicon-cloud-download"></span> Download Manager</a>
                    <h3 class="modal-title" id="myModalLabel">Data Manager</h3>
                </div>
                <div class="modal-body modal-body-full row">
                    @* BCC- 24-Sep-2015 - To ensure the horizontal scroll bar appears in Apple Safari running under OS X, set 'max-width: none' on the table element!!
                        NOTE: This 'more specific' setting overrides 'max-width: 100%' as set for the <table> element in bootstrap.css (line 1407)
                        Source: http://stackoverflow.com/questions/30500857/overflow-xauto-not-working-in-safari-and-firefox
                    *@
                    <table id="tblDataManager" class="display disable-selection" cellspacing="0" style="max-width: none;">
                        <thead>
                            <tr>
                                @Html.Partial("_DataManagerTimeseriesColumns")
                            </tr>
                        </thead>
                        <tfoot>
                            <tr>
                                @Html.Partial("_DataManagerTimeseriesColumns")
                            </tr>
                        </tfoot>
                    </table>
                </div>
                <div class="modal-footer">
                    @*<p>This is the modal footer!!</p>*@
                </div>
            </div>
        </div>
    </div>

