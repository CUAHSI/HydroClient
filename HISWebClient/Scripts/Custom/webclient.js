var map;
var clusteredMarkersArray = [];
var areaRect;
var clusterMarkerPath = "/Content/Images/Markers/ClusterIcons/";
var markerPath = "/Content/Images/Markers/Services/";
var infoWindow;
var myTimeSeriesList;
var myTimeSeriesClusterDatatable;
var myServicesList;
var myServicesDatatable;

var mySelectedServices = [];
var mySavedServices = [];
var bObservedValuesOnly = false;
var mySelectedTimeSeries = [];
var sessionGuid;
var randomId;
var currentPlaceName = '';
var currentMarkerPlaceName = '';
var timeSeriesRequestStatus;
var timeSeriesFormat;
var slider;

var conceptsType = '';

var selectedRowCounts = {
    'dtMarkers': {'targetIds': ['spanClearSelections', 'spanZipSelections','spanManageSelections', 'spanCombinedFiles', 'spanSeparateFiles'], 'count': 0, 'customCounter': customCounter},
    'dtTimeseries': { 'targetIds': ['spanClearSelectionsTS', 'spanZipSelectionsTS', 'spanManageSelectionsTS', 'spanCombinedFilesTS', 'spanSeparateFilesTS'], 'count': 0, 'customCounter': customCounter },
    'tblDataManager': { 'targetIds': ['spanClearSelectionsDM', 'spanRemoveSelectionsDM'], 'count': 0, 'customCounter': customCounter }
};

var targetIdTexts = {
    'spanClearSelections' : { 'none': ' Clear selection(s)',
                              'one': ' Clear selection',
                              'many': ' Clear @@count@@ selections'
                            },
    'spanClearSelectionsDM' : { 'none': ' Clear selection(s)',
                                'one': ' Clear selection',
                                'many': ' Clear @@count@@ selections'
                            },
    'spanClearSelectionsTS' : { 'none': ' Clear selection(s)',
                                'one': ' Clear selection',
                                'many': ' Clear @@count@@ selections'
                            },
    'spanCombinedFiles' : { 'none': ' Export selections in a combined file',
                            'one': ' Export selection in a combined file',
                            'many': ' Export @@count@@ selections in a combined file'
                          },
    'spanCombinedFilesTS': {
                             'none': ' Export selection(s) in a combined file',
                             'one': ' Export selection in a combined file',
                             'many': ' Export @@count@@ selections in a combined file'
                            },
    'spanManageSelections' : { 'none': ' Save selection(s) to workspace',
                               'one': ' Save selection to workspace',
                               'many': ' Save @@count@@ selections to workspace'
                             },
    'spanManageSelectionsTS' : { 'none': ' Save selection(s) to workspace',
                                 'one': ' Save selection to workspace',
                                 'many': ' Save @@count@@ selections to workspace'
                             },
    'spanRemoveSelectionsDM' : { 'none': ' Delete selection(s)',
                                 'one': ' Delete selection',
                                 'many': ' Delete @@count@@ selections'
                            },
    'spanSeparateFiles' : { 'none': ' Export each selection in a separate file',
                            'one':  ' Export selection in a separate file',
                            'many': ' Export each (of @@count@@) selections in a separate file'
                          },
    'spanSeparateFilesTS' : { 'none': ' Export each selection in a separate file',
                              'one':  ' Export selection in a separate file',
                              'many': ' Export each (of @@count@@) selections in a separate file'
                          },
    'spanZipSelections' : { 'none': ' Export selection(s)',
                            'one': ' Export selection',
                            'many': ' Export @@count@@ selections'
                          },
    'spanZipSelectionsTS' : { 'none': ' Export selection(s)',
                              'one': ' Export selection',
                              'many': ' Export @@count@@ selections'
                          }
};

var downloadMonitor = { 'intervalId' : null,
                        'timeSeriesMonitored': {}
                      };

var labelMonitorIntervalId = null;

var unknownTaskCounts = {};

//list of services that only have 
var ArrayOfNonObservedServices = ["84","187","189","226","262","267","274"]

var taskCount = 0;

var byuAppsList = {};

var currentUser = { 'authenticated' : false,
                    'dataManagerLoaded' : false,
                    'exportsLoaded' : false,
                    'login' : null,
                    'userName' : null
                  };

var currentPosition = {
                        'latLng': null,
                        'update': true
                      };

var bResetMap = false;


var currentFilters = {
                        'timeout': null,
                        'filters': {
                                     'search': null,
                                     'filters': []
                                   }
                     };

//Array of open InfoBubbles...
var openInfoBubbles = [];

//Number.isInteger 'polyfill' for use with browsers (like IE) which do not support the 'native' function
//Sources: http://stackoverflow.com/questions/31720269/internet-explorer-11-object-doesnt-support-property-or-method-isinteger
//         https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/isInteger 
Number.isInteger = Number.isInteger || function (value) {
    return typeof value === "number" &&
           isFinite(value) &&
           Math.floor(value) === value;
};


//Clear the download and label monitors...
function clearMonitors() {

    var monitors = [ downloadMonitor.intervalId, labelMonitorIntervalId];

    while (0 < monitors.length) {
    
        var monitor = monitors.pop();
        if ( null !== monitor) {
            clearInterval(monitor);
            monitor = null;
        }
    }

    monitors = null;
}

$(document).ready(function() {

//    console.log('document ready called!!');

    $(window).on('load', function() {

//        console.log('window is loaded!!');

    $("#pageloaddiv").hide();
    initialize();

    initializeMap();

    //Periodically check selected time series...
    labelMonitorIntervalId = setInterval(function () {
        //console.log('Checking selected time series...');

        //Enable/disable 'Apply Filter to Map' checkbox, per user search and filter entries...
        var criteria = retrieveSearchAndFilterCriteria('dtTimeseries', false);

        var chkbx = $('#' + 'chkbxApplyFilterToMapTS');
        var className = 'disabled';
        if ('' === criteria.Search && 0 >= criteria.filters.length) {
            //No search or filter entries - disable checkbox...

            //chkbx.prop('checked', false);
            //chkbx.addClass(className);
        }
        else {
            //Search and/or filter entries exist - enable checkbox...
            chkbx.removeClass(className);            
        }

        //For each tableId in selected row counts...
        for (var tableId in selectedRowCounts) {
            if ($('#' + tableId).is(":visible")) {
                //table visible - evaluate count
                var count = selectedRowCounts[tableId].count;

                //Update control(s) text with current count...
                var length = selectedRowCounts[tableId].targetIds.length;
                for (var tI = 0; tI < length; ++tI) {
                    //Determine control type and associated jQuery method to retrieve text...
                    var targetId = selectedRowCounts[tableId].targetIds[tI];
                    var control = $('#' + targetId);
                    var method = null;

                    if (control.is('input[type="button"]')) {
                        method = control.val;
                    }
                    else {
                        if (control.is('label') || control.is('span')) {
                            method = control.text;
                        }
                    }

                    if (null === method) {
                        continue;
                    }

                    //Retrieve texts for the current targetId
                    var texts = targetIdTexts[targetId];

                    //Retrieve text per the count value
                    var text = '';
                    switch (count) {
                        case 0:
                            text = texts.none;
                            break;
                        case 1: 
                            text = texts.one;
                            break;
                        default:
                            text = texts.many;
                            break;
                    }

                    if ('' !== text) {
                        //Text retrieved - substitute count in text, if indicated
                        var outText = text;
                        if ( -1 !== text.indexOf('@@count@@')) {
                    
                            var txts = text.split('@@count@@');
                            outText = txts[0] + count.toString() + txts[1];
                        }
                        
                        //Update targetId text...
                        method.call(control, outText);
                    }
                }

                if ('undefined' !== typeof selectedRowCounts[tableId].customCounter) {
                    //Custom counter defined - call...
                    selectedRowCounts[tableId].customCounter(tableId);
                }
            }
        }
    }, 250);

    });

});

function customCounter(tableName) {
    
    //console.log('customCounter called for: ' + tableName);
    var countClear = 0;
    var countLaunch = 0;
    var countRemove = 0;
    var countSelectable = 0;
    var countSave = 0;

    if ('tblDataManager' === tableName) {
        //Initialize counts...
        countClear = 0;
        countLaunch = 0;
        countRemove = 0;
        countSelectable = 0;
        countSave = 0;

        //Retrieve data for all table rows... 
        var table = $('#' + tableName).DataTable();
        var rows = table.rows();
        var nodes = rows.nodes();
        var data = rows.data();

        //For each instance...
        $.each(nodes, function (i, obj) {
            //Retrieve node, check for class: selected
            var jqueryObj = $(obj);

            if (jqueryObj.hasClass('selected')) {
                //Row is selected...
                var dataItem = data[i];

                //Update counts...
                ++countClear;
                ++countRemove;

                if ( ! dataItem.Saved) {
                    ++countSave;
                }

                if ('true' !== $('#ddIntegratedDataTools').attr('data-noneselected')) {
                    ++countLaunch;
                }
            }
            else {
                //Row is NOT selected...
                ++countSelectable;
            }
        });

        //Update button text with counts...
        var buttons = [{ 'name': 'btnLaunchHydrodataToolDataMgr', 'count': countLaunch, 'disableCheck': btnDisableCheck }];
        var bLength = buttons.length;

        for (var i = 0; i < bLength; ++i) {
            var button = $('#' + buttons[i].name); 
            var value = button.val();
            var count = buttons[i].count;
            //var values = value.split(' ');
            var values = value.split(':');

            if (0 >= count) {
                button.val(values[0]);
                button.prop('disabled', true);
            }
            else {
                button.val(values[0] + ': (' + count.toString() + ')');
                button.prop('disabled', null !== buttons[i].disableCheck ? buttons[i].disableCheck(buttons[i].name)  : false); 
            }
        }

        //Update dropdown items per counts...
        var anchors = [{'name': 'anchorAllSelectionsDataMgr', 'count': countSelectable},
                       { 'name': 'anchorClearSelectionsDataMgr', 'count': countClear},
                       { 'name': 'anchorRemoveSelectionsDataMgr', 'count': countRemove},
                       { 'name': 'anchorSaveSelectionsDataMgr', 'count': countSave} ];
        var aLength = anchors.length;

        for (var ii = 0; ii < aLength; ++ii) {
            var anchor = $('#' + anchors[ii].name); 
            var li = anchor.parent();
            var count = anchors[ii].count;

            if (0 >= count) {
                li.addClass('disabled');
            }
            else {
                li.removeClass('disabled'); 
            }
        }
    }
    else if ('dtTimeseries' === tableName || 'dtMarkers' === tableName) {
        //Initialize counts...
        countClear = 0;
        countSelectable = 0;

        //Retrieve data for all table rows... 
        var table = $('#' + tableName).DataTable();

        //Since table: dtTimeseries is configured as 'deferRender': true, the table loads data
        // one displayed page at a time.  Thus, if the user has viewed only three pages of a 
        // five page table, only three pages of rows are contained in the table. Yet the user
        // can 'select all rows' for such tables, even when they have not viewed all table pages.
        //To find the total number of selected rows for such tables, refer to the selectedRowCounts array...
        var selectedRows = selectedRowCounts[tableName].count;

        countClear = selectedRows;

        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        var totalRows = rows[0].length;

        countSelectable = totalRows - selectedRows;

        //Update dropdown items per counts...
        var anchors = [];

        if ('dtTimeseries' === tableName) { 
            anchors = [{ 'name': 'anchorAllSelectionsTS', 'count': countSelectable},
                       { 'name': 'anchorClearSelectionsTS', 'count': countClear},
                       { 'name': 'anchorAddSelectionsToWorkspaceTS', 'count': countClear},
                       { 'name': 'anchorAllTimeseriesInOneFileTS', 'count': countClear},
                       { 'name': 'anchorEachTimeseriesInSeparateFileTS', 'count': countClear}];
        } else if ('dtMarkers' === tableName) {
            anchors = [{ 'name': 'anchorAllSelections', 'count': countSelectable},
                       { 'name': 'anchorClearSelections', 'count': countClear},
                       { 'name': 'anchorAddSelectionsToWorkspace', 'count': countClear},
                       { 'name': 'anchorAllTimeseriesInOneFile', 'count': countClear},
                       { 'name': 'anchorEachTimeseriesInSeparateFile', 'count': countClear}];        
        }

        var aLength = anchors.length;

        for (var ii = 0; ii < aLength; ++ii) {
            var anchor = $('#' + anchors[ii].name); 
            var li = anchor.parent();
            var count = anchors[ii].count;

            if (0 >= count) {
                li.addClass('disabled');
            }
            else {
                li.removeClass('disabled'); 
            }
        }
    }
}


//Format the input date string for output in a DataTables instance
function formatDate(data, bIncludeTime) {

    //Validate/initialize input parameters
    var date = '';
    if ( 'undefined' === typeof data || null === data) {
        return date;
    }

    var dateString = data.trim();
    if ( 0 >= dateString.length ) {
        return date;
    }

    //Check dateString format...
    if ( '/Date(' === dateString.substr(0,6)) {
        //JSON format - convert to JavaScript date as explained here: http://blog.degree.no/2012/10/converting-json-date-string-to-javascript-date-object/
       date = new Date(parseInt(dateString.substr(6)));
    }
    else {
        //Assume NewtonSoft format: YYYY-MM-DDTHH:MM:SS - convert to JavaScript date
      if ( ! isNaN(Date.parse(dateString))) {
         // Success - create new Date instance
         date = new Date(dateString);
      }
      else {
        //Failure - Some browsers may return NaN on Date.parse() calls, as explained here:
        //Source: http://stackoverflow.com/questions/6427204/date-parsing-in-javascript-is-different-between-safari-and-chrome

        //Attempt to split the string...
        var parts = dateString.split(/[^0-9]/);

        if (('undefined' !== typeof parts) && (null !== parts) && (3 <= parts.length)) 
        {
            //Success - date string contains at least 'year', 'month' and 'day' values
            date = new Date(parts[0],                                               //Year
                            parts[1] - 1,                                           //Month
                            parts[2],                                               //Day
                            'undefined' !== typeof parts[3] ? parts[3] : '00',      //Hour
                            'undefined' !== typeof parts[4] ? parts[4] : '00',      //Minute
                            'undefined' !== typeof parts[5] ? parts[5] : '00',      //Second
                            'undefined' !== typeof parts[6] ? parts[6] : '000');    //Millisecond
        }
      }
    }

    if (date instanceof Date) {
        //Successful conversion to Date type
        if ('undefined' !== typeof bIncludeTime && null !== bIncludeTime && bIncludeTime) {
            //Return formatted year-month-day hh:mm:ss
            return (date.getFullYear().toString() + '-' + 
                    ('0' + (date.getMonth() + 1)).toString().slice(-2) + '-' + 
                    ('0' + date.getDate()).toString().slice(-2) + ' ' +
                    ('0' + date.getHours()).toString().slice(-2) + ':' + 
                    ('0' + date.getMinutes()).toString().slice(-2) + ':' + 
                    ('0' + date.getSeconds()).toString().slice(-2));        
        }
        else {
            //Return formatted year-month-day
            return (date.getFullYear().toString() + '-' + 
                    ('0' + (date.getMonth() + 1)).toString().slice(-2) + '-' + 
                    ('0' + date.getDate()).toString().slice(-2));
        }
    }

    return date;
}

//Format the input service title...
function formatServiceTitle( serviceCode, serviceTitle) {
    
    return '<img style="height: 2em; width: 2em;" src="/home/getIcon?id=' + serviceCode + '">' + ' - ' + serviceTitle;
}

//Format the input service title...
function formatServiceTitleAbbreviated( serviceCode, serviceTitle) {
    
    var markup = '<div  class="abbreviate-long-text">' + 
                 '<img style="height: 2em; width: 2em;" src="/home/getIcon?id=' + serviceCode + '">' + ' - ' + serviceTitle +
                 '</div>'

    return markup;

}

//Datatables 'render' function to abbreviate long text...
//ASSUMPTION: Input data is displayed 'as is' in a div
function renderAbbreviatedText(data, type, full, meta) {

    var text = (('undefined' !== typeof data ) && (null !== data)) ? data.toString() : 'unknown';
    var markup = '<div  class="abbreviate-long-text">' + text + '</div>';
    return markup;
}

//Datatables 'createdCell' function to display a tooltip...
//ASSUMPTION: Cell text is displayed 'as is' on mouseenter/mouseleave (formerly known as 'hover'...)
function createdTooltipText(cell, cellData, rowData, rowIndex, colIndex) {

    var jqueryObj = $(cell);

    //Create a tooltip for the cell text...
    //Source: http://chadkuehn.com/show-tooltip-over-truncated-text/
    //Setting tooltip width...
    //Source: http://stackoverflow.com/questions/25246391/bootstrap-tool-tip-setting-different-widths-for-different-tooltips
    jqueryObj.on('mouseenter', function(event) {
        var childDiv = jqueryObj.children()[0];
        if ((childDiv.offsetWidth < childDiv.scrollWidth) && ((! jqueryObj.attr('title')) || (0 >= jqueryObj.attr('title').length))) { //Element's content is wider than the content area - create a tooltip...
            jqueryObj.tooltip({
                'title': jqueryObj.text(),
                'trigger': 'hover',
                'placement': 'auto',
                'template': '<div class="tooltip" role="tooltip"><div class="tooltip-arrow"></div><div style="text-align: justify; text-justify: distribute" class="tooltip-inner"></div></div>',
                'container': 'body' //MUST use container option in datatables... source: http://stackoverflow.com/questions/33858135/bootstrap-popover-overlay-by-datatables-jquery
            });
            jqueryObj.tooltip('show');
        }
    });

    jqueryObj.on('mouseleave', function(event) {
        jqueryObj.tooltip('destroy');
    });
}

function initialize() {

    //init list od datatables for modal 
    myServicesList = setupServices();

    //Assign click handlers to panel date inputs
    $('#startDate').on('click', function () { $('#btnDateRange').trigger('click', {'clicked': 'startDate'}); });
    $('#endDate').on('click', function () { $('#btnDateRange').trigger( 'click', {'clicked': 'endDate'}); });

    //Initialize multiple datepicker instances on different input ids...
    $('#startDateModal').datepicker({ 'todayHighlight': true });
    $('#endDateModal').datepicker({ 'todayHighlight': true });

    //Set start and end of date range...
    var initDate = new Date();

    initializeDate(initDate, 'endDate');

    //Calculate start date as one year earlier...
    //NOTE: For an end date === 29-Feb-<YYYY>, the resulting start date === 01-Mar-<YYYY - 1>
    //      Since setFullYear() returns a number, not a Date object, the setFullYear(...) expression cannot passed to initializeDate(...) directly!!
    initDate.setFullYear(initDate.getFullYear() - 1);
    initializeDate(initDate, 'startDate');

    //Assign date validation handlers...
    $('#startDateModal').on('blur', { 'groupId': 'grpStartDateModal', 'errorLabelId': 'lblStartDateErrorModal' }, validateDateString);
    $('#endDateModal').on('blur', { 'groupId': 'grpEndDateModal', 'errorLabelId': 'lblEndDateErrorModal' }, validateDateString);


    //Assign future date checks...
    $('#startDateModal, #endDateModal').on('blur', { 'groupId': 'grpStartDateModal', 'errorLabelId': 'lblStartDateErrorModal', 'inputIdStartDate': 'startDateModal', 'inputIdEndDate': 'endDateModal' }, compareFromDateAndToDate);

    //Button click handler for Select Date Range...
    $('#btnDateRange').on('click', function (event, clicked) {
        //Assign current start and end date values to their modal counterparts...
        var startDate = $('#startDate').val();
        var endDate = $('#endDate').val();

        //Assign start and end date values to inputs...
        $('#startDateModal').val(startDate);
        $('#endDateModal').val(endDate);

        //Validate start and end date values...
        $('#startDateModal').triggerHandler('blur');
        $('#endDateModal').triggerHandler('blur');

        //Assign current start and end date values to the associated datepicker instances
        $('#startDateModal').datepicker('setDate', startDate);
        $('#endDateModal').datepicker('setDate', endDate);

        if ('undefined' === typeof clicked || null === clicked ) {
            return;     //No 'clicked' indicator - return early... 
        } 

        setTimeout(function() {
            //Set focus to input control, as indicated
            if ( 'startDate' === clicked.clicked) {
                $('#startDateModal').focus();
            }
            else {
                if ('endDate' === clicked.clicked) {
                    $('#endDateModal').focus();
                }
            }
        }, 500);
    });

    //Button click handler for Date Range Modal Save 
    $('#btnDateRangeModalSave').on('click', function (event) {

        //Simulate 'blurs' to start and end date inputs to invoke date validation checking...
        $('#startDateModal').blur();
        $('#endDateModal').blur();

        //Check for date validation errors...
        if ($('#startDateModal').hasClass('has-error') || $('#endDateModal').hasClass('has-error')) {
            return false;   //Validation error(s) - DO NOT close the dialog...
        }

        //Assign current modal start and end date values to their main page counterparts...
        $('#startDate').val($('#startDateModal').val());

        $('#endDate').val($('#endDateModal').val());
    });

    //initialize show/hide for search box
    $('.expander').on('click', function () {
        $('#selectors').slideToggle();
    });


    //allocate a RandomId instance...
    randomId = new RandomId({
        'iterationCount': 10,
        'characterSets': ['alpha', 'numeric']
    });

    //Allocate a TimeSeriesRequestStatus instance...
    timeSeriesRequestStatus = (new TimeSeriesRequestStatus()).getEnum();

    var steInt = timeSeriesRequestStatus.EndTaskError;

    var steObj = timeSeriesRequestStatus.properties[steInt];

    var desc  = steObj.description;

    //Allocate a TimeSeriesFormat instance...
    timeSeriesFormat = (new TimeSeriesFormat()).getEnum();

    //Download All button click handler..
    $('#btnDownloadAll').on('click', function (event) {
        //For each zip blob download button element...
        $("tr > td > button.zipBlobDownload").each(function (index) {
            //Initiate individual file downloads at three second intervals
            var buttonThis = this;
            setTimeout(function () {
                $(buttonThis).click();
            }, (3000 * index));
        });
    });

    $('#btnTopSelect').click(function () {

        //updateKeywordList("Common");       
        updateKeywordList();

        //Check current search parameters...
        enableSearch();

    });

    $('#btnHierarchySelect').click(function () {

        updateKeywordList();

        //Check current search parameters...
        enableSearch();
    });

    $("input[name='checkOnlyObservedValues']").change(function (e) {

        var rows = $("#dtServices").dataTable().fnGetNodes();
        //reset global array
        mySelectedServices = [];

        if ($("input[name='checkOnlyObservedValues']").is(':checked')) {


            for (var i = 0; i < rows.length; i++) {
                // Get HTML of 3rd column (for example)
                //get value from (network)id field 
                var id = $(rows[i]).find("td:eq(5)").html();
                //check if value in array of non observed services
                if ($.inArray(id, ArrayOfNonObservedServices) == -1) {

                    //add
                    mySelectedServices.push(id);
                    //mark with select class
                    $(rows[i]).addClass('selected');
                }
            }
        }
        else {
            for (var i = 0; i < rows.length; i++) {
                // Get HTML of 3rd column (for example)

                //check if value in array of non observed services



                //mark with select class
                $(rows[i]).removeClass('selected');

            }
        }

        //Enable 'Clear Selections' button, if indicated...
        if (0 < mySelectedServices.length) {
            $('#btnClearSelectionsDS').prop('disabled', false);
        }
        else {
            $('#btnClearSelectionsDS').prop('disabled', true);
        }

    });

    //Submit function for 'Search Map' button - see index.cshtml searchBtnDiv...
    $("#Search").submit(function (e) {

        resetMap();
        //prevent Default functionality
        e.preventDefault();
        e.stopImmediatePropagation();
        var path = [];
        path = GetPathForBounds(map.getBounds())

        //Update the map per the new search parameters...
        updateMap(true);

        if ( 'undefined' !== typeof areaRect && null !== areaRect) {
            //Remove any previously assigned listeners...
            google.maps.event.clearInstanceListeners(areaRect);
        }

        // Construct the polygon.
        areaRect = new google.maps.Polygon({
            paths: path,
            strokeColor: '#FF0000',
            strokeOpacity: 0.5,
            strokeWeight: 1,
            fillColor: '#F8F8F8',
            fillOpacity: 0
        });

        areaRect.setMap(map);
        var areaLeft = google.maps.geometry.spherical.computeArea(areaRect.getPath());

        //Add mousemove listener for latitude/longitude reporting...
        google.maps.event.addListener(areaRect, 'mousemove', updateLatLng);

        //NOTE: BCC - 21-Sep-2015 - Per review meeting, the following current place name logic is not used.
        //                          However, the logic is retained for possible future use...
        //Determine the center point of the polygon...
        //ASSUMPTION: The polygon is always a rectangle, otherwise a 'centroid' calculation is required...
        //Source: http://stackoverflow.com/questions/5187806/trying-to-get-the-cente-lat-lon-of-a-polygon - Answer 0
        //        http://jsfiddle.net/argiropoulostauros/cQLRr/
        var bounds = new google.maps.LatLngBounds();
        var pLength = path.length;

        for (var i = 0; i < pLength; ++i) {
            bounds.extend(path[i]);
        }

        var centerLatLng = bounds.getCenter();
        var roundedLat = Math.round(centerLatLng.lat() * 1000) / 1000;      //Round to three decimal places...
        var roundedLng = Math.round(centerLatLng.lng() * 1000) / 1000;     //Round to three decimal places...

        //Set the current place name...
        currentPlaceName = 'lat: ' + Math.abs(roundedLat) + '\u00b0' + (0 < roundedLat ? ' N' : ' S') + ', long: ' + Math.abs(roundedLng) + '\u00b0' + (0 < roundedLng ? ' E' : ' W');
        //console.log(currentPlaceName);

        //BCC - 21-Sep-2015 - RESET current place name
        currentPlaceName = 'Selected Area';
    });

    //BCC - 02-Jul-2015 - External QA Issue #48 
    $("#btnSetDefaults").on('click', function () {
        //Remove all map markers, if any...
        resetMap();

        //reset search parameters...
        resetSearchParameters();

        //Check current search parameters...
        enableSearch();

    });

    //Click event for search results button...
    $('#' + 'btnSearchResults').on('click', function(event) {

        var chkbx = $('#' + 'chkbxApplyFilterToMapTS')
        if ( (! chkbx.prop('checked')) || chkbx.hasClass('disabled') || bResetMap) {
            //'Apply to map' NOT engaged --OR-- disabled - --OR-- map reset, initialize Data pane...
            
            if (bResetMap) {
                bResetMap = false;
            }

            //Reset the current marker place name...
            currentMarkerPlaceName = '';

            setUpTimeseriesDatatable();
            var table = $('#dtTimeseries').DataTable();

            //BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
            //Assign a handler to the DataTable 'draw.dt' event
            table.off('draw.dt', addFilterPlaceholders);
            table.on('draw.dt', { 'tableId': 'dtTimeseries', 'placeHolders': ['Publisher', 'Service Title', 'Keyword', 'Site Name', 'Data Type', 'Value Type', 'Sample Medium'] }, addFilterPlaceholders);

            //BCC - 09-Jul-2015 -  - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
            //Remove/add column adjustment event handler...
            $('#tableModal').off('shown.bs.modal', adjustColumns);
            $('#tableModal').on('shown.bs.modal', {
                'tableId': 'dtTimeseries', 'containerId': 'dtTimeSeriesModal'
            }, adjustColumns);

            table.order([0, 'asc']).draw();
        }
    
    });

    //click event for tab
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {

        if ('#dataMgrPane' === e.target.hash ||         //tabbed panes - 'Data' tab clicked --OR--
            ('#tabbedDataMgr' === e.target.hash &&      //navigation tabs - 'Workspace' tab clicked --AND--
             $('#liDataMgrPane').hasClass('active'))) { //tabbed panes - 'Data' tab active

            //Whenever the tab containing the data manager table is shown, adjust the table's columns and redraw to ensure columns are properly spaced...
            var tableName = 'tblDataManager';
            var tableId = '#' + tableName;
            var jQueryDataTable = $(tableId).DataTable();

            jQueryDataTable.columns.adjust();
            jQueryDataTable.draw();
        }

        // activated tab
    });

    //click event for 'top level' nav buttons...
    $('button[data-toggle="tab"]').on('shown.bs.tab', function (event) {

        //Adjust columns and draw table whenever the workspace tab appears...
        //NOTE: The draw() call appears effective only when the table is visible.
        //      The 'shown.bs.tab' event indicates the tab containing the table is visible...
        if ( 'tabbedDataMgrTab' == event.target.id) {
            var tableName = 'tblDataManager';
            var tableId = '#' + tableName;
            var jQueryDataTable = $(tableId).DataTable();

            jQueryDataTable.columns.adjust();
            jQueryDataTable.draw();        
        }
    });


    $('.enableWhenDataReceived').addClass('disabled');

    

    //disable 
    $('body').on('click', '.disabled', function (e) {
        e.preventDefault();
        return false;
    });

    //BCC - 26-Jun-2015 - QA Issue #20 - Select Keywords: selected keywords saved even popup were closed without user click Save
    //Click handler for 'Select Keyword(s)' button
    $('#btnSelectKeywords').on('click', clickSelectKeywords);

    $('#btnSelectDataServices').on('click', clickSelectDataServices);

    //Click handler for 'Common' keywords checkboxes
    $('input[name="keywords"]').on('click', clickCommonKeyword);

    //Shown and hidden handlers for Select Data Services...
    $('#SelectServicesModal').on('shown.bs.modal', shownSelectDataServices);
    $('#SelectServicesModal').on('hidden.bs.modal', hiddenSelectDataServices);

    //Initialize the Data Manager datatables instance...
    setupDataManagerTable();

    //Initialize static tooltips...
    setupToolTips();

    //Set up static tooltips on dialog open/close...
    var modals = ['tableModal', 'SeriesModal'];
    var moLength = modals.length;
    var moFunc = setupToolTips;

    for (var mi = 0; mi < moLength; ++mi) {
        var modal = '#' + modals[mi];
        var event = 'shown.bs.modal';
        $(modal).off(event, moFunc); 
        $(modal).on(event, moFunc); 

        event = 'hidden.bs.modal';
        $(modal).off(event, moFunc); 
        $(modal).on(event, moFunc); 
    }

    //Set an event handler on the Google form...
    $('#formGoogleLogin').on('submit', googleFormSubmit);

    //Set event handlers for map and workspace tabs...
    $('#' + 'liMapTab').on('click', function(event) {
        $(this).addClass('hidden');
        $('#' + 'liTabbedDataMgrTab').removeClass('hidden');
    });
     
    $('#' + 'liTabbedDataMgrTab').on('click', function(event) {
        $(this).addClass('hidden');
        $('#' + 'liMapTab').removeClass('hidden');
    });

    //Set event handler for 'Open Workspace' button(s)...
    var owsBtns = ['tableModal-DataMgr', 'tableModal-DownloadMgr', 'tableModal-DataMgrTS', 'tableModal-DownloadMgrTS'];
    var owsBtnsLength = owsBtns.length;

    for (var owsBtnsI = 0; owsBtnsI < owsBtnsLength; ++owsBtnsI) {
    
        $('#' + owsBtns[owsBtnsI]).on('click', function(event) {
    
            $('#tabbedDataMgrTab').tab('show');  //Show the tab...
        
            //Source: http://stackoverflow.com/questions/7862233/twitter-bootstrap-tabs-go-to-specific-tab-on-page-reload-or-hyperlink
            if ( -1 !== (event.target.id).indexOf('DataMgr')) { 
                $('.nav-tabs a[href="#dataMgrPane"]').tab('show');  //Show the pane...
            }
            else {
                $('.nav-tabs a[href="#downloadMgrPane"]').tab('show');  //Show the pane...            
            }

            $('#' + 'liTabbedDataMgrTab').triggerHandler('click');  //Toggle the Workspace button on the top bar...
        });
    
    }
    
    //Add click handlers for Google SignIn/SignOut...
    $('#' + 'btnSignIn').on('click', clearMonitors);
    $('#' + 'btnSignOut').on('click', clearMonitors);
 
    //Click handlers for 'Workspace' buttons...
    var wsButtonIds = ['tabbedDataMgrTab', 'tableModal-DataMgrTS', 'tableModal-DataMgr'];
    var wsbidsLength = wsButtonIds.length;

    for (var wsbidsI = 0; wsbidsI < wsbidsLength; ++wsbidsI) {
    
        var wsButton = $('#' + wsButtonIds[wsbidsI]);
        wsButton.off('click', adjustColumns);
        wsButton.on('click', {'tableId': 'tblDataManager'}, adjustColumns);
    }

    //Click handler for 'Search' button
    $('#' + 'mapTab').on('click', function(event) {    
        setTimeout(function() {
            resizeAndPositionMap(map, slider);
        }, 500);
    });
}

//BCC - 09-May-2016 - Separate all map initialization logic into a separate function...
function initializeMap() {

    var myCenter = new google.maps.LatLng(39, -92); //us
    //var myCenter = new google.maps.LatLng(42.3, -71);//boston
    // var myCenter = new google.maps.LatLng(41.7, -111.9);//Salt Lake

    //BC - disable infoWindow for now...
    //    infoWindow = new google.maps.InfoWindow();
   
    var mapProp = {
        center: myCenter,
        zoom: 5,
        draggable: true,
        scrollwheel: true,
        mapTypeId: google.maps.MapTypeId.TERRAIN,
        mapTypeControl: true,
        scaleControl: true,
        overviewMapControl: true,
        overviewMapControlOptions: {
            opened: true
        },
        zoomControl: true,
        panControl: false,
        zoomControlOptions: {
            //style: google.maps.ZoomControlStyle.SMALL,
            position: google.maps.ControlPosition.LEFT_TOP
        },
        mapTypeControlOptions: {
            style: google.maps.MapTypeControlStyle.DEFAULT,
            position: google.maps.ControlPosition.LEFT_BOTTOM,
            mapTypeIds: [google.maps.MapTypeId.ROADMAP, google.maps.MapTypeId.HYBRID, google.maps.MapTypeId.TERRAIN]
          
        }//,
    };
  
    //get reference to map sizing of window happens later to prevent gap
   var element = document.getElementById("map-canvas");
   if ( null !== element) {
    map = new google.maps.Map(element, mapProp);
   }
   else {
    console.log('map-canvas element is null!!');
    return;
   }
  
    //set map zoom limits...
    //map.setOptions({minZoom: 5, maxZoom: 1});

    //UI
   addCustomMapControls();

    slider = addSlider();
    //Add custom LAyer control for Map Overlays
    addLayerControl()

    addLocationSearch();

    //trigger update of map on these events
    google.maps.event.addListener(map, 'dblclick', function () {
        if ((clusteredMarkersArray.length > 0)) {
            //If a filter applied to the map, include the filter in the updateMap(...) call
            var criteria = retrieveSearchAndFilterCriteria('dtTimeseries', false);
            if ('' !== criteria.Search || 0 < criteria.filters.length) {
                //Filter criteria exists - be ready to apply criteria when you visit the server...
                updateMap(false, retrieveSearchAndFilterCriteria('dtTimeseries', true));
            }
            else {
                //No filter criteria - no need to apply criteria when you visit the server... 
                updateMap(false);        
            }
        }
    });

    google.maps.event.addListener(map, 'dragend', function () {
        if ((clusteredMarkersArray.length > 0)) {
            //If a filter applied to the map, include the filter in the updateMap(...) call
            var criteria = retrieveSearchAndFilterCriteria('dtTimeseries', false);
            if ('' !== criteria.Search || 0 < criteria.filters.length) {
                //Filter criteria exists - be ready to apply criteria when you visit the server...
                updateMap(false, retrieveSearchAndFilterCriteria('dtTimeseries', true));
            }
            else {
                //No filter criteria - no need to apply criteria when you visit the server... 
                updateMap(false);        
            }
        }
    });

    google.maps.event.addListener(map, 'zoom_changed', function () {

        if ((clusteredMarkersArray.length > 0)) {
            //If a filter applied to the map, include the filter in the updateMap(...) call
            var criteria = retrieveSearchAndFilterCriteria('dtTimeseries', false);
            if ('' !== criteria.Search || 0 < criteria.filters.length) {
                //Filter criteria exists - be ready to apply criteria when you visit the server...
                updateMap(false, retrieveSearchAndFilterCriteria('dtTimeseries', true));
            }
            else {
                //No filter criteria - no need to apply criteria when you visit the server... 
                updateMap(false);        
            }
        }
    });

    //added to load size on startup
    google.maps.event.addListener(map, 'idle', function () {

        var area = getMapAreaSize();
        var control = $("#MapAreaControl");

        control.attr('data-areasizeinsqkm', area);

        control = $('#' + 'spanAreaValue');

        control.text( area.toLocaleString());

        //Check current search parameters...
        enableSearch();

        //}
    });

    //Add mousemove listener for latitude/longitude reporting...
    google.maps.event.addListener(map, 'mousemove', updateLatLng);

    //Trigger mousemove to initialize latitude/longitude display...
    var mouseEvent = {};
    mouseEvent.latLng = myCenter;

    setTimeout( function() {
        google.maps.event.trigger(map, 'mousemove', mouseEvent);
    }, 1500);

    //On window resize, resize and position the map...
    //NOTE: A visible slider appears to distort window height and width values 
    //      under this user scenario:
    //       - connect browser to HydroClient website
    //       - click broswer size button to maximize browser window
    //       - click browser size button to return browser window to original size
    //          here values returned by getMapHeight() and getMapWidth(...) are smaller than original values...
    google.maps.event.addDomListener(window, "resize", function () {
        //console.log('window resize event...');

        //Retain current slider visibility...
        var visible = isSliderVisible(slider);

        //Hide slider, if indicated
        if (visible) {
            slider.slideReveal('hide');    
        }

        //establish new map div dimensions and position...
        resizeAndPositionMap(map, slider);
        
        //Always show the slider...
        slider.slideReveal('show');    
        if (! visible) {
            //Slider originally hidden - hide slider, set map div dimensions and position again...
            slider.slideReveal('hide');    
            resizeAndPositionMap(map, slider);
        }
    });

    resizeAndPositionMap(map, slider);
 
    setTimeout( function() {
        slider.slideReveal('show');
    }, 1000);
}

//Update the displayed latitude and longitude values per the updated currentPosition value...
function updateLatLng(event) {

    if ( ! currentPosition.update) {
        return; //Position updating disabled - return early
    }

    //Update/display current position...
    currentPosition.latLng = event.latLng;
    $('#' + 'spanLatitudeValue').text(event.latLng.lat().toFixed(3).toString());
    $('#' + 'spanLongitudeValue').text(event.latLng.lng().toFixed(3).toString());
}

//Event handler for Google form submit...
function googleFormSubmit(event) {

    //console.log('googleFormSubmit called!!');

    //Check for remote logout...
    if ( 'undefined' !== typeof event.currentTarget.action && -1 !== event.currentTarget.action.indexOf('ExternalLogOut')) {
        //Remote logout - prevent default
        event.preventDefault();
    
        //Confirm user wants to log out of Google...
        var localeName = 'remoteLogoff';
        bootbox.addLocale(localeName, {'OK': 'Log out from Google',
                                       'CANCEL': 'Remain on Google',
                                       'CONFIRM': 'Log out from Google' });
        bootbox.setLocale(localeName);
        bootbox.confirm({ 'closeButton': false,
                          'message': 'You are logging out from the HydroClient.  Do you want to log out from Google?', 'callback': function(result) {
        
            if (result) {
                //Google logout confirmed - submit...
                event.target.submit();
            }
            else {
                //Google logout NOT confirmed - perform a local logout...
                event.currentTarget.action += '&localLogout=true';
                event.target.submit();
            }
          }});

        bootbox.removeLocale(localeName);
    }
}


//Check search conditions - enable/disable search button per findings...
//                          return - true (search enabled), false otherwise
function enableSearch() {

    //Retrieve current search area value, convert to numeric
    var area = $('#MapAreaControl').attr('data-areasizeinsqkm');
    area *= 1;  
    bSearchEnabled = true;  //Assume success...

    //Retrieve currently selected keywords
    // NOTE: Since keyword selections on 'Common' and 'Full' tabs are kept in sync at all times, 
    //          retrieve selected keys from 'Full' tab...
    var selectedKeys = [];
    var tree = $("#tree").fancytree("getTree");

    selectedKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes
        return node.title;
    });

    //Check search conditions, set tooltip if indicated...
    var tooltip = '';
    var allMax = 1000000;
    var max = 250000;
    if (area > allMax) {
        bSearchEnabled = false;
        tooltip = "Please limit search area to less than " + allMax.toLocaleString() + " sq km for 1 keyword or "+max.toLocaleString() + " for multiple keyword searches.";
    }
    else {
            
            if (area > max) {

                if ( 0 >= selectedKeys.length ) {
                    bSearchEnabled = false;
                    tooltip = "Please limit search area to less than " +max.toLocaleString() + " sq km or select only 1 keyword .";                
                }
                else if (1 < selectedKeys.length ) {
                    bSearchEnabled = false;
                    tooltip = "For the selected area, you chose more than one keyword. Please reduce the area or choose only one keyword.";                                    
                }

            }
    }


    //Destroy current tooltip, if any...
    var divTooltip = $('#searchBtnDiv');
    var btnSearch = $('form#Search button[type="submit"]');
    var areaControl = $('#MapAreaControl');
    var className = 'disabled';

    divTooltip.tooltip('destroy');

    areaControl.css('color', '');
    areaControl.css('background-color', '');
    if (bSearchEnabled) {
        //Search enabled - enable search button, 
        btnSearch.removeClass(className);
        //areaControl.css('border', '5px solid green');
        areaControl.removeClass('text-danger');
        areaControl.removeClass('alert-danger');
        areaControl.addClass('text-success')
        areaControl.addClass('alert-success');
    }
    else {
        //Search disabled - disable search button
        btnSearch.addClass(className);
        //areaControl.css('border', '5px solid red');
        areaControl.removeClass('text-success')
        areaControl.removeClass('alert-success');
        areaControl.addClass('text-danger');
        areaControl.addClass('alert-danger');
    
        if (0 < tooltip.length) {
            //Tooltip text exists - set tooltip
            divTooltip.tooltip({
                            'animation': true,
                            'placement': 'auto',
                            'trigger': 'hover',
                            'title': tooltip });
        }
    }

    //Add 'thumbs up' or 'thumbs down' to area display, as indicated...
    var spanArea = $('#' + 'spanAreaGlyphicon');
    spanArea.removeClass('glyphicon glyphicon-thumbs-up glyphicon-thumbs-down')
    spanArea.addClass('glyphicon ' + (bSearchEnabled ? 'glyphicon-thumbs-up' : 'glyphicon-thumbs-down'));
    spanArea.css( {'font-size': '1.5em',
                   'margin-right': '0.5em',
                   'vertical-align': 'middle'} );

    //Proceesing complete - return result
    return bSearchEnabled;
}


//Shown handler for Select Data Services...
function shownSelectDataServices() {
    $('#launcher').hide();
}

//Hidden handler for Select Data Services...
//NOTE: Since the 'olServices' list maintains a count of the selected data services, 
//      the mySavedServices array maintains the ids of those data services selected by
//      the user.
//
//      Since the 'Select Data Services' dialog is modal, we can save processing time by 
//      updating the mySaveServices array whenever the user closes the dialog:
//       - clicks the 'close' button
//       - clicks the 'x' in the upper right corner 
//       - presses the ESC key
//
function hiddenSelectDataServices() {
    $('#launcher').show();

    //Capture any changes in user selections...
    //Rebuild the saved services array...
    mySavedServices.length = 0;

    var rows = $("#dtServices").dataTable().fnGetNodes();
    var length = rows.length;

    for (var i = 0; i < length; ++i) {
        var row = $(rows[i]);

        if (row.hasClass('selected')) {
            mySavedServices.push(row.find('td:eq(5)').html());
        }
    }

    //Update the services list from the newely rebuilt saved services array...
    updateServicesList();

    //Retain the state of the 'Select all non-gridded...' checkbox
    bObservedValuesOnly = $('#checkOnlyObservedValues').prop('checked');
}

//Return all search parameters to initial state
function resetSearchParameters() {
    //Reset 'From' and 'To' Dates...
    var initDate = new Date();

    initializeDate(initDate, 'endDate');
    initDate.setFullYear(initDate.getFullYear() - 1);
    initializeDate(initDate, 'startDate');

    //Reset Keyword(s)...
    initializeKeywordList('Common');
    initializeKeywordList('Full');

    //Reset Data Service(s)...
    initializeDataServices();
}

//Initialize the input id to the current date
function initializeDate(date, inputId) {

    $('#' + inputId).val(('0' + (date.getMonth() + 1)).toString().slice(-2) + '/' + ('0' + date.getDate()).toString().slice(-2) + '/' + (date.getFullYear()).toString());
}

//Initialize the keyword list per the input list type
function initializeKeywordList(type) {

    if ('Common' === type) {
        //'Most Common' keywords...
        $("input[name='keywords']:checked").prop('checked', false);
        $("input[name='keywords']").prop("disabled", false);
    }
    else {
        if ('Full' === type) {
            //'Full' keywords...
            $("#tree").fancytree("getTree").visit(function (node) {
                node.setSelected(false);
                node.unselectable = false;
                node.hideCheckbox = false;
            });
        }
    }

    //Clear the concepts list
    var list = $('#olConcepts');

    list.empty();
    list.append('<li>All</li>');
}

//Initialize the data services table
function initializeDataServices() {

    //Uncheck gridded data services checkbox
    bObservedValuesOnly = false;
    $('#checkOnlyObservedValues').prop('checked', bObservedValuesOnly);

    //Unselect all table rows
    if ($.fn.DataTable.isDataTable('#dtServices')) {
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        var table = $('#dtServices').DataTable();
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        var nodesRendered = rows.nodes();                                    //Retrieve all the rendered nodes for these rows
        //NOTE: Rendered nodes retrieved in the same order as the rows...
        var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
        var className = 'selected';

        //Remove selected class from all rendered rows...
        jqueryObjects.removeClass(className);
    }

    //Clear the services list/reset global array
    mySelectedServices =[];
    mySavedServices = [];
    list = $('#olServices');

    list.empty();
    list.append('<li>All</li>');
}

    
//Validate the input date string - return true: valid, false otherwise
function validateDateString(event) {

    var jqueryObj = $(this);
    var dateString = jqueryObj.val();
    var jqueryGrpObj = $(('#' + event.data.groupId));
    var jqueryLabelObj = $(('#' + event.data.errorLabelId));

    var date = Date.parse(dateString);
    var dateArray = dateString.split('/');

    var month = 'undefined' === typeof dateArray[0] ? '' : dateArray[0]; 
    var date = 'undefined' === typeof dateArray[1] ? '' : dateArray[1];
    var year = 'undefined' === typeof dateArray[2] ? '' : dateArray[2];

    if ((! isNaN(date)) && (isValidDate(month, date, year))) {
        //Date string valid - remove error class, hide error message
        jqueryLabelObj.text('');
        jqueryLabelObj.hide();
        jqueryGrpObj.removeClass('has-error');
        jqueryObj.removeClass('has-error');
        jqueryObj.removeClass('invalid-date');
    }
    else {
        //Date string invalid - stop propagation of the event to ensure error message displays...
        event.stopImmediatePropagation();

        //Set error class, show error message
        jqueryLabelObj.text('Please enter a valid date...');
        jqueryLabelObj.show();
        jqueryGrpObj.addClass('has-error');
        jqueryObj.addClass('has-error');
        jqueryObj.addClass('invalid-date');
        //jqueryObj.focus();    //Allow change of focus for bootstrap datepicker!!
    }
}


//For reasons unknown, the JavaScript Date object 'handles' an invalid date, like 31-Sep-2015, 
// by 'promoting' it to the next valid date - in this case, 01-Oct-2015.  
//
//This function relies on this 'promotion' phenomenon to validate the input date...
function isValidDate(monthString, dateString, yearString) {

    //Validate/initialize input parameters...
    if ('undefined' === typeof monthString || null === monthString ||
        'undefined' === typeof dateString || null === dateString ||
        'undefined' === typeof yearString || null === yearString ) {
        return false;
    }

    //Create a new date from the input date
    var month = parseInt(monthString);
    var date = parseInt(dateString);
    var year = parseInt(yearString); 

    var testDate = new Date(year, month - 1, date); //Month parameter is zero-based!!

    //If both dates match then the input date is valid, otherwise the input date is invalid...
    if ((! isNaN(testDate)) && year === testDate.getFullYear() && (month - 1) === testDate.getMonth() && date === testDate.getDate()) {
        return true;
    }

    return false;
}

//Validate the 'from' date string is earlier than or equal to the 'to' date
//ASSUMPTION: Separate validation calls for 'from' and 'to' dates occur before any calls to this function!!
function compareFromDateAndToDate(event) {

    //Retrieve 'from' and 'to' date strings...
    var jqueryStartDate = $(('#' + event.data.inputIdStartDate));
    var jqueryEndDate = $(('#' + event.data.inputIdEndDate));

    //Check for an existing error...
    if (jqueryStartDate.hasClass('invalid-date') || jqueryEndDate.hasClass('invalid-date')) {
        return false;
    }

    var dateStringStart = jqueryStartDate.val();
    var dateStringEnd = jqueryEndDate.val();

    var jqueryGrpObj = $(('#' + event.data.groupId));
    var jqueryLabelObj = $(('#' + event.data.errorLabelId));


    //Parse both date strings...
    var msStart = Date.parse(dateStringStart);
    var msEnd = Date.parse(dateStringEnd);

    if ((!isNaN(msStart)) && (!isNaN(msEnd))) {
        //Date strings represent valid dates - compare 

        if (msStart <= msEnd) {
            //Success - remove error class, hide error message
            jqueryLabelObj.text('');
            jqueryLabelObj.hide();
            jqueryGrpObj.removeClass('has-error');
            jqueryStartDate.removeClass('has-error');
        }
        else {
            //Failure - set error class, show error message
            jqueryLabelObj.text("Please enter a 'From' date earlier than or equal to the 'To' date...");
            jqueryLabelObj.show();
            jqueryGrpObj.addClass('has-error');
            jqueryStartDate.addClass('has-error');
        }
    }

    //If either date string is invalid - take no action
    return;
}

//Fancy tree click handler...
var keywordClicks = 0;
function keywordClickHandler(event, data) {

    var node = data.node;

    if (( 'undefined' !== typeof node) && (null !== node)) {
        //console.log( 'Click: ' + ++keywordClicks);
        //if (node.isSelected()) {
        //    console.log( node.title + ' is selected!!');
        //}
        //else {
        //    console.log(node.title + ' is NOT selected!!');
        //}

        //if (node.isActive()) {
        //    console.log(node.title + ' is active!!');
        //}
        //else {
        //    console.log(node.title + ' is NOT active!!');
        //}
        //console.log('----------------------------------------------------');
    }
}

//Fancy tree activate handler...
function keywordActivateHandler(event, data) {

    var node = data.node;

    if (('undefined' !== typeof node) && (null !== node)) {
        //if (node.isSelected()) {
        //    console.log(node.title + ' is selected!!');
        //}
        //else {
        //    console.log(node.title + ' is NOT selected!!');
        //}

        //if (node.isActive()) {
        //    console.log(node.title + ' is active!!');
        //}
        //else {
        //    console.log(node.title + ' is NOT active!!');
        //}
    }
}

//Fancy tree click handler...
function keywordSelectHandler(event, data) {

    var node = data.node;

    //Scan the checkboxes on the 'Common' tab for a matching value...
    $("input[name='keywords']").each(function (index, element) {
        var checkbox = $(this);
        if( node.title.toLowerCase() === checkbox.prop('value').toLowerCase() && node.isSelected() !== checkbox.is(':checked')) {
                //Matching value found but non-matching states, update checkbox state
                checkbox.prop('checked', node.isSelected());
        }
    });
}

//Update the keyword list from the keyword tab entries
//NOTE: Since the two tabs, 'Common' and 'Full', are kept in sync at all times, update the keyword list from the 'Full' tab...
function updateKeywordList() {

    //Clear and re-populate concepts list...
    var list = $('#olConcepts');

    list.empty();

    var tree = $("#tree").fancytree("getTree");

    var titleCount = 0;
    var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes

            ++titleCount;
            return node.title;

        }).join("##");

     if ((0 < titleCount) && (selKeys.split('##').length > 0)) {
            for (var i = 0; i < (selKeys.split('##').length) ; i++) {
            list.append('<li>' + selKeys.split('##')[i] + '</li>');
        }
     }
     else {
        //'No' concepts selected means 'All' concepts selected...
        list.append('<li>All</li>');
     }
}


//BCC - 26-Jun-2015 - QA Issue #20 - Select Keywords: selected keywords saved even popup were closed without user click Save
//Click handler for 'Select Keyword(s)' button
function clickSelectKeywords(event) {

    //Clear selections from 'Most Common' and 'Full List' tabs...

    //'Most Common' tab checkboxes
    $("input[name='keywords']:checked").prop('checked', false);

    //'Full List' tab nodes
    $("#tree").fancytree("getTree").visit(function (node) {
        node.setSelected(false);
    });

    //Assume keywords data loaded at page initialization - populate tabs, show the dialog...
    populateKeywordTabs();
    $('#SelectVariableModal').modal('show');

}

//Load fancytree nodes, if indicated.  Conditionally display modal 'loading keywords' dialog until fancytree is completely loaded 
//NOTE: While this implementation works OK, it does rely on the called data URL to determine the 
//      type of keyword data received: Physical, Chemical or Biological.  
//      If re-factoring is required later, please refer to the following article for a better
//      designed approach:  Roel van Lisdonk - How to pass extra / additional parameters to the deferred.then() function in jQuery, $q (AngularJS) or Q. 
//      http://www.roelvanlisdonk.nl/?p=3952 - 
function loadKeywordsIntoTree( pShowUI, pbtnKeyword, pbtnText, pglyphiconSpan ) {

    //Validate/initialize input parameters
    var showUI = false;     //Default
    var btnKeyword = null;
    var btnText = null;
    var glyphiconSpan = null;
    
    if (('undefined' !== typeof pShowUI) && ('boolean' === typeof pShowUI)) {
        showUI = pShowUI;
    }

    if (('undefined' !== typeof pbtnKeyword) && (null !== pbtnKeyword)) {
        btnKeyword = '#' + pbtnKeyword; 
    }

    if (('undefined' !== typeof pbtnText) && (null !== pbtnText)) {
        btnText = pbtnText;
    }

    if (('undefined' !== typeof pglyphiconSpan) && (null !== pglyphiconSpan)) {
        glyphiconSpan = '#' + pglyphiconSpan;
    }

    var tree = $("#tree").fancytree("getTree");
    var rootNode = tree.rootNode;
    if (('undefined' !== typeof rootNode) && (null !== rootNode)) {

        var children = rootNode.children;
        if (('undefined' !== typeof children) && (null !== children)) {

            //Disable the keyword button...
            if (null !== btnKeyword) {
                $(btnKeyword).prop('disabled', true);
            }

            //For each child node...
            var bShowModal = true;  //Assume keyword loading dialog will be shown...

            var length = children.length;
            var callsToGo = 0;

            for (var i = 0; i < length; ++i) {

                if (!children[i].isLoaded()) {
                    ++callsToGo;
                    if (bShowModal) {
                        bShowModal = false;
                        if (showUI) {
                            $('#keywordModal').modal('show');   //Show the keyword loading dialog
                        }
                    }

                    children[i].load().done(function (data, textStatus, jqXHR) {
                        //alert('Child node loaded!!');

                        if (('undefined' !== typeof data.testData) && (null !== data.testData)) {
                            //console.log('testData received: ' +data.testData);
                    }

                        //Determine the keyword category - Physical, Chemical or Biological from the url...
                        var url = this.url;
                        var urlComponents = url.split('/');
                        var keywordCategory = urlComponents[urlComponents.length -1];

                        var glyphiconSpan = $('#glyphiconSpan' +keywordCategory);
                        var jqueryText = $('#text' +keywordCategory);

                        glyphiconSpan.removeClass('glyphicon-refresh spin');
                        glyphiconSpan.addClass('glyphicon-thumbs-up');

                        var text = jqueryText.html();
                        jqueryText.html(text + ' Completed!!');

                        if (0 >= --callsToGo) {
                            setTimeout(function () {
                                //All load calls complete - close the loading dialog...
                                $('#keywordModal').modal('hide');

                                //Populate keyword tabs
                                populateKeywordTabs();

                                //Open keywords dialog...
                                if (showUI) {
                                    $('#SelectVariableModal').modal('show');
                                }

                                //Enable the keyword button...
                                if (null !== btnKeyword) {
                                    $(btnKeyword).prop('disabled', false);
                                }

                                //Hide the glyphiconSpan...
                                if (null !== glyphiconSpan) {
                                    $(glyphiconSpan).hide();
                                }

                                //Set the keyword button text...
                                if ((null !== btnKeyword) && (null !== btnText)) {
                                    $(btnKeyword).text(btnText);
                                }

                            }, 1000);
                        }
                    }).fail(function (data, textStatus, jqXHR) {
                        //For now - just display an alert...
                        bootbox.alert('Keyword retrieval error - please try again...');
                    });
                }
            }

            if (bShowModal) {
                //Keywords data previously loaded - populate tabs, show the dialog...
                populateKeywordTabs();

                if (showUI) {
                    $('#SelectVariableModal').modal('show');
                }

                //Enable the keyword button...
                if (null !== btnKeyword) {
                    $(btnKeyword).prop('disabled', false);
                }

            }
        }
    }
}

//Populate the 'Most Common' and 'Full List' Keyword tabs...
function populateKeywordTabs() {
    //Retrieve concept values from list...
    var listItems = $('#olConcepts li');
    var listArray = [];

    listItems.each(function (index, element) {

        var text = $(this).text();
        listArray.push(text);
    });

    if (0 < listArray.length) {
        //Re-populate 'Most Common' tab entries...
        $("input[name='keywords']").each(function (index, element) {
            var checkbox = $(this);
            if (-1 !== listArray.indexOf(checkbox.attr('value'))) {
                //Checkbox value found in concepts list - check the checkbox
                checkbox.prop('checked', true);
            }
        });


        //Re-populate 'Full List' tab entries...
        $("#tree").fancytree("getTree").visit(function (node) {
            if (-1 !== listArray.indexOf(node.title)) {
                //Node title found in concepts list - select the node
                node.setSelected(true);
            }
        });
    }
}


//Click handler for 'Common' keyword checkboxes
function clickCommonKeyword(event) {

    //Retrieve the checkbox value and state...
    var checked = $(event.target).is(':checked');
    var value = $(event.target).prop('value');

    //console.log('Value: ' + value + ' checked? ' + checked);

    //Update the associated value on the 'Full' tab, if indicated...
    $("#tree").fancytree("getTree").visit(function (node) {
        if (value.toLowerCase() === node.title.toLowerCase() && checked !== node.isSelected()) {
            node.setSelected(checked);
        }
    });

}

//Click handler for 'Select Data Service(s)' button
function clickSelectDataServices(event) {

    //If any data service(s) previously selected - select associated data table rows...
    mySelectedServices.length = 0;
    mySelectedServices = mySavedServices.slice(0);

    $('#checkOnlyObservedValues').prop('checked', bObservedValuesOnly);

    var rows = $("#dtServices").dataTable().fnGetNodes();
    var length = rows.length;
    var className = 'selected';
        
    for (var i = 0; i < length; ++i) {
        var id = $(rows[i]).find("td:eq(5)").html();

        if (-1 !== mySelectedServices.indexOf(id)) {
            $(rows[i]).addClass(className);            //Previously selected service Ids contain current id - select the row
        }
        else {
            $(rows[i]).removeClass(className);         //Previously selected service Ids DO NOT contain current id - DO NOT select the row
        }
    }

    enableDisableButton('dtServices', 'btnClearSelectionsDS');
}

function getMapHeight()
{
    //Get window and top bar heights...
    var hWin = $(window).height();
    var hBar = $('#cuahsi-bar').height();

    //Map height = window height - top bar height
    return (hWin - hBar);
}

function getMapWidth(panelVisible)
{    
    var myPanelVisible = false;
    if ('undefined' !== typeof panelVisible && null !== panelVisible) {
        myPanelVisible = panelVisible;
    }

    //Get window and slider widths...
    var wWin = $(window).width();
    var wSld = $('#slider').width();

    //Map width = window width - slider width (if visible)
    return ( wWin - (myPanelVisible ? wSld : 0));
}

//update the map dimensions and position...
function resizeAndPositionMap(mapIn, sliderIn) {

    if ('undefined' === typeof mapIn || null === mapIn ||
        'undefined' === typeof sliderIn || null === sliderIn) {
        return;     //Map and/or slider not defined - return early
    }

    //Map height and width
    var hMap = getMapHeight();
    var wMap = getMapWidth(isSliderVisible(sliderIn));
    //console.log('resizeAndPositionMap - map height/width: ' + hMap.toString() + ' / ' + wMap.toString());

    $("#map-canvas").height(hMap); //setMapHeight
    $("#map-canvas").width(wMap); //setMapWidth

    //Map position
    var hBar = $("#cuahsi-bar").height();
    $("#map-canvas").css("margin-top", hBar);

    //Trigger Google maps API resize event...
    google.maps.event.trigger(mapIn, "resize");
}

function addCustomMapControls()
{
    var toggleSidePanelDiv = document.createElement('div');
    var toggleSidePanel = new toggleSidePanelControl(toggleSidePanelDiv, map);

    toggleSidePanel.index = 1;
    map.controls[google.maps.ControlPosition.RIGHT_CENTER].push(toggleSidePanelDiv);

    var AreaSizeDiv = document.createElement('div');
    var AreaSize = new AreaSizeControl(AreaSizeDiv, map);

    AreaSize.index = 1;
    map.controls[google.maps.ControlPosition.BOTTOM_CENTER].push(AreaSizeDiv);

    //Custom close button for streetview
    window.addEventListener('DOMContentLoaded', function (e) {

        // Get close button and insert it into streetView
        // #button can be anyt dom element
        var closeButton = document.querySelector('#StreetViewCloseButton'),
            controlPosition = google.maps.ControlPosition.RIGHT_CENTER;
        
        // Assumes map has been initiated 
        var streetView = map.getStreetView();

        // Hide useless and tiny default close button
        streetView.setOptions({ enableCloseButton: false });

        // Add to street view
        streetView.controls[controlPosition].push(closeButton);

        // Listen for click event on custom button
        // Can also be $(document).on('click') if using jQuery
        google.maps.event.addDomListener(closeButton, 'click', function () {
            streetView.setVisible(false);
        });

        //Add 'visible changed' listener for street view...
        //Source: http://stackoverflow.com/questions/7251738/detecting-google-maps-streetview-mode
        google.maps.event.addListener(streetView, 'visible_changed', function () {

            if (streetView.getVisible()) {
                // Street view is visible - show the custom close button
                $('#StreetViewCloseButton').show();
            } else {
                // Street view is NOT visible - hide the custom close button
                $('#StreetViewCloseButton').hide();
            }
        });

    });
}

function addSlider()
{
    var _slider = $("#slider").slideReveal({
        //BC - Increase slider panel width...
        width: 225,
        push: false,
        position: "right",
        top: 50,
        speed: 100,
        trigger: $("#trigger"),
        // autoEscape: false,
        show: function (obj) {
            var mapWidth = getMapWidth(true);
            //console.log('slider show - mapWidth: ' + mapWidth.toString());
            $("#map-canvas").width(mapWidth); //setMapWidth
            //Trigger Google maps API resize event...
            if ( 'undefined' !== map && null !== map) {
                google.maps.event.trigger(map, "resize");
            }
        },
        shown: function (obj) {
            //var result = isSliderVisible(slider);
            //console.log('shown - #slider visible? ' + result);
        },
        hide: function (obj) {
            var mapWidth = getMapWidth(false);
            //console.log('slider hide - mapWidth: ' + mapWidth.toString());
            $("#map-canvas").width(mapWidth); //setMapWidth
            //Trigger Google maps API resize event...
            if ( 'undefined' !== map && null !== map) {
                google.maps.event.trigger(map, "resize");
            }
        },
        hidden: function (obj) {
            //var result = isSliderVisible(slider);
            //console.log('hidden - #slider visible? ' + result);
        }
    });

    return _slider;
}

//Per the terminology and internals for the SliderReveal library ONLY:
//  If input instance 'shown', return true
//  else return false 
//Per jQuery terminology, SliderReveal is always 'visible' and never 'hidden'
function isSliderVisible(sliderIn) {
    //Validate/initialize input parameters...
    if ( 'undefined' === typeof sliderIn || null === sliderIn) {
        return false;   //Invalid parameter - return early
    }

    var result = sliderIn.data('slide-reveal');
    return (result);
}

function toggleSidePanelControl(controlDiv, map)
   {


        // Set CSS for the control border
        var controlUI = document.createElement('div');
        //controlUI.style.backgroundColor = 'red';
        controlUI.style.border = '1px';
        controlUI.style.borderRadius = '3px';
        controlUI.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
        controlUI.style.cursor = 'pointer';
        controlUI.style.marginBottom = '2px';
        controlUI.style.textAlign = 'center';
        controlUI.title = 'Click to show/hide side panel';
        controlDiv.appendChild(controlUI);
       

        // Set CSS for the control interior
        var controlText = document.createElement('button');
        controlText.style.color = 'rgb(25,25,25)';
        controlText.style.fontFamily = 'Roboto,Arial,sans-serif';
        controlText.style.fontSize = '16px';
        controlText.style.lineHeight = '25px';
        controlText.style.paddingLeft = '3px';
        controlText.style.paddingRight = '3px';
        controlText.style.border = '5px'
        controlText.innerHTML = '< >';
        controlText.id = 'trigger';
        controlUI.appendChild(controlText);

        // Setup the click event listeners: simply set the map to
        google.maps.event.addDomListener(controlUI, 'click', function () {
            if (isSliderVisible(slider)) {
                slider.slideReveal("hide");
            }
            else {
                slider.slideReveal("show");
            }
        });

        return controlUI;
}

function AreaSizeControl(controlDiv, map) {


    // Set CSS for the control border
    var controlUI = document.createElement('div');
//    controlUI.style.backgroundColor = 'rgb(250, 250, 250)';
    controlUI.style.border = '1px';
    controlUI.style.borderRadius = '5px';
    controlUI.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
    controlUI.style.cursor = 'pointer';
    controlUI.style.marginBottom = '12px';
    controlUI.style.textAlign = 'center';
    controlUI.title = 'Current size of map area';
    controlDiv.appendChild(controlUI);


    // Set CSS for the control interior
    var controlText = document.createElement('div');
    controlText.style.color = 'rgb(125, 125, 125)';
    controlText.style.fontFamily = 'Roboto,Arial,sans-serif';
    controlText.style.fontWeight = 'bold';
    controlText.style.fontSize = '12px';
    controlText.style.lineHeight = '25px';
    controlText.style.paddingTop = '0px';
    controlText.style.paddingLeft = '3px';
    controlText.style.paddingRight = '3px';
    controlText.style.paddingBottom = '0px';
    controlText.style.borderRadius = '5px';
    controlText.style.backgroundColor = 'rgba(255,255,255,0.8)'/* slighly transparent white */
    controlText.style.border = '5px'
    controlText.id = 'MapAreaControl';

    var controlSpan = document.createElement('span');
    controlSpan.id = 'spanAreaGlyphicon';
    controlText.appendChild(controlSpan);

    var controlLabel = document.createElement('label');
    controlLabel.style.margin = "0 auto";
    controlLabel.innerHTML = "Area:&nbsp;";
    controlText.appendChild(controlLabel);

    controlSpan = document.createElement('span');
    controlSpan.id = 'spanAreaValue';
    controlText.appendChild(controlSpan);

    controlLabel = document.createElement('label');
    controlLabel.style.margin = "0 auto";
    controlLabel.innerHTML = "&nbsp;sq km";
    controlText.appendChild(controlLabel);

    controlUI.appendChild(controlText);

    //Append divs for Latitude and Longitude...
    controlText = document.createElement('div');
    controlText.id = 'MapLatitudeControl';
    controlText.classList.add('badge');

    controlLabel = document.createElement('label');
    controlLabel.style.margin = "0 auto";
    controlLabel.innerHTML = "Latitude:&nbsp;";

    controlSpan = document.createElement('span');
    controlSpan.id = 'spanLatitudeValue';

    controlLabel.appendChild(controlSpan);
    controlText.appendChild(controlLabel);
    controlUI.appendChild(controlText);
    
    controlText = document.createElement('div');
    controlText.id = 'MapLongitudeControl';
    controlText.classList.add('badge');
  
    controlLabel = document.createElement('label');
    controlLabel.style.margin = "0 auto";
    controlLabel.innerHTML = "Longitude:&nbsp;";

    controlSpan = document.createElement('span');
    controlSpan.id = 'spanLongitudeValue';

    controlLabel.appendChild(controlSpan);
    controlText.appendChild(controlLabel);
    controlUI.appendChild(controlText);

}

function addLocationSearch() {
    var input = /** @type {HTMLInputElement} */(
      document.getElementById('pac-input'));

    var types = document.getElementById('type-selector');
    var filters = document.getElementById('panelMapFilters');
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(input);
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(types);
    map.controls[google.maps.ControlPosition.TOP_LEFT].push(filters);

    var autocomplete = new google.maps.places.Autocomplete(input);
    autocomplete.bindTo('bounds', map);


    google.maps.event.addListener(autocomplete, 'place_changed', function() {
        var place = autocomplete.getPlace();
        if (!place.geometry) {
            return;
        }

        // If the place has a geometry, then present it on a map.
        if (place.geometry.viewport) {
            map.fitBounds(place.geometry.viewport);
        } else {
            map.setCenter(place.geometry.location);
            map.setZoom(14);  // Why 17? Because it looks good.
        }

        var address = '';
        if (place.address_components) {
            address = [
              (place.address_components[0] && place.address_components[0].short_name || ''),
              (place.address_components[1] && place.address_components[1].short_name || ''),
              (place.address_components[2] && place.address_components[2].short_name || '')
            ].join(' ');
        }
    });

    // Sets a listener on a radio button to change the filter type on Places
    // Autocomplete.
    function setupClickListener(id, types) {
        var radioButton = document.getElementById(id);
        google.maps.event.addDomListener(radioButton, 'click', function() {
            autocomplete.setTypes(types);
        });
    }

    setupClickListener('changetype-all', []);
    setupClickListener('changetype-geocode', ['geocode']);
}

function getMapAreaSize() {
    var path = GetPathForBounds(map.getBounds())
    var area = GetAreaInSquareKilometers(path)
    return (area *= 1);
}

function resetMap() {
    deleteClusteredMarkersOverlays();

    removeInfoWindows();

    $('.enableWhenDataReceived').addClass('disabled');
    //resetUserSelection()
    if (typeof areaRect != "undefined") areaRect.setMap(null);
    
    //Hide the filters panel...
    $('#' + 'panelMapFilters2').addClass('hidden');

    //Reset the search and filter criteria...
    resetSearchAndFilterCriteria('dtTimeseries');

    //Set reset indicator...
    bResetMap = true;
}

function processMarkers(geoJson, isNewRequest) {

    var json = JSON.parse(geoJson);
    if (json != null) {
        if (json.features.length === 0 && isNewRequest)
        {
            //BCC - 29-Jun-2015 - QA Issue #29 - GUI: "No timeseries for specified parameters found" have different font size. 
            bootbox.alert("<h4>No timeseries for specified parameters found.</h4>")
            return;
        }

        //features = json.features;
        if (typeof json.features !== "undefined") {

            //BCC - 26-Jun-2015 - Conditionally enable 'Data' button... 
            //QA Issue #13 - Data tab (usability): if search doesn't return any results, data tab should be disabled
            var bMarkerFound = false;

            for (var i = 0, len = json.features.length; i < len; ++i) {
                if (json.features[i].geometry.type == "Point") {

                    //get values
                    var clusterid = parseFloat(json.features[i].properties.clusterid);
                    var id = json.features[i].id;
                    var lat = parseFloat(json.features[i].geometry.coordinates[1]);
                    var lng = parseFloat(json.features[i].geometry.coordinates[0]);
                    var point = new google.maps.LatLng(lat, lng);
                    var count = 1;
                    if (typeof json.features[i].properties.count != "undefined") { count = parseFloat(json.features[i].properties.count) };
                    var icontype = json.features[i].properties.icontype;
                    //create marker
                    marker = updateClusteredMarker(map, point, count, icontype, id, clusterid, "", json.features[i].properties.serviceCodeToTitle);
                    clusteredMarkersArray.push(marker);

                    //QA Issue #13
                    bMarkerFound = true;

                }
                else if (json.features[i].geometry.type == "Polygon") {


                    var id = json.features[i].id
                    var guid = json.features[i].geometry.properties.guid;
                    var sector = json.features[i].geometry.properties.sector;
                    var assessment = json.features[i].geometry.properties.assessment;
                    var dateassessed = json.features[i].geometry.properties.dateassessed;
                    var strokeColor = json.features[i].geometry.properties.strokeColor;
                    var strokeWeight = json.features[i].geometry.properties.strokeWeight;
                    var strokeOpacity = json.features[i].geometry.properties.strokeOpacity;
                    var fillColor = json.features[i].geometry.properties.fillColor;
                    var fillOpacity = json.features[i].geometry.properties.fillOpacity;
                    var label = ""; json.features[i].geometry.properties.sector;

                    var pts = new Array();

                    var points = json.features[i].geometry.coordinates;
                    for (l = 0; l < points.length - 1; l++) {
                        var p = points[l]
                        var pt = new google.maps.LatLng(p[0], p[1]);
                        pts.push(pt)


                    }
                    polygonMarker = createPolygon(pts, label, id, strokeColor, strokeWeight, strokeOpacity, fillColor, fillOpacity)


                    clusteredMarkersArray.push(polygonMarker);
                    polygonMarker.setMap(map);

                    //QA Issue #13
                    bMarkerFound = true;
                }
            };

            //QA Issue #13
            if (bMarkerFound) {
                //$('.data').removeClass('disabled')
                $('.enableWhenDataReceived').removeClass('disabled')
            }
        }
    }
}

function getFormData() {
    var formdata = []//$("#Search").serializeArray()

    var bounds = map.getBounds();
    var ne = bounds.getNorthEast(); // LatLng of the north-east corner
    var sw = bounds.getSouthWest(); // LatLng of the south-west corder 
    var startDate = $("#startDate").val().toString();
    var endDate = $("#endDate").val().toString();
    var keywords = new Array();

   //selected Services
    var services = mySavedServices;

    //BCC - 13-Jul-2015 - Since 'Common' and 'Full' keyword tab contents are kept in sync at all times, 
    //       take the selected keys from the 'Full' tab only...
    var tree = $("#tree").fancytree("getTree");
    var selKeys = $.map(tree.getSelectedNodes(true), function (node) { //true switch returns all top level nodes
        return node.title;
    }).join("##");

    keywords.push(selKeys)
                
    var xMin = Math.min(ne.lng(), sw.lng())
    var xMax = Math.max(ne.lng(), sw.lng())
    var yMin = Math.min(ne.lat(), sw.lat())
    var yMax = Math.max(ne.lat(), sw.lat())
    var zoomLevel = map.getZoom();
    //Extent
    formdata.push({ name: "xMin", value: xMin });
    formdata.push({ name: "xMax", value: xMax });
    formdata.push({ name: "yMin", value: yMin });
    formdata.push({ name: "yMax", value: yMax });
    formdata.push({ name: "zoomLevel", value: zoomLevel })
    //Date range
    formdata.push({ name: "startDate", value: startDate });
    formdata.push({ name: "endDate", value: endDate });
    //Keywords
    formdata.push({ name: "keywords", value: keywords });
    //Services
    formdata.push({ name: "services", value: services });
    formdata.push({ name: "sessionGuid", value: sessionGuid })

    return formdata;
}
//http://stackoverflow.com/questions/5465375/javascript-date-regex-dd-mm-yyyy

function checkReg2(date) {
    var date_regex = /^(0[1-9]|1[0-2])\/(0[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/;
    if (!(date_regex.test(date))) {
        return false;
    }
}
//upddate map wit new clusters
function updateMap(isNewRequest, filterAndSearchCriteria) {
    
    if (clusteredMarkersArray.length == 0 && isNewRequest == false && typeof filterAndSearchCriteria == "undefined" ) return;//only map navigation
    var formData = getFormData();
    if (typeof formData == "undefined") return; //error in formdata retrieval

    $("#pageloaddiv").show();

    if (clusteredMarkersArray.length == 0 &&  typeof filterAndSearchCriteria == "undefined" ) {
        formData.push({ name: "isNewRequest", value: true });
    }
    else {
        formData.push({ name: "isNewRequest", value: false });
    }

    if ( 'undefined' !== typeof filterAndSearchCriteria && null !== filterAndSearchCriteria && ('' !== filterAndSearchCriteria.Search || 0 < filterAndSearchCriteria.filters.length)) {
    
        formData.push({name: 'filterAndSearchCriteria', value: JSON.stringify(filterAndSearchCriteria)});
    }

   //Clean up
        deleteClusteredMarkersOverlays();
        removeInfoWindows();
   // }
   //get Markers
   var actionurl = '/home/updateMarkers';

   //BCC - 19-Nov-2015 - Alternate $.ajax call using the Promise interface available in jQuery 1.5+
    var promise = $.ajax({
        url: actionurl,
        type: 'POST',
        dataType: 'json',
        timeout: 60000,
        data: formData
    });
    
    promise.done( function (data) {
        
            processMarkers(data, isNewRequest);
            $("#pageloaddiv").hide();
        });
        
    promise.fail( function (jqXHR, textstatus, errorThrown) {
        
            serviceFailed(jqXHR, textstatus, errorThrown);
            $("#pageloaddiv").hide();
    });
}

// Deletes all markers in the array by removing references to them.
function deleteClusteredMarkersOverlays() {
    clearOverlays();
    clusteredMarkersArray.length = 0;
}
// Removes the overlays from the map, but keeps them in the array.
function clearOverlays() {
    //Remove all listeners from each marker...
    for (var i = 0; i < clusteredMarkersArray.length; i++) {
        google.maps.event.clearInstanceListeners( clusteredMarkersArray[i] );
    }

    //Remove all markers...
    setAllMap(null);
}
// Sets the map on all markers in the array.
function setAllMap(map) {
    for (var i = 0; i < clusteredMarkersArray.length; i++) {
        clusteredMarkersArray[i].setMap(map);
    }
}
// Deletes all markers in the array by removing references to them
function deleteOverlays(arrayName) {
    if (arrayName) {
        for (i in arrayName) {
            arrayName[i].setMap(null);

        }
        arrayName.length = 0;
    }
}


//Attempt to retrieve the position name from the input marker's position...
//Source: https://developers.google.com/maps/documentation/javascript/examples/geocoding-reverse
//More information: https://developers.google.com/maps/documentation/javascript/geocoding
//                  https://developers.google.com/maps/articles/geocodestrat
function getMarkerPositionName(marker) {
    var geocoder = new google.maps.Geocoder();

    //NOTE: Asynchronous call - 
    //ASSUMPTION: This call returns before the user issues a time series request to the server...
    geocoder.geocode({ 'latLng': marker.getPosition() }, function (results, status) {
        if (status == google.maps.GeocoderStatus.OK) {
            //Success - retrieve the location name...
            currentMarkerPlaceName = getMapLocationName(results);
            if ('' !== currentMarkerPlaceName) {
                $('#SeriesModal #myModalLabel').html('Search Results near: ' + currentMarkerPlaceName);
            }
        } else {
            //Failure - reset variable
            currentMarkerPlaceName = '';
        }
    });
}

function updateClusteredMarker(map, point, count, icontype, id, clusterid, label, serviceCodeToTitle) {

    var icon_choice;
    var icon;
    var icons;


    icons = {
        0: { file: "blue-22.png", printImage: "blue-22.gif", width: 44, cssClass: "GblueLabel22", labelXoffset: 22, labelYoffset: 22 },
        1: { file: "blue-24.png", printImage: "blue-24.gif", width: 44, cssClass: "GblueLabel24", labelXoffset: 22, labelYoffset: 22 },
        2: { file: "blue-28.png", printImage: "blue-28.gif", width: 44, cssClass: "GblueLabel28", labelXoffset: 22, labelYoffset: 22 },
        3: { file: "blue-32.png", printImage: "blue-32.gif", width: 44, cssClass: "GblueLabel32", labelXoffset: 22, labelYoffset: 22 },
        4: { file: "blue-36.png", printImage: "blue-36.gif", width: 44, cssClass: "GblueLabel36", labelXoffset: 22, labelYoffset: 22 },
        5: { file: "blue-40.png", printImage: "blue-40.gif", width: 44, cssClass: "GblueLabel40", labelXoffset: 22, labelYoffset: 22 },
        6: { file: "blue-44.png", printImage: "blue-44.gif", width: 44, cssClass: "GblueLabel44", labelXoffset: 22, labelYoffset: 22 }
    }

    if (count == 1) {
       
        var image = {
            url: clusterMarkerPath + 'm6_single_36.png',
            // This marker is 20 pixels wide by 32 pixels high.
            size: new google.maps.Size(36, 36),
            // The origin for this image is (0, 0).
            origin: new google.maps.Point(0, 0),
            // The anchor for this image is the base of the flagpole at (0, 32).
            anchor: new google.maps.Point(13, 13)
        };
        var marker = new google.maps.Marker({
            position: point,
            map: map,
            icon: image,
            zIndex: 1500
        });

        //Add a 'once' map event to create an infowindow for marker, if indicated
        if ('undefined' !== typeof serviceCodeToTitle && null !== serviceCodeToTitle) {

            google.maps.event.addListenerOnce(marker, 'mouseover', function(event) {

                createInfoWindow( map, marker, serviceCodeToTitle, 'google.maps.Marker');
            });
        }

        google.maps.event.addListener(marker, 'click', function () {
            $('#SeriesModal #myModalLabel').html('Search Results near Selected Marker');
            getMarkerPositionName(marker);

            setUpDatatables(clusterid);

            $('#SeriesModal').modal('show');
        });

    }

    else {
        if (count < 5) {
            icon_choice = 0;
            icon_height = 28;
            icon_width = 32;
        }
        else if (count < 10) {
            icon_choice = 0;
            icon_height = 30;
            icon_width = 34;
        }
        else if (count < 25) {
            icon_choice = 1;
            icon_height = 32;
            icon_width = 36;
        }
        else if (count < 50) {
            icon_choice = 2;
            icon_height = 36;
            icon_width = 38;
        }
        else if (count < 100) {
            icon_choice = 3;
            icon_height = 40;
            icon_width = 40;
        }
        else if (count < 250) {
            icon_choice = 3;
            icon_height = 44;
            icon_width = 44;
        }
        else if (count < 500) {
            icon_choice = 4;
            icon_height = 48;
            icon_width = 48;
        }
        else if (count < 1000) {
            icon_choice = 5;
            icon_height = 52;
            icon_width = 52;
        }
        else if (count < 5000) {
            icon_choice = 6;
            icon_height = 56;
            icon_width = 56;
        }
        else if (count > 5000) {
            icon_choice = 6;
            icon_height = 60;
            icon_width = 60;
            //count=""
        }

        var image = {
            url: clusterMarkerPath + "m6.png",
            size: new google.maps.Size(icon_width, icon_height),
            origin: new google.maps.Point(0, 0),
            anchor: new google.maps.Point(icon_width / 2, icon_height / 2),
            scaledSize: new google.maps.Size(icon_width, icon_height)
        };

        var marker = new MarkerWithLabel({
            position: point,
            icon:image,
            draggable: false,
            raiseOnDrag: true,
            map: map,
            labelContent: count,
            labelAnchor: new google.maps.Point(21, 7),
            labelClass: icons[icon_choice].cssClass, // the CSS class for the label
            labelStyle: { opacity: 0.95 },
            tooltip: '',
            zIndex: 1500,
            flat: true,
            visible: true
        });

        //Add a 'once' map event to create an infowindow for marker, if indicated
        if ('undefined' !== typeof serviceCodeToTitle && null !== serviceCodeToTitle) {

            google.maps.event.addListenerOnce(marker, 'mouseover', function(event) {

                createInfoWindow( map, marker, serviceCodeToTitle, 'MarkerWithLabel');
            });
        }


        google.maps.event.addListener(marker, 'click', function () {

            $('#SeriesModal #myModalLabel').html('Search Results near Selected Marker');
            getMarkerPositionName(marker);

            setUpDatatables(clusterid);
                
            $('#SeriesModal').modal('show');
        });
    }
    return marker
}

function createInfoWindow( map, marker, serviceCodeToTitle, markerTypeName) {

    //Validate/initialize input parameters...
    if ('undefined' === typeof map || null === map ||
        'undefined' === typeof marker || null === marker ||
        'undefined' === typeof serviceCodeToTitle || null === serviceCodeToTitle) {
        return; //Invalid parameter(s) - return early...
    }

    //Create content for InfoWindow...

    //Header
    var listCount = 0;
    var infoId = randomId.generateId();
    
    var infoContent = '<div style="max-width: 25em;" class="side-panel" id="' + infoId + '">'; 
    infoContent += '<ul style="list-style-type:none; margin-left: -2.5em;">';
    //List of service icons, codes and titles...
    for (var servCode in serviceCodeToTitle) {
        infoContent += '<li style="font-weight: bold;">';
        infoContent += '<img style="height: 2em; width: 2em; margin-bottom: 0.5em;" src="/home/getIcon?id=' + servCode + '">'

        var servicePlusCount = serviceCodeToTitle[servCode];
        var lastRight = servicePlusCount.lastIndexOf('(');
        var lastLeft = servicePlusCount.lastIndexOf(')');

        var service = servicePlusCount.slice(0, lastRight+1);
        var count = servicePlusCount.slice(lastRight+1, lastLeft);
        var restOfString = servicePlusCount.slice(lastLeft);

        infoContent += ' - ' + service.toString() + '<i>' + ((count * 1.0).toLocaleString()) + '</i>' + restOfString.toString();
        infoContent += '</li>';

        ++listCount;
    }
        
    infoContent += '</ul>';
    infoContent += '</div>';

    //Allocate InfoWindow instance, assign handlers...
    var iw = new InfoBubble({'backgroundColor': '#E6E6E6',
                             'content': infoContent, 
                             'disableAnimation': true,
                             'disableAutoPan': true,
                             'hideCloseButton': true });
    openInfoBubbles.push(iw);
    //console.log('InfoBubble count: ' + openInfoBubbles.length);

    //Calculate minimum height and width in 'px' units...
    //Convert from ems: (size on ems) * (parent font size in px)
    //Source: http://pxtoem.com/ ('Learn' tab)
    var fontsize = ($( '#' + infoId).parent().css('fontSize')) * 1; //Multiply by 1 to remove trailing 'px'

    iw.setMinHeight((listCount * fontsize).toString());
    iw.setMaxWidth((20 * fontsize).toString());

    //Open on mouseover...
    google.maps.event.addListener(marker, 'mouseover', function openInfoBubble(event) {
        if ( ! iw.isOpen()) {
            iw.open(map, marker);
        }

        //Make current position the marker's position
        var cp = marker.position;

        currentPosition.update = false;  //Temporarily suppress position updates
        currentPosition.latLng = cp;

        //Check for change in current position, close window if indicated...
        //NOTE: Fail-safe code for those instances in which the Google maps API does not report a 'mouseout' for the marker
        //      The interval clears itself once it determines the current position is no longer the marker's position...
        var intervalId;
        intervalId = setInterval( (function(myCp, myIw) {
            return function() {
                if (myCp.lat() !== currentPosition.latLng.lat() || 
                    myCp.lng() !== currentPosition.latLng.lng() ) {
                    myIw.close();
                    clearInterval(intervalId);  //Can bind to the reference here since this code runs after the reference is defined...
                }
            }
        } )(cp, iw), 1000 )  //NOTE: Immediate execution of function to pass in cp and iw values
                             //      If the IntervalId is passed as a parameter here, its value is undefined...

        setTimeout( function() {
            currentPosition.update = true;  //Restore position updates
        }, 1000)

    });
    
    //Close on mouseout...
    //NOTE: Google maps API returns 'mouseout' events most of the time.
    //      If the user moves the mouse too quickly, however, 'mouseouts' 
    //      can be lost.  See fail-safe code in 'mouseover' handler for 
    //      these cases...
    google.maps.event.addListener(marker, 'mouseout', function () {
        iw.close();
    });
    
}

//Remove all InfoBubble instances...
function removeInfoWindows() {

    if ( 'undefined' !== typeof openInfoBubbles && null !== openInfoBubbles) {
    
        while ( 0 < openInfoBubbles.length) {
            var infoBubble = openInfoBubbles.shift();

            infoBubble.close();
            infoBubble = null;
        }
    }
}


//BCC - 29-Jun-2015 - QA Issue # 26 - Data tab: filters under the timeseries table have no titles
//Prepend disabled options as 'placeholders' to filtering selects... 
//NOTE: When registered with the 'draw.dt', this function is called three times, once for each of the datatables sub-tables: header, body and footer.
//       The selector only works on the third call for the footer table...
//       Also, the function is called each time the user applies a filter. Thus the method always removes the 'placeholder' value from the select before prepending it to the select
//Source: http://stackoverflow.com/questions/22822427/bootstrap-select-dropdown-list-placeholder
function addFilterPlaceholders(event) {

    var tableId = event.data.tableId;
    var placeHolders = event.data.placeHolders;

    var selector = '#' + tableId + '_wrapper > div.dataTables_scroll > div.dataTables_scrollFoot > div > table > tfoot > tr > th > select';

    var selects = $(selector);

    //console.log('Select count: ' +selects.length);

    for(var i = 0; i < placeHolders.length; ++i) {
        var select = $(selects[i]);

        select.find("option[value='999999']").remove();

        if ('' === select.val()) {
            //No selection made - prepend 'placeholder' to selections
            
            //BCC - 30-Mar-2016 - Explicitly set the elements selected index to make selected/disabled element visible in IE 11
            //Source: http://stackoverflow.com/questions/27283887/adding-selected-and-disabled-attributes-to-an-option-tag-in-ie-11
            select.prepend('<option value="999999" selected disabled style="color: grey;">By: ' + placeHolders[i] + '</option>');
            select.prop( "selectedIndex", 0 );
        }
    }
}


//BCC - 29-Jun-2015 - QA Issue #25 - Data tab: table column titles stay in wrong order after sorting table
//Force a Datatable redraw to render now visible headers in the correct widths...
//Source: http://datatables.net/reference/api/columns.adjust()
function adjustColumns(event) {

    var tableId = event.data.tableId;
    var containerId = event.data.containerId;
    
    //Validate/initialize input parameters...
    if ( 'undefined' === typeof tableId || null === tableId) {
        return; //Invalid parameter(s) - return early...
    }
    
    if ( 'undefined' !== typeof containerId && null !== containerId) {
        $(('#' + containerId)).css('display', 'block');
    }

    var table = $(('#' + tableId)).DataTable();

    table.columns.adjust().draw();
}

function getDetailsForMarker(clusterid)
{
    var actionUrl = "/home/getDetailsForMarker/" + clusterid

   // $('body').modalmanager('loading');
     
    
    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        timeout: 60000,
        processData: false,
        //data: formData,
        success: function (data) {
           
        },
        error: function (xmlhttprequest, textstatus, message) {
            serviceFailed(xmlhttprequest, textstatus, message)
        }
    });
}

function setupServices()
{

    if ($.fn.DataTable.isDataTable("#dtServices")) {
        $('#dtServices').DataTable().clear().destroy();
    }

    var actionUrl = "/home/getServiceList"
    // $('#demo').html( '<table cellpadding="0" cellspacing="0" border="0" class="display" id="example"></table>' );
 
    myServicesDatatable = $('#dtServices').dataTable({
        "ajax": {
                 "url": actionUrl,
                 "content-type": "application/json; charset=utf-8"
                },
        "order": [2, 'asc'],
        "dom": '<"toolbarDS">frtilp', //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
        "autoWidth": false,
         "columns": [

            { "data": "Organization", "visible": true, "className": "tableColWrap20pct" },
            { "data": "ServiceCode", "visible": false, "searchable": false },
            { "data": "Title", "visible": true, "className": "tableColWrap20pct" },
            { "data": "DescriptionUrl", "visible": false, "searchable": false },
            { "data": "ServiceUrl", "visible": false, "searchable": false },
            { "data": "Checked", "visible": false, "searchable": false },

            { "data": "Sites", "visible": true },
            { "data": "Variables", "visible": true },
            { "data": "Values", "visible": true },
           { "data": "ServiceBoundingBox", "visible": false, "searchable": false },
            { "data": "ServiceID", "visible": true }
         ],
        "createdRow": function (row, data, index) {

             var id = $('td', row).eq(5).html();
             var title = $('td', row).eq(1).html();
             var url = 'http://hiscentral.cuahsi.org/pub_network.aspx?n=';
             $('td', row).eq(1).html("<a href='" + url + id + "' target='_Blank'>" + title + " </a>");


         },
         "initComplete": function () {
             var dt = $('#dtServices').DataTable();

             dt.columns.adjust();
             dt.draw();

             //Revise layout for table's info, length and pagination controls...
             var tableName = 'dtServices';
             $('#' + tableName + '_info').css({'width': '25%'});

             $('#' + tableName + '_length').css({'width': '50%', 
                                             'height': '2.5em'});

             $('#' + tableName + '_length' + ' > label').css({'text-align': 'center',
                                                         'display': 'block',
                                                         'margin-top': '1.0em'});

             $('#' + tableName + '_paginate').css({'width': '25%'});

         }
    });

    //Add click handler for row selection...
    $('#dtServices tbody').on('click', 'tr', function () {
        $(this).toggleClass('selected');
        var id = this.cells[5].innerHTML;
        if ($.inArray(id, mySelectedServices ) == -1) {
            //add
            mySelectedServices.push(id);
        }
        else {            
            //remove
            mySelectedServices = $.grep(mySelectedServices, function (element, index) {
                return element !== id;
            });
        }

        enableDisableButton('dtServices', 'btnClearSelectionsDS');
    });

    //Add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbarDS").html('<input type="button" style="margin-left: 1em; float:left;" class="btn btn-warning" disabled id="btnClearSelectionsDS" value="Clear Selection(s)"/>');


    //Avoid multiple registrations of the same handler...
    $('#btnClearSelectionsDS').off('click', clearServicesSelections);
    $('#btnClearSelectionsDS').on('click', { 'tableId': 'dtServices', 'btnId': 'btnClearSelectionsDS' }, clearServicesSelections);

    //return table;
}
//Datatable for Marker
function setUpDatatables(clusterid) {
    
    if ($.fn.DataTable.isDataTable("#dtMarkers")) {
        $('#dtMarkers').DataTable().clear().destroy();
    }
    
    //Reset the selected row count...
    selectedRowCounts['dtMarkers'].count = 0;

    var actionUrl = "/home/getDetailsForMarker/" + clusterid
 
    var oTable= $('#dtMarkers').dataTable({
        "ajax": actionUrl,
        "deferRender": true,
        "dom": 'C<"clear"><"toolbar">frtilp',   //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
        "autoWidth": false,
        "columns": [
           { "data": "Organization", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText
           },
            //BCC - 09-Sep-2015 - GitHub Issue #23 - Replace Network Name with Data Service Title
           { "data": "ServTitle", "sTitle": "Service Title", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": function (data, type, full, meta) { 
                    if ('display' === type) {
                        return formatServiceTitleAbbreviated( full.ServCode, data );
                    }
                    else {
                        return data;
                    }
             },
            "createdCell": createdTooltipText                 
           },
           { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText           
           },
           { "data": "SiteName", "sTitle": "Site Name", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
           },
           { "data": "DataType", "visible": true, "width": "5em", "className": "td-textalign-center"},
           { "data": "ValueType", "visible": true, "width": "5em", "className": "td-textalign-center"},
           { "data": "SampleMedium", "visible": true, "width": "5em", "className": "td-textalign-center"},
           { "data": "QCLID", "visible": false },
           { "data": "QCLDesc", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
           },
           { "data": "MethodId", "visible": false },
           { "data": "MethodDesc", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
           },
           { "data": "SourceId", "visible": false },
           { "data": "SourceOrg", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
           },
           { "data": "BeginDate", "sTitle": "Start Date", "visible": true, "width": "10em",
                 'render': function (data, type, full, meta) { 
                     return formatDate(data);
                 }           
           },
           { "data": "EndDate","sTitle": "End Date", "visible": true, "width": "10em",
                 'render': function (data, type, full, meta) { 
                     return formatDate(data);
                 }           
           },
           { "data": "ValueCount", "visible": true, "width": "10em", "className": "td-textalign-center",
                 'render': function (data, type, full, meta) { 
                     return (0 === data) ? 'unknown' : data;
                 }                      
           },
           { "data": "VariableName", "sTitle": "Variable Name", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
           },
           { "data": "TimeSupport", "visible": true, "width": "5em", "className": "td-textalign-center"},
           { "data": "TimeUnit", "visible": true, "width": "5em", "className": "td-textalign-center"},
           { "data": null, "sTitle": "Service URL", "visible": true, "width": "10em"},

           { "data": "SiteCode", "sTitle": "Site Code", "visible": true,  "width": "10em" },
           { "data": "VariableCode", "sTitle": "Variable Code", "visible": true,  "width": "10em" },           
           { "data": "SeriesId", "visible": false, "searchable": false,  "width": "10em" },
           { "data": "ServURL", "sTitle": "Web Service Description URL", "visible": false, "searchable": false, "width": "10em" },
           { "data": "ServCode", "name": "ServiceCode", "visible": false, "searchable": false,  "width": "10em" },
           { 'data': 'VariableUnits', 'visible': false, 'width': '5em'}
        ],
        "order": [[0,'asc'],    //Publisher
                  [1,'asc'],    //Service Title
                  [2,'asc']],    //Keyword
        "scrollX": true,
        "scrollY": "30em",
        "scrollCollapse": true,
         "createdRow": function (row, data, index) {

                 //Create a link to the Service URL
                 var org = $('td', row).eq(0).html();
                 var servCode = data.ServCode;

                 var descUrl = getDescriptionUrl(servCode);
                 $('td', row).eq(16).html("<a href='" + descUrl + "' target='_Blank'>" + org + " </a>");

                //If 'Select All' in progress, add the 'selected' class to the row...
                var selectCheck = $('#' + 'spanSelectCheck'); 
                if ( selectCheck.length && (! selectCheck.hasClass('hidden'))) {
                    $(row).addClass('selected');  //Only correct if ALL the rows are being selected...
                }

         },
        "initComplete": function () {

            //BCC - 10-Aug-2015 - GitHub Issue #35 - Add filter by Site Name 
            setfooterFilters('dtMarkers', [0, 1, 2, 3, 4, 5, 6]);

            var tempEvent = { 'data': { 'tableId': 'dtMarkers', 'placeHolders': ['Publisher', 'Service Title', 'Keyword', 'Site Name', 'Data Type', 'Value Type', 'Sample Medium'] } };
            addFilterPlaceholders(tempEvent);

            var dt = $('#dtMarkers').DataTable();
            
            dt.columns.adjust();
            dt.draw();

            //Revise layout for table's info, length and pagination controls...
            $('#' + 'dtMarkers_info').css({'width': '25%'});

            $('#' + 'dtMarkers_length').css({'width': '50%', 
                                             'height': '2.5em'});

           $('#' + 'dtMarkers_length' + ' > label').css({'text-align': 'center',
                                                         'display': 'block',
                                                         'margin-top': '1.0em'});

            $('#' + 'dtMarkers_paginate').css({'width': '25%'});

            //Set up tooltips
            setupToolTips();
        }
    });


    //Suppress DataTable display of server errors as alerts...
    $.fn.dataTable.ext.errMode = 'none',

    //Server-error handler for dtMarkers...
    $('#dtMarkers').on('error.dt', function (event, settings, techNote, message) {

        //Close current dialog - open 'Server Error' dialog
        $('#SeriesModal').modal('hide');

        $('#serverMessageText').text(message);
        $('#serverErrorModal').modal('show');

        //Log messsage received from server...
        console.log('dtMarkers reports error: ' + message);
    });

    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtMarkers tbody').off('click', 'tr', toggleSelected);
    $('#dtMarkers tbody').on('click', 'tr', {'tableId': 'dtMarkers', 'btnIds': ['btnZipSelections','btnManageSelections'], 'btnClearId': 'btnClearSelections'},toggleSelected);

    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbar").html(
                          '<div class="inline-form">' +
                          '<div class="form-group" style="margin-left: 1em; float:left; display:inline-block;">' +
                          
                            '<div class="dropdown" style="position: relative; display: inline-block; float: left; font-size: 1.00em;">' +
                                '<button class="btn btn-primary dropdown-toggle" type="button" id="ddMenuSelections" data-toggle="dropdown" aria-haspopup="true" aria-expanded="true">' +
                                '<span class="glyphicon glyphicon-list-alt" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Selections&nbsp;</span>' +
                                '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                '</button>' +
                                '<ul class="dropdown-menu" aria-labelledby="ddMenuSelections">' +
                                   
                                (currentUser.authenticated ? 
                                    '<li><a href="#" id="anchorAllSelections" data-selectall="true" style="font-weight: bold;" >' +
                                    '<span class="glyphicon glyphicon-plus-sign"></span>' +
                                    '<span class="text-muted">&nbsp;Select All&nbsp;</span>' +
                                    '<span id="spanSelectCheck" class="glyphicon glyphicon-ok-circle hidden"></span>' + 
                                    '</a></li>': '' ) +                                    

                                '<li><a href="#" id="anchorClearSelections" style="font-weight: bold;">' +
                                '<span class="glyphicon glyphicon-minus-sign"></span>' +
                                '<span id="spanClearSelections" class="text-warning">&nbsp;Clear Selections</span>' + 
                                '</a></li>' +
                                '</ul>' +
                            '</div>' +

                            '<div style="position: relative; display: inline-block; float: left; margin-left: 0.5em; vertical-align: top">' +
                            '<div>' +
                                '<div class="dropdown" id="ddActions">' +
                                    '<button class="btn btn-primary" data-toggle="dropdown" style="font-size: 1.00em;">' +

                                    '<div id="ddActionOriginal" style="display: none;">' + 
                                    '<span class="glyphicon glyphicon-circle-arrow-right" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                    '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Action&nbsp;</span>' +
                                    '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                    '</div>' +
                                    '<div id="ddAction" data-noneselected="true">' + 
                                    '<span class="glyphicon glyphicon-circle-arrow-right" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                    '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Action&nbsp;</span>' +
                                    '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                    '</div>' +

                                    '</button>' +
                                    '<ul class="dropdown-menu" style="width: 32em;">' +

                                    '<li data-toggle="tooltip" data-placement="top" title="Move all selected time series to the workspace">' +
                                    '<a href="#" id="anchorAddSelectionsToWorkspace" style="font-weight: bold;" >' + 
                                    '<span class="glyphicon glyphicon-plus-sign" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                    '<span id="spanManageSelections" style="font-weight: bold; display: inline-block; vertical-align: super;">Save Selection(s) to Workspace</span>' +
                                    '</a>' +
                                    '</li>' +

                                    (currentUser.authenticated ?
                                    '<li>' +
                                        '<a href="#" id="anchorAllTimeseriesInOneFile" style="font-weight: bold;" >' +
                                        '<span class="glyphicon glyphicon-file" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                        '<span id="spanCombinedFiles" style="font-weight: bold; display: inline-block; vertical-align: super;">Download in combined file</span>' +
                                        '</a>' +
                                    '</li>' +
                                    '<li>' +
                                        '<a href="#" id="anchorEachTimeseriesInSeparateFile" style="font-weight: bold;" >' +
                                        '<span class="glyphicon glyphicon-duplicate" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                        '<span id="spanSeparateFiles" style="font-weight: bold; display: inline-block; vertical-align: super;">Download in separate file(s)</span>' +
                                        '</a>'
                                    :
                                    '<li data-toggle="tooltip" data-placement="top" title="Export all selected time series to the client in CSV format">' +
                                        '<a href="#" id="anchorEachTimeseriesInSeparateFile" style="font-weight: bold;">' +                                    
                                        '<span class="glyphicon glyphicon-export" style="max-width: 100%; font-size: 1.5em;  margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +
                                        '<span id="spanZipSelections"  style="font-weight: bold; display: inline-block; vertical-align: super;">Export Selection(s)</span>' +
                                        '</a>') +

                                    '</li>' +

                                    '</ul>' +
                                    '</div>' +
                                '</div>' +
                            '</div>' +
    

                            '<span class="clsMessageArea" style="display: none; float:left; margin-left: 2em;"></span>' +
                          '</div>' + 
                          '</div>');

    //$("div.toolbar").css('border', '1px solid red');
  
    // BC - Do not respond to data table load event...

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    if (currentUser.authenticated) {
        $('#anchorAllSelections').off('click', selectAll);
        $('#anchorAllSelections').on('click', { 'tableId': 'dtMarkers', 'anchorId': 'anchorAllSelections', 'checkId': 'spanSelectCheck', 'clearId': 'anchorClearSelections', 'selectAll' : $('#anchorAllSelections').attr('data-selectall') }, selectAll);
    }

    //Authenticated user only...
    if (currentUser.authenticated) {
        $('#anchorAllTimeseriesInOneFile').off('click', zipSelections_2);
        $('#anchorAllTimeseriesInOneFile').on('click', { 'tableId': 'dtMarkers', 'selectAll' : $('#anchorAllSelections').attr('data-selectall'), 'checkId': 'spanSelectCheck', 'isMerged': true }, zipSelections_2);
    }

    //Avoid multiple registrations of the same handler...
    $('#anchorEachTimeseriesInSeparateFile').off('click', zipSelections_2);
    $('#anchorEachTimeseriesInSeparateFile').on('click', { 'tableId': 'dtMarkers', 'selectAll' : $('#anchorAllSelections').attr('data-selectall'), 'checkId': 'spanSelectCheck', 'isMerged': false }, zipSelections_2);

    //Avoid multiple registrations of the same handler...
    $('#anchorAddSelectionsToWorkspace').off('click', copySelectionsToDataManager);
    $('#anchorAddSelectionsToWorkspace').on('click', { 'tableId': 'dtMarkers', 'selectAll' : $('#anchorAllSelections').attr('data-selectall'), 'checkId' : 'spanSelectCheck' /*, 'currentAnchor': 'anchorAddSelectionsToWorkspace' */ }, copySelectionsToDataManager)

    //Avoid multiple registrations of the same handler...
    $('#anchorClearSelections').off('click', clearSelections);
    $('#anchorClearSelections').on('click', { 'tableId': 'dtMarkers', 'anchorId': 'anchorAllSelections', 'checkId': 'spanSelectCheck' }, clearSelections);
}

//Initialize 'static' tooltips - those not subject to change during the application session...
function setupToolTips() {

    var texts = [/* 0*/ "Select one or more rows to export. You can check the status by opening the workspace and selecting the 'Exports' tab.",
                 /* 1*/ "Select one or more rows to add to your workspace. Work with your data by opening the workspace and selecting the 'Data' tab.",
                 /* 2*/ "Display the map with current search results, if any.",
                 /* 3*/ "Display current search results in a table.",
                 /* 4*/ "Display time series selected for additional processing and/or download.",
                 /* 5*/ "Display CUAHSI contact information.",
                 /* 6*/ "Display information about the CUAHSI HydroClient.",
                 /* 7*/ "Display a brief HydroClient tutorial.",
                 /* 8*/ "Login via Google to retrieve saved Workspace entries.",
                 /* 9*/ "Display HydroClient licensing information.",
                 /*10*/ "The organization responsible for publishing the data set, which may or may not be the same organization responsible for collecting or creating the data set.",
                 /*11*/ "A name for this collection of data. Defined by the data publisher.",
                 /*12*/ "Search term used to tag the data set that describes the parameter being measured. Publishers must choose from a set of CUAHSI approved terms.",
                 /*13*/ "Name given by the data collector of the location where observations in the data set have been made. Defined by the data publisher.",
                 /*14*/ "Describes the recorded value over the time interval being sampled. Publishers must choose from a set of CUAHSI approved terms.",
                 /*15*/ "Distinguishes between various types of observations, such as derived values versus field observations. Publishers must choose from a set of CUAHSI approved terms.",
                 /*16*/ "Describes the medium which is being sampled for laboratory analysis or the medium in which a sensor is placed. Publishers must choose from a set of CUAHSI approved terms.",
                 /*17*/ "Describes how the data were (or were not) subjected to quality assurance. Defined by the data publisher. ",
                 /*18*/ "Describes the process and technique used to make the measurements in a data set. Defined by the data publisher.",
                 /*19*/ "Organization responsible for collecting or otherwise creating the data set. Defined by the data publisher.",
                 /*20*/ "Date of first observation in the data set. Created during the cataloging process.",
                 /*21*/ "Date of the last observation in the data set. Created during the cataloging process.",
                 /*22*/ "An estimate of the number of observations in this data set, which is created during the cataloging process. " +
                         "This value is calculated using a ratio between the time range of your search and the overall time range of the data series. " +
                         "The estimated Value Count is not available if the time series has observations that are irregularly spaced over time.",
                 /*23*/ "The parameter observed in the data set. Publishers must choose from a set of CUAHSI approved terms.",
                 /*24*/ "Indicates the period of time which the measurement represents. Defined by the data publisher.",
                 /*25*/ "The unit in which the time support is expressed (e.g. “day” or “minute”). Publishers must choose from a set of CUAHSI approved terms.",
                 /*26*/ "The address of the server hosting the data set. ",
                 /*27*/ "A shortened name for the site where the observations in the data set were made. Defined by the data publisher.",
                 /*28*/ "A shortened name for the parameter being measured. Defined by the data publisher.",
                 /*29*/ "The current status of the server process.",
                 /*30*/ "The units of measure for the variable.",
                 /*31*/ "A unique system identifier for the time series.",
                 /*32*/ "The creation date and time for the entry.",
                 /*33*/ "Tootip text - TBD" ];

    var divs = { 'divZipSelections': {'type': 'id', 'text': texts[0]},
                 'divManageSelections' : {'type': 'id', 'text': texts[1]},
                 'divZipSelectionsTS': {'type': 'id', 'text': texts[0]},
                 'divManageSelectionsTS' : {'type': 'id', 'text': texts[1]},
                 'liMapTab': {'type': 'id', 'text': texts[2]},
                 'liTableTab': {'type': 'id', 'text': texts[3]},
                 'liTabbedDataMgrTab': {'type': 'id', 'text': texts[4]},
                 'liContactTab': {'type': 'id', 'text': texts[5], 'placement': 'right'},
                 'liHelpTab': {'type': 'id', 'text': texts[6], 'placement': 'right'},
                 'liQuickStartTab': {'type': 'id', 'text': texts[7], 'placement': 'right'},
                 'liGoogleTab': {'type': 'id', 'text': texts[8]},
                 'liLicenseTab': {'type': 'id', 'text': texts[9], 'placement': 'right'},
                 'cl_thPublisher': {'type': 'class', 'text': texts[10]},
                 'cl_thServiceTitle': {'type': 'class', 'text': texts[11]},
                 'cl_thKeyword': {'type': 'class', 'text': texts[12]},
                 'cl_thSiteName': {'type': 'class', 'text': texts[13]},
                 'cl_thDataType': {'type': 'class', 'text': texts[14]},
                 'cl_thValueType': {'type': 'class', 'text': texts[15]},
                 'cl_thSampleMedium': {'type': 'class', 'text': texts[16]},
                 'cl_thQcLevel': {'type': 'class', 'text': texts[17]},
                 'cl_thMethod': {'type': 'class', 'text': texts[18]},
                 'cl_thCollector': {'type': 'class', 'text': texts[19]},
                 'cl_thStartDate': {'type': 'class', 'text': texts[20]},
                 'cl_thEndDate': {'type': 'class', 'text': texts[21]}, 
                 'cl_thValueCount': {'type': 'class', 'text': texts[22]},
                 'cl_thVariableName': {'type': 'class', 'text': texts[23]},
                 'cl_thTimeSupport': {'type': 'class', 'text': texts[24]},
                 'cl_thTimeUnit': {'type': 'class', 'text': texts[25]},
                 'cl_thServiceUrl': {'type': 'class', 'text': texts[26]},
                 'cl_thSiteCode': {'type': 'class', 'text': texts[27]},
                 'cl_thVariableCode': {'type': 'class', 'text': texts[28]},
                 'cl_thStatus': {'type': 'class', 'text': texts[29]},
                 'cl_thVariableUnits': {'type': 'class', 'text': texts[30]},
                 'cl_thSeriesId': {'type': 'class', 'text': texts[31]},
                 'cl_thCreated': {'type': 'class', 'text': texts[32]}
               };

    for (var div in divs) {

        //Select id or class - as indicated...
        var divTooltip = ('id' === divs[div].type) ? $('#' + div) : $('.' + div);

        if ( 0 < divTooltip.length) {
            //jQuery object exists...
            
            //Create tooltip on 'mouseenter'...
            divTooltip.off('mouseenter', tooltipCreate);
            divTooltip.on('mouseenter', {'div': div, 'divs': divs}, tooltipCreate);

            //Destroy tooltip on 'mouseleave'...
            divTooltip.off('mouseleave', tooltipDestroy);
            divTooltip.on('mouseleave', tooltipDestroy);
        }
    }

}

//Create a tooltip on the current instance...
function tooltipCreate(event) {
                var myDiv = $(this);
                var div = event.data.div;
                var divs = event.data.divs;

                myDiv.tooltip({
                                'placement': divs[div].placement ? divs[div].placement: 'auto',
                                'trigger': 'hover',
                                'title': divs[div].text,
                                'delay': divs[div].delay ? divs[div].delay: {'show': 100, 'hide': 100},
                                'template': '<div class="tooltip" role="tooltip"><div class="tooltip-arrow"></div><div style="text-align: justify; text-justify: distribute" class="tooltip-inner"></div></div>',
                                'container': 'body'}); //MUST use container option in datatables... source: http://stackoverflow.com/questions/33858135/bootstrap-popover-overlay-by-datatables-jquery
                myDiv.tooltip('show');                
}

//Destroy the tooltip on the current instance...
function tooltipDestroy() {
    $(this).tooltip('destroy');
}


//Initialize an individual 'static' tooltip...
function setupToolTip( divToolTip, textToolTip) {

        var div = $('#' + divToolTip);

        if ('undefined' === typeof div || null === div) {
            return;
        }

        div.tooltip('destroy');
        div.tooltip({
                    'animation': true,
                    'placement': 'auto',
                    'trigger': 'hover',
                    'title': textToolTip });
}


//Set up Data Manager datatables instance...
function setupDataManagerTable() {

    var tableName = 'tblDataManager';
    var tableId = '#' + tableName;

    if ($.fn.DataTable.isDataTable(tableId)) {
        $(tableId).DataTable().clear().destroy();
    }

    //Reset the selected row count...
    selectedRowCounts[tableName].count = 0;

    //BCC - 30-Nov-2015 - DataTables feature!!!
    //      If you specify a column title using the 'sTitle' option, DataTables uses that value, not the one specified in the *.cshtml file containing
    //       the <th> elements for the table headers. So if you have added links/icons for zendesk help files in the *.cshtml file - you won't see them 
    //       when the table displays in the browser...
    var oTable = $(tableId).dataTable({
        'deferRender': false,
        'dom': 'C<"clear"><"toolbarDataMgr">frtilp',   //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
        "autoWidth": false,
        'columns': [
           { 'data': 'TimeSeriesRequestStatus', 'visible': true,
             'render': function (data, type, full, meta) { 

                 var html = ''
                 var val = parseInt(data);
                 switch (val) {
                    case timeSeriesRequestStatus.NotStarted:
                        html = '<span class="glyphicon glyphicon-refresh spin" style="color: #32cd32"></span><em> Not Started</em>';
                        break;
                    case timeSeriesRequestStatus.Starting:
                        html = '<span class="glyphicon glyphicon-refresh spin" style="color: #32cd32"></span><em> Starting</em>';
                        break;
                    case timeSeriesRequestStatus.SavingZipArchive:
                        html = '<span class="glyphicon glyphicon-refresh spin" style="color: #32cd32"></span><em> Saving archive</em>';
                        break;
                    case timeSeriesRequestStatus.ProcessingTimeSeriesId:
                    case timeSeriesRequestStatus.Started:
                        html = '<span class="glyphicon glyphicon-refresh spin" style="color: #32cd32"></span><em> Generating</em>';
                        break;
                    case timeSeriesRequestStatus.Completed:
                        html = '<span class="glyphicon glyphicon-thumbs-up" style="color: #32cd32"></span><em> Completed</em>';
                        break;
                    case timeSeriesRequestStatus.CheckTaskError:
                    case timeSeriesRequestStatus.EndTaskError:
                    case timeSeriesRequestStatus.ProcessingError:
                    case timeSeriesRequestStatus.RequestTimeSeriesError:
                        html = '<span class="glyphicon glyphicon-thumbs-down"></span><em> Error</em>';
                            if ( 'undefined' !== typeof full.statusTooltipText) {
                                //Add status tooltip...
                                html = '<span class="glyphicon glyphicon-thumbs-down" data-toggle="tooltip" title="' + full.statusTooltipText + '"></span><em> Error</em>';
                            }
                        break;
                    case timeSeriesRequestStatus.UnknownTask:
                        html = '<span class="glyphicon glyphicon-thumbs-down"></span><em> Unknown task</em>';
                        break;
                    default:
                        console.log('tblDataManager data = ' + data.toString());
                        html = '<span class="glyphicon glyphicon-question-sign"></span><em> Unknown Status</em>';
                        break;
                 }

                 return html;
             }
           },
           { 'sTitle': 'Saved?',
             //'data': 'Saved',  'visible': false,
             'data': 'Saved',  'visible': false,
             'render': function (data, type, full, meta) { 
                 return '<span class="glyphicon ' + (data ? 'glyphicon-cloud' : 'glyphicon-cloud-upload') + '"></span> ' + (data ? '<em>Saved</em>' : '<em>Not Saved</em>');
             }
           },
           { 'data': 'Organization', 'visible': true, 'width': '10em',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText
           },
           { 'data': 'ServTitle', 'visible': true, 'width': '10em',
                 'render': function (data, type, full, meta) { 
                            if ('display' === type) {
                                return formatServiceTitleAbbreviated( full.ServiceCode, data );
                            }
                            else {
                                return data;
                            }
                 },
                 'createdCell': createdTooltipText
           },
           { 'data': 'ConceptKeyword', 'visible': true, 'width': '10em',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText
           },
           { 'data': 'SiteName', 'visible': true, 'width': '10em',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText
           },
           { 'data': 'VariableName', 'visible': true, 'width': '10em',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText
           },   
           { 'data': 'QCLID', 'visible': false },
           { 'data': 'QCLDesc', 'visible': true, 'width': '10em', 'className': 'td-textalign-center',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText             
           },
           { 'data': 'MethodId', 'visible': false },
           { 'data': 'MethodDesc', 'visible': true, 'width': '10em', 'className': 'td-textalign-center',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText             
           },
           { 'data': 'SourceId', 'visible': false },
           { 'data': 'SourceOrg', 'visible': true, 'width': '10em', 'className': 'td-textalign-center',
             'render': renderAbbreviatedText,
             'createdCell': createdTooltipText             
           },
           { 'data': 'VariableUnits', 'visible': true, 'width': '5em', 'className': 'td-textalign-center',
             'render': function (data, type, full, meta) {
                 var msg = 'VariableUnits is: ';
                 var msgOut = '';

                 if ( 'undefined' == typeof data ) {
                    msgOut = msg + 'undefined'
                 }
                 else if (null === data) {
                    msgOut = msg + 'null'
                 } 
                 else if ( '' === data) {
                    msgOut = msg + 'empty'
                 }
                 
                 if ( '' !== msgOut) {
                    console.log(msgOut);
                 }

                return renderAbbreviatedText(data, type, full, meta);
             }                      
           },
           { 'data': 'DataType', 'visible': true, 'width': '5em', 'className': 'td-textalign-center' },
           { "data": "ValueType", "visible": true, 'width': '5em', 'className': 'td-textalign-center' },
           { 'data': 'SampleMedium', 'visible': true, 'width': '5em', 'className': 'td-textalign-center' },
           { 'data': 'BeginDate', 'sTitle': 'Start Date', 'visible': true, 'width': '10em',
             'render': function (data, type, full, meta) { 
                 return formatDate(data);
             }           
           },
           { 'data': 'EndDate', 'sTitle': 'End Date', 'visible': true, 'width': '10em',
             'render': function (data, type, full, meta) { 
                 return formatDate(data);
             }           
           },
           { 'data': 'ValueCount', 'visible': true, 'width': '10em', 'className': 'td-textalign-center',
                 'render': function (data, type, full, meta) { 
                     return (0 === data) ? 'unknown' : data;
                 }                      
           },
           { 'data': 'TimeSupport', 'visible': true, 'width': '5em', 'className': 'td-textalign-center' },
           { 'data': 'TimeUnit', 'visible': true, 'width': '5em', 'className': 'td-textalign-center' },
           { 'data': 'SeriesId', 'visible': true, 'width': '5em', 'className': 'td-textalign-center' },
           //BCC - Make these columns visible for testing...
           { 'data': 'WofUri', 'visible': false },
           { 'data': 'WofTimeStamp', 'visible': true,
             'render': function (data, type, full, meta) { 
                 return formatDate(data, true);
             }           
           },
           { 'data': 'TimeSeriesRequestId', 'visible': false}
        ],

        'scrollX': true,
        'scrollY': '30em',
        'scrollCollapse': true,
        'initComplete': function () {


            //Adjust column sizing...
            var dt = $(tableId).DataTable();
            
            dt.columns.adjust();
            dt.draw();

            //Revise layout for table's info, length and pagination controls...
            $('#' + tableName + '_info').css({'width': '25%'});

            $('#' + tableName + '_length').css({'width': '50%', 
                                             'height': '2.5em'});

           $('#' + tableName + '_length' + ' > label').css({'text-align': 'center',
                                                         'display': 'block',
                                                         'margin-top': '1.0em'});

            $('#' + tableName + '_paginate').css({'width': '25%'});

            setupToolTips();
        }
    });

    //Suppress DataTable display of server errors as alerts...
    $.fn.dataTable.ext.errMode = 'none',

    //Server-error handler for tblDataManager...
    $(tableId).on('error.dt', function (event, settings, techNote, message) {

        //open 'Server Error' dialog
        $('#serverMessageText').text(message);
        $('#serverErrorModal').modal('show');

        //Log messsage received from server...
        console.log( tableName + ' reports error: ' + message);
    });

    //Make each row selectable by clicking anywhere on the row
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    var tblSelector = tableId + ' tbody';
    $(tblSelector).off('click', 'tr', toggleSelectedDataManager);
    $(tblSelector).on('click', 'tr', { 'tableId': tableName}, toggleSelectedDataManager);

    //Add a custom toolbar to the table...
    $('div.toolbarDataMgr').addClass('container');
    $('div.toolbarDataMgr').html(
                                  '<div class="inline-form">' +
                                  '<div class="form-group" style="float:left; display:inline-block;">' +

                                    '<div class="dropdown" style="position: relative; display: inline-block; float: left; font-size: 1.00em;">' +
                                      '<button class="btn btn-primary dropdown-toggle" type="button" id="dropdownMenu1" data-toggle="dropdown" aria-haspopup="true" aria-expanded="true">' +
                                        '<span class="glyphicon glyphicon-list-alt" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                        '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Selections&nbsp;</span>' +
                                        '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                      '</button>' +
                                      '<ul class="dropdown-menu" aria-labelledby="dropdownMenu1">' +
                                        '<li><a href="#" id="anchorAllSelectionsDataMgr" style="font-weight: bold;" >' + 
                                        '<span class="glyphicon glyphicon-plus-sign"></span>' +
                                        '<span class="text-muted">&nbsp;Select All</span>' +
                                        '</a></li>' +
                                        '<li><a href="#" id="anchorClearSelectionsDataMgr" style="font-weight: bold;">' + 
                                        '<span class="glyphicon glyphicon-minus-sign"></span>' +
                                        '<span id="spanClearSelectionsDM" class="text-warning">&nbsp;Clear Selections</span>' +
                                        '</a></li>' +
                                        '<li><a href="#" id="anchorRemoveSelectionsDataMgr" style="font-weight: bold;">' +
                                        '<span class="glyphicon glyphicon-trash"></span>' +                                
                                        '<span id="spanRemoveSelectionsDM" class="text-danger">&nbsp;Delete Selections</span>' + 
                                        '</a></li>' +
                                      '</ul>' +
                                    '</div>' +

                                  '<div style="display: inline-block; margin-left: 0.5em; vertical-align: top">' +
                                  '<div>' +

                                       //Source(s) for Bootstrap dropdown styling:
                                       //    http://jsfiddle.net/KyleMit/5p341amh/
                                       //    http://jsfiddle.net/74eu3y15/
                                      '<div class="dropdown" id="ddHydrodataToolDataMgr">' +    //Integrated Data Tools Dropdown
                                        '<button class="btn btn-primary" data-toggle="dropdown" style="font-size: 1.00em;">' +
                                              '<div id="ddIntegratedDataToolsOriginal" style="display: none;">' + 
                                                '<span class="glyphicon glyphicon-wrench" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                                '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Tool&nbsp;</span>' +
                                                '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                              '</div>' +
                                              '<div id="ddIntegratedDataTools" data-noneselected="true">' + 
                                                '<span class="glyphicon glyphicon-wrench" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                                '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Tool&nbsp;</span>' +
                                                '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                              '</div>' +
                                        '</button>' +

                                           //Application list...
                                           '<ul id="ddHydroshareList" class="dropdown-menu" style="width: 20em;">' +
                                           //Export section...
                                            '<li class="dropdown-header" id="exportApps">' +
                                               '<ul style="list-style: none; padding-left: 0em;">' +
                                                '<li data-toggle="tooltip" data-placement="top" title="Export all selected time series to the client in CSV data format">' +
                                                    '<a  id="idDownloadToClient" tabindex="-1" href="#" style="font-weight: bold; font-size: 1.0em;">' +
                                                            '<span class="glyphicon glyphicon-download" style="max-width: 100%; font-size: 1.5em;"></span>' +
                                                            '<span style="font-weight: bold; display: inline-block; vertical-align: super;">' + '&nbsp;Save as CSV' + '</span>' +
                                                    '</a>' +
                                                '</li>' +
                                               '</ul>' +

                                            '</li>' +
                                            '<li role="separator" class="divider"></li>' +

                                            //Visualization section...
                                            '<li class="dropdown-header" id="byuApps">' +
                                            '</li>' +

                                            '<li role="separator" class="divider"></li>' +

                                            //'None' option...
                                            '<li><a id="idNone" tabindex="-1" href="#" style="font-size: 1.0em;">' +
                                               '<span class="glyphicon glyphicon-remove-sign" style="max-width: 100%; font-size: 1.5em;"></span>' +
                                               ' <span style="font-weight: bold; display: inline-block; vertical-align: super;">' + 'None' + '</span>' +
                                            '</a></li>' +

                                          '</ul>' +
                                      '</div>' +
                                  '</div>' +

                                  '</div>' +
                                  '<div id="divLaunchHydrodataToolDataMgr" style="display: inline-block;">' +
                                    '<input type="button" style="margin-left: 0.5em;" class="ColVis-Button btn btn-primary" disabled id="btnLaunchHydrodataToolDataMgr" value="Launch Tool" />' +
                                  '</div>' +
                                  '<span class="clsMessageArea" style="display: none; margin-left: 2em;"></span>' +
                                  '</div>' + 
                                  '</div>' );

    $('div.toolbarDataMgr').css('display', 'inline-block');

    //Populate the select with the BYU apps...
    loadByuHydroshareApps();

    //Avoid multiple registrations of the same handler 
    $('#anchorAllSelectionsDataMgr').off('click', selectAll);
    $('#anchorAllSelectionsDataMgr').on('click', { 'tableId': tableName  }, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#anchorClearSelectionsDataMgr').off('click', clearSelections);
    $('#anchorClearSelectionsDataMgr').on('click', { 'tableId': tableName }, clearSelections);

    //Avoid multiple registrations of the same handler...
    $('#anchorRemoveSelectionsDataMgr').off('click', removeSelections);
    $('#anchorRemoveSelectionsDataMgr').on('click', { 'tableId': tableName}, removeSelections);

    //Avoid multiple registrations of the same handler...
    $('#anchorSaveSelectionsDataMgr').off('click', saveSelections);
    $('#anchorSaveSelectionsDataMgr').on('click', { 'tableId': tableName}, saveSelections);

    //Avoid multiple registrations of the same handler...
    $('#btnLaunchHydrodataToolDataMgr').off('click', downloadSelections);
    $('#btnLaunchHydrodataToolDataMgr').on('click', { 'divId': 'ddIntegratedDataTools', 'tableId': tableName, 'appName': 'Save as CSV' }, downloadSelections);

    //Avoid multiple registrations of the same handler...
    $('#btnLaunchHydrodataToolDataMgr').off('click', launchByuHydroshareApp);
    $('#btnLaunchHydrodataToolDataMgr').on('click', {'divId': 'ddIntegratedDataTools', 'tableId': tableName, 'getApps': getByuAppsList}, launchByuHydroshareApp);
    
    //Avoid multiple registrations of the same handler...
    $('#ddHydrodataToolDataMgr').off('click', 'li a', ddHydrodataToolDataMgr);
    $('#ddHydrodataToolDataMgr').on('click', 'li a', {'divId': 'ddIntegratedDataTools', 'divIdOriginal': 'ddIntegratedDataToolsOriginal', 
                                                        'tableId': tableName, 'getApps': getByuAppsList, 'divLaunchId': 'divLaunchHydrodataToolDataMgr' }, ddHydrodataToolDataMgr);
}

//Clear all table selections...
function clearDatatableSelections(event) {
    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });     //Retrieve rows per current sort/search order...
    var nodesRendered = rows.nodes();                                       //Retrieve all the rendered nodes for these rows
    //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Update data in each row...
    rows.every(function() {
        var rowData = this.data();

        rowData.Selected = false;
        this.data(rowData);
    });

    //Check button state...
    enableDisableButton(event.data.tableId, event.data.btnId);
}

function getByuAppsList() {
    return byuAppsList;
}

//Load the list of BYU Hydroshare apps...
function loadByuHydroshareApps() {

    //Initialize list contents and apps object
    var appsDropdown = $('ul#ddHydroshareList > li.dropdown-header#byuApps');

    byuAppsList = {};

    var proxyURL = '/Export/GetHydroshareAppsList';
    $.ajax({
        url: proxyURL,
        type: 'GET',
        async: true,
        dataType: 'json',
        cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
        success: function (data, textStatus, jqXHR) {

            //Success - populate apps object and select
            byuAppsList = data;

            var apps = byuAppsList.apps;
            var length = apps.length;
            var listItems = '<ul style="list-style: none; padding-left: 0em;">';

            for (var i = 0; i < length; ++i) {
                //Source(s) for Bootstrap dropdown styling:
                //    http://jsfiddle.net/KyleMit/5p341amh/
                //    http://jsfiddle.net/74eu3y15/
                //NOTE: Use of styles: max-width: 100%, height and width to scale app icons...
                //Source: https://wpbeaches.com/make-images-scale-responsive-web-design/
                listItems += '<li data-toggle="tooltip" data-placement="top" title="' + apps[i].description + '"><a  style="font-weight: bold; font-size: 1.0em;" tabindex="-1" href="#">' +
                             '<img src="' + apps[i].icon + '"' +
                             ' style="max-width: 100%;height: 2em; width: 2em;">' + 
                              ' ' + apps[i].name +
                             '</a></li>';
            }

            listItems += '</ul>';
            appsDropdown.append(listItems);

            //initialize tooltips...
            $('[data-toggle="tooltip"]').tooltip();
        },
        error: function (xmlhttprequest, textStatus, message) {

            //Failure - Log messsage received from server...
            console.log('BYU Hydroshare API reports error: ' + xmlhttprequest.status + ' (' + message + ')');
        }                
                    });
}

//Update the span text and image with the selection from the Bootstrap 'dropdown'
function ddHydrodataToolDataMgr(event) {

    var appName = $(this).text();
    var img = $('img', this);
    var id = $(this).attr("id");
    var imgStyle = null;

    $('#' + event.data.divLaunchId).tooltip('destroy');     //Remove any previously set tooltip on the 'launch' button... 
    $('#' + event.data.divId).removeClass();                //Remove any previously set classes...

    if ( 'idNone' === id) {
        //'None' option selected - retrieve 'original' values...
        $('#' + event.data.divId).html($('#' + event.data.divIdOriginal).html());
        $('#' + event.data.divId).attr('data-noneselected', "true");
        $('#' + event.data.divId).show();

        return;
    }
    else if ('idDownloadToClient' === id ) {
        //'Download to Client' option selected - set option name and glyphicons...
        $('#' + event.data.divId + ' img').html('');

        var obj = $('#' + id);
    
        $('#' + event.data.divId).html(obj.html());
        $('#' + event.data.divId).css('height', '1.5em');
        $('#' + event.data.divId).attr('data-noneselected', "false");

        return;
    }
    else {
        imgStyle = {'max-width': '100%', 'height': '1.5em', 'width': '2.0em'};    
        }

    //Set the selected option...
    $('#' + event.data.divId + ' img').removeAttr( 'style');
    $('#' + event.data.divId + ' img').removeAttr( 'class');
    $('#' + event.data.divId + ' img').html('');

    $('#' + event.data.divId).text(appName);
    $('#' + event.data.divId).prepend( '<img src="' + img.attr('src') + '">' );
    $('#' + event.data.divId).css('font-weight', 'bold');
    $('#' + event.data.divId).css('font-size', '1.0em');
    $('#' + event.data.divId).attr('data-noneselected', "false");


    if (null !== imgStyle) {
        $('#' + event.data.divId + ' img').css(imgStyle);
    }

    //Set min and max series tooltip for current selection...
    var apps = event.data.getApps().apps;
    var length = apps.length;

    for (var i = 0; i < length; ++i) {
        if ( appName.trim() === apps[i].name) {
            var maxSeries = apps[i].max_series;
            var minSeries = apps[i].min_series;
    
            if ( 0 < minSeries && 0 < maxSeries ) {
                var tooltip = 'Please select ' + minSeries + ' to ' + maxSeries + ' time series';
                if (maxSeries === minSeries) {
                    if ( 1 < maxSeries) {
                        tooltip = 'Please select ' + maxSeries + ' time series';
                    }
                    else {
                        tooltip = 'Please select one time series';                    
                    }
                }
            
                $('#' + event.data.divLaunchId).tooltip({'title': tooltip});    //Set tooltip on the 'launch' button... 
            }

            break;    
        }
    }
}

//Return true to disable the input button Id, false otherwise
function btnDisableCheck(buttonId) {

    var button = $('#' + buttonId);
    var result = false;

    if ('btnLaunchHydrodataToolDataMgr' === buttonId ) {
        var appName = $('#' + 'ddIntegratedDataTools').text().trim();
//        console.log(appName);

        var table = $('#' + 'tblDataManager').DataTable();
        var selectedRows = table.rows('.selected').data();
        var rowsLength = selectedRows.length;

        var apps = getByuAppsList().apps;
        var length = apps.length;

        for (var i = 0; i < length; ++i) {
            if ( -1 !== appName.indexOf(apps[i].name)) {    //Need index of here - Bootstrap tooltip display appends tooltip text to string returned by text() call!!
                var maxSeries = apps[i].max_series;
                var minSeries = apps[i].min_series;

                if (minSeries > rowsLength || maxSeries < rowsLength ) {
                    result = true;  //Rows selected out of range - disable launch button...
                }

                break;
            }
        }
    }

    return result;
}

//Old style code...
//function launchByuHydroshareApp(event) {

//    var tableId = '#' + event.data.tableId;
//    var table = $(tableId).DataTable();
//    var apps = event.data.getApps().apps;

//    //Currently selected BYU app
//    var valueSelected = $('#' + event.data.divId).text().trim();
//    var byuUrl= null;

//    //New selection - find the associated app URL...
//    var length = apps.length;

//    for (var i = 0; i < length; ++i) {
//        if (valueSelected === apps[i].name) {
//            byuUrl = apps[i].url;
//            break;
//        }
//    }

//    if (null !== byuUrl) {
//        //URL found - find selected water one flow archives...
//        var selectedRows = table.rows('.selected').data();
//        var rowsLength = selectedRows.length;
//        var wofParams = '';
//        var extension = '.zip';

//        for (var ii = 0; ii < rowsLength; ++ii) {
//            if (timeSeriesRequestStatus.Completed == selectedRows[ii].TimeSeriesRequestStatus) {
//                if ('' !== wofParams) {
//                    wofParams += ',';
//                }
//                wofParams += (selectedRows[ii].WofUri.split(extension))[0];
//            }
//        }

//        if ( '' !== wofParams) {
//            //Selections found - call BYU app with parameters...
//            // URL format: [app base name]/?src=cuahsi&res_id=abcdefj+abcdefh+abcedfi+abcdefk 
//            var fullUrl = byuUrl + '/?src=cuahsi&res_id=' + wofParams;

//            window.open(fullUrl, '_blank', '', false);
            
//        }
//    }
//}

//New style code - form-based...
//function launchByuHydroshareApp(event) {

//    var tableId = '#' + event.data.tableId;
//    var table = $(tableId).DataTable();
//    var apps = event.data.getApps().apps;

//    //Currently selected BYU app
//    var valueSelected = $('#' + event.data.divId).text().trim();
//    var byuUrl= null;

//    //New selection - find the associated app URL...
//    var length = apps.length;

//    for (var i = 0; i < length; ++i) {
//        if (valueSelected === apps[i].name) {
//            byuUrl = apps[i].url;
//            break;
//        }
//    }

//    if (null !== byuUrl) {
//        //URL found - find selected water one flow archives...
//        var selectedRows = table.rows('.selected').data();
//        var rowsLength = selectedRows.length;
//        var wofParams = [];
//        var extension = '.zip';

//        for (var ii = 0; ii < rowsLength; ++ii) {
//            if (timeSeriesRequestStatus.Completed == selectedRows[ii].TimeSeriesRequestStatus) {
//                var row = selectedRows[ii];
//                var item = { 'WofUri': (row.WofUri.split(extension))[0],
//                             'QCLID': row.QCLID,
//                             'MethodId': row.MethodId,
//                             'SourceId': row.SourceId
//                            };
//                wofParams.push(item);
//            }
//        }

//        //Create a dynamic form and submit to BYU URL...
//        // Sources: http://htmldog.com/guides/javascript/advanced/creatingelements/
//        //          http://stackoverflow.com/questions/30835990/how-to-submit-form-to-new-window
//        //          http://jsfiddle.net/qqzxtk67/
//        //          http://stackoverflow.com/questions/17431760/create-a-form-dynamically-with-jquery-and-submit
//        //          http://jsfiddle.net/MVXXX/1/
//        if ( 0 < wofParams.length) {
//            //Remove/create form...
//            $('form#dataViewerForm').remove();
//            var jqForm = $('<form id="dataViewerForm"></form>').appendTo(document.body);

//            //Add method, action and target...
//            var targetWindow = 'dataViewerWindow';

//            //BCC - Test - 07-Oct-2016 - Try 'get' rather than 'post'
//            // Form arguments added to URI line as explained in source - however, appsdev server still returns a 301...
//            //SOURCE: https://developer.mozilla.org/en-US/docs/Web/Guide/HTML/Forms/Sending_and_retrieving_form_data
//            //jqForm.attr('method', 'post');
//            jqForm.attr('method', 'get');
//            jqForm.attr('action', byuUrl);
//            jqForm.attr('target', targetWindow);

//            //Append source...
//            jqForm.append('<input type="hidden" name="Source" value="cuahsi">');

//            //Append child list...
//            jqForm.append('<ul id="wofParams"></ul>');

//            //For each wofParams item...
//            var itemsLength = wofParams.length;
//            var jqList = $('#wofParams');


//            for (var iii = 0; iii < itemsLength; ++iii) {
//                //Append to child list...
//                var item = wofParams[iii];
//                jqList.append('<li>' +
//                              '<input type="hidden" name="WofUri" value="' + item.WofUri + '">' +
//                              '<input type="hidden" name="QCLID" value="' + item.QCLID + '">' +
//                              '<input type="hidden" name="MethodId" value="' + item.MethodId + '">' +
//                              '<input type="hidden" name="SourceId" value="' + item.SourceId + '">' +
//                              '</li>'
//                             ); 
//            }

//            //Open Data Viewer window, submit form...
//            window.open('', targetWindow, '', false);    
//            jqForm.submit();
//        }
//    }
//}

//New style code - expanded URI parameters...
function launchByuHydroshareApp(event)
{
    var tableId = '#' + event.data.tableId;
    var table = $(tableId).DataTable();
    var apps = event.data.getApps().apps;

    //Currently selected BYU app
    var valueSelected = $('#' + event.data.divId).text().trim();
    var byuUrl= null;

    //New selection - find the associated app URL...
    var length = apps.length;

    for (var i = 0; i < length; ++i) {
        if (valueSelected === apps[i].name) {
            byuUrl = apps[i].url;
            break;
        }
    }

    if (null !== byuUrl) {
        //URL found - find selected water one flow archives...
        var selectedRows = table.rows('.selected').data();
        var rowsLength = selectedRows.length;
        var wofParams = '';
        var extension = '.zip';

        for (var ii = 0; ii < rowsLength; ++ii) {
            if (timeSeriesRequestStatus.Completed == selectedRows[ii].TimeSeriesRequestStatus) {
                var row = selectedRows[ii];

                var params = 'WofUri=' + (row.WofUri.split(extension))[0] +
                             '&QCLID=' + row.QCLID +
                             '&MethodId=' + row.MethodId +
                             '&SourceId=' + row.SourceId;
                wofParams += '&' + params;
            }
        }

        if ( '' !== wofParams) {
            //Selections found - call BYU app with parameters...
            // URL format: [app base name]/?Source=cuahsi&WofUri=...&QCLID=...&MethodId=...&SourceId=...[&WofUri=... ...]
            var fullUrl = byuUrl + '/?Source=cuahsi' + wofParams;

            window.open(fullUrl, '_blank', '', false);
        }
    }
}

//Draw event handler...
function retrieveWaterOneFlowForTimeSeries(event) {

    //console.log('retrieveWaterOneFlowForTimeSeries(...) called for table: ' + event.data.tableId);
    var tableName = event.data.tableId;
    var tableId = '#' + tableName;
    var table = $(tableId).DataTable();

    var modalDialogName = event.data.modalDialogId;

    //Scan table data for 'Not started' time series request(s)...
    var reDraw = false;

    table.rows().every(function (rowIdx, tableLoop, rowLoop) {

        var rowData = this.data();
        if (timeSeriesRequestStatus.NotStarted === rowData.TimeSeriesRequestStatus) {
            //'Not started' request found - initiate request...
            var requestId = randomId.generateId();

            //Update table row with newly generated request Id and 'Starting' status...
            rowData.TimeSeriesRequestId = requestId;
            rowData.TimeSeriesRequestStatus = timeSeriesRequestStatus.Starting;
            this.invalidate();
            reDraw = true;

            //Request time series from server...
            requestTimeSeries(tableName, { 'RequestName': 'CUAHSI-WDC',
                                              'RequestId': requestId,
                                              'TimeSeriesIds': [rowData.SeriesId],
                                              'RequestFormat': timeSeriesFormat.WaterOneFlow
                                            }, modalDialogName);
        }

    });

    if ( reDraw) {
        table.draw();
    }
}

//Call Export Controller RequestTimeSeries per the input values...
function requestTimeSeries(tableName, timeSeriesRequest, modalDialogName) {

    //Check for current entry...
    if (timeSeriesRequest.requestId in downloadMonitor.timeSeriesMonitored) {
        return;     //Current entry found - return early...
    }

    //Set monitor entry
    downloadMonitor.timeSeriesMonitored[timeSeriesRequest.RequestId] = { 'tableName': tableName,
                                                                         'timeSeriesRequest': timeSeriesRequest,
                                                                         'timeSeriesRequestStatus': timeSeriesRequestStatus.Starting 
                                                                       };

    //Call controller method...
    var timeSeriesRequestString = JSON.stringify(timeSeriesRequest);
    var actionUrl = "/Export/RequestTimeSeries";

    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        cache: false,           //Per IE...
        async: true,
        data: timeSeriesRequestString,
        contentType: 'application/json',
        context: timeSeriesRequestString,
        success: function (data, textStatus, jqXHR) {
            var response = jQuery.parseJSON(data);

            startRequestTimeSeriesMonitor();    //Start monitoring all current requests, if indicated...
        },
        error: function (xmlhttprequest, textstatus, message) {
            //Server error: Close modal dialog, if indicated - open 'Server Error' dialog
            if ($('#' + modalDialogName).is(":visible")) {
                $('#' + modalDialogName).modal('hide');
            }

            $('#serverMessageText').text(message);
            $('#serverErrorModal').modal('show');

            //Log messsage received from server...
            console.log('RequestTimeSeries reports error: ' + xmlhttprequest.status + ' (' + message + ')');

            //Set monitor status...
            var requestId = timeSeriesRequest.requestId;
            downloadMonitor.timeSeriesMonitored[requestId].timeSeriesRequestStatus = timeSeriesRequestStatus.RequestTimeSeriesError;

            //Update Data Manager row...
            updateTimeSeriesRequestStatus( tableName, requestId, timeSeriesRequestStatus.RequestTimeSeriesError);
        }
    });
}

//For the input table name and requestId, update the associated row with the input status...
function updateTimeSeriesRequestStatus( tableName, requestId, timeSeriesRequestStatus, status) {

    var tableId = '#' + tableName;
    var table = $(tableId).DataTable();

    //Scan table data for input requestId...
    var reDraw = false;
    //table.rows().every(function (rowIdx, tableLoop, rowLoop) {    //parameters available in DataTables v1.10.8+
    table.rows().every(function () {

        var rowData = this.data();
        if (requestId === rowData.TimeSeriesRequestId) {
            //Found - update row with input status
            rowData.TimeSeriesRequestStatus = timeSeriesRequestStatus;

            //Add status tooltip text to row, if indicated...
            // See related changes in setupDataManagerTable(), render function for table colummns
            //Source: https://datatables.net/forums/discussion/32240/how-to-implement-a-popup-tooltip-on-a-datatables-cell-that-displays-all-data
            if ( 'undefined' !== typeof status && null !== status) {
                
                //Add status tooltip text to row data...
                rowData.statusTooltipText = status;

                //Activate tooltip(s) on draw
                $(tableId).off('draw.dt', enableStatusTooltip);
                $(tableId).on('draw.dt', {'tableId': tableName }, enableStatusTooltip);
            }

            this.invalidate();
            reDraw = true;
        }
    });

    if ( reDraw) {
        table.draw();
    }
}

function enableStatusTooltip(event) {

    var tableId = event.data.tableId;

    if ( 'undefined' === typeof tableId || null === tableId) {
        return; //Invalid parameter(s) - return early...
    }

    $('#' + tableId + ' span[data-toggle="tooltip"]').tooltip();
}

function updateTimeSeriesBlobUri( tableName, requestId, blobUri, blobTimeStamp) {

    var tableId = '#' + tableName;
    var table = $(tableId).DataTable();

    //Scan table data for input requestId...
    var reDraw = false;
    table.rows().every(function () {

        var rowData = this.data();
        if (requestId === rowData.TimeSeriesRequestId) {
            //Found - update row with input status
            rowData.WofUri = blobUri;
            rowData.WofTimeStamp = blobTimeStamp;
            this.invalidate();
            reDraw = true;
        }
    });

    if ( reDraw) {
        table.draw();
    }
}

//Start monitoring all current time series requests, if indicated...
function startRequestTimeSeriesMonitor() {

    if ( null === downloadMonitor.intervalId) {
        //Monitor function not running - start...
        downloadMonitor.intervalId = setInterval(function() {

            //Create array of intervalIds
            var Ids = [];
            for (var requestId in downloadMonitor.timeSeriesMonitored) {
                Ids.push(requestId);
            }

            if ( 0 >= Ids.length) {
                return; //Return early - no requests to monitor...
            }

            var actionUrl =  "/Export/CheckTasks";
            $.ajax({
                url: actionUrl,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(Ids),
                success: function (data, textStatus, jqXHR) {

                    var timeSeriesResponses = JSON.parse(data);
                    var length = timeSeriesResponses.length;

                    for (var i = 0; i < length; ++i) {
                        var timeSeriesResponse = timeSeriesResponses[i];
                        var requestId = timeSeriesResponse.RequestId;

                        if ('undefined' !== typeof downloadMonitor.timeSeriesMonitored[requestId]) {
                            downloadMonitor.timeSeriesMonitored[requestId].timeSeriesRequestStatus = timeSeriesResponse.RequestStatus;
                            var tableName = downloadMonitor.timeSeriesMonitored[requestId].tableName;

                            if ( 'tblDownloadManager' === tableName) {
                                //Retrieve associated row from Download Manager table...
                                var tableRow = $("#tblDownloadManager tr td").filter(function () {
                                    return $(this).text() === requestId;
                                }).parent("tr");

                                //Check current status...
                                var statusCurrent = parseInt(tableRow.find('td:eq(5)').html());

                                //Update table row, if indicated...
                                if (statusCurrent !== timeSeriesRequestStatus.EndTaskError &&
                                    statusCurrent !== timeSeriesRequestStatus.UnknownTask &&
                                    statusCurrent != timeSeriesRequestStatus.RequestTimeSeriesError &&
                                    ( ! (statusCurrent === timeSeriesRequestStatus.ClientSubmittedCancelRequest && 
                                         timeSeriesResponse.RequestStatus === timeSeriesRequestStatus.ProcessingTimeSeriesId))) {
                                    tableRow.find('#statusMessageText').text(timeSeriesResponse.Status);
                                    tableRow.find('#blobUriText').html(timeSeriesResponse.BlobUri);
                                    tableRow.find('td:eq(5)').html(timeSeriesResponse.RequestStatus);

                                    //Color row as 'info'
                                    tableRow.addClass('info');
                                }

                                //Check for completed/cancelled task/unknown task/error
                                var requestStatus = parseInt(tableRow.find('td:eq(5)').html());

                                if (timeSeriesRequestStatus.Completed === requestStatus ||
                                    timeSeriesRequestStatus.CanceledPerClientRequest === requestStatus ||
                                    timeSeriesRequestStatus.RequestTimeSeriesError === requestStatus ||
                                    timeSeriesRequestStatus.EndTaskError === requestStatus ||
                                    timeSeriesRequestStatus.UnknownTask === requestStatus) {

                                    var dataRequestId = timeSeriesResponse.RequestId; 

                                    //If task completed - re-assign 'Stop Task' button to 'Download'
                                    if (timeSeriesRequestStatus.Completed === requestStatus) {
                                        //Success - retrieve the base blob URI... 
                                        var baseUri = (timeSeriesResponse.BlobUri).split('.zip');
                                        var blobUri = baseUri[0] += '.zip';

                                        //Retrieve the full file name from the base blob URI...
                                        var uriComponents = baseUri[0].split('/');
                                        var fileName = uriComponents[(uriComponents.length - 1)];

                                        //Set base blob URI into text in column 3
                                        //NOTE: Need to decode the string twice because:
                                        //       If the string contains an encoded character like %27 ('),
                                        //       the browser(?) separely encodes the (&) character as %25
                                        //       resulting in %25%27 in the string.  Two string decodes 
                                        //       are required to remove the two escape sequences...
                                        var decoded = decodeURIComponent(fileName);
                                        var decoded1 = decodeURIComponent(decoded);
                                        tableRow.find('#blobUriText').html(decoded1);

                                        //Create a download button...
                                        var button = tableRow.find('td:eq(4)');

                                        button = $('<button class="zipBlobDownload btn btn-success" style="font-size: 1.5vmin" data-blobUri="' + blobUri +
                                                        '" data-requestId="' + dataRequestId + '">Download Archive</button>"');

                                        button.on('click', function (event) {
                                            var blobUri = $(this).attr('data-blobUri');
                                            location.href = blobUri;

                                            if ( currentUser.authenticated) {
                                                var requestId = $(this).attr('data-requestId');
                                                deleteDownload(requestId);
                                            }

                                            //Fade row and remove from table...
                                            tableRow.fadeTo(1500, 0.5, function() {
                                                    $(this).remove();
                                            });

                                            event.stopPropagation();
                                        });

                                        tableRow.find('td:eq(4)').html(button);

                                        //Change the glyphicon and stop the animation
                                        var glyphiconSpan = tableRow.find('#glyphiconSpan');

                                        glyphiconSpan.removeClass('glyphicon-refresh spin');
                                        glyphiconSpan.addClass('glyphicon-thumbs-up');

                                        tableRow.find('#statusMessageText').text(timeSeriesResponse.Status);

                                        tableRow.addClass('success');   //Color row as 'successful'

                                        if ($("#chkbxAutoDownload").prop("checked")) {
                                                //Autodownload checkbox checked - click the newly created button...
                                            button.click();
                                        }
                                    }
                                    else {
                                        if (timeSeriesRequestStatus.CanceledPerClientRequest === requestStatus ||
                                            timeSeriesRequestStatus.UnknownTask === requestStatus || 
                                            timeSeriesRequestStatus.EndTaskError === requestStatus) {
                                            //Task canceled OR unknown OR end task error - color row as 'danger'
                                            tableRow.addClass('danger');

                                            //Fade row and remove from table...
                                            tableRow.fadeTo(1500, 0.5, function () {
                                                $(this).remove();
                                            });

                                            //Remove the entry from the database...
                                            if ( currentUser.authenticated) {
                                                deleteDownload(dataRequestId);
                                            }
                                        }
                                        else {
                                            if (timeSeriesRequestStatus.RequestTimeSeriesError === requestStatus) {
                                        
                                                //Request Time Series Processing Error - DO NOT remove the row!! 
                                        
                                                //Reset blob URI
                                                tableRow.find('#blobUriText').html('');

                                                //Hide the 'Stop Processing' button
                                                tableRow.find('td:eq(4)').hide();

                                                //Change the glyphicon and stop the animation
                                                var glyphiconSpan = tableRow.find('#glyphiconSpan');

                                                glyphiconSpan.removeClass('glyphicon-refresh spin');
                                                glyphiconSpan.addClass('glyphicon-thumbs-down');
                                                glyphiconSpan.css('color', 'red');

                                                //Color table row as 'warning'
                                                tableRow.removeClass('info');
                                                tableRow.addClass('warning');

                                                //Remove the entry from the database...
                                                if ( currentUser.authenticated) {
                                                    deleteDownload(dataRequestId);
                                                }
                                            }
                                        }
                                    }
                            
                                    //remove monitoring entry...
                                    delete downloadMonitor.timeSeriesMonitored[timeSeriesResponse.RequestId];
                                }
                            }
                            else { 
                                if ( 'tblDataManager' === tableName ) {
                                    var statusString = null;
                                    if (timeSeriesRequestStatus.RequestTimeSeriesError === timeSeriesResponse.RequestStatus ||
                                        timeSeriesRequestStatus.ProcessingError === timeSeriesResponse.RequestStatus ) {
                                            //Error - value status string...
                                            statusString = timeSeriesResponse.Status;
                                        }

                                    updateTimeSeriesRequestStatus( downloadMonitor.timeSeriesMonitored[requestId].tableName, requestId, timeSeriesResponse.RequestStatus, statusString);

                                    //Write the blob URI to the console...
                                    //console.log('requestId: ' + requestId);
                                    //console.log('Status: ' + timeSeriesResponse.Status);
                                    //console.log('BlobTimeStamp' + timeSeriesResponse.BlobTimeStamp);
                                    //console.log('BlobUri' + timeSeriesResponse.BlobUri);
                                    //console.log('*****************');

                                    if (timeSeriesRequestStatus.Completed === timeSeriesResponse.RequestStatus) {
                                        //Completed status - update table entry with received blob URI and time stamp, remove monitoring entry...
                                        var wofFileName = retrieveWaterOneFileArchiveFileName(timeSeriesResponse.BlobUri);
                                        var wofTimeStamp = timeSeriesResponse.BlobTimeStamp;
                                        updateTimeSeriesBlobUri( downloadMonitor.timeSeriesMonitored[requestId].tableName, requestId, wofFileName, wofTimeStamp);
                                        delete downloadMonitor.timeSeriesMonitored[timeSeriesResponse.RequestId];

                                        //Save the newly completed entry to the database...
                                        saveCompletedTimeseries(tableName, requestId);
                                    }
                                    else {
                                        if ( timeSeriesRequestStatus.UnknownTask === timeSeriesResponse.RequestStatus) {
                                            //BCC - TEST - 28-Oct-2015 - Comment out the delete here - early status calls for a new task might return unknown task

                                            if ( 'undefined' === typeof unknownTaskCounts[requestId] ) {
                                                unknownTaskCounts[requestId] = {'count': 0 };
                                                //console.log('Initialize unknown task count for: ' + requestId);
                                            }
                                            else {
                                                unknownTaskCounts[requestId].count += 1;
                                                //console.log('Increment unknown task count for: ' + requestId);

                                                if ( 20 <= unknownTaskCounts[requestId].count) {
                                                    //unknown task count exceeded - delete monitor entry...
                                                    delete downloadMonitor.timeSeriesMonitored[requestId];
                                                    delete unknownTaskCounts[requestId];
                                                }
                                            }
                                        }
                                        else {
                                            if (timeSeriesRequestStatus.CanceledPerClientRequest === timeSeriesResponse.RequestStatus || 
                                                timeSeriesRequestStatus.EndTaskError === timeSeriesResponse.RequestStatus ||
                                                timeSeriesRequestStatus.RequestTimeSeriesError === timeSeriesResponse.RequestStatus ||
                                                timeSeriesRequestStatus.ProcessingError === timeSeriesResponse.RequestStatus) {
                                                    //Request cancelled --OR-- server error - delete monitor entry...
                                                    delete downloadMonitor.timeSeriesMonitored[timeSeriesResponse.RequestId];
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                error: function (xmlhttprequest, textStatus, message) {

                    //For now - take no action...
                    //ASSUMPTION: Next request will succeed

                    //Log messsage received from server...
                    console.log(actionUrl + ' reports error: ' + xmlhttprequest.status + ' (' + message + ')');
                }                
            });

//        }, 1000);
        }, 5000);
    }

}

//Delete the server download record associated with the input request Id
function deleteDownload(requestId) {

    if ('undefined' === typeof requestId || null === requestId) {
        return; //Invalid parameter - return early...
    }

    var url = '/DownloadManager/Delete/' + requestId;

    var promise = $.ajax({
                        url: url,
                        type: 'DELETE',
                        dataType: 'json',
                        cache: false,           //Per IE...
                        async: true,
                        contentType: 'application/json' //,
                    });

    promise.done( function (data) {
            //console.log('/DownloadManager/Delete - successful!!')
        });

    promise.fail( function (jqXHR, textStatus, errorThrown) {
            console.log('/DownloadManager/Delete - error - ' + textStatus + ' - ' + errorThrown);
        });
}

//Retrieve the WaterOneFlow zip archive file name from a full Azure blob URI
// ASSUMPTION: Full Azure blob URI format: https://ziptest.blob.core.windows.net/time-series/cuahsi-wdc-2015-10-20-34754857.zip?
//                                          sv=2014-02-14&sr=b&sig=P2GRXRgmDZBuvCVeNp1b2922vJ%2FMMG4cj5HYWuB2y5I%3D&se=2015-11-19T13%3A39%3A14Z&sp=r'
function retrieveWaterOneFileArchiveFileName(fullAzureBlobUri) {

    if ('undefined' === typeof fullAzureBlobUri || null === fullAzureBlobUri) {
        return null;  //Invalid input parameter - return early
    }

    var firstIndex = fullAzureBlobUri.indexOf('cuahsi-wdc');
    var extension = '.zip';
    var lastIndex = fullAzureBlobUri.indexOf(extension)

    if ( -1 === firstIndex || -1 === lastIndex ) {
	    console.log('retrieveWaterOneFlowArchiveFileName - bad index for: ' + fullAzureBlobUri);
        return null;
    }

    var waterOneFlowArchiveFilename = fullAzureBlobUri.slice(firstIndex, (lastIndex + extension.length));
    return waterOneFlowArchiveFilename;
}

//Retrieve the Description Url from the Services DataTable for the input service code- Not found, return null
//Assumption: The services DataTable is loaded immediately upon page load or refresh!!
function getDescriptionUrl(serviceCode) {

    var descriptionUrl = null;
    if (('undefined' !== typeof serviceCode) && (null !== serviceCode)) {
        
        var table = $('#dtServices').DataTable();
        var data = table.rows().data();

        $.each(data, function (i, obj) {
            if (serviceCode === obj.ServiceCode) {
                descriptionUrl = obj.DescriptionUrl;
                return false;
            }
        });
    }

    //Processing complete - return
    return descriptionUrl;
}

//Create 'select'-based filters for the input tableId and columns array
function setfooterFilters(tableId, columnsArray, translatesArray, chkbxApplyFilterToMapId) {

    //Check for optional fourth parameter...
    var bTranslates = (('undefined' !== typeof translatesArray) && (null !== translatesArray));

    var tableColumns = {};
    var caLength = columnsArray.length;

    //Record tableId's and associated column indices of those columns containing footer selects...
    tableColumns[tableId] = {'columns': {} };

    for (var caI = 0; caI < caLength; ++caI) {
        tableColumns[tableId].columns[columnsArray[caI]] = '';
    }

    var api = $('#' + tableId).DataTable();

    //For each such column...
    api.columns().indexes().flatten().each(function (i) {
        if (-1 !== columnsArray.indexOf(i)) {
            var column = api.column(i);
            var select = $('<select class="max-width-10em"><option value="">Remove filter...</option></select>');
            var colIndex = column.index();
            var myTableId = tableId;
            var mychkbxApplyFilter = ('undefined' === typeof chkbxApplyFilterToMapId) ? null : chkbxApplyFilterToMapId;

            //Populate a select and add it to the column's footer...
            select.appendTo($(column.footer()).empty())
                .on('change', {'colIndex': colIndex }, function myChangeFunction(event) {   //Change event handler select...

                    //Retrieve the current values for each 'footer' select...
                    for (var prop in tableColumns[myTableId].columns) {

                        var myColumn2 = api.column(prop);
                        var mySelect2 = $(myColumn2.footer()).find('select');

                        //Question:  The placeholder option is disabled.  Does jQuery return a value === null for such options?
                        tableColumns[myTableId].columns[prop] = mySelect2.val();
                    }

                    //Retrieve the search string from the select...
                    var val = $.fn.dataTable.util.escapeRegex($(this).val());

                    //NOTE: Datatables API interprets 'data' for a <td> containing: <span class="glyphicon glyphicon-cloud-upload"></span> <em>Not Saved</em>
                    //      as: ' Not Saved'.  Therefore the regex pattern includes zero or more leading space character <' '> occurrences...

                    //Search the column for the search string...
                    var colIndex = 0;
                    if ( 'undefined' !== typeof event.data && null !== event.data) {
                        colIndex = parseInt(event.data.colIndex);
                    }

                    var myColumn1 = api.column(colIndex);
                    var dt = myColumn1.search(val ? '^[ ]*' + val + '$'  : '', true, false);

                    //Re-draw the table to display the results of the search...
                    dt.draw();

                    //Scan each column containing a footer select...
                    var ii = 0;
                    var length = columnsArray.length;
                    while (ii <= length) {
                        //NOTE: DO NOT skip the current column - in case user has removed a filter on the current column...
                        if (-1 === columnsArray.indexOf(ii) ) {
                            ++ii;       
                            continue;   //No footer select --OR-- the current column - skip
                        }

                        //Retrieve column contents per table's current search/sort order...
                        var myColumn = dt.column(ii, { 'order': 'current', 'search': 'applied' });
                        select = $('<select class="max-width-10em"><option value="">Remove filter...</option></select>');
                        if (null !== myColumn) {
                            //Retrieve the filter placeholder...
                            var footer = $(myColumn.footer());

                            var placeholder = footer.find("option[value='999999']").remove();
                            var footer = $(myColumn.footer()).empty();

                            //Re-populate the select...
                            setSelectOptions( select, ii, myColumn, translatesArray);

                            select.prepend(placeholder);    //Add and select the placeholder...
                            select.appendTo(footer).on('change', {'colIndex': ii }, myChangeFunction);        //Append the newly valued select to the column's footer...
                        }
                        ++ii;
                    }

                    //Re-select all previous selections, if indicated
                    for (var prop1 in tableColumns[myTableId].columns) {

                        var myColumn3 = api.column(prop1);
                        var mySelect3 = $(myColumn3.footer()).find('select');
                        var myValue3 = tableColumns[myTableId].columns[prop1];

                        if (null !== myValue3) {
                            if ('' != myValue3) {
                                mySelect3.find('option').filter(function() { return myValue3 === $(this).val(); }).prop('selected', true);
                            }
                            else {
                                //'Remove filter...' option selected - replace with placeholder...
                                mySelect3.find('option').filter(function() { return '999999' === $(this).val(); }).prop('selected', true); 
                            }
                        }
                        else {
                            //Placeholder option selected...
                            mySelect3.find('option').filter(function() { return '999999' === $(this).val(); }).prop('selected', true);                                                        
                        }
                    }

                    //Apply the change to the map...
                    var chkbx = $('#' + mychkbxApplyFilter);
                    chkbx.triggerHandler('click');
                });

            setSelectOptions( select, colIndex, column, translatesArray);
        }
   });
}


function setSelectOptions( select, currentIndex, dtColumn, translatesArray) {

    //Validate/initialize input parameters...
    //NOTE: translatesArray may be null!!
    if ( 'undefined' === typeof select || null === select ||
         'undefined' === currentIndex || null === currentIndex ||
         'undefined' === dtColumn || null === dtColumn) {
        return;     
    }

    var bTranslates = ('undefined' !== typeof translatesArray && null !== translatesArray);

    //var jqueryObj = $(dtColumn);
    dtColumn.data().unique().sort().each(function (d, j) {
    
                //Set the option string - may change...
                var optionString = '<option value="' + d + '">' + d + '</option>';

                //BCC - TO DO - bTranslates not used - remove logic and parameters...
                if (bTranslates) {
                    var length = translatesArray.length;
                    var bTranslated = false;
                    for (var tI = 0; (tI < length) && (! bTranslated); ++tI) {
                        var translate = translatesArray[tI];
                        if (currentIndex === translate.columnIndex) {
                            var translates = translate.translates;
                            var tsLength = translates.length;
                            for (var tsI = 0; (tsI < tsLength) && (! bTranslated); ++tsI) {
                                if (d === translates[tsI].value) {
                                    optionString = '<option value="' + translates[tsI].value + '">' + translates[tsI].translate + '</option>';
                                    bTranslated = true;
                                }
                            }
                        }
                    }
                }

                select.append(optionString);
            });
}


function updateServicesList()
{
    //Clear and re-populate services list...
    var list = $('#olServices');
    var length = mySavedServices.length;
    

    list.empty();

    if (0 < length) {
        //Certain services selected...
        list.append('<li>Selected ' + length.toString() + ' of ' + myServicesDatatable.fnGetData().length + '</li>');
    }
    else {
        //All services selected...
        list.append('<li>All</li>');
    }
}

function toggleSelectedDataManager(event) {

    //Conditionally select the current row...
    var rowData = $('#' + event.data.tableId).DataTable().row(this._DT_RowIndex).data();
    var requestId = rowData.TimeSeriesRequestId;
    var status = rowData.TimeSeriesRequestStatus;

    if ('undefined' === typeof downloadMonitor.timeSeriesMonitored[requestId] && 
         timeSeriesRequestStatus.Completed === status ) {
        //Current row is not monitored AND completed - allow selection...
        toggleSelected.call(this, event);   //Bind to the value of this!!!
    }
}

function toggleSelected(event) {
    var className = 'selected';

    var startRange = 'startRange';
    var endRange = 'endRange';

    //Check selected row count...
    var count = selectedRowCounts[event.data.tableId].count;

    var selectedTimeSeriesMax = parseInt($('#' + event.data.tableId).attr('data-selectedtimeseriesmax'));
    if ( (! currentUser.authenticated) && Number.isInteger(selectedTimeSeriesMax) && (selectedTimeSeriesMax <= count) && (!$(this).hasClass(className))) {
        //Maximum selections reached - current row not yet selected - warn and return early
        bootbox.alert('<h4>A maximum of ' + selectedTimeSeriesMax + ' time series rows may be selected.</h4>');
        return;
    }

    //Retrieve current table rows as jqueryObjects...
    var jqueryRows = $('#' + event.data.tableId).DataTable().rows().nodes().to$();
    var bStart = jqueryRows.hasClass(startRange);

    if (event.shiftKey && (!$(this).hasClass(className)) && bStart) {
        //User has pressed SHIFT+click - current row not yet selected - 'start range' assigned to another row - process range selection
        $(this).addClass(endRange);
        processRangeSelection(event.data.tableId, startRange, endRange, className);
        return;
    } 

    $(this).toggleClass(className);

    //Update selected row count...
    if ($(this).hasClass(className)) {
        selectedRowCounts[event.data.tableId].count = count + 1;

        //Set current row as 'start of range'
        jqueryRows.removeClass(startRange);
        $(this).addClass(startRange);

    } else {
        selectedRowCounts[event.data.tableId].count = count - 1;

        //Remove 'range' classes, if any
        $(this).removeClass(startRange);
        $(this).removeClass(endRange);
    }

    //Check button(s) state...
    if ('undefined' !== typeof event.data.btnIds) {
        enableDisableButtons(event.data.tableId, event.data.btnIds);
    }

    if ( 'undefined' !== typeof event.data.btnClearId) {
    enableDisableButton(event.data.tableId, event.data.btnClearId);
    }
}

//Process the range selection for the input table Id
function processRangeSelection(tableId, startRangeClass, endRangeClass, selectClass) {
    
    //Validate/initialize input parameters...
    if (( 'undefined' === typeof tableId) || (null === tableId) ||
        ('undefined' === typeof startRangeClass) || (null === startRangeClass) ||
        ('undefined' === typeof endRangeClass) || (null === endRangeClass) ||
        ('undefined' === typeof selectClass) || (null === selectClass)) {
        return;
    }

    //Check for presence of 'startRange' and 'endRange' classes on table rows...
    var table = $('#' + tableId).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
    var jqueryRows = rows.nodes().to$();

    var startRow = table.row('.' + startRangeClass);
    var endRow = table.row('.' + endRangeClass);

    if ((0 >= startRow.length) || (0 >= endRow.length)) {
        //Class(es) not found - return
        return;
    }

    //Determine start and end range positions
    var startPos = rows[0].indexOf(startRow.index());
    var endPos = rows[0].indexOf(endRow.index());

    //Verify start and end range positions are on the current table 'page'...
    var pageInfo = table.page.info();

    if (((startPos < endPos) && (startPos < pageInfo.start || endPos > pageInfo.end)) ||
        ((startPos > endPos) && (endPos < pageInfo.start || startPos > pageInfo.end))) {
        //Start and/or end range positions NOT on current table 'page' - warn, remove start and end range classe, return
        bootbox.alert("Multiple row selection limited to the current table 'page'.");

        jqueryRows.removeClass(startRangeClass);
        jqueryRows.removeClass(endRangeClass);

        return;
    }

    //Check 'rows to select' count and current rows selected count against allowed maximum...
    var toSelect = Math.abs(endPos - startPos);
    var count = selectedRowCounts[tableId].count;
    var selectedTimeSeriesMax = parseInt($('#' + tableId).attr('data-selectedtimeseriesmax'));

    if (Number.isInteger(selectedTimeSeriesMax) && (selectedTimeSeriesMax < (toSelect + count))) {
        //Allowed maximum exceeded - warn, remove start and end range classes, return
        bootbox.alert('Specified range and selected rows (' + (toSelect + count) + ') exceed the maximum allowed (' + selectedTimeSeriesMax + ')');

        jqueryRows.removeClass(startRangeClass);
        jqueryRows.removeClass(endRangeClass);

        return;
    }

    //All rows in specified range selectable, update count
    selectedRowCounts[tableId].count = toSelect + count;
    
    //Remove start and end range classes
    jqueryRows.removeClass(startRangeClass);
    jqueryRows.removeClass(endRangeClass);

    //Apply 'select' class to range 
    var lower = startPos;
    var upper = endPos;

    if (startPos > endPos) {
        //Reverse range - adjust lower and upper values
        lower = endPos;
        upper = startPos
    }

    var length = jqueryRows.length;
    for (var i = 0; i < length; ++i) {
        var pos = rows[0].indexOf(jqueryRows[i]._DT_RowIndex);

        if (pos >= lower && pos <= upper) {
            var jqueryObject = $(jqueryRows[i]);

            jqueryObject.addClass(selectClass);
        }
    }
}

function clovisButtonClick(event) {
    var table = $('#dtMarkers');
    var option = false;
    var jsonScrollX = { 'scrollX': option };

    var datatable = $('#dtMarkers').DataTable();
    var colvis = new $.fn.DataTable.ColVis(datatable);
    var colvisbutton = colvis.button();

    //if ($(button).is(':checked')) {
    if ($(colvisbutton).is(':checked')) {
            option = true;
    }

    table.dataTable(jsonScrollX);
}

//Add a new row to the input table...
function newRow($table, cols) {
    var $row = $('<tr/>');
    for (i = 0; i < cols.length; i++) {
        var $col = $('<td/>');
        $col.append(cols[i]);
        $row.append($col);
    }
    $table.append($row);
    return ($row);
}

//BCC - 30-Jun-2015 - Revised formatting for QA issue #32 - Download Manager (GUI): table entries arу scattered all over the page
//Add styles to row for download manager table...
function addRowStylesDM(newrow) {

    //Cell: 0
    var td = newrow.find('td:eq(0)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'display': 'none' });

    //Cell: 1
    td = newrow.find('td:eq(1)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '15%' });

    //Cell: 2
    td = newrow.find('td:eq(2)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '25%', 'word-wrap': 'break-word' });

    //Cell: 3
    td = newrow.find('td:eq(3)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '40%', 'overflow': 'hidden'});

    //Cell: 4
    td = newrow.find('td:eq(4)');
    td.addClass('text-center');
    td.css({ 'vertical-align': 'middle', 'height': '3em', 'width': '20%' });
}

function selectAll(event) {

    //Check 'Select...' check mark
    if ('undefined' !== typeof event.data.checkId && null !== event.data.checkId) {
        var checkMark = $('#' + event.data.checkId);
        var className1 = 'hidden';

        if (checkMark.hasClass(className1)) {
            //Check mark disabled - enable
            checkMark.removeClass(className1);
        }
        else {
            //Check mark enabled - disable, clear selections...
            checkMark.addClass(className1);
            if ('undefined' !== typeof event.data.clearId && null !== event.data.clearId) {
                $('#' + event.data.clearId).triggerHandler('click');
            }

            return;
        }
    }

    //Clear ALL table selections, regardless of current sort/search order
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows();                    //Retrieve ALL rows
    var nodesRendered = rows.nodes();           //Retrieve all the rendered nodes for these rows
                                                //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Apply 'selected' class to the 'top' <selectedTimeSeriesMax> rendered nodes, if indicated 
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        var totalRows = rows[0].length;

        nodesRendered = rows.nodes();                                    //Retrieve all the rendered nodes for these rows
        var length = nodesRendered.length;

        //Initialize the selected row count...
        // Check for the 'time series max' attribute - if found, compare it to the 'total rows' value to determine the count. Otherwise, set the count from the 'total rows' value
        var selectedTimeSeriesMax = parseInt($('#' + event.data.tableId).attr('data-selectedtimeseriesmax'));
        //var count = Number.isInteger(selectedTimeSeriesMax) ? (totalRows < selectedTimeSeriesMax ? totalRows : selectedTimeSeriesMax) : totalRows;
        var count = (('true' === event.data.selectAll) ? totalRows : ((Number.isInteger(selectedTimeSeriesMax) ? (totalRows < selectedTimeSeriesMax ? totalRows : selectedTimeSeriesMax) : totalRows)));

        if ( isNaN(selectedTimeSeriesMax)) {
            //data-selectedtimeseriesmax NOT defined - assign value of count...
            selectedTimeSeriesMax = count;
        }

        //For each rendered node...
        for (var i = 0; i < length; ++i) {

            //Determine the position of the associated row in the current sort/search order...
            var position = rows[0].indexOf(nodesRendered[i]._DT_RowIndex)
            
            //if (isNaN(selectedTimeSeriesMax) || (position < selectedTimeSeriesMax)) {
            if (('true' === event.data.selectAll) || (position < selectedTimeSeriesMax)) {
                //Row is within the 'top' <selectedTimeSeriesMax> - apply class, if indicated
                var jqueryObject = $(nodesRendered[i]);

                if (null !== jqueryObject) {
                    jqueryObject.addClass(className);   //Apply class...
                }
            }
        }

        selectedRowCounts[event.data.tableId].count = count;

    //Check button(s) states...
    if ( 'undefined' !== typeof event.data.btnIds) {
        enableDisableButtons(event.data.tableId, event.data.btnIds);
    }

    if ( 'undefined' !== typeof event.data.btnClearId) {
        enableDisableButton(event.data.tableId, event.data.btnClearId);
    }
}

//Clear --ALL-- table selections, regardless of current sort/search order...
function clearSelections(event) {

    //Indicate a select is NOT in progress...
    if (('undefined' !== typeof event.data.checkId) && $('#' + event.data.checkId).length ) {
        $('#' + event.data.checkId).addClass('hidden');
    }

    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows();     //Retrieve ALL rows
    var nodesRendered = rows.nodes();                                       //Retrieve all the rendered nodes for these rows
                                                                            //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Reset the selected row count...
    selectedRowCounts[event.data.tableId].count = 0;

    //Check button(s) states...
    if ( 'undefined' !== typeof event.data.btnIds) {
        enableDisableButtons(event.data.tableId, event.data.btnIds);
    }

    if ( 'undefined' !== typeof event.data.btnClearId) {
        enableDisableButton(event.data.tableId, event.data.btnClearId);
    }
}

//Apply the current timeseries filter(s) to the map...
function applyFilterToMap(event) {
    //Retrieve the table's current search/filter criteria
    var criteria = retrieveSearchAndFilterCriteria(event.data.tableId, true);

    var panelId = 'panelMapFilters2';
    var className = 'hidden';
    if ( (! $(this).prop('checked')) || $(this).hasClass('disabled')) {
        //'Apply to Map' NOT engaged - remove filter from map, hide filter panel on map...
        updateMap(false,criteria);
        $('#' + panelId).addClass(className);
        return;
    }

    //Apply filter to map, show filter panel on map...
    updateMap(false, criteria);


      if ('' !== criteria.Search || 0 < criteria.filters.length) {
        //Filter criteria exists - build filter pane markup...

        var filters = '<ul  class ="list-unstyled">';
        var length = 0;

        for( var prop in criteria) {
            if ( 0 >= criteria[prop].length || 'dataSources' === prop) {
                continue;   //Omit data sources from filter display...
            }

            if ('Search' === prop) {

                filters += '<li style="margin-bottom: 0.25em;">' + prop + ': ' + criteria[prop] + ' <span class="glyphicon glyphicon-remove-sign" data-prop-search="' + criteria[prop] + '"/></li>';
                continue;
            }

            if ('filters' === prop) {
                length = criteria[prop].length;

                for (var li = 0; li < length; ++li) {

                    filters += '<li style="margin-bottom: 0.25em;">' + criteria[prop][li].title + ': ' + criteria[prop][li].value + ' <span class="glyphicon glyphicon-remove-sign" data-prop-filter="' + criteria[prop][li].title + 
                                '" data-prop-value="' + criteria[prop][li].value + '"/></li>';
                }
            }
        }

        //Apply click handler to list elements, if indicated
        var fLength = filters.length;
        if (0 < fLength) {
            filters += '</ul>';

            var filtersId = '#' + 'badgeMapFilters';
            $(filtersId).html(filters);

            var buttonId = $(this).attr('id');

            //Assign a handler to all list elements...
            var searchElement = $(filtersId + ' [data-prop-search]');
            searchElement.off('click', handleFilterClick);
            searchElement.on('click', {'tableId': event.data.tableId, 'buttonId': buttonId }, handleFilterClick);

            var filterElements = $(filtersId + ' [data-prop-filter]');
            filterElements.off('click', handleFilterClick);
            filterElements.on('click', {'tableId': event.data.tableId, 'buttonId': buttonId }, handleFilterClick);


            $('#' + panelId).removeClass(className);
        }
    }
    else {
        //No filtering criteria - hide the filters panel...
        $('#' + panelId).addClass(className);
    }
}

//Delayed application of changes in search and/or filter criteria to the map... 
function applyFilterToMapDelayed(event) {

    var value = $(this).val();
    //console.log('Current search value: ' + value);

    //Clear current timeout, update current filters
   clearTimeout(currentFilters.timeout);

    var tableId = event.data.tableId;
    currentFilters.filters = retrieveSearchAndFilterCriteria( tableId, false);

    //Check input in one second...
    currentFilters.timeout = setTimeout(function() {

        var bMatch = true;
        var filters = retrieveSearchAndFilterCriteria( tableId, false);

        //Compare search strings...
        if (filters.Search !== currentFilters.filters.Search) {
            bMatch = false; //No match!!
        }

        if (bMatch) {
            //Search strings match - compare filters...
            var length = filters.filters.length;

            bMatch = (length === currentFilters.filters.filters.length);

            if (bMatch) {
                //Filter array lengths match - compare elements...
                for (var i = 0; i < length; ++i) {
                        
                    var filter = filters.filters[i];
                    var fLength = currentFilters.filters.filters.length;
                    for (var fi = 0; fi < fLength; ++fi) {
                        if (filter.value !== currentFilters.filters.filters[fi].value || filter.title !== currentFilters.filters.filters[fi].title) {
                            bMatch = false; //No match!!
                            break;
                        }
                    }
                }
            }

            if (bMatch) {
                //console.log('Filters match!!');

                var chkbx = $('#' + event.data.chkbxId); 
                chkbx.triggerHandler('click');
            }
            else {
                //console.log('Filters DO NOT match!!');
            }
        }
    }, 1000);

}

//Click event for a glyphicon 'span' on a filter list element - remove the associated list element...
function handleFilterClick(event) {
    
    //Determine the clicked element
    var search = $(this).attr('data-prop-search');
    var filter = $(this).attr('data-prop-filter');
    var value = $(this).attr('data-prop-value');

    if ('undefined' !== typeof search) {
        //Removing search...
        updateSearchCriteria( event.data.tableId, '');
    }
    else if ('undefined' != typeof filter  && 'undefined' != typeof value) {
        //Removing a filter...
        updateFilterCriteria(event.data.tableId, filter, '');
    } else {
        //Invalid parameter(s) - return early...
        return;
    }

    //Remove the associated list element (the parent of the glyphicon 'span')
    $(this).parent().remove();

    //If all list elements removed, remove the list, hide the filter panel...
    var panelList = $('#' + 'badgeMapFilters' + ' > ul');
    var children = panelList.children( 'li');
    if (0 >= children.length) {

        panelList.remove();
        var className = 'hidden';
        var panelId = 'panelMapFilters2';

        $('#' + panelId).addClass(className);

        //No search or filter values - uncheck the 'apply filters to map' button,
        //  call the click handler once more to redraw the map...
        $('#' + event.data.buttonId).prop('checked', false);
        $('#' + event.data.buttonId).triggerHandler('click');
    }
    else {
        //Redraw the table, invoke the button handler to re-apply the filters to the map...
        $('#' + event.data.buttonId).triggerHandler('click');
    }

}

//For the input tableId, update the search criteria to the input value...
function updateSearchCriteria( tableId, value) {

    if ('undefined' === typeof tableId || null === tableId || 
        'undefined' === typeof value || null === value ) {
        return;
    }

    //Update the search field, re-apply the search to the datatables instance...
    $('#' + tableId + '_filter input').val(value);
    $('#' + tableId).DataTable().search(value).draw();

}

//For the input tableId, update the select corresponding to the input title to the input value...
function updateFilterCriteria( tableId, title, value) {

    if ('undefined' === typeof tableId || null === tableId || 
        'undefined' === typeof title || null === title ||
        'undefined' === typeof value || null === value ) {
        return;
    }

    //Scan 'Filter by' dropdowns...
    var bFound = false;
    var api = new $('#' + tableId).DataTable();
    api.columns().indexes().flatten().each(function (i) {

        if (bFound) {
            return;
        }

        //Retrieve column title
        var column = api.column(i);
        var colTitle = column.header().innerHTML;

        if (title === colTitle) {
            //Match found - set indicator
            bFound = true;
        
            //Retrieve select, set value, trigger change handler...
            var select = $(column.footer()).find('select');
            if (select) {
                select.val(value);
                select.triggerHandler('change');
            }
        }
    });
}

//Copy the input Data Manager record to a Server record.  Return the Server Record
function copyDmRecordToServerRecord( userEmail, dmRecord) {

    var serverRecord = {};

    if ('undefined' === typeof userEmail || null === userEmail ||
        'undefined' === typeof dmRecord || null === dmRecord) {
        return serverRecord;  //Invalid input parameter - return early
    }

    serverRecord.UserEmail = userEmail;

    serverRecord.Organization = dmRecord.Organization;
    serverRecord.ServiceTitle = dmRecord.ServTitle;
    serverRecord.ServiceCode = dmRecord.ServiceCode;
    serverRecord.Keyword = dmRecord.ConceptKeyword;
    serverRecord.VariableUnits = dmRecord.VariableUnits;
    serverRecord.DataType = dmRecord.DataType;
    serverRecord.ValueType = dmRecord.ValueType;
    serverRecord.SampleMedium = dmRecord.SampleMedium;
    serverRecord.StartDate = formatDate(dmRecord.BeginDate);
    serverRecord.EndDate = formatDate(dmRecord.EndDate);

    serverRecord.ValueCount = dmRecord.ValueCount;
    serverRecord.SiteName = dmRecord.SiteName;
    serverRecord.VariableName = dmRecord.VariableName;
    serverRecord.TimeUnit = dmRecord.TimeUnit;
    serverRecord.TimeSupport = dmRecord.TimeSupport;

    serverRecord.SeriesId = dmRecord.SeriesId;
    serverRecord.WaterOneFlowURI = dmRecord.WofUri;
    serverRecord.WaterOneFlowTimeStamp = dmRecord.WofTimeStamp;
    serverRecord.Status = dmRecord.TimeSeriesRequestStatus;
    serverRecord.TimeSeriesRequestId = dmRecord.TimeSeriesRequestId;

    serverRecord.QCLID = dmRecord.QCLID;
    serverRecord.QCLDesc = dmRecord.QCLDesc;
    serverRecord.MethodId = dmRecord.MethodId;
    serverRecord.MethodDesc = dmRecord.MethodDesc; 
    serverRecord.SourceId = dmRecord.SourceId;
    serverRecord.SourceOrg = dmRecord.SourceOrg;

    //Processing complete - return Server record
    return serverRecord;
}

//Remove --ALL-- table selections, regardless of current sort/search order...
function removeSelections(event) {

    //Retrieve and remove all selected rows, re-draw the table...
    //Source: https://datatables.net/reference/api/rows().remove()
    var table = $('#' + event.data.tableId).DataTable();

    //Retain selected rows for later reference...
    var rows = table.rows( '.selected' );

    //Reset the selected row count...
    selectedRowCounts[event.data.tableId].count = 0;

    //Check button(s) states...
    if ( 'undefined' !== typeof event.data.btnIds) {
        enableDisableButtons(event.data.tableId, event.data.btnIds);
    }

    if ( 'undefined' !== typeof event.data.btnClearId) {
    enableDisableButton(event.data.tableId, event.data.btnClearId);
    }

    //Check for user authentication...
    if ( (! currentUser.authenticated ) || 
            (null === currentUser.login)) {
        table.rows( '.selected' ).remove().draw();
        return; 
    }

    var userTimeSeries = { 'UserEmail' : currentUser.login,
                            'TimeSeries' : [] };
    rows.every(function () {
        var rowData = this.data();

        var serverRecord = copyDmRecordToServerRecord(currentUser.login, rowData);
        userTimeSeries.TimeSeries.push(serverRecord);
    });

    if (0 >= userTimeSeries.TimeSeries.length) {
        table.rows( '.selected' ).remove().draw();
        return; //No selected rows - return
    }

    var url = '/DataManager/Delete';
    var utsString = JSON.stringify(userTimeSeries);

    var promise = $.ajax({
                        url: url,
                        type: 'DELETE',
                        dataType: 'json',
                        cache: false,           //Per IE...
                        async: true,
                        data: utsString,
                        contentType: 'application/json',
                        context: utsString,
                    });

    promise.done( function (data) {
            //console.log('/DataManager/Delete - successful!!')
            table.rows( '.selected' ).remove().draw();
        });

    promise.fail( function (jqXHR, textStatus, errorThrown) {
            console.log('/DataManager/Delete - error - ' + textStatus + ' - ' + errorThrown);
            //table.rows( '.selected' ).remove().draw();
        });

}

//Save all the --UNSAVED-- table selections, regardless of current sort/search order...
function saveSelections(event) {

    //Check for user authentication...
    if ( (! currentUser.authenticated ) || 
            (null === currentUser.login)) {
        return; 
    }

    var userTimeSeries = { 'UserEmail' : currentUser.login,
                           'TimeSeries' : [] };

    //Retrieve and save all unsaved selected rows
    var table = $('#' + event.data.tableId).DataTable();

    var selectedRows = table.rows( '.selected' ).data();
    var selectedCount = selectedRows.length;

    //For each unsaved timeseries...
    for ( var i = 0; i < selectedCount; ++i) {
        var datamgrRecord = selectedRows[i];
        
        if ( datamgrRecord.Saved) {
            continue;
        }

        var serverRecord = copyDmRecordToServerRecord(currentUser.login, datamgrRecord);

        userTimeSeries.TimeSeries.push(serverRecord);
    }

    //If no unsaved time series, return early
    if ( 0 >= userTimeSeries.TimeSeries.length) {
        return;
    }

    var url = '/DataManager/Post';
    var utsString = JSON.stringify(userTimeSeries);

    var promise = $.ajax({
                        url: url,
                        type: 'POST',
                        dataType: 'json',
                        cache: false,           //Per IE...
                        async: true,
                        data: utsString,
                        contentType: 'application/json',
                        context: utsString,
                    });

    promise.done( function (data) {
            //console.log('/DataManager/Post - successful!!')
        });

    promise.fail( function (jqXHR, textStatus, errorThrown) {
            console.log('/DataManager/Post - error - ' + textStatus + ' - ' + errorThrown);
        });
}

//Save the time series per the input requestId...
function saveCompletedTimeseries( tableName, requestId ) {

    //Check for user authentication...
    if ( (! currentUser.authenticated ) || 
            (null === currentUser.login)) {
        return; 
    }

    var tableId = '#' + tableName;
    var table = $(tableId).DataTable();

    //Scan table data for input requestId...
    table.rows().every(function () {

        var rowData = this.data();
        if (requestId === rowData.TimeSeriesRequestId && false === rowData.Saved ) {
            //Found - POST to server to update database
            var rowThis = this;
            var foundRowData = rowThis.data();

            var userTimeSeries = { 'UserEmail' : currentUser.login,
                                   'TimeSeries' : [] };

            var serverRecord = copyDmRecordToServerRecord(currentUser.login, foundRowData);

            userTimeSeries.TimeSeries.push(serverRecord);

            var url = '/DataManager/Post';
            var utsString = JSON.stringify(userTimeSeries);

            var promise = $.ajax({
                                url: url,
                                type: 'POST',
                                dataType: 'json',
                                cache: false,           //Per IE...
                                async: true,
                                data: utsString,
                                contentType: 'application/json',
                                context: utsString,
                            });

            promise.done( function (data) {
                    //console.log('/DataManager/Post - successful!!')
                    //Success save - update table row Saved indicator...
                    foundRowData.Saved = true;
                    rowThis.invalidate();
                });

            promise.fail( function (jqXHR, textStatus, errorThrown) {
                    console.log('/DataManager/Post - error - ' + textStatus + ' - ' + errorThrown);
                });
        }
    });
}

function downloadSelections(event) {

    //console.log('downloadSelections(...) called!!');
    var appName = event.data.appName;
    var valueSelected = $('#' + event.data.divId).text().trim();

    if ( appName !== valueSelected) {
        return; //Currently selected app is NOT the download app - return early...
    }

    var taskId = getNextTaskId();
    var txtClass = '.clsMessageArea';

    //Update the text...
    var txt = getDownloadTaskMessage();
    var txts = txt.split('@@taskId@@');

    $(txtClass).text( txts[0] + taskId.toString() + txts[1]);

    //Display the 'Zip processing started... message
    displayAndFadeLabel(txtClass);

    //Retrieve selected rows...
    var table = $('#' + event.data.tableId).DataTable();

    var selectedRows = table.rows( '.selected' ).data();
    var selectedCount = selectedRows.length;
    var  wofIds = [];

    //Retrieve water one flow ids...
    for ( var i = 0; i < selectedCount; ++i) {
        var row = selectedRows[i];
        var currentWofIds = { 'WofId' : row.WofUri,
                              'QCLID': row.QCLID,
                              'MethodId': row.MethodId,
                              'SourceId': row.SourceId
                            };

        wofIds.push(currentWofIds);
    }

      var convertWaterMlToCsvRequest = {};
      var requestId = randomId.generateId();


      convertWaterMlToCsvRequest.RequestName = 'CUAHSI-WDC';
      convertWaterMlToCsvRequest.RequestId = requestId;
      convertWaterMlToCsvRequest.WofIds = wofIds;

    //Call controller method...
    var convertWaterMlToCsvRequestString = JSON.stringify(convertWaterMlToCsvRequest);
    var actionUrl = "/Export/ConvertWaterMlToCsv";

    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        cache: false,           //Per IE...
        async: true,
        data: convertWaterMlToCsvRequestString,
        contentType: 'application/json',
        context: convertWaterMlToCsvRequestString,
        success: function (data, textStatus, jqXHR) {
            //console.log('ConvertWaterMlToCsv success!!');
            var response = jQuery.parseJSON(data);

            //Add new row to download manager table...
            addDownloadManagerRow(response, taskId);

            //Set monitor entry
            downloadMonitor.timeSeriesMonitored[convertWaterMlToCsvRequest.RequestId] = { 'tableName': 'tblDownloadManager',
                                                                                          'timeSeriesRequest': convertWaterMlToCsvRequest,
                                                                                          'timeSeriesRequestStatus': timeSeriesRequestStatus.Starting 
                                                                               };
           startRequestTimeSeriesMonitor();    //Start monitoring all current requests, if indicated...
        },
        error: function (xmlhttprequest, textstatus, message) {
            //Server error: open 'Server Error' dialog
            $('#serverMessageText').text(message);
            $('#serverErrorModal').modal('show');

            //Log messsage received from server...
            console.log('ConvertWaterMlToCsv reports error: ' + xmlhttprequest.status + ' (' + message + ')');

            //Set monitor status...
            var requestId = convertWaterMlToCsvRequest.RequestId;
            downloadMonitor.timeSeriesMonitored[requestId].timeSeriesRequestStatus = timeSeriesRequestStatus.RequestTimeSeriesError;
        }
    });
}

//Clear all table selections...
function clearServicesSelections(event) {
    //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
    //Source: http://datatables.net/reference/api/rows().nodes()
    var table = $('#' + event.data.tableId).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });     //Retrieve rows per current sort/search order...
    var nodesRendered = rows.nodes();                                       //Retrieve all the rendered nodes for these rows
    //NOTE: Rendered nodes retrieved in the same order as the rows...
    var jqueryObjects = nodesRendered.to$();    //Convert to jQuery Objects!!
    var className = 'selected';

    //Remove selected class from all rendered rows...
    jqueryObjects.removeClass(className);

    //Clear the selected services array
    mySelectedServices.length = 0;

    //Uncheck the 'Select all non-gridded...' checkbox
    bObservedValuesOnly = false;
    $("input[name='checkOnlyObservedValues']").prop('checked', bObservedValuesOnly);

    //Check button state...
    enableDisableButton(event.data.tableId, event.data.btnId);
}

//Set/reset 'disabled' attribute on referenced button per contents of referenced Data Table...
function enableDisableButton(tableId, btnId) {

    var table = $('#' + tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedCount = selectedRows.length;

    if (0 < selectedCount) {
        $('#' + btnId).prop('disabled', false);
    }
    else {
        $('#' + btnId).prop('disabled', true);
    }
}

//Set/reset 'disabled' attribute on referenced buttons per contents of referenced Data Table...
function enableDisableButtons(tableId, btnIds) {

    var table = $('#' + tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedCount = selectedRows.length;

    var length = btnIds.length;
    for (var i = 0; i < length; ++i) {
        if (0 < selectedCount) {
            $('#' + btnIds[i]).prop('disabled', false);
        }
        else {
            $('#' + btnIds[i]).prop('disabled', true);
        }
    }
}


//Dispaly the referenced label class and then fade...
function displayAndFadeLabel(labelClass) {

    //Display inline... 
    $(labelClass).fadeIn({'duration': 1500});

    setTimeout(function () {
        $(labelClass).fadeOut({ 'duration': 1500 });
    }, 10000);
}

//show dialog for export
function openDownloadModal(event) {
    $('#ExportModal').modal('show');
}

//Zip selections click handler...
function zipSelections_2(event) {

    //Get the next Task Id...
    var taskId = getNextTaskId();
    var txtClass = '.clsMessageArea';

    //Update the text...
    var txt = getDownloadTaskMessage();
    var txts = txt.split('@@taskId@@');

    $(txtClass).text( txts[0] + taskId.toString() + txts[1]);

    //Display the 'Zip processing started... message
    displayAndFadeLabel(txtClass);

    //Create the list of selected time series ids...
    var table = $('#' + event.data.tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedTimeSeriesMax = parseInt($('#' + event.data.tableId).attr('data-selectedtimeseriesmax'));

    if (currentUser.authenticated) {
        //Current user is authenticated - allow selection of ALL table rows...
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        selectedTimeSeriesMax = rows[0].length;
    }

    var selectCheck = $('#' + event.data.checkId);
    if ( selectCheck.length && (! selectCheck.hasClass("hidden")) && Number.isInteger(selectedTimeSeriesMax) && (selectedTimeSeriesMax > selectedRows.length)) {
        //User has clicked the 'Select Top ...' check box but not all selected rows have been rendered
        //NOTE: If the DataTable instance has the 'deferRender' option set - not all rows may have been rendered at this point.
        //        Thus one cannot rely on the selectedRows above, since in this case only rendered rows appear in the selectedRows...
        selectedRows = getSelectedRows(event.data.tableId, currentUser.authenticated ? selectedTimeSeriesMax : null);
    }

    var selectedCount = selectedRows.length;
    var timeSeriesIds = [];

    for (var i = 0; i < selectedCount; ++i) {
        var row = selectedRows[i];
        var id = row.SeriesId;
        timeSeriesIds.push(id);
    }
    //if user selected to combine data otherwise download individual data
    retrieveCSVTimeSeries(taskId, timeSeriesIds, event.data.isMerged);
}

//Retrieve the time series per the input series ids...
function retrieveCSVTimeSeries(taskId, timeSeriesIds,isMerged) {

    //Create the request object...
    var requestId = randomId.generateId();
    var requestName = 'SelectedArea';

    //Set request name from marker place name (more precise) --OR-- place name (less precise)...
    var regexp = /\s*[,:.\u00b0 ]\s*/g; 

    if (('undefined' !== typeof currentMarkerPlaceName) && (null !== currentMarkerPlaceName) && ('' !== currentMarkerPlaceName)) {
        requestName = currentMarkerPlaceName.replace(regexp, '_');  //Replace whitespace and commas with '_' 
    }
    else {
        if (('undefined' !== typeof currentPlaceName) && (null !== currentPlaceName) && ('' !== currentPlaceName)) {
            if ('Selected Area' === currentPlaceName) {
                requestName = 'CUAHSI-WDC';
            }
            else {
                requestName = currentPlaceName.replace(regexp, '_');  //Replace whitespace and commas with '_' 
            }
        }
    }
    
     var tsf = (isMerged)?  timeSeriesFormat.CSVMerged : timeSeriesFormat.CSV;
        var timeSeriesRequest = {
            'RequestName': requestName,
            'RequestId': requestId,
            'TimeSeriesIds': timeSeriesIds,
            'RequestFormat': tsf

        };

    var timeSeriesRequestString = JSON.stringify(timeSeriesRequest);

    var actionUrl = "/Export/RequestTimeSeries";

    //POST to create the time series...
    $.ajax({
        url: actionUrl,
        type: 'POST',
        dataType: 'json',
        cache: false,           //Per IE...
        async: true,
        data: timeSeriesRequestString,
        contentType: 'application/json',
        context: timeSeriesRequestString,
        success: function (data, textStatus, jqXHR) {
            //Retrieve response data
            var response = jQuery.parseJSON(data);

            //alert("Response received: " + response.Status.toString());

            //Add new row to the download manager table
            addDownloadManagerRow(response, taskId);

            //Set monitor entry
            downloadMonitor.timeSeriesMonitored[timeSeriesRequest.RequestId] = { 'tableName': 'tblDownloadManager',
                                                                                 'timeSeriesRequest': timeSeriesRequest,
                                                                                 'timeSeriesRequestStatus': timeSeriesRequestStatus.Starting 
                                                                               };
           startRequestTimeSeriesMonitor();    //Start monitoring all current requests, if indicated...
         },
         error: function (xmlhttprequest, textstatus, message) {
            //Server error: Close modal dialog, if indicated - open 'Server Error' dialog
            if ($('#SeriesModal').is(":visible")) {
                $('#SeriesModal').modal('hide');
            }

            $('#serverMessageText').text(message);
            $('#serverErrorModal').modal('show');

            //Log messsage received from server...
            console.log('retrieveCSVTimeSeries reports error: ' + xmlhttprequest.status + ' (' + message + ')');
        }
    });
}

//Add an 'end task' click handler to the input jquery button object
function addEndTaskClickHandler(jqueryButton, response) {

    jqueryButton.click(function(event) {
                //Hide the button...
                $(event.target).hide();
                //Send request to server to stop task...
                event.preventDefault();
                var actionUrl = "/Export/EndTask";

                $.ajax({
                        url: actionUrl + '/' + response.RequestId,
                        type: 'GET',
                        contentType: 'application/json',
                        context: response.RequestId,
                        success: function (data, textStatus, jqXHR) {

                        var statusResponse = jQuery.parseJSON(data);

                        //Update status in download manager table...
                        var tableRow = $("#tblDownloadManager tr td").filter(function () {
                            return $(this).text() === statusResponse.RequestId;
                        }).parent("tr");

                        tableRow.find('#statusMessageText').text(statusResponse.Status);
                        tableRow.find('#blobUriText').html(statusResponse.BlobUri);
                        tableRow.find('td:eq(5)').html(statusResponse.RequestStatus);

                        //Color table row as 'warning'
                        tableRow.removeClass('info');
                        tableRow.addClass('warning');
                        },
                        error: function (xmlhttprequest, textStatus, message) {
                            //alert('Failed to request server to stop task ' + message);

                            //Update row with End Task Error status
                            var requestId = this.valueOf();   //From context: parameter on ajax call - convert string 'object' to string 'primitive'...
                            var errorDescription = (timeSeriesRequestStatus.properties[timeSeriesRequestStatus.EndTaskError]).description;

                            var tableRow = $("#tblDownloadManager tr td").filter(function () {
                                return $(this).text() === requestId;
                            }).parent("tr");

                            tableRow.find('#statusMessageText').text(errorDescription);
                            tableRow.find('#blobUriText').html('');
                            tableRow.find('td:eq(5)').html(timeSeriesRequestStatus.EndTaskError);

                            //Change the glyphicon and stop the animation
                            var glyphiconSpan = tableRow.find('#glyphiconSpan');

                            glyphiconSpan.removeClass('glyphicon-refresh spin');
                            glyphiconSpan.addClass('glyphicon-thumbs-down');
                            glyphiconSpan.css('color', 'red');

                            //Color table row as 'warning'
                            tableRow.removeClass('info');
                            tableRow.addClass('warning');

                            //Log messsage received from server...
                            console.log('EndTask reports error: ' + xmlhttprequest.status + ' (' + message + ')');
                        }
            });    
            });
}

function copySelectionsToDataManager(event) {

    //Retrieve the selected rows...
    var table = $('#' + event.data.tableId).DataTable();
    var selectedRows = table.rows('.selected').data();
    var selectedTimeSeriesMax = parseInt($('#' + event.data.tableId).attr('data-selectedtimeseriesmax'));

    if (currentUser.authenticated) {
        //Current user is authenticated - allow selection of ALL table rows...
        //Retrieve all the table's RENDERED <tr> elements whether visible or not, in the current sort/search order...
        //Source: http://datatables.net/reference/api/rows().nodes()
        var rows = table.rows({ 'order': 'current', 'search': 'applied' });    //Retrieve rows per current sort/search order...
        selectedTimeSeriesMax = rows[0].length;
    }

    var selectCheck = $('#' + event.data.checkId); 
    if ( selectCheck.length && (! selectCheck.hasClass("hidden")) && Number.isInteger(selectedTimeSeriesMax) && (selectedTimeSeriesMax > selectedRows.length)) {
        //User has clicked the 'Select Top ...' check box but not all selected rows have been rendered
        //NOTE: If the DataTable instance has the 'deferRender' option set - not all rows may have been rendered at this point.
        //        Thus one cannot rely on the selectedRows above, since in this case only rendered rows appear in the selectedRows...
        selectedRows = getSelectedRows(event.data.tableId, currentUser.authenticated ? selectedTimeSeriesMax : null );
    }

    var selectedCount = selectedRows.length;

    var datamgrTableName = 'tblDataManager';
    var datamgrTableId = '#' + datamgrTableName;
    var datamgrTable = $(datamgrTableId).DataTable();
    var timeseriesmax = parseInt($(datamgrTableId).attr('data-timeseriesmax'));
    var countAdded = 0;
    var dupsFound = 0;

    //For each selected row...
    for (var rI = 0; rI < selectedCount; ++rI ) {
        //Check row count...
        var count = datamgrTable.rows().data().length;
        if (count >= timeseriesmax) {
            bootbox.alert('<h4>The workspace may contain a maximum of ' + timeseriesmax + ' time series rows.</h4>');
            break;
        }

        //Check for duplicate entry...
        var bFound = false;
        var currentRow = selectedRows[rI];

        //For each Data Manager entry...
        datamgrTable.rows().data().each(function(dmRow, index) {
            if (bFound) {
                return; //Duplicate found - return early...
            }

            if ( dmRow.Organization === currentRow.Organization &&
                 dmRow.ServTitle === currentRow.ServTitle &&
                 dmRow.ServiceCode === currentRow.ServCode &&
                 dmRow.ConceptKeyword === currentRow.ConceptKeyword &&
                 dmRow.DataType === currentRow.DataType &&
                 dmRow.ValueType === currentRow.ValueType &&
                 dmRow.SampleMedium === currentRow.SampleMedium &&
                 dmRow.BeginDate.split('T', 1)[0] === currentRow.BeginDate.split('T', 1)[0] &&
                 dmRow.EndDate.split('T', 1)[0] === currentRow.EndDate.split('T', 1)[0] &&
                 dmRow.ValueCount === currentRow.ValueCount &&
                 dmRow.SiteName === currentRow.SiteName &&
                 dmRow.VariableName === currentRow.VariableName &&
                 dmRow.QCLID === currentRow.QCLID &&
                 dmRow.MethodId === currentRow.MethodId &&
                 dmRow.SourceId === currentRow.SourceId ) {
                    bFound = true;  //Duplicate found - set indicator
            }
        });

        if (bFound) {
            ++dupsFound;
            continue;   //Duplicate found - increment count, continue...
        }

        //Create a 'Data Manager' record
        var datamgrRecord = {};

        datamgrRecord.Saved = false;
        datamgrRecord.Organization = currentRow.Organization;
        datamgrRecord.ServTitle = currentRow.ServTitle;
        datamgrRecord.ServiceCode = currentRow.ServCode;
        datamgrRecord.ConceptKeyword = currentRow.ConceptKeyword;
        datamgrRecord.DataType = currentRow.DataType;
        datamgrRecord.ValueType = currentRow.ValueType;
        datamgrRecord.SampleMedium = currentRow.SampleMedium;
        datamgrRecord.BeginDate = currentRow.BeginDate;
        datamgrRecord.EndDate = currentRow.EndDate;
        datamgrRecord.ValueCount = currentRow.ValueCount;
        datamgrRecord.SiteName = currentRow.SiteName;
        datamgrRecord.VariableName = currentRow.VariableName;
        datamgrRecord.VariableUnits = currentRow.VariableUnits;
        datamgrRecord.TimeUnit = currentRow.TimeUnit;
        datamgrRecord.TimeSupport = currentRow.TimeSupport;

        datamgrRecord.QCLID = currentRow.QCLID;
        datamgrRecord.QCLDesc = currentRow.QCLDesc;
        datamgrRecord.MethodId = currentRow.MethodId;
        datamgrRecord.MethodDesc = currentRow.MethodDesc;        
        datamgrRecord.SourceId = currentRow.SourceId;
        datamgrRecord.SourceOrg = currentRow.SourceOrg;

        datamgrRecord.SeriesId = currentRow.SeriesId;
        datamgrRecord.WofUri = 'Not yet available';
        datamgrRecord.WofTimeStamp = '1901-01-01';
        datamgrRecord.TimeSeriesRequestStatus = timeSeriesRequestStatus.NotStarted;
        datamgrRecord.TimeSeriesRequestId = null;

        //Add the newly created record...
        var newRow = datamgrTable.row.add(datamgrRecord);
        ++countAdded;
    }

    //Display a 'copied...' message, if indicated
    var txtClass = '.clsMessageArea';
    if ( 0 < dupsFound || 0 < countAdded) {
        var dupsText = 'Rejected ' + dupsFound.toString() + ' duplicate selection(s).';
        var copiedText = 'Copied ' + countAdded.toString() + ' selection(s) to Workspace.';

        $(txtClass).text( (0 < dupsFound) ? dupsText + ' ' + copiedText : copiedText );
        displayAndFadeLabel(txtClass);
    }

    //Set footer filters...
    setfooterFilters( datamgrTableName, [2, 3, 4, 5, 13, 14, 15, 16], null);

    //Add filter placeholders...
    var tempEvent = { 'data': { 'tableId': datamgrTableName, 'placeHolders': ['Publisher', 'Service Title', 'Keyword', 'Site Name', 'Variable Units', 'Data Type', 'Value Type', 'Sample Medium'] }};
    addFilterPlaceholders(tempEvent);

    //Adjust column sizing...
    datamgrTable.columns.adjust().draw();

    var event1 = { 'data': 
                        { 'tableId': datamgrTableName, 'modalDialogId': 'datamgrModal' }
                };

    retrieveWaterOneFlowForTimeSeries(event1);
}

//For the current authenticated user, load DataManager entries from the server...
function loadDataManager() {

    //console.log('loadDataManager called!!');

    //NOTE: IIS feature...
	//To successfully transfer something like an e-mail address to the server - you MUST include a trailing forward slash 
	// as explained here.  The trailing forward slash tells IIS that the string does not represent a file path!!!
	// http://stackoverflow.com/questions/11728846/dots-in-url-causes-404-with-asp-net-mvc-and-iis

    //Query server for saved time series, if any...
    if ( (! currentUser.dataManagerLoaded ) && 
            currentUser.authenticated && 
            (null != currentUser.login)) {
        var url = '/DataManager/Get/' + currentUser.login + '/';

        $.ajax({
            url: url,
            type: 'GET',
            async: true,
            dataType: 'json',
            cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
            success: function (data, textStatus, jqXHR) {
                //Success - update indicator
                currentUser.dataManagerLoaded = true;

                //Load retrieve time series into table...
                var datamgrTableName = 'tblDataManager';
                var datamgrTableId = '#' + datamgrTableName;
                var datamgrTable = $(datamgrTableId).DataTable();

                var userTimeSeries = JSON.parse(data);
                var length = userTimeSeries.length;
                var reDraw = false;

                for (var i = 0; i < length; ++i) {
                    var serverRecord = userTimeSeries[i];
                    var datamgrRecord = {};

                    datamgrRecord.Saved = true;
                    datamgrRecord.Organization = serverRecord.Organization;
                    datamgrRecord.ServTitle = serverRecord.ServiceTitle;
                    datamgrRecord.ServiceCode = serverRecord.ServiceCode;
                    datamgrRecord.ConceptKeyword = serverRecord.Keyword;
                    datamgrRecord.VariableUnits = serverRecord.VariableUnits;
                    datamgrRecord.DataType = serverRecord.DataType;
                    datamgrRecord.ValueType = serverRecord.ValueType;
                    datamgrRecord.SampleMedium = serverRecord.SampleMedium;                    

                    datamgrRecord.BeginDate = serverRecord.StartDate;
                    datamgrRecord.EndDate = serverRecord.EndDate;

                    datamgrRecord.ValueCount = serverRecord.ValueCount;
                    datamgrRecord.SiteName = serverRecord.SiteName;
                    datamgrRecord.VariableName = serverRecord.VariableName;
                    datamgrRecord.TimeUnit = serverRecord.TimeUnit;
                    datamgrRecord.TimeSupport = serverRecord.TimeSupport;

                    datamgrRecord.QCLID = serverRecord.QCLID;
                    datamgrRecord.QCLDesc = serverRecord.QCLDesc;
                    datamgrRecord.MethodId = serverRecord.MethodId;
                    datamgrRecord.MethodDesc = serverRecord.MethodDesc;
                    datamgrRecord.SourceId = serverRecord.SourceId;
                    datamgrRecord.SourceOrg = serverRecord.SourceOrg;

                    datamgrRecord.SeriesId = serverRecord.SeriesId;
                    datamgrRecord.WofUri = serverRecord.WaterOneFlowURI;
                    datamgrRecord.WofTimeStamp = serverRecord.WaterOneFlowTimeStamp;
                    datamgrRecord.TimeSeriesRequestStatus = serverRecord.Status;
                    datamgrRecord.TimeSeriesRequestId = serverRecord.TimeSeriesRequestId;

                    //Add the newly created record...
                    var newRow = datamgrTable.row.add(datamgrRecord);    
                    reDraw = true;
                }

                //Redraw table, if indicated...
                if ( reDraw) {
                    //Set footer filters...
                    setfooterFilters( datamgrTableName, [2, 3, 4, 5, 13, 14, 15, 16], null);

                    //Add filter placeholders...
                    var tempEvent = { 'data': { 'tableId': datamgrTableName, 'placeHolders': ['Publisher', 'Service Title', 'Keyword', 'Site Name', 'Variable Units', 'Data Type', 'Value Type', 'Sample Medium'] }};
                    addFilterPlaceholders(tempEvent);

                    //Adjust column sizing...
                    datamgrTable.columns.adjust().draw();
                                }
            },
            error: function (xmlhttprequest, textStatus, message) {
                //Failure - Log messsage received from server...
                console.log('DataManager GET reports error: ' + xmlhttprequest.status + ' (' + message + ')');
            }                
        });
    }
}

//For the current authenticated user, signal the server to restore Export entries.
//  The client's status polling should report the restored export entries...
function loadExports() {

    //console.log('loadExports called!!');

    //NOTE: IIS feature...
	//To successfully transfer something like an e-mail address to the server - you MUST include a trailing forward slash 
	// as explained here.  The trailing forward slash tells IIS that the string does not represent a file path!!!
	// http://stackoverflow.com/questions/11728846/dots-in-url-causes-404-with-asp-net-mvc-and-iis

    //Signal server to restore export entries, if any...
    if ( (! currentUser.exportsLoaded ) && 
            currentUser.authenticated && 
            (null != currentUser.login)) {

        var url = '/DownloadManager/Get/' + currentUser.login + '/';

        $.ajax({
            url: url,
            type: 'GET',
            async: true,
            dataType: 'json',
            cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
            success: function (data, textStatus, jqXHR) {

                //Success - update indicator
                currentUser.exportsLoaded = true;

                //Parse the received data...
                var exportTaskData = JSON.parse(data);
                var length = exportTaskData.length;

                //Load download manager table...
                for (var i = 0; i < length; ++i) {
                    var serverRecord = exportTaskData[i];
                
                    clientRecord = {};
                    clientRecord.RequestId = serverRecord.RequestId;
                    clientRecord.RequestStatus = serverRecord.RequestStatus;
                    clientRecord.Status = timeSeriesRequestStatus.properties[serverRecord.RequestStatus].description;
                    clientRecord.BlobUri = serverRecord.BlobUri;
                    clientRecord.BlobTimeStamp = serverRecord.BlobTimeStamp;

                    var taskId = getNextTaskId();
                    addDownloadManagerRow(clientRecord, taskId);
                    updateDownloadManagerRow(clientRecord);
                }
            },
            error: function (xmlhttprequest, textStatus, message) {
                //Failure - Log messsage received from server...
                console.log('Export RestoreTasks reports error: ' + xmlhttprequest.status + ' (' + message + ')');
            }                
        });
    }
}

//Retrieve all the selected rows per the input table name...
function getSelectedRows(tableName, timeSeriesMaxOverride) {
    //Validate/initialize input parameters...
    if ('undefined' === typeof tableName || null === tableName) {
        return null;
    }

    var table = $('#' + tableName).DataTable();
    var rows = table.rows({ 'order': 'current', 'search': 'applied' });           //Retrieve rows per current sort/search order...
    var length = rows[0].length;    //Need length of rows array here!!
    var selectedRows = [];
    var selectedTimeSeriesMax = parseInt($('#' + tableName).attr('data-selectedtimeseriesmax'));

    if ('undefined' !== timeSeriesMaxOverride && null !== timeSeriesMaxOverride && Number.isInteger(timeSeriesMaxOverride) ) {
        selectedTimeSeriesMax = timeSeriesMaxOverride;
    }

    for (var i = 0; i < length; ++i) {
        var position = rows[0][i];  //Reference the row's position per the current sort/search!!

        //If row is rendered, check if selected...
        var node = table.row(position).node();
        var bSelected = false;

        if (null !== node) {
            var jqueryObj = $(node);
            if (jqueryObj.hasClass('selected')) {
                bSelected = true;   //NOTE: User can manually select rows outside the 'Select Top ...'
            }
        }

        if ((Number.isInteger(selectedTimeSeriesMax) && (i < selectedTimeSeriesMax)) || bSelected) {
            //Current row position within 'Select Top ...' --OR-- user has manually selected the row - append row data to selected rows...
            selectedRows.push(table.row(position).data());
                    }
            }

    return selectedRows;
}

//Format the html for the status message as follows - all elements inline:  <h3> - glyphicon spinner </h3> <span> status message text </span> 
function formatStatusMessage(statusText) {
    
    var formattedMessage = '<h3 class="text-center" style="display: inline; vertical-align: middle;">' + 
                           '<span id="glyphiconSpan" class="glyphicon glyphicon-refresh spin" style="color: #32cd32;"></span></h3>' + //color is CSS LimeGreen
                           '<div id="statusMessageText" class="text-center" style="display:inline-block; margin: 0em 0em 0em 1em; vertical-align: top; white-space: normal;">' +
                           statusText +
                           '</div>';
    return ( formattedMessage)
}

//Return the next task Id in format: <++taskCount>@hh:mm:ss
function getNextTaskId() {
    var date = new Date();

    return ((++taskCount).toString() + '@' + date.getHours().toString() + ':' + date.getMinutes().toString() + ':' + date.getSeconds().toString()); 
}

//Return the Google Maps location name per the preferred order of address types...
function getMapLocationName(results) {
    
    var locationName = '';

    if ('undefined' !== typeof results && null !== results && Array.isArray(results)) {
        //Input parameters valid - set preferred order of address types...
        var addressTypes = [ 'administrative_area_level_5', 'administrative_area_level_4', 'administrative_area_level_3', 'postal_town', 'postal_code', 'locality', 'administrative_area_level_2', 'administrative_area_level_1', 'country'];

        //Scan results array for address types...
        var componentIndices = {};

        var rLength = results.length;
        for (var ri = 0; ri < rLength; ++ri) {
            //Scan address types for current result...
            var rTypes = results[ri].types;
            var tLength = rTypes.length;
            for (var ti = 0; ti < tLength; ++ti) {
                var rType = rTypes[ti];
                if (-1 !== addressTypes.indexOf(rType)) {
                    componentIndices[rType] = ri;   //Address type found - record associated results index
                }
            }
        }

        //Scan found address types - set current place name...
        var atLength = addressTypes.length;
        for (var ati = 0; ati < atLength; ++ati) {
            if ('undefined' !== typeof componentIndices[addressTypes[ati]]) {
                locationName = results[componentIndices[addressTypes[ati]]].formatted_address;
                //console.log('location name is: ' + locationName);
                break;
            }
        }
    }

    return locationName;
}

//Data table for data tab
function setUpTimeseriesDatatable() {
    if (clusteredMarkersArray.length == 0)
    {
        return;
    }
    // $('#dtTimeseries').(':visible')
    $('#dtTimeseries').removeClass("hidden");

    $.fn.DataTable.isDataTable("#dtTimeseries")
    {
        $('#dtTimeseries').DataTable().clear().destroy();
    }

    //Reset the selected row count...
    selectedRowCounts['dtTimeseries'].count = 0;

    //Initialize current place name, if indicated...
    if ('undefined' === typeof currentPlaceName || null === currentPlaceName) {
        currentPlaceName = 'Selected Area';
    }

    //ALWAYS set the screen title...
    $('#tableModal #myModalLabel').html('Search Results in: ' + currentPlaceName);
        
    var actionUrl = "/home/getTimeseries"
    var oTable = $('#dtTimeseries').dataTable({
        "ajax": actionUrl,
        "dom": 'C<"clear"><"toolbarTS">frtilp',   //Add a custom toolbar - source: https://datatables.net/examples/advanced_init/dom_toolbar.html
        "deferRender": true,
        "autoWidth": false,
        "columns": [
            { "data": "Organization", "visible": true, "width": "10em", "className": "td-textalign-center",
             "render": renderAbbreviatedText,
             "createdCell": createdTooltipText
            },
            //BCC - 09-Sep-2015 - GitHub Issue #23 - Replace Network Name with Data Service Title
            { "data": "ServTitle", "sTitle": "Service Title", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": function (data, type, full, meta) { 
                    if ('display' === type) {
                        return formatServiceTitleAbbreviated( full.ServCode, data );
                    }
                    else {
                        return data;
                    }
              },                       
              "createdCell": createdTooltipText                 
            },
            { "data": "ConceptKeyword", "sTitle": "Keyword", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": renderAbbreviatedText,
              "createdCell": createdTooltipText           
            },
            { "data": "SiteName", "sTitle": "Site Name", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": renderAbbreviatedText,
              "createdCell": createdTooltipText             
            },
            { "data": "DataType", "visible": true, "width": "5em", "className": "td-textalign-center" },
            { "data": "ValueType", "visible": true, "width": "5em", "className": "td-textalign-center" },
            { "data": "SampleMedium", "visible": true, "width": "5em", "className": "td-textalign-center" },
            { "data": "QCLID", "visible": false },
            { "data": "QCLDesc", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": renderAbbreviatedText,
              "createdCell": createdTooltipText             
            },
            { "data": "MethodId", "visible": false },
            { "data": "MethodDesc", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": renderAbbreviatedText,
              "createdCell": createdTooltipText             
            },
            { "data": "SourceId", "visible": false },
            { "data": "SourceOrg", "visible": true, "width": "10em", "className": "td-textalign-center",
              "render": renderAbbreviatedText,
              "createdCell": createdTooltipText             
            },
            { "data": "BeginDate", "sTitle": "Start Date", "visible": true, "width": "10em",
                 "render": function (data, type, full, meta) { 
                     return formatDate(data);
                 }           
            },
            { "data": "EndDate", "sTitle": "End Date", "visible": true, "width": "10em",
                 "render": function (data, type, full, meta) { 
                     return formatDate(data);
                 }           
            },
            { "data": "ValueCount", "visible": true, "width": "10em", "className": "td-textalign-center",
                 'render': function (data, type, full, meta) { 
                     return (0 === data) ? 'unknown' : data;
                 }                      
            },
            { "data": "VariableName", "sTitle": "Variable Name", "visible": true, "width": "10em",  "className": "td-textalign-center",
              "render": renderAbbreviatedText,
             "createdCell": createdTooltipText             
          },
            { "data": "TimeSupport", "visible": true, "width": "5em", "className": "td-textalign-center" },
            { "data": "TimeUnit", "visible": true, "width": "5em", "className": "td-textalign-center" },
            { "data": null, "sTitle": "Service URL", "visible": true, "width": "10em" },
            { "data": "SiteCode", "sTitle": "Site Code", "visible": true,  "width": "10em"  },
            { "data": "VariableCode", "sTitle": "Variable Code", "visible": true,  "width": "10em"  },
            { "data": "SeriesId", "visible": false, "searchable": false,  "width": "10em"   },
            { "data": "ServURL", "sTitle": "Web Service Description URL", "visible": false, "searchable": false,  "width": "10em"  },
            { "data": "ServCode", "name": "ServiceCode", "visible": false, "searchable": false,  "width": "10em" },
            { 'data': 'VariableUnits', 'visible': false, 'width': '5em'}
           ],
        "order": [[0,'asc'],    //Publisher
                  [1,'asc'],    //Service Title
                  [2,'asc']],    //Keyword
        "scrollX": true,
        "scrollY": "30em",
        "scrollCollapse": true,
        "createdRow": function (row, data, index) {

            //Create a link to the Service URL
            var org = $('td', row).eq(0).html();
            var servCode = data.ServCode;

            var descUrl = getDescriptionUrl(servCode);
            $('td', row).eq(16).html("<a href='" + descUrl + "' target='_Blank'>" + org + " </a>");

            //If 'Select All' in progress, add the 'selected' class to the row...
            var selectCheck = $('#' + 'spanSelectCheckTS'); 
            if ( selectCheck.length && (! selectCheck.hasClass('hidden'))) {
                $(row).addClass('selected');  //Only correct if ALL the rows are being selected...
            }


        },
        "initComplete": function () {

            //BCC - 10-Aug-2015 - GitHub Issue #35 - Add filter by Site Name
            setfooterFilters('dtTimeseries', [0, 1, 2, 3, 4, 5, 6], null, 'chkbxApplyFilterToMapTS');

            var tempEvent = { 'data': { 'tableId': 'dtTimeseries', 'placeHolders': ['Publisher', 'Service Title', 'Keyword', 'Site Name', 'Data Type', 'Value Type', 'Sample Medium'] } };
            addFilterPlaceholders(tempEvent);

            var dt = $('#dtTimeseries').DataTable();
            
            dt.columns.adjust();
            dt.draw();

            //Revise layout for table's info, length and pagination controls...
            $('#' + 'dtTimeseries_info').css({'width': '25%'});

            $('#' + 'dtTimeseries_length').css({'width': '50%', 
                                                'height': '2.5em'});

           $('#' + 'dtTimeseries_length' + ' > label').css({'text-align': 'center',
                                                            'display': 'block',
                                                            'margin-top': '1.0em'});

            $('#' + 'dtTimeseries_paginate').css({'width': '25%'});

            //Set up tooltips - ALWAYS set up tooltips last!!
            setupToolTips();
        }

    });
      
    //BC - Test - make each table row selectable by clicking anywhere on the row...
    //Source: https://datatables.net/examples/api/select_row.html
    //Avoid multiple registrations of the same handler...
    $('#dtTimeseries tbody').off('click', 'tr', toggleSelected);
    $('#dtTimeseries tbody').on('click', 'tr', { 'tableId': 'dtTimeseries', 'btnIds': ['btnZipSelectionsTS', 'btnManageSelectionsTS'], 'btnClearId': 'btnClearSelectionsTS' }, toggleSelected);

    //Server-error handler for dtMarkers...
    $('#dtTimeseries').on('error.dt', function (event, settings, techNote, message) {

        //Close current dialog - open 'Server Error' dialog
        $('#dtTimeSeriesModal').modal('hide');

        $('#serverMessageText').text(message);
        $('#serverErrorModal').modal('show');

        //Log messsage received from server...
        console.log('dtTimeseries reports error: ' + message);
    });


    //BC - Test - add a custom toolbar to the table...
    //source: https://datatables.net/examples/advanced_init/dom_toolbar.html
    $("div.toolbarTS").html(
                            '<div class="inline-form">' +
                            '<div class="form-group" style="margin-left: 1em; float:left; display:inline-block;">' +
    
                              '<div class="dropdown" style="position: relative; display: inline-block; float: left; font-size: 1.00em;">' +
                                  '<button class="btn btn-primary dropdown-toggle" type="button" id="ddMenuSelectionsTS" data-toggle="dropdown" aria-haspopup="true" aria-expanded="true">' +
                                   '<span class="glyphicon glyphicon-list-alt" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                   '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Selections&nbsp;</span>' +
                                   '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                  '</button>' +
                                  '<ul class="dropdown-menu" aria-labelledby="ddMenuSelectionsTS">' +
                                   
                                   (currentUser.authenticated ? 
                                       '<li><a href="#" id="anchorAllSelectionsTS" data-selectall="true" style="font-weight: bold;" >' + 
                                       '<span class="glyphicon glyphicon-plus-sign"></span>' +
                                       '<span class="text-muted">&nbsp;Select All&nbsp;' +
                                       '<span id="spanSelectCheckTS" class="glyphicon glyphicon-ok-circle hidden"></span>'  +
                                       '</a></li>' : '') + 

                                   '<li><a href="#" id="anchorClearSelectionsTS" style="font-weight: bold;">' +
                                   '<span class="glyphicon glyphicon-minus-sign"></span>' +
                                   '<span id="spanClearSelectionsTS" class="text-warning">&nbsp;Clear Selections</span>' + 
                                   '</a></li>' +
                                  '</ul>' +
                              '</div>' +

                              '<div style="position: relative; display: inline-block; float: left; margin-left: 0.5em; vertical-align: top">' +
                                '<div>' +
                                  '<div class="dropdown" id="ddActionsTS">' +
                                      '<button class="btn btn-primary" data-toggle="dropdown" style="font-size: 1.00em;">' +

                                        '<div id="ddActionTSOriginal" style="display: none;">' + 
                                        '<span class="glyphicon glyphicon-circle-arrow-right" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                        '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Action&nbsp;</span>' +
                                        '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                        '</div>' +
                                        '<div id="ddActionTS" data-noneselected="true">' + 
                                        '<span class="glyphicon glyphicon-circle-arrow-right" style="font-weight: bold; max-width: 100%; font-size: 1.0em;"></span>' +
                                        '<span style="font-weight: bold; font-size: 1.00em;">&nbsp;Select Action&nbsp;</span>' +
                                        '<span class="caret" style="font-weight: bold; font-size: 1.00em;"></span>' +
                                        '</div>' +

                                      '</button>' +
                                      '<ul class="dropdown-menu" style="width: 32em;">' +
                                       //'<ul style="list-style: none; padding-left: 0em;">' +

                                       '<li data-toggle="tooltip" data-placement="top" title="Move all selected time series to the workspace">' +
                                        '<a href="#" id="anchorAddSelectionsToWorkspaceTS" style="font-weight: bold;" >' + 
                                        '<span class="glyphicon glyphicon-plus-sign" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                        '<span id="spanManageSelectionsTS" style="font-weight: bold; display: inline-block; vertical-align: super;">Save Selection(s) to Workspace</span>' +
                                       '</a>' +
                                       '</li>' +

                                       (currentUser.authenticated ?
                                        '<li>' +
                                            '<a href="#" id="anchorAllTimeseriesInOneFileTS" style="font-weight: bold;" >' +
                                            '<span class="glyphicon glyphicon-file" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                            '<span id="spanCombinedFilesTS" style="font-weight: bold; display: inline-block; vertical-align: super;">Download selections in one combined file</span>' +
                                            '</a>' +
                                        '</li>' +
                                        '<li>' +
                                            '<a href="#" id="anchorEachTimeseriesInSeparateFileTS" style="font-weight: bold;" >' +
                                            '<span class="glyphicon glyphicon-duplicate" style="max-width: 100%; font-size: 1.5em; margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +  
                                            '<span id="spanSeparateFilesTS" style="font-weight: bold; display: inline-block; vertical-align: super;">Download in separate file(s)</span>' +
                                            '</a>'
                                        :
                                       '<li data-toggle="tooltip" data-placement="top" title="Export all selected time series to the client in CSV format">' +
                                        '<a href="#" id="anchorEachTimeseriesInSeparateFileTS" style="font-weight: bold;">' +                                    
                                        '<span class="glyphicon glyphicon-export" style="max-width: 100%; font-size: 1.5em;  margin-left: 1.0em; margin-right: -0.5em;">&nbsp;</span>' +
                                        '<span id="spanZipSelectionsTS"  style="font-weight: bold; display: inline-block; vertical-align: super;">Export Selection(s)</span>' +
                                       '</a>') +

                                       '</li>' +

                                      '</ul>' +
                                      '</div>' +
                                  '</div>' +
                              '</div>' +

                              '<div id="divApplyFilterToMapTS" style="display: inline-block; margin-left: 1em; float:left;" class="hidden">' +
                                '<label style="float:right;">' +
                                '<input type="checkbox" checked id="chkbxApplyFilterToMapTS"/>&nbsp;Apply Filter to Map' +
                                '</label>' +
                              '</div>' +
                                '<span class="clsMessageArea" style="display: none; float:left; margin-left: 2em;"></span>' +
                            
                            '</div>' + 
                            '</div>');

    //Add click handlers...

    //Avoid multiple registrations of the same handler...
    $('#anchorAllSelectionsTS').off('click', selectAll);
    $('#anchorAllSelectionsTS').on('click', { 'tableId': 'dtTimeseries', 'anchorId': 'anchorAllSelectionsTS', 'checkId': 'spanSelectCheckTS', 'clearId': 'anchorClearSelectionsTS', 'selectAll' : $('#anchorAllSelectionsTS').attr('data-selectall') }, selectAll);

    //Avoid multiple registrations of the same handler...
    $('#anchorClearSelectionsTS').off('click', clearSelections);
    $('#anchorClearSelectionsTS').on('click', { 'tableId': 'dtTimeseries', 'anchorId': 'anchorAllSelectionsTS', 'checkId': 'spanSelectCheckTS' }, clearSelections);

    //Avoid multiple registrations of the same handler...
    $('#anchorAddSelectionsToWorkspaceTS').off('click', copySelectionsToDataManager);
    $('#anchorAddSelectionsToWorkspaceTS').on('click', { 'tableId': 'dtTimeseries', 'selectAll' : $('#anchorAllSelectionsTS').attr('data-selectall'), 'checkId' : 'spanSelectCheckTS' /*, 'currentAnchor': 'anchorAddSelectionsToWorkspaceTS' */ }, copySelectionsToDataManager)

    //Authenticated user only...
    if (currentUser.authenticated) {
        $('#anchorAllTimeseriesInOneFileTS').off('click', zipSelections_2);
        $('#anchorAllTimeseriesInOneFileTS').on('click', { 'tableId': 'dtTimeseries', 'selectAll' : $('#anchorAllSelectionsTS').attr('data-selectall'), 'checkId': 'spanSelectCheckTS', 'isMerged': true }, zipSelections_2);
    }

    //Avoid multiple registrations of the same handler...
    $('#anchorEachTimeseriesInSeparateFileTS').off('click', zipSelections_2);
    $('#anchorEachTimeseriesInSeparateFileTS').on('click', { 'tableId': 'dtTimeseries', 'selectAll' : $('#anchorAllSelectionsTS').attr('data-selectall'), 'checkId': 'spanSelectCheckTS', 'isMerged': false }, zipSelections_2);

    $('#chkbxApplyFilterToMapTS').off('click', applyFilterToMap);
    $('#chkbxApplyFilterToMapTS').on('click', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxApplyFilterToMapTS'}, applyFilterToMap);

    //Apply search input changes to the map after a delay...
    var searchInput = $('div' + '#' + 'dtTimeseries_filter > label > input');
    searchInput.off('keyup', applyFilterToMapDelayed);  //NOTE: Must use 'keyup' here - 'change' event works only on forms!!
    searchInput.on('keyup', { 'tableId': 'dtTimeseries', 'chkbxId': 'chkbxApplyFilterToMapTS'}, applyFilterToMapDelayed);       
}

//Handler for dtTimeseries 'Action' drop down...
function ddActionsTS(event) {

    var appName = $(this).text();
    var id = $(this).attr("id");
    var html = $(this).html();

    if ('anchorExportSelectionsTS' === id && currentUser.authenticated) {
        //console.log('Sub-menu header clicked, current user authenticated - take no action!!');
        return;
    }
    
    $('#' + event.data.divLaunchId).tooltip('destroy');     //Remove any previously set tooltip on the 'launch' button... 
    $('#' + event.data.divId).removeClass();                //Remove any previously set classes...

    if ( 'idNoneTS' === id) {
        //'None' option selected - retrieve 'original' values...
        $('#' + event.data.divId).html($('#' + event.data.divIdOriginal).html());
        $('#' + event.data.divId).attr('data-noneselected', "true");
        $('#' + event.data.divId).show();

        $('#' + event.data.divLaunchId).removeAttr('data-currentanchor');

        return;
    }

    //Set the 'current anchor' attribute on the 'launch' button for later reference...
    $('#' + event.data.divLaunchId).attr('data-currentanchor', id);

    //Set the selected option...
    $('#' + event.data.divId + ' img').removeAttr( 'style');
    $('#' + event.data.divId + ' img').removeAttr( 'class');
    $('#' + event.data.divId + ' img').html('');

    $('#' + event.data.divId).html(html);
    $('#' + event.data.divId).attr('data-noneselected', "false");
}

//Retrieve current search and filter criteria from input tableId...
function retrieveSearchAndFilterCriteria(tableId, includeDataSources) {

    var criteria = {};

    //Validate/initialize input parameters...
    if ( 'undefined' === typeof tableId || null === tableId ) {
        return criteria;    //Invalid parameter - return empty criteria...
    }

    //Check 'Search' input... 
    var val = $('#' + tableId + '_filter input').val();
    criteria['Search'] = val;

    //Check 'Filter by' dropdowns...
    var api = new $('#' + tableId).DataTable();

    //Retrieve data source names for visible table columns, if indicated (ASSUMPTION: only visible columns are searchable!!)
    // NOTE: Separate calls to dataSrc() and visible() are required - calls cannot be 'chained'...
    if ( 'undefined' !== typeof includeDataSources && true === includeDataSources ) {
        var dataSources = [];
        var options = api.init();

        var colLength = options.columns.length;
        for (var colI= 0; colI < colLength; ++colI) {
            var dataSrc = {};

            dataSrc['dataSource'] = options.columns[colI].data;
            dataSrc['title'] = ('undefined' != typeof options.columns[colI].sTitle) ? options.columns[colI].sTitle : options.columns[colI].data;
            dataSrc['searchable'] = ('undefined' != typeof options.columns[colI].bSearchable) ? options.columns[colI].bSearchable : true;

            dataSources.push(dataSrc);
        }
    
        criteria['dataSources'] = dataSources;
    }

    var filters = [];
    api.columns().indexes().flatten().each(function (i) {

        var column = api.column(i);
        var select = $(column.footer()).find('select');
        var selVal = (select && select.length) ? (null === select.val() ? '' : select.val() ) : '';
        if ('' !== selVal) {
            var filter = {};

            var colHeader = column.header();
            var colTitle = $(colHeader).html();

            filter['value'] = selVal;
            filter['title'] = colTitle;
            
            if ( 'undefined' !== typeof includeDataSources && true === includeDataSources ) {
                filter['dataSource'] = criteria['dataSources'][i].dataSource;
            }

            filters.push(filter);
        }
    });

    criteria['filters'] = filters;

    //Processing complete - return criteria
    return criteria;
}

function  resetSearchAndFilterCriteria( tableId ) {

    if ( 'undefined' === typeof tableId || null === tableId ) {
        return;    //Invalid parameter - return early
    }

    //Reset 'Search' input...
    var val = $('#' + tableId + '_filter input').val('');

    //Reset 'Filter by' dropdowns...
    var api = new $('#' + tableId).DataTable();
    api.columns().indexes().flatten().each(function (i) {

        var column = api.column(i);
        var select = $(column.footer()).find('select');
        var selVal = (select && select.length) ? (null === select.val() ? '' : select.val() ) : '';
        if ('' !== selVal) {
            select.val('')
        }
    });


    //Processing complete - return
    return;
}


function downloadtimeseries(format, id)
{
    url = "/Export/downloadFile/" + id


    $.ajax({
        url: url,
        data:  null,
        success: function (data) {
            //Do something with data that is returned. (The downloaded file?)
            alert();
        }
    });

}

function fnGetSelected(oTableLocal) {
    var aReturn = new Array();
    oTableLocal.$("tr").filter(".row_selected").each(function (index, row) {
        aReturn.push(row);// this should work, if not try aReturn.push($(row));
        //to get the information in the first column 
        aReturn.push($(row).eq(0).text());
        return aReturn;
    })
}

function serviceFailed(xmlhttprequest, textstatus, message)
{

    //BCC - 19-Nov-2015 - per jQuery docs (http://api.jquery.com/jQuery.ajax/#jqXHR) 
    //                      parsed JSON is available in the jqXHR.responseJSON object. 
    //                      NOTE: IIS/IIS Express create the responseJSON object ONLY when 
    //                              TrySkipIisCustomErrors = true on the controller's response object!!  
    //                              See HomeController.UpdateMarkers(...) for more information...
    if ('undefined' !== typeof xmlhttprequest.responseJSON) {
        var msg = xmlhttprequest.responseJSON.Message;
        bootbox.alert(msg);
    }
    else {
        //BCC - 26-Jun-2015 - QA Issue #9 - Timeout error message: typo in word 'occurred'
        bootbox.alert("<H4>An error occurred: '" + message + "'. Please limit search area or Keywords. Please contact Support if the problem persists.</H4>");
    }

    //clean up
    $("#pageloaddiv").hide();
    resetMap();

};

function GetAreainAcres(poly) {
    var result = parseFloat(google.maps.geometry.spherical.computeArea(poly.getPath())) * 0.000247105;
    return result.toFixed(4);
}

function GetAreaInSquareKilometers(path) {
    
    

    var result = parseFloat(google.maps.geometry.spherical.computeArea(path)) * 0.000001;
    if (result < 100) { return result.toFixed(3); }
    if (result < 1000) { return result.toFixed(2); }
    if (result < 10000) { return result.toFixed(1); }
    return result.toFixed(0);
}

function GetPathForBounds(bounds)
{
    var ne = bounds.getNorthEast(); // LatLng of the north-east corner
    var sw = bounds.getSouthWest(); // LatLng of the south-west corder 

    var path = [];

    path.push(new google.maps.LatLng(ne.lat(), sw.lng())),
    path.push(ne),
    path.push(new google.maps.LatLng(sw.lat(), ne.lng())),
    path.push(sw)

    return path;
}

//Redirect to a help article per the input identifier
function helpIconClicked(identifier) {

    if ('undefined' === typeof identifier || null === identifier) {
        return; //Invalid input parameter!!
    }

    var url = null;
    switch (identifier) {
        case 'TimeUnit':
        case 'TimeSupport':
            url = 'https://cuahsi.zendesk.com/hc/en-us/articles/209177787';
            break;
        case 'checkOnlyObservedValues':
            url = 'https://cuahsi.zendesk.com/hc/en-us/articles/209045187';
            break;
        default:
            //Take no action...
            break;
    }
    if (null !== url) {
        var newBrowserTab = window.open(url, "_blank", "", false);
        if (null !== newBrowserTab) {
            newBrowserTab.focus();
        }
    }

}

//Query the server for the current authenticated user...
function loadCurrentUser() {

    var url = '/Account/CurrentUser';

    $.ajax({
        url: url,
        type: 'GET',
        async: true,
        dataType: 'json',
        cache: false,   //So IE does not cache when calling the same URL - source: http://stackoverflow.com/questions/7846707/ie9-jquery-ajax-call-first-time-doing-well-second-time-not
        success: function (data, textStatus, jqXHR) {

            var CurrentUser = jQuery.parseJSON(data);

            if (null !== CurrentUser) {
                //Success - assign/reset retrieved current user values...
                currentUser.authenticated = CurrentUser.Authenticated;
                currentUser.login = CurrentUser.UserEmail;
                currentUser.userName = CurrentUser.UserName;
                currentUser.dataManagerLoaded = false;
            }

            if (currentUser.authenticated) {
                //Current user authenticated - set up for user logout...
                $('form#formGoogleLogin').attr('action', '/Account/ExternalLogOut?ReturnUrl=%2FHome%2FIndex');
                $('button#btnSignIn').addClass('hidden');
                $('button#btnSignOut').removeClass('hidden');
                setupToolTip('liGoogleTab','Logout from HydroClient and Google when your session is complete.');

                //Load saved time series for current user, if indicated
                if (! currentUser.dataManagerLoaded) {
                    loadDataManager();
                }

                //Load export tasks completed since last user logout, if indicated
                if ( ! currentUser.exportsLoaded) {
                    loadExports();
                }
            }
        },
        error: function (xmlhttprequest, textStatus, message) {

            //Failure - Log messsage received from server...
            console.log('Account/CurrentUser reports error: ' + xmlhttprequest.status + ' (' + message + ')');
        }  
    });              
}


//Add a new row to the download manager table, per in the input values...
function addDownloadManagerRow( response, taskId ) {

    if ('undefined' === typeof response || null === response ||
        'undefined' === typeof taskId || null === taskId) {
        return; //Invalid input parameter(s) - return early
    }

    var cols = [];
    var div = '';

    //Limit additions to selected status values...
    if (timeSeriesRequestStatus.Completed === response.RequestStatus              ||
        timeSeriesRequestStatus.ProcessingTimeSeriesId === response.RequestStatus ||
        timeSeriesRequestStatus.RequestTimeSeriesError === response.RequestStatus ||
        timeSeriesRequestStatus.SavingZipArchive === response.RequestStatus       ||
        timeSeriesRequestStatus.Started === response.RequestStatus                ||
        timeSeriesRequestStatus.Starting === response.RequestStatus ) {

        //Column 0
        cols.push(response.RequestId.toString());

        //Column 1
        div = $('<div class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto;">' + taskId + '</div>');
        cols.push(div);

        //Column 2 
        div = $('<div class="btn" style="font-size: 1em; font-weight: bold; margin: 0 auto; word-wrap: break-word;">' + formatStatusMessage(response.Status) + '</div>');
        cols.push(div);

        //Column 3
        div = $('<div class="btn" id="blobUriText" style="font-size: 1em; font-weight: bold; margin: 0 auto; text-overflow: ellipsis;">' + response.BlobUri + '</div>');
        cols.push(div);

        if ( timeSeriesRequestStatus.RequestTimeSeriesError !== response.RequestStatus ) {
            //Button for Column 4
            var button = $("<button class='stopTask btn btn-warning' style='font-size: 1.5vmin'>Stop Processing</button>");
            addEndTaskClickHandler(button, response);

            //Column 4
            cols.push(button);
        }

        //Column 5
        cols.push(response.RequestStatus);

        //Add the new row and hide the RequestStatus column...
        var newrow = newRow($("#tblDownloadManager"), cols);
        newrow.find('td:eq(5)').hide();

        //Add row styles
        addRowStylesDM(newrow);
    }
}

//Update a previously added row in the download manager table, per in the input values...
// For now, handle the case status === completed only...
function updateDownloadManagerRow( response ) {

    if ('undefined' === typeof response || null === response ) {
        return; //Invalid input parameter(s) - return early
    }

    var requestId = response.RequestId;
    var tableRow = $("#tblDownloadManager tr td").filter(function () {
        return $(this).text() === requestId;
    }).parent("tr");

    if ( 0 >= tableRow.length) {
        //table row not found - return early
        return;
    }

    if ( timeSeriesRequestStatus.Completed === response.RequestStatus) {
        //Task completed - retrieve the base blob URI...
        var baseUri = (response.BlobUri).split('.zip');
        var blobUri = baseUri[0] += '.zip';
        var dataRequestId = requestId; 

        //Retrieve the full file name from the base blob URI...
        var uriComponents = baseUri[0].split('/');
        var fileName = uriComponents[(uriComponents.length - 1)];

        //Set base blob URI into text in column 3
        //NOTE: Need to decode the string twice because:
        //       If the string contains an encoded character like %27 ('),
        //       the browser(?) separely encodes the (&) character as %25
        //       resulting in %25%27 in the string.  Two string decodes 
        //       are required to remove the two escape sequences...
        var decoded = decodeURIComponent(fileName);
        var decoded1 = decodeURIComponent(decoded);
        tableRow.find('#blobUriText').html(decoded1);
    
        //Create a download button...
        var button = tableRow.find('td:eq(4)');

        button = $('<button class="zipBlobDownload btn btn-success" style="font-size: 1.5vmin" data-blobUri="' + blobUri +
                        '" data-requestId="' + dataRequestId + '">Download Archive</button>"');

        button.on('click', function (event) {
            var blobUri = $(this).attr('data-blobUri');
            location.href = blobUri;

            if ( currentUser.authenticated) {
                var requestId = $(this).attr('data-requestId');
                deleteDownload(requestId);
            }

            //Fade row and remove from table...
            tableRow.fadeTo(1500, 0.5, function() {
                    $(this).remove();
            });

            event.stopPropagation();
        });

        tableRow.find('td:eq(4)').html(button);

        //Change the glyphicon and stop the animation
        var glyphiconSpan = tableRow.find('#glyphiconSpan');

        glyphiconSpan.removeClass('glyphicon-refresh spin');
        glyphiconSpan.addClass('glyphicon-thumbs-up');

        tableRow.find('#statusMessageText').text(response.Status);

        tableRow.addClass('success');   //Color row as 'successful'

        if ($("#chkbxAutoDownload").prop("checked")) {
                //Autodownload checkbox checked - click the newly created button...
            button.click();
        }
    }
    else {
            if (timeSeriesRequestStatus.ProcessingTimeSeriesId === response.RequestStatus ||
                timeSeriesRequestStatus.SavingZipArchive === response.RequestStatus       ||
                timeSeriesRequestStatus.Started === response.RequestStatus                ||
                timeSeriesRequestStatus.Starting === response.RequestStatus ) {
                //Task NOT completed - create monitor record, start monitor...
                //ASSUMPTION: - values other than requestId here should not make any difference to controller behaviour...
                var timeSeriesRequest = {
                    'RequestName': 'temp',
                    'RequestId': response.RequestId,
                    'TimeSeriesIds': null,
                    'RequestFormat': timeSeriesFormat.CSV
                };

                downloadMonitor.timeSeriesMonitored[response.RequestId] = { 'tableName': 'tblDownloadManager',
                                                                            'timeSeriesRequest': timeSeriesRequest,
                                                                            'timeSeriesRequestStatus': response.RequestStatus 
                                                                          };
               startRequestTimeSeriesMonitor();    //Start monitoring all current requests, if indicated...
            }
            else {
                    if (timeSeriesRequestStatus.RequestTimeSeriesError === response.RequestStatus) {
                        //Task errored - decorate row, remove task from database - DO NOT remove the row!! 
                                        
                        //Reset blob URI
                        tableRow.find('#blobUriText').html('');

                        //Hide the 'Stop Processing' button
                        tableRow.find('td:eq(4)').hide();

                        //Change the glyphicon and stop the animation
                        var glyphiconSpan = tableRow.find('#glyphiconSpan');

                        glyphiconSpan.removeClass('glyphicon-refresh spin');
                        glyphiconSpan.addClass('glyphicon-thumbs-down');
                        glyphiconSpan.css('color', 'red');

                        //Color table row as 'warning'
                        tableRow.removeClass('info');
                        tableRow.addClass('warning');

                        if ( currentUser.authenticated) {
                            deleteDownload(requestId);
                        }
                    }
            }
    }
}

//Return a download task message for use in further processing
// Expected format: 'Task: @@taskId@@ started.  <messasge text>
function getDownloadTaskMessage() {

    var msgBase = 'Task: @@taskId@@ started.  ';
    if ($("#chkbxAutoDownload").prop("checked")) {
            //Autodownload checkbox checked - check status message...
        return msgBase + 'To check the download status, please open the Exports tab in the Workspace';
    }
    else {
            //Autodownload checkbox NOT checked - complete download... 
        return msgBase + 'To complete the download, please open the Exports tab in the Workspace';
    }
}